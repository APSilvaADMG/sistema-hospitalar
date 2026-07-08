using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.OfficialUpdates;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.OfficialUpdates;

public class OfficialUpdatesService(
    AppDbContext dbContext,
    IEnumerable<IOfficialUpdateProvider> providers,
    IOptions<OfficialUpdatesSettings> settings,
    ILogger<OfficialUpdatesService> logger) : IOfficialUpdatesService
{
    private readonly IReadOnlyDictionary<OfficialSourceType, IOfficialUpdateProvider> _providers =
        providers.ToDictionary(p => p.SourceType);

    public async Task<OfficialUpdatesDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await EnsureVersionRowsAsync(cancellationToken);
        var versions = await dbContext.OfficialVersions
            .AsNoTracking()
            .OrderBy(v => v.SourceType)
            .ToListAsync(cancellationToken);

        var lastCheck = versions
            .Where(v => v.LastCheckedAt.HasValue)
            .Select(v => v.LastCheckedAt)
            .DefaultIfEmpty()
            .Max();

        return new OfficialUpdatesDashboardDto(
            lastCheck,
            versions.Select(MapSource).ToList());
    }

    public async Task<OfficialUpdatesDashboardDto> CheckAllAsync(
        string? triggeredBy,
        CancellationToken cancellationToken = default)
    {
        await EnsureVersionRowsAsync(cancellationToken);

        foreach (var sourceType in Enum.GetValues<OfficialSourceType>())
        {
            await RunCheckAsync(sourceType, triggeredBy ?? "manual", cancellationToken);
        }

        return await GetDashboardAsync(cancellationToken);
    }

    public async Task<OfficialUpdateActionResultDto> UpdateSourceAsync(
        OfficialSourceType sourceType,
        string? triggeredBy,
        CancellationToken cancellationToken = default)
    {
        await EnsureVersionRowsAsync(cancellationToken);

        if (!_providers.TryGetValue(sourceType, out var provider))
        {
            return new OfficialUpdateActionResultDto(
                sourceType.ToString(),
                "Failed",
                "Provedor não registrado.",
                null);
        }

        var versionRow = await GetOrCreateVersionRowAsync(sourceType, cancellationToken);
        versionRow.Status = OfficialVersionStatus.Importing;
        await dbContext.SaveChangesAsync(cancellationToken);

        var sw = Stopwatch.StartNew();
        OfficialImportResult importResult;
        try
        {
            importResult = await provider.ImportAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Erro ao importar {Source}", sourceType);
            await LogAsync(
                sourceType,
                "Import",
                IntegrationLogStatus.Failed,
                ex.Message,
                triggeredBy,
                sw.ElapsedMilliseconds,
                null,
                cancellationToken);

            versionRow.Status = OfficialVersionStatus.CheckFailed;
            versionRow.Notes = ex.Message;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new OfficialUpdateActionResultDto(
                sourceType.ToString(),
                "Failed",
                ex.Message,
                null);
        }

        sw.Stop();

        if (importResult.Success)
        {
            versionRow.VersionLabel = importResult.NewVersion ?? versionRow.VersionLabel;
            versionRow.InstalledFileHash = importResult.FileHash ?? versionRow.InstalledFileHash;
            versionRow.LastImportedAt = DateTime.UtcNow;
            versionRow.InstalledRecordCount = importResult.ImportedCount
                ?? versionRow.InstalledRecordCount;
            versionRow.Status = OfficialVersionStatus.UpToDate;
            versionRow.Notes = importResult.Message;
        }
        else
        {
            versionRow.Status = OfficialVersionStatus.ManualDownloadRequired;
            versionRow.Notes = importResult.Message;
        }

        versionRow.LastCheckedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await LogAsync(
            sourceType,
            "Import",
            importResult.Success ? IntegrationLogStatus.Success : IntegrationLogStatus.Warning,
            importResult.Message,
            triggeredBy,
            sw.ElapsedMilliseconds,
            JsonSerializer.Serialize(importResult),
            cancellationToken);

        return new OfficialUpdateActionResultDto(
            sourceType.ToString(),
            importResult.Success ? "Success" : "ManualRequired",
            importResult.Message,
            importResult.ImportedCount);
    }

    public async Task<IReadOnlyList<IntegrationLogDto>> GetLogsAsync(
        int take = 50,
        OfficialSourceType? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.IntegrationLogs.AsNoTracking().AsQueryable();
        if (sourceType.HasValue)
            query = query.Where(l => l.SourceType == sourceType.Value);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

        return logs.Select(l => new IntegrationLogDto(
            l.Id,
            l.SourceType.ToString(),
            l.Action,
            l.Status.ToString(),
            l.Message,
            l.TriggeredBy,
            l.DurationMs,
            l.CreatedAt)).ToList();
    }

    internal async Task RunCheckAsync(
        OfficialSourceType sourceType,
        string triggeredBy,
        CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(sourceType, out var provider))
            return;

        var sw = Stopwatch.StartNew();
        OfficialCheckResult check;
        try
        {
            check = await provider.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Verificação falhou para {Source}", sourceType);
            await LogAsync(
                sourceType,
                "Check",
                IntegrationLogStatus.Failed,
                ex.Message,
                triggeredBy,
                sw.ElapsedMilliseconds,
                null,
                cancellationToken);
            return;
        }

        sw.Stop();
        var row = await GetOrCreateVersionRowAsync(sourceType, cancellationToken);
        row.VersionLabel = check.CurrentVersion;
        row.RemoteVersionLabel = check.RemoteVersion;
        row.InstalledFileHash = check.InstalledFileHash;
        row.RemoteFileHash = check.RemoteFileHash;
        row.Status = check.Status;
        row.InstalledRecordCount = check.RecordCount;
        row.Notes = check.Notes;
        row.SourceUrl = GetSourceUrl(sourceType);
        row.LastCheckedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var logStatus = check.Status switch
        {
            OfficialVersionStatus.CheckFailed => IntegrationLogStatus.Failed,
            OfficialVersionStatus.UpdateAvailable => IntegrationLogStatus.Warning,
            OfficialVersionStatus.ManualDownloadRequired => IntegrationLogStatus.Info,
            _ => IntegrationLogStatus.Success,
        };

        await LogAsync(
            sourceType,
            "Check",
            logStatus,
            $"Versão local: {check.CurrentVersion}; remota: {check.RemoteVersion ?? "—"}; status: {check.Status}",
            triggeredBy,
            sw.ElapsedMilliseconds,
            JsonSerializer.Serialize(check),
            cancellationToken);
    }

    private async Task EnsureVersionRowsAsync(CancellationToken cancellationToken)
    {
        foreach (var sourceType in Enum.GetValues<OfficialSourceType>())
        {
            await GetOrCreateVersionRowAsync(sourceType, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<OfficialVersion> GetOrCreateVersionRowAsync(
        OfficialSourceType sourceType,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.OfficialVersions
            .FirstOrDefaultAsync(v => v.SourceType == sourceType, cancellationToken);

        if (row is not null)
            return row;

        row = new OfficialVersion
        {
            SourceType = sourceType,
            VersionLabel = "—",
            SourceUrl = GetSourceUrl(sourceType),
            Status = OfficialVersionStatus.NeverChecked,
        };
        dbContext.OfficialVersions.Add(row);
        return row;
    }

    private string? GetSourceUrl(OfficialSourceType sourceType) => sourceType switch
    {
        OfficialSourceType.Tuss => settings.Value.Tuss.SourceUrl,
        OfficialSourceType.Tiss => settings.Value.Tiss.SourceUrl,
        OfficialSourceType.Sigtap => settings.Value.Sigtap.SourceUrl,
        OfficialSourceType.Ans => settings.Value.Ans.SourceUrl,
        OfficialSourceType.SusTables => settings.Value.SusTables.SourceUrl,
        OfficialSourceType.Anvisa => settings.Value.Anvisa.SourceUrl,
        OfficialSourceType.Brasindice => settings.Value.Brasindice.SourceUrl,
        OfficialSourceType.Simpro => settings.Value.Simpro.SourceUrl,
        _ => null,
    };

    private OfficialSourceStatusDto MapSource(OfficialVersion v)
    {
        var config = v.SourceType switch
        {
            OfficialSourceType.Tuss => settings.Value.Tuss,
            OfficialSourceType.Tiss => settings.Value.Tiss,
            OfficialSourceType.Sigtap => settings.Value.Sigtap,
            OfficialSourceType.Ans => settings.Value.Ans,
            OfficialSourceType.SusTables => settings.Value.SusTables,
            OfficialSourceType.Anvisa => settings.Value.Anvisa,
            OfficialSourceType.Brasindice => settings.Value.Brasindice,
            OfficialSourceType.Simpro => settings.Value.Simpro,
            _ => new OfficialSourceSettings(),
        };

        var canAutoImport = v.SourceType switch
        {
            OfficialSourceType.Tuss => config.AutoImportBundled,
            OfficialSourceType.Sigtap => config.AutoSyncOfficial,
            _ => false,
        };

        return new OfficialSourceStatusDto(
            v.SourceType.ToString(),
            config.DisplayName,
            v.VersionLabel,
            v.RemoteVersionLabel,
            v.Status.ToString(),
            v.SourceUrl,
            v.Notes,
            v.LastCheckedAt,
            v.LastImportedAt,
            v.InstalledRecordCount,
            canAutoImport);
    }

    private async Task LogAsync(
        OfficialSourceType sourceType,
        string action,
        IntegrationLogStatus status,
        string message,
        string? triggeredBy,
        long durationMs,
        string? detailsJson,
        CancellationToken cancellationToken)
    {
        dbContext.IntegrationLogs.Add(new IntegrationLog
        {
            SourceType = sourceType,
            Action = action,
            Status = status,
            Message = message.Length > 2000 ? message[..2000] : message,
            DetailsJson = detailsJson,
            DurationMs = durationMs,
            TriggeredBy = triggeredBy,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
