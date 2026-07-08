using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Tiss;

namespace SistemaHospitalar.Infrastructure.Services;

public class TissExtendedService(
    AppDbContext dbContext,
    MockOperatorTissClient mockOperatorClient,
    HttpOperatorTissClient httpOperatorClient,
    ISigtapOfficialSyncService sigtapOfficialSyncService,
    ILogger<TissExtendedService> logger) : ITissExtendedService
{
    private const int TussDescriptionMaxLength = 300;
    private const int SigtapDescriptionMaxLength = 400;

    public async Task<PagedResult<TussCatalogDto>> GetTussCatalogAsync(
        string? search,
        TussTableType? tableType,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = dbContext.TussCatalogs.AsNoTracking().Where(t => t.IsActive);
        if (tableType.HasValue)
            query = query.Where(t => t.TableType == tableType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Code.Contains(term) || t.Description.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await CatalogImportHelpers.OrderByCatalogCode(query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TussCatalogDto(t.Id, t.Code, t.Description, t.TableType, t.Unit, t.ReferencePrice, t.ValidFrom, t.ValidUntil))
            .ToListAsync(cancellationToken);

        return new PagedResult<TussCatalogDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<int> ImportTussCatalogAsync(ImportTussRequest request, CancellationToken cancellationToken = default)
    {
        var stats = await ImportTussCatalogWithStatsAsync(request, cancellationToken);
        return stats.Changed;
    }

    private async Task<CatalogImportHelpers.MergeStats> ImportTussCatalogChunkAsync(
        IReadOnlyList<TussCatalogImportItem> chunk,
        CancellationToken cancellationToken)
    {
        var stats = new CatalogImportHelpers.MergeStats();
        var codes = chunk.Select(i => i.Code).Distinct().ToList();
        var tableTypes = chunk.Select(i => i.TableType).Distinct().ToList();
        var existingByKey = await dbContext.TussCatalogs
            .Where(t => codes.Contains(t.Code) && tableTypes.Contains(t.TableType))
            .ToDictionaryAsync(t => TussCatalogKey(t.Code, t.TableType), cancellationToken);

        foreach (var item in chunk)
        {
            var description = Truncate(item.Description, TussDescriptionMaxLength);
            var key = TussCatalogKey(item.Code, item.TableType);
            if (existingByKey.TryGetValue(key, out var existing))
            {
                if (CatalogImportHelpers.TussContentEquals(existing, item, description))
                {
                    stats.Skipped++;
                    continue;
                }

                CatalogImportHelpers.ApplyTussUpdate(existing, item, description);
                stats.Updated++;
                continue;
            }

            var entity = new TussCatalog
            {
                Code = item.Code,
                Description = description,
                TableType = item.TableType,
                Unit = string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit.Trim(),
                ReferencePrice = item.ReferencePrice,
            };
            dbContext.TussCatalogs.Add(entity);
            existingByKey[key] = entity;
            stats.Inserted++;
        }

        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(cancellationToken);

        return stats;
    }

    public async Task<ImportTussResultDto> ImportTussCsvAsync(
        ImportTussCsvRequest request,
        CancellationToken cancellationToken = default)
    {
        var parsed = TissCatalogCsvImporter.Parse(request.CsvContent);
        if (parsed.Count == 0)
            return new ImportTussResultDto(0, 0, "Nenhum procedimento válido encontrado no CSV. Use: codigo;descricao;tipo;unidade;valor");

        var stats = await ImportTussCatalogWithStatsAsync(new ImportTussRequest(parsed), cancellationToken);
        return new ImportTussResultDto(
            stats.Changed,
            parsed.Count,
            CatalogImportHelpers.FormatMergeMessage("termo(s) TUSS", parsed.Count, stats));
    }

    public async Task<ImportTussResultDto> ImportTussXlsxFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tuss-import-{Guid.NewGuid():N}.xlsx");
        try
        {
            await using (var fs = File.Create(tempPath))
            {
                await fileStream.CopyToAsync(fs, cancellationToken);
            }

            var parsed = 0;
            var stats = new CatalogImportHelpers.MergeStats();
            var streamResult = await TissCatalogXlsxImporter.StreamFile(
                tempPath,
                async batch =>
                {
                    var distinct = batch
                        .GroupBy(i => (i.Code, i.TableType))
                        .Select(g => g.First())
                        .ToList();
                    parsed += distinct.Count;
                    stats = MergeImportStats(stats, await ImportTussCatalogWithStatsAsync(new ImportTussRequest(distinct), cancellationToken));
                },
                cancellationToken: cancellationToken);

            if (streamResult.ParsedRows == 0)
            {
                return new ImportTussResultDto(
                    0,
                    0,
                    $"Nenhum termo TUSS válido encontrado em {fileName}.");
            }

            return new ImportTussResultDto(
                stats.Changed,
                parsed,
                $"{CatalogImportHelpers.FormatMergeMessage("termo(s) TUSS", parsed, stats)} Arquivo: {fileName}.");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public async Task<ImportTussResultDto> ImportBundledTuss202601Async(CancellationToken cancellationToken = default)
    {
        var folder = TissBundledCatalogLocator.ResolveFolder202601()
            ?? throw new InvalidOperationException(
                "Pacote TUSS 202601 não encontrado. Extraia os arquivos em Diversos/TISS/202601.");

        var files = TissBundledCatalogLocator.FindTussXlsxFiles(folder);
        if (files.Count == 0)
            throw new InvalidOperationException("Nenhum arquivo .xlsx TUSS encontrado em Diversos/TISS/202601.");

        var fileStats = new List<string>();
        var stats = new CatalogImportHelpers.MergeStats();
        var parsedTotal = 0;

        foreach (var file in files)
        {
            var fileParsed = 0;
            var streamResult = await TissCatalogXlsxImporter.StreamFile(
                file,
                async batch =>
                {
                    var distinct = batch
                        .GroupBy(i => (i.Code, i.TableType))
                        .Select(g => g.First())
                        .ToList();
                    fileParsed += distinct.Count;
                    stats = MergeImportStats(stats, await ImportTussCatalogWithStatsAsync(new ImportTussRequest(distinct), cancellationToken));
                },
                cancellationToken: cancellationToken);

            if (streamResult.ParsedRows == 0)
                continue;

            parsedTotal += fileParsed;
            fileStats.Add($"{Path.GetFileName(file)} ({fileParsed})");
        }


        if (fileStats.Count == 0)
        {
            return new ImportTussResultDto(
                0,
                0,
                "Arquivos encontrados, mas nenhum termo pôde ser lido. Verifique o formato ANS 202601.");
        }

        var total = await dbContext.TussCatalogs.CountAsync(t => t.IsActive, cancellationToken);
        var mergeMessage = CatalogImportHelpers.FormatMergeMessage("termo(s) TUSS 202601", parsedTotal, stats);

        return new ImportTussResultDto(
            stats.Changed,
            total,
            $"{mergeMessage} ({fileStats.Count} arquivo(s): {string.Join(", ", fileStats)}). Total ativo no catálogo: {total}.");
    }

    public async Task<ImportTussResultDto> SeedExpandedTussCatalogAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = await dbContext.TussCatalogs
            .Select(t => t.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet();
        var toAdd = TissCatalogExpandedSeed.Items.Where(i => !existingSet.Contains(i.Code)).ToList();

        if (toAdd.Count == 0)
        {
            var total = await dbContext.TussCatalogs.CountAsync(t => t.IsActive, cancellationToken);
            return new ImportTussResultDto(0, total, $"Catálogo já contém {total} itens TUSS. Nenhum novo item adicionado.");
        }

        dbContext.TussCatalogs.AddRange(toAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
        var finalTotal = await dbContext.TussCatalogs.CountAsync(t => t.IsActive, cancellationToken);
        return new ImportTussResultDto(
            toAdd.Count,
            finalTotal,
            $"{toAdd.Count} procedimento(s) adicionados. Total no catálogo: {finalTotal}.");
    }

    public async Task<ImportSigtapResultDto> ImportSigtapZipAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var parsed = await SigtapZipImporter.ParseAsync(fileStream, fileName, cancellationToken);
        if (parsed.Items.Count == 0)
            return new ImportSigtapResultDto(0, 0, parsed.Competence, parsed.Message);

        var result = await ImportSigtapParsedAsync(parsed, cancellationToken);
        return new ImportSigtapResultDto(
            result.Stats.Changed,
            result.TotalInFile,
            result.Competence,
            result.Message);
    }

    public async Task<SyncSigtapOfficialResultDto> SyncSigtapOfficialAsync(CancellationToken cancellationToken = default)
    {
        var syncedAt = DateTime.UtcNow;
        logger.LogInformation("Iniciando sincronização oficial SIGTAP (DATASUS)");

        try
        {
            var release = await sigtapOfficialSyncService.DiscoverLatestAsync(cancellationToken);
            var download = await sigtapOfficialSyncService.DownloadOfficialZipAsync(release, cancellationToken);

            await using var stream = new MemoryStream(download.Data, writable: false);
            var parsed = await SigtapZipImporter.ParseAsync(stream, download.FileName, cancellationToken);
            if (parsed.Items.Count == 0)
            {
                var emptyMessage = parsed.Message.Length > 0
                    ? parsed.Message
                    : "ZIP oficial não contém procedimentos SIGTAP reconhecíveis.";
                logger.LogWarning(
                    "Sincronização SIGTAP sem dados: competência remota {RemoteCompetence}, arquivo {FileName}",
                    release.Competence,
                    download.FileName);

                return new SyncSigtapOfficialResultDto(
                    false,
                    parsed.Competence,
                    release.Competence,
                    download.SourceUrl,
                    download.Sha256,
                    download.SizeBytes,
                    0,
                    0,
                    0,
                    0,
                    emptyMessage,
                    syncedAt);
            }

            var import = await ImportSigtapParsedAsync(parsed, cancellationToken);
            var success = import.Stats.Changed > 0 || import.TotalInFile > 0;

            logger.LogInformation(
                "Sincronização SIGTAP concluída: competência {Competence}, inseridos {Inserted}, atualizados {Updated}, ignorados {Skipped}, total {TotalInFile}",
                import.Competence,
                import.Stats.Inserted,
                import.Stats.Updated,
                import.Stats.Skipped,
                import.TotalInFile);

            return new SyncSigtapOfficialResultDto(
                success,
                import.Competence,
                release.Competence,
                download.SourceUrl,
                download.Sha256,
                download.SizeBytes,
                import.Stats.Inserted,
                import.Stats.Updated,
                import.Stats.Skipped,
                import.TotalInFile,
                $"{import.Message} Fonte oficial DATASUS.",
                syncedAt);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Falha na sincronização oficial SIGTAP");
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    private async Task<(CatalogImportHelpers.MergeStats Stats, string Competence, int TotalInFile, string Message)> ImportSigtapParsedAsync(
        SigtapParseResult parsed,
        CancellationToken cancellationToken)
    {
        const int chunkSize = 1500;
        var previousTimeout = dbContext.Database.GetCommandTimeout();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

        try
        {
            var stats = new CatalogImportHelpers.MergeStats();
            for (var offset = 0; offset < parsed.Items.Count; offset += chunkSize)
            {
                var chunk = parsed.Items.Skip(offset).Take(chunkSize).ToList();
                stats = MergeImportStats(stats, await ImportSigtapChunkAsync(chunk, cancellationToken));
            }

            var message =
                $"{CatalogImportHelpers.FormatMergeMessage("procedimento(s) SIGTAP", parsed.Items.Count, stats)} Competência {parsed.Competence}.";
            return (stats, parsed.Competence, parsed.Items.Count, message);
        }
        finally
        {
            dbContext.Database.SetCommandTimeout(previousTimeout);
        }
    }

    public async Task<SigtapCatalogSummaryDto> GetSigtapSummaryAsync(CancellationToken cancellationToken = default)
    {
        var total = await dbContext.SigtapProcedures.CountAsync(s => s.IsActive, cancellationToken);
        var latest = await dbContext.SigtapProcedures.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Competence)
            .ThenByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(s => new { s.Competence, LastImportAt = s.UpdatedAt ?? s.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return new SigtapCatalogSummaryDto(
            total,
            latest?.Competence,
            latest?.LastImportAt);
    }

    public async Task<PagedResult<SigtapProcedureDto>> GetSigtapProceduresAsync(
        string? search,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = dbContext.SigtapProcedures.AsNoTracking().Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Code.Contains(term) || s.Description.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await CatalogImportHelpers.OrderByCatalogCode(query)
            .ThenByDescending(s => s.Competence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SigtapProcedureDto(
                s.Id, s.Code, s.Competence, s.Description, s.GroupName, s.Complexity,
                s.HospitalAmount, s.ProfessionalAmount))
            .ToListAsync(cancellationToken);

        return new PagedResult<SigtapProcedureDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IReadOnlyList<TissDemonstrativoDto>> GetDemonstrativosAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TissDemonstrativos.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new TissDemonstrativoDto(
                d.Id,
                d.DemonstrativoNumber,
                d.HealthInsuranceId,
                d.HealthInsurance.Name,
                d.Competence,
                d.Status,
                d.TotalBilled,
                d.TotalPaid,
                d.TotalGlosa,
                d.Items.Count(i => i.IsActive),
                d.Items.Count(i => i.IsActive && i.IsMatched),
                d.CreatedAt,
                d.ProcessedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TissDemonstrativoDetailDto?> GetDemonstrativoByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await MapDemonstrativoDetail(id, cancellationToken);
    }

    public async Task<TissDemonstrativoDetailDto> ImportDemonstrativoCsvAsync(
        ImportDemonstrativoRequest request,
        CancellationToken cancellationToken = default)
    {
        var insurer = await dbContext.HealthInsurances
            .FirstOrDefaultAsync(h => h.Id == request.HealthInsuranceId && h.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Convênio não encontrado.");

        var demo = new TissDemonstrativo
        {
            HealthInsuranceId = insurer.Id,
            DemonstrativoNumber = $"IMP-{request.Competence}-{DateTime.UtcNow:HHmmss}",
            Competence = request.Competence.Trim(),
            SourceFileName = request.SourceFileName,
            RawContent = request.CsvContent,
            Status = TissDemonstrativoStatus.Imported,
        };

        foreach (var line in ParseDemonstrativoCsv(request.CsvContent))
        {
            demo.Items.Add(new TissDemonstrativoItem
            {
                GuideNumber = line.GuideNumber,
                TussCode = line.TussCode,
                BilledAmount = line.BilledAmount,
                PaidAmount = line.PaidAmount,
                GlosaAmount = line.GlosaAmount,
                GlosaReason = line.GlosaReason,
                AnsGlosaCode = line.AnsGlosaCode,
            });
        }

        demo.TotalBilled = demo.Items.Sum(i => i.BilledAmount);
        demo.TotalPaid = demo.Items.Sum(i => i.PaidAmount);
        demo.TotalGlosa = demo.Items.Sum(i => i.GlosaAmount);

        dbContext.TissDemonstrativos.Add(demo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await MapDemonstrativoDetail(demo.Id, cancellationToken))!;
    }

    public async Task<TissDemonstrativoDetailDto> ProcessDemonstrativoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var demo = await dbContext.TissDemonstrativos
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Demonstrativo não encontrado.");

        foreach (var item in demo.Items.Where(i => i.IsActive))
        {
            var guide = await dbContext.TissGuides
                .Include(g => g.Items)
                .Include(g => g.Glosas)
                .FirstOrDefaultAsync(g => g.IsActive && g.GuideNumber == item.GuideNumber, cancellationToken);

            if (guide is null)
                continue;

            item.IsMatched = true;
            item.TissGuideId = guide.Id;

            if (item.GlosaAmount > 0 && !string.IsNullOrWhiteSpace(item.GlosaReason))
            {
                dbContext.TissGlosas.Add(new TissGlosa
                {
                    TissGuideId = guide.Id,
                    Reason = item.GlosaReason,
                    AnsGlosaCode = item.AnsGlosaCode,
                    GlosaAmount = item.GlosaAmount,
                });
                guide.Status = TissGuideStatus.Glosa;
            }

            await TissFinancialReconciliation.ApplyDemonstrativoPaymentAsync(dbContext, guide, item.PaidAmount, cancellationToken);
        }

        demo.Status = TissDemonstrativoStatus.Processed;
        demo.ProcessedAt = DateTime.UtcNow;
        demo.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapDemonstrativoDetail(id, cancellationToken))!;
    }

    public async Task<TissDemonstrativoDetailDto> FetchDemonstrativoFromOperatorAsync(
        FetchOperatorDemonstrativoRequest request,
        CancellationToken cancellationToken = default)
    {
        var insurer = await dbContext.HealthInsurances
            .FirstOrDefaultAsync(h => h.Id == request.HealthInsuranceId && h.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Convênio não encontrado.");

        TissBatch? batch = null;
        if (request.TissBatchId.HasValue)
        {
            batch = await dbContext.TissBatches
                .Include(b => b.Guides).ThenInclude(g => g.Items)
                .FirstOrDefaultAsync(b => b.Id == request.TissBatchId.Value && b.IsActive, cancellationToken);
        }

        var competence = batch?.Competence ?? DateTime.UtcNow.ToString("yyyy-MM");
        var client = OperatorTissClientFactory.Resolve(insurer, mockOperatorClient, httpOperatorClient);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.FetchDemonstrativoAsync(insurer, batch, competence, cancellationToken);
            sw.Stop();

            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.DemonstrativoFetch,
                response.Success ? OperatorTransactionStatus.Success : OperatorTransactionStatus.Failure,
                response.DemonstrativoNumber, JsonSerializer.Serialize(request), response.RawJson, null,
                (int)sw.ElapsedMilliseconds, cancellationToken);

            var csv = string.Join('\n', response.Lines.Select(l =>
                $"{l.GuideNumber};{l.TussCode};{l.BilledAmount.ToString(CultureInfo.InvariantCulture)};{l.PaidAmount.ToString(CultureInfo.InvariantCulture)};{l.GlosaAmount.ToString(CultureInfo.InvariantCulture)};{l.GlosaReason};{l.AnsGlosaCode}"));

            var imported = await ImportDemonstrativoCsvAsync(new ImportDemonstrativoRequest(
                request.HealthInsuranceId,
                competence,
                $"operadora-{response.DemonstrativoNumber}.csv",
                csv), cancellationToken);

            return await ProcessDemonstrativoAsync(imported.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.DemonstrativoFetch,
                OperatorTransactionStatus.Failure, null, JsonSerializer.Serialize(request), null, ex.Message,
                (int)sw.ElapsedMilliseconds, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TissGuideAnnexDto>> GetGuideAnnexesAsync(
        Guid guideId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TissGuideAnnexes.AsNoTracking()
            .Where(a => a.TissGuideId == guideId && a.IsActive)
            .Select(MapAnnex())
            .ToListAsync(cancellationToken);
    }

    public async Task<TissGuideAnnexDto> CreateGuideAnnexAsync(
        CreateTissGuideAnnexRequest request,
        CancellationToken cancellationToken = default)
    {
        var guideExists = await dbContext.TissGuides.AnyAsync(g => g.Id == request.TissGuideId && g.IsActive, cancellationToken);
        if (!guideExists)
            throw new InvalidOperationException("Guia não encontrada.");

        var annex = new TissGuideAnnex
        {
            TissGuideId = request.TissGuideId,
            AnnexType = request.AnnexType,
            Cid10Code = request.Cid10Code?.Trim(),
            ClinicalIndication = request.ClinicalIndication?.Trim(),
            CycleInfo = request.CycleInfo?.Trim(),
            Notes = request.Notes?.Trim(),
        };

        if (request.OpmeItems is not null)
        {
            foreach (var item in request.OpmeItems)
            {
                annex.OpmeItems.Add(new TissOpmeItem
                {
                    TussCode = item.TussCode.Trim(),
                    Description = item.Description.Trim(),
                    Manufacturer = item.Manufacturer?.Trim(),
                    AuthorizationNumber = item.AuthorizationNumber?.Trim(),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                });
            }
        }

        dbContext.TissGuideAnnexes.Add(annex);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await dbContext.TissGuideAnnexes.AsNoTracking()
            .Where(a => a.Id == annex.Id)
            .Select(MapAnnex())
            .FirstOrDefaultAsync(cancellationToken))!;
    }

    public async Task<IReadOnlyList<HealthInsuranceIntegrationDto>> GetInsuranceIntegrationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HealthInsurances.AsNoTracking()
            .Where(h => h.IsActive && h.Name != "Particular")
            .OrderBy(h => h.Name)
            .Select(h => new HealthInsuranceIntegrationDto(
                h.Id, h.Name, h.AnsRegistration, h.TissVersion, h.OperatorCode,
                h.PortalUrl, h.WebServiceUrl, h.IntegrationUser, h.UseMockIntegration, h.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<HealthInsuranceIntegrationDto?> UpdateInsuranceIntegrationAsync(
        Guid id,
        UpdateHealthInsuranceIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var insurer = await dbContext.HealthInsurances.FirstOrDefaultAsync(h => h.Id == id && h.IsActive, cancellationToken);
        if (insurer is null)
            return null;

        insurer.TissVersion = request.TissVersion?.Trim();
        insurer.OperatorCode = request.OperatorCode?.Trim();
        insurer.PortalUrl = request.PortalUrl?.Trim();
        insurer.WebServiceUrl = request.WebServiceUrl?.Trim();
        insurer.IntegrationUser = request.IntegrationUser?.Trim();
        if (!string.IsNullOrWhiteSpace(request.IntegrationSecret))
            insurer.IntegrationSecret = request.IntegrationSecret.Trim();
        insurer.UseMockIntegration = request.UseMockIntegration;
        insurer.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new HealthInsuranceIntegrationDto(
            insurer.Id, insurer.Name, insurer.AnsRegistration, insurer.TissVersion, insurer.OperatorCode,
            insurer.PortalUrl, insurer.WebServiceUrl, insurer.IntegrationUser, insurer.UseMockIntegration, insurer.IsActive);
    }

    public async Task<IReadOnlyList<OperatorTransactionLogDto>> GetOperatorTransactionLogsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        return await dbContext.OperatorTransactionLogs.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .Select(l => new OperatorTransactionLogDto(
                l.Id, l.HealthInsuranceId, l.HealthInsurance.Name,
                l.TransactionType, l.Status, l.ReferenceId, l.ErrorMessage, l.DurationMs, l.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TissReconciliationSummaryDto> GetReconciliationSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var guidesWithReceivable = await dbContext.FinancialAccounts.CountAsync(
            f => f.IsActive && f.TissGuideId != null, cancellationToken);

        var guidesPaidInFinance = await dbContext.FinancialAccounts.CountAsync(
            f => f.IsActive && f.TissGuideId != null && f.Status == FinancialAccountStatus.Paid, cancellationToken);

        var open = await dbContext.FinancialAccounts
            .Where(f => f.IsActive && f.TissGuideId != null
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);

        var paid = await dbContext.FinancialAccounts
            .Where(f => f.IsActive && f.TissGuideId != null)
            .SumAsync(f => f.PaidAmount, cancellationToken);

        return new TissReconciliationSummaryDto(guidesWithReceivable, guidesPaidInFinance, open, paid);
    }

    private static IEnumerable<(string GuideNumber, string? TussCode, decimal BilledAmount, decimal PaidAmount, decimal GlosaAmount, string? GlosaReason, string? AnsGlosaCode)> ParseDemonstrativoCsv(string csv)
    {
        foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("guia", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(';');
            if (parts.Length < 4)
                continue;

            yield return (
                parts[0].Trim(),
                parts.Length > 1 ? NullIfEmpty(parts[1]) : null,
                ParseDec(parts.Length > 2 ? parts[2] : "0"),
                ParseDec(parts.Length > 3 ? parts[3] : "0"),
                ParseDec(parts.Length > 4 ? parts[4] : "0"),
                parts.Length > 5 ? NullIfEmpty(parts[5]) : null,
                parts.Length > 6 ? NullIfEmpty(parts[6]) : null);
        }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static decimal ParseDec(string s) =>
        decimal.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];

    private async Task<CatalogImportHelpers.MergeStats> ImportSigtapChunkAsync(
        IReadOnlyList<SigtapParsedItem> chunk,
        CancellationToken cancellationToken)
    {
        var stats = new CatalogImportHelpers.MergeStats();
        var keys = chunk
            .Select(i => new { i.Code, i.Competence })
            .Distinct()
            .ToList();
        var codes = keys.Select(k => k.Code).Distinct().ToList();
        var competences = keys.Select(k => k.Competence).Distinct().ToList();

        var existing = await dbContext.SigtapProcedures
            .Where(s => codes.Contains(s.Code) && competences.Contains(s.Competence))
            .ToListAsync(cancellationToken);
        var existingByKey = existing.ToDictionary(x => $"{x.Code}|{x.Competence}", StringComparer.Ordinal);

        foreach (var item in chunk)
        {
            var key = $"{item.Code}|{item.Competence}";
            var (validFrom, validUntil) = ParseCompetenceDates(item.Competence);
            var description = Truncate(item.Description, SigtapDescriptionMaxLength);
            if (existingByKey.TryGetValue(key, out var entity))
            {
                if (CatalogImportHelpers.SigtapContentEquals(entity, item, description, validFrom, validUntil))
                {
                    stats.Skipped++;
                    continue;
                }

                CatalogImportHelpers.ApplySigtapUpdate(entity, item, description, validFrom, validUntil);
                stats.Updated++;
                continue;
            }

            entity = new SigtapProcedure
            {
                Code = item.Code,
                Competence = item.Competence,
                Description = description,
                GroupName = string.IsNullOrWhiteSpace(item.GroupName) ? null : item.GroupName.Trim(),
                Complexity = string.IsNullOrWhiteSpace(item.Complexity) ? null : item.Complexity.Trim(),
                HospitalAmount = item.HospitalAmount,
                ProfessionalAmount = item.ProfessionalAmount,
                ValidFrom = validFrom,
                ValidUntil = validUntil,
            };
            dbContext.SigtapProcedures.Add(entity);
            existingByKey[key] = entity;
            stats.Inserted++;
        }

        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(cancellationToken);

        return stats;
    }

    private async Task<CatalogImportHelpers.MergeStats> ImportTussCatalogWithStatsAsync(
        ImportTussRequest request,
        CancellationToken cancellationToken)
    {
        const int chunkSize = 1500;
        var validItems = request.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Code))
            .Select(i => i with
            {
                Code = i.Code.Trim(),
                Description = Truncate(i.Description.Trim(), TussDescriptionMaxLength)
            })
            .Where(i => !string.IsNullOrWhiteSpace(i.Description)
                && !string.Equals(i.Code, i.Description, StringComparison.Ordinal))
            .GroupBy(i => (i.Code, i.TableType))
            .Select(g => g.First())
            .ToList();

        if (validItems.Count == 0)
            return new CatalogImportHelpers.MergeStats();

        var previousTimeout = dbContext.Database.GetCommandTimeout();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

        try
        {
            var stats = new CatalogImportHelpers.MergeStats();
            for (var offset = 0; offset < validItems.Count; offset += chunkSize)
            {
                var chunk = validItems.Skip(offset).Take(chunkSize).ToList();
                stats = MergeImportStats(stats, await ImportTussCatalogChunkAsync(chunk, cancellationToken));
            }

            return stats;
        }
        finally
        {
            dbContext.Database.SetCommandTimeout(previousTimeout);
        }
    }

    private static CatalogImportHelpers.MergeStats MergeImportStats(
        CatalogImportHelpers.MergeStats current,
        CatalogImportHelpers.MergeStats chunk)
    {
        current.Inserted += chunk.Inserted;
        current.Updated += chunk.Updated;
        current.Skipped += chunk.Skipped;
        return current;
    }

    private static string TussCatalogKey(string code, TussTableType tableType)
        => $"{code}|{(int)tableType}";

    private static (DateOnly? ValidFrom, DateOnly? ValidUntil) ParseCompetenceDates(string competence)
    {
        var parts = competence.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || month is < 1 or > 12)
            return (null, null);

        var validFrom = new DateOnly(year, month, 1);
        var validUntil = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return (validFrom, validUntil);
    }

    private async Task<TissDemonstrativoDetailDto?> MapDemonstrativoDetail(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.TissDemonstrativos.AsNoTracking()
            .Where(d => d.Id == id && d.IsActive)
            .Select(d => new TissDemonstrativoDetailDto(
                d.Id,
                d.DemonstrativoNumber,
                d.HealthInsuranceId,
                d.HealthInsurance.Name,
                d.Competence,
                d.Status,
                d.TotalBilled,
                d.TotalPaid,
                d.TotalGlosa,
                d.SourceFileName,
                d.CreatedAt,
                d.ProcessedAt,
                d.Items.Where(i => i.IsActive).Select(i => new TissDemonstrativoItemDto(
                    i.Id, i.GuideNumber, i.TussCode, i.BilledAmount, i.PaidAmount, i.GlosaAmount,
                    i.GlosaReason, i.AnsGlosaCode, i.IsMatched, i.TissGuideId)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<TissGuideAnnex, TissGuideAnnexDto>> MapAnnex() =>
        a => new TissGuideAnnexDto(
            a.Id,
            a.TissGuideId,
            a.AnnexType,
            a.Cid10Code,
            a.ClinicalIndication,
            a.CycleInfo,
            a.Notes,
            a.OpmeItems.Where(o => o.IsActive).Select(o => new TissOpmeItemDto(
                o.Id, o.TussCode, o.Description, o.Manufacturer, o.AuthorizationNumber,
                o.Quantity, o.UnitPrice, o.Quantity * o.UnitPrice)).ToList());
}
