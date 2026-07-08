using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Tiss;

namespace SistemaHospitalar.Infrastructure.Services;

public class InsuranceIntegrationService(
    AppDbContext dbContext,
    MockOperatorTissClient mockOperatorClient,
    HttpOperatorTissClient httpOperatorClient) : IInsuranceIntegrationService
{
    private static readonly TimeSpan OperatorEligibilityCacheTtl = TimeSpan.FromHours(24);
    public async Task<IReadOnlyList<TussSearchResultDto>> SearchTussAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var term = query?.Trim() ?? string.Empty;
        if (term.Length < 2)
            return [];

        var master = dbContext.TussCatalogs.AsNoTracking()
            .Where(t => t.IsActive && (t.Code.Contains(term) || t.Description.Contains(term)))
            .Take(20)
            .Select(t => new TussSearchResultDto(t.Code, t.Description, "TUSS Master", t.ReferencePrice));

        var cbhpm = dbContext.CbhpmProcedures.AsNoTracking()
            .Where(c => c.IsActive && (c.Code.Contains(term) || c.Description.Contains(term)))
            .Take(10)
            .Select(c => new TussSearchResultDto(c.Code, c.Description, "CBHPM", c.ReferencePrice));

        var bras = dbContext.BrasindiceItems.AsNoTracking()
            .Where(b => b.IsActive && (b.Code.Contains(term) || b.Description.Contains(term)))
            .Take(8)
            .Select(b => new TussSearchResultDto(b.Code, b.Description, "Brasíndice", b.ReferencePrice));

        var simpro = dbContext.SimproItems.AsNoTracking()
            .Where(s => s.IsActive && (s.Code.Contains(term) || s.Description.Contains(term)))
            .Take(8)
            .Select(s => new TussSearchResultDto(s.Code, s.Description, "SIMPRO", s.ReferencePrice));

        var lab = dbContext.LabExamCatalogs.AsNoTracking()
            .Where(e => e.IsActive && (e.TussCode.Contains(term) || e.Name.Contains(term)))
            .Take(10)
            .Select(e => new TussSearchResultDto(e.TussCode ?? string.Empty, e.Name, "Laboratório", null));

        var imaging = dbContext.ImagingProcedureCatalogs.AsNoTracking()
            .Where(e => e.IsActive && (e.TussCode.Contains(term) || e.Name.Contains(term)))
            .Take(10)
            .Select(e => new TussSearchResultDto(e.TussCode ?? string.Empty, e.Name, "Imagem", null));

        return await master.Concat(cbhpm).Concat(bras).Concat(simpro).Concat(lab).Concat(imaging).Take(30).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TissGuideItemRequest>> BuildSuggestedItemsAsync(
        SuggestedGuideItemsRequest request,
        CancellationToken cancellationToken = default)
        => await TissGuideAutoFillService.BuildItemsAsync(dbContext, request, cancellationToken);

    public async Task<TissGuidePrefillDto> GetGuidePrefillAsync(
        GuidePrefillRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientInsurance = await dbContext.PatientInsurances.AsNoTracking()
            .Include(pi => pi.HealthInsurance)
            .Where(pi => pi.PatientId == request.PatientId && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.HealthInsuranceId.HasValue)
        {
            patientInsurance = await dbContext.PatientInsurances.AsNoTracking()
                .Include(pi => pi.HealthInsurance)
                .Where(pi => pi.PatientId == request.PatientId
                    && pi.HealthInsuranceId == request.HealthInsuranceId.Value
                    && pi.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        Appointment? appointment = null;
        if (request.AppointmentId.HasValue)
        {
            appointment = await dbContext.Appointments.AsNoTracking()
                .Include(a => a.Professional).ThenInclude(p => p.Specialty)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value
                    && a.PatientId == request.PatientId
                    && a.IsActive, cancellationToken);
        }
        else
        {
            appointment = await dbContext.Appointments.AsNoTracking()
                .Include(a => a.Professional).ThenInclude(p => p.Specialty)
                .Where(a => a.PatientId == request.PatientId && a.IsActive
                    && a.Status != AppointmentStatus.Cancelled)
                .OrderByDescending(a => a.ScheduledAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        Hospitalization? hospitalization = null;
        if (request.HospitalizationId.HasValue)
        {
            hospitalization = await dbContext.Hospitalizations.AsNoTracking()
                .Include(h => h.Professional)
                .Include(h => h.Bed).ThenInclude(b => b.Ward)
                .FirstOrDefaultAsync(h => h.Id == request.HospitalizationId.Value
                    && h.PatientId == request.PatientId
                    && h.IsActive, cancellationToken);
        }
        else
        {
            hospitalization = await dbContext.Hospitalizations.AsNoTracking()
                .Include(h => h.Professional)
                .Include(h => h.Bed).ThenInclude(b => b.Ward)
                .Where(h => h.PatientId == request.PatientId && h.IsActive)
                .OrderByDescending(h => h.AdmittedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        Surgery? surgery = null;
        if (request.SurgeryId.HasValue)
        {
            surgery = await dbContext.Surgeries.AsNoTracking()
                .Include(s => s.Surgeon)
                .FirstOrDefaultAsync(s => s.Id == request.SurgeryId.Value
                    && s.PatientId == request.PatientId
                    && s.IsActive, cancellationToken);
        }
        else
        {
            surgery = await dbContext.Surgeries.AsNoTracking()
                .Include(s => s.Surgeon)
                .Where(s => s.PatientId == request.PatientId && s.IsActive)
                .OrderByDescending(s => s.ScheduledAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var latestCid = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.IsActive
                && e.MedicalRecord.PatientId == request.PatientId
                && e.Cid10Code != null)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Cid10Code, e.Content })
            .FirstOrDefaultAsync(cancellationToken);

        string? cidDescription = null;
        if (latestCid?.Cid10Code is not null)
        {
            cidDescription = await dbContext.Cid10Catalogs.AsNoTracking()
                .Where(c => c.Code == latestCid.Cid10Code)
                .Select(c => c.Description)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var auth = patientInsurance is null ? null : await dbContext.InsuranceAuthorizations.AsNoTracking()
            .Where(a => a.PatientId == request.PatientId
                && a.HealthInsuranceId == patientInsurance.HealthInsuranceId
                && a.IsActive
                && a.Status == InsuranceAuthorizationStatus.Approved
                && (!a.ValidUntil.HasValue || a.ValidUntil.Value >= DateTime.UtcNow))
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        InsuranceEligibilityCheck? cachedEligibility = null;
        string? operatorDataSource = null;
        if (patientInsurance is not null)
        {
            cachedEligibility = await dbContext.InsuranceEligibilityChecks.AsNoTracking()
                .Where(e => e.PatientId == request.PatientId
                    && e.HealthInsuranceId == patientInsurance.HealthInsuranceId
                    && e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var eligibility = cachedEligibility;
        if (patientInsurance is not null && request.IncludeOperatorData)
        {
            var (resolved, source) = await ResolveOperatorEligibilityForPrefillAsync(
                request,
                patientInsurance,
                cachedEligibility,
                cancellationToken);
            eligibility = resolved ?? cachedEligibility;
            operatorDataSource = source;
        }
        else if (cachedEligibility is not null)
        {
            operatorDataSource = "cache";
        }

        var planName = patientInsurance?.PlanName;
        var accommodation = patientInsurance?.AccommodationType;
        DateTime? cardValidUntil = patientInsurance?.ValidUntil?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        if (eligibility is not null)
        {
            if (!string.IsNullOrWhiteSpace(eligibility.PlanName))
                planName = eligibility.PlanName;
            if (eligibility.ValidUntil.HasValue)
                cardValidUntil = eligibility.ValidUntil;
            if (string.IsNullOrWhiteSpace(accommodation)
                && eligibility.CoverageSummary?.Contains("Acomodação", StringComparison.OrdinalIgnoreCase) == true)
            {
                accommodation = eligibility.CoverageSummary;
            }
        }

        var suggestedItems = await BuildSuggestedItemsAsync(new SuggestedGuideItemsRequest(
            request.PatientId,
            hospitalization?.Id ?? request.HospitalizationId,
            appointment?.Id ?? request.AppointmentId,
            request.GuideType,
            surgery?.Id), cancellationToken);

        return new TissGuidePrefillDto(
            patientInsurance?.HealthInsuranceId,
            patientInsurance?.HealthInsurance.Name,
            patientInsurance?.CardNumber,
            planName,
            patientInsurance?.CnsNumber,
            accommodation,
            auth?.AuthorizationNumber,
            appointment?.Id,
            hospitalization?.Id,
            surgery?.Id,
            latestCid?.Cid10Code,
            cidDescription,
            appointment?.ProfessionalId ?? hospitalization?.ProfessionalId,
            appointment?.Professional.FullName ?? hospitalization?.Professional.FullName,
            appointment?.Professional.Crm ?? hospitalization?.Professional.Crm,
            appointment?.ProfessionalId,
            appointment?.Professional.FullName,
            hospitalization?.AdmittedAt,
            hospitalization?.DischargedAt,
            hospitalization?.Bed?.Ward?.Name,
            request.GuideType,
            suggestedItems,
            eligibility?.Status,
            eligibility?.CreatedAt,
            cardValidUntil,
            eligibility?.ResponseMessage,
            eligibility?.CoverageSummary,
            operatorDataSource);
    }

    public async Task<IReadOnlyList<ProcedureLookupDto>> LookupProcedureAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var term = query?.Trim() ?? string.Empty;
        if (term.Length < 2)
            return [];

        var results = new List<ProcedureLookupDto>();

        var tuss = await CatalogImportHelpers.OrderByCatalogCode(
                dbContext.TussCatalogs.AsNoTracking()
                    .Where(t => t.IsActive && (t.Code.Contains(term) || t.Description.Contains(term))))
            .Take(12)
            .Select(t => new ProcedureLookupDto(
                t.Code, t.Description, "TUSS", t.ReferencePrice,
                TissGuideAutoFillService.SuggestGuideType("TUSS", t.TableType), t.TableType))
            .ToListAsync(cancellationToken);
        results.AddRange(tuss);

        var cbhpm = await dbContext.CbhpmProcedures.AsNoTracking()
            .Where(c => c.IsActive && (c.Code.Contains(term) || c.Description.Contains(term)))
            .Take(8)
            .Select(c => new ProcedureLookupDto(
                c.Code, c.Description, "CBHPM", c.ReferencePrice,
                TissGuideType.SpSadt, null))
            .ToListAsync(cancellationToken);
        results.AddRange(cbhpm);

        var bras = await dbContext.BrasindiceItems.AsNoTracking()
            .Where(b => b.IsActive && (b.Code.Contains(term) || b.Description.Contains(term)))
            .Take(8)
            .Select(b => new ProcedureLookupDto(
                b.Code, b.Description, "Brasíndice", b.ReferencePrice,
                TissGuideType.OtherExpenses, TussTableType.Medication))
            .ToListAsync(cancellationToken);
        results.AddRange(bras);

        var simpro = await dbContext.SimproItems.AsNoTracking()
            .Where(s => s.IsActive && (s.Code.Contains(term) || s.Description.Contains(term)))
            .Take(8)
            .Select(s => new ProcedureLookupDto(
                s.Code, s.Description, "SIMPRO", s.ReferencePrice,
                TissGuideType.OtherExpenses, TussTableType.Material))
            .ToListAsync(cancellationToken);
        results.AddRange(simpro);

        return results.Take(30).ToList();
    }

    public async Task<BillingCatalogSummaryDto> GetBillingCatalogSummaryAsync(CancellationToken cancellationToken = default)
        => new(
            await dbContext.TussCatalogs.CountAsync(t => t.IsActive, cancellationToken),
            await dbContext.CbhpmProcedures.CountAsync(c => c.IsActive, cancellationToken),
            await dbContext.BrasindiceItems.CountAsync(b => b.IsActive, cancellationToken),
            await dbContext.SimproItems.CountAsync(s => s.IsActive, cancellationToken),
            await dbContext.Cid10Catalogs.CountAsync(c => c.IsActive, cancellationToken));

    public async Task<EligibilityCheckDto> CheckEligibilityAsync(
        EligibilityCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        var insurer = await dbContext.HealthInsurances
            .FirstOrDefaultAsync(h => h.Id == request.HealthInsuranceId && h.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Convênio não encontrado.");

        var insurance = await dbContext.PatientInsurances.AsNoTracking()
            .Where(pi => pi.PatientId == request.PatientId
                && pi.HealthInsuranceId == request.HealthInsuranceId
                && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        var cardNumber = request.CardNumber?.Trim()
            ?? insurance?.CardNumber
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new InvalidOperationException("Informe o número da carteirinha.");

        var check = await QueryOperatorEligibilityAsync(
            request.PatientId,
            request.HealthInsuranceId,
            cardNumber,
            insurer,
            insurance,
            cancellationToken);

        return await MapEligibilityAsync(check.Id, cancellationToken)
            ?? throw new InvalidOperationException("Falha ao registrar elegibilidade.");
    }

    public async Task<IReadOnlyList<EligibilityCheckDto>> GetEligibilityHistoryAsync(
        Guid? patientId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.InsuranceEligibilityChecks.AsNoTracking().Where(e => e.IsActive);
        if (patientId.HasValue)
            query = query.Where(e => e.PatientId == patientId.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .Select(e => new EligibilityCheckDto(
                e.Id,
                e.PatientId,
                e.Patient.FullName,
                e.HealthInsuranceId,
                e.HealthInsurance.Name,
                e.CardNumber,
                e.Status,
                e.PlanName,
                e.CoverageSummary,
                e.ValidUntil,
                e.ResponseMessage,
                e.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InsuranceAuthorizationDto>> GetAuthorizationsAsync(
        Guid? patientId,
        Guid? healthInsuranceId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.InsuranceAuthorizations.AsNoTracking().Where(a => a.IsActive);
        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);
        if (healthInsuranceId.HasValue)
            query = query.Where(a => a.HealthInsuranceId == healthInsuranceId.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(MapAuthorization())
            .ToListAsync(cancellationToken);
    }

    public async Task<InsuranceAuthorizationDto> CreateAuthorizationAsync(
        CreateAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AuthorizationNumber))
            throw new InvalidOperationException("Informe o número da autorização/senha ou use solicitação online.");

        return await SaveAuthorizationAsync(
            request.PatientId,
            request.HealthInsuranceId,
            request.AuthorizationType,
            request.AuthorizationNumber.Trim(),
            InsuranceAuthorizationStatus.Approved,
            request.ValidFrom,
            request.ValidUntil,
            request.ProcedureSummary,
            request.TissGuideId,
            request.Notes,
            cancellationToken);
    }

    public async Task<InsuranceAuthorizationDto> RequestOnlineAuthorizationAsync(
        RequestOnlineAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var insurer = await dbContext.HealthInsurances
            .FirstOrDefaultAsync(h => h.Id == request.HealthInsuranceId && h.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Convênio não encontrado.");

        if (!await dbContext.Patients.AsNoTracking()
                .AnyAsync(p => p.Id == request.PatientId && p.IsActive, cancellationToken))
            throw new InvalidOperationException("Paciente não encontrado.");

        var insurance = await dbContext.PatientInsurances.AsNoTracking()
            .Where(pi => pi.PatientId == request.PatientId
                && pi.HealthInsuranceId == request.HealthInsuranceId
                && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        var createPayload = new CreateAuthorizationRequest(
            request.PatientId,
            request.HealthInsuranceId,
            request.AuthorizationType,
            string.Empty,
            request.ValidFrom,
            request.ValidUntil,
            request.ProcedureSummary,
            request.TissGuideId,
            request.Notes);

        var client = OperatorTissClientFactory.Resolve(insurer, mockOperatorClient, httpOperatorClient);
        var sw = Stopwatch.StartNew();
        OperatorAuthorizationResponse opResponse;
        try
        {
            opResponse = await client.RequestAuthorizationAsync(insurer, createPayload, cancellationToken);
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.Authorization,
                opResponse.Approved ? OperatorTransactionStatus.Success : OperatorTransactionStatus.Failure,
                opResponse.AuthorizationNumber,
                JsonSerializer.Serialize(request),
                opResponse.RawJson,
                opResponse.Approved ? null : opResponse.Message,
                (int)sw.ElapsedMilliseconds,
                cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.Authorization,
                OperatorTransactionStatus.Failure,
                insurance?.CardNumber,
                JsonSerializer.Serialize(request),
                null,
                ex.Message,
                (int)sw.ElapsedMilliseconds,
                cancellationToken);
            throw;
        }

        if (!opResponse.Approved)
            throw new InvalidOperationException(opResponse.Message);

        var validUntil = request.ValidUntil
            ?? DateTime.UtcNow.AddDays(insurer.AuthorizationDeadlineDays ?? 30);

        return await SaveAuthorizationAsync(
            request.PatientId,
            request.HealthInsuranceId,
            request.AuthorizationType,
            opResponse.AuthorizationNumber,
            InsuranceAuthorizationStatus.Approved,
            request.ValidFrom ?? DateTime.UtcNow,
            validUntil,
            request.ProcedureSummary,
            request.TissGuideId,
            $"[Online {insurer.OperatorCode ?? insurer.Name}] {opResponse.Message}",
            cancellationToken);
    }

    private async Task<InsuranceAuthorizationDto> SaveAuthorizationAsync(
        Guid patientId,
        Guid healthInsuranceId,
        InsuranceAuthorizationType authorizationType,
        string authorizationNumber,
        InsuranceAuthorizationStatus status,
        DateTime? validFrom,
        DateTime? validUntil,
        string? procedureSummary,
        Guid? tissGuideId,
        string? notes,
        CancellationToken cancellationToken)
    {
        var auth = new InsuranceAuthorization
        {
            PatientId = patientId,
            HealthInsuranceId = healthInsuranceId,
            AuthorizationType = authorizationType,
            Status = status,
            AuthorizationNumber = authorizationNumber,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            ProcedureSummary = procedureSummary?.Trim(),
            TissGuideId = tissGuideId,
            Notes = notes?.Trim(),
        };

        dbContext.InsuranceAuthorizations.Add(auth);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await dbContext.InsuranceAuthorizations.AsNoTracking()
            .Where(a => a.Id == auth.Id)
            .Select(MapAuthorization())
            .FirstOrDefaultAsync(cancellationToken))!;
    }

    public async Task<InsuranceAuthorizationDto?> UpdateAuthorizationAsync(
        Guid id,
        UpdateAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var auth = await dbContext.InsuranceAuthorizations
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        if (auth is null)
            return null;

        auth.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.AuthorizationNumber))
            auth.AuthorizationNumber = request.AuthorizationNumber.Trim();
        auth.ValidFrom = request.ValidFrom;
        auth.ValidUntil = request.ValidUntil;
        auth.ProcedureSummary = request.ProcedureSummary?.Trim();
        auth.Notes = request.Notes?.Trim();
        auth.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.InsuranceAuthorizations.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapAuthorization())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TissBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.TissBatches.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new TissBatchDto(
                b.Id,
                b.BatchNumber,
                b.HealthInsuranceId,
                b.HealthInsurance.Name,
                b.Competence,
                b.Status,
                b.ProtocolNumber,
                b.SentAt,
                b.TotalAmount,
                b.GuideCount,
                b.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TissBatchDetailDto?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TissBatches.AsNoTracking()
            .Where(b => b.Id == id && b.IsActive)
            .Select(b => new TissBatchDetailDto(
                b.Id,
                b.BatchNumber,
                b.HealthInsuranceId,
                b.HealthInsurance.Name,
                b.Competence,
                b.Status,
                b.ProtocolNumber,
                b.SentAt,
                b.TotalAmount,
                b.GuideCount,
                b.XmlContent,
                b.Guides.Where(g => g.IsActive).Select(g => new TissGuideSummaryDto(
                    g.Id,
                    g.GuideNumber,
                    g.Patient.FullName,
                    g.TotalAmount,
                    g.Status)).ToList(),
                b.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TissBatchDetailDto> CreateBatchAsync(
        CreateTissBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var competence = request.Competence.Trim();
        if (competence.Length != 7 || competence[4] != '-')
            throw new InvalidOperationException("Competência deve estar no formato AAAA-MM.");

        var insurer = await dbContext.HealthInsurances.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == request.HealthInsuranceId && h.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Convênio não encontrado.");

        var guidesQuery = dbContext.TissGuides
            .Include(g => g.Patient)
            .Include(g => g.Items)
            .Where(g => g.IsActive
                && g.HealthInsuranceId == request.HealthInsuranceId
                && g.Status == TissGuideStatus.Sent
                && g.TissBatchId == null);

        if (request.GuideIds is { Count: > 0 })
            guidesQuery = guidesQuery.Where(g => request.GuideIds.Contains(g.Id));

        var guides = await guidesQuery.OrderBy(g => g.GuideNumber).ToListAsync(cancellationToken);
        if (guides.Count == 0)
            throw new InvalidOperationException("Nenhuma guia enviada disponível para o lote.");

        var batchCount = await dbContext.TissBatches.CountAsync(cancellationToken);
        var batchNumber = $"LOTE-{competence.Replace("-", "")}-{(batchCount + 1):D4}";

        var batch = new TissBatch
        {
            BatchNumber = batchNumber,
            HealthInsuranceId = request.HealthInsuranceId,
            Competence = competence,
            Status = TissBatchStatus.Generated,
            TotalAmount = guides.Sum(g => g.TotalAmount),
            GuideCount = guides.Count,
        };

        batch.XmlContent = TissXmlBuilder.Build(insurer, batchNumber, competence, guides);

        dbContext.TissBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var guide in guides)
        {
            guide.TissBatchId = batch.Id;
            guide.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetBatchByIdAsync(batch.Id, cancellationToken))!;
    }

    public async Task<TissBatchDetailDto?> MarkBatchSentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.TissBatches
            .Include(b => b.HealthInsurance)
            .Include(b => b.Guides).ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        if (batch is null || batch.Status is TissBatchStatus.Sent or TissBatchStatus.Processed)
            return null;

        var insurer = batch.HealthInsurance;
        var guides = batch.Guides.Where(g => g.IsActive).ToList();
        var client = OperatorTissClientFactory.Resolve(insurer, mockOperatorClient, httpOperatorClient);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await client.SendBatchAsync(insurer, batch, guides, cancellationToken);
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.BatchSend,
                response.Success ? OperatorTransactionStatus.Success : OperatorTransactionStatus.Failure,
                batch.BatchNumber, batch.XmlContent, response.RawJson, null,
                (int)sw.ElapsedMilliseconds, cancellationToken);

            batch.Status = TissBatchStatus.Sent;
            batch.SentAt = DateTime.UtcNow;
            batch.ProtocolNumber = response.ProtocolNumber;
            batch.UpdatedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.BatchSend,
                OperatorTransactionStatus.Failure, batch.BatchNumber, batch.XmlContent, null, ex.Message,
                (int)sw.ElapsedMilliseconds, cancellationToken);
            throw;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetBatchByIdAsync(id, cancellationToken);
    }

    public async Task<TissConvenioDashboardDto> GetConvenioDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var guides = await dbContext.TissGuides.AsNoTracking()
            .Where(g => g.IsActive)
            .Select(g => new
            {
                g.TotalAmount,
                g.Status,
                g.SentAt,
                g.HealthInsurance.Name,
                GlosaOpen = g.Glosas.Where(gl => gl.IsActive && !gl.IsResolved).Sum(gl => gl.GlosaAmount),
            })
            .ToListAsync(cancellationToken);

        var totalBilled = guides.Sum(g => g.TotalAmount);
        var totalPaid = guides.Where(g => g.Status == TissGuideStatus.Paid).Sum(g => g.TotalAmount);
        var totalGlosaOpen = guides.Sum(g => g.GlosaOpen);
        var glosaRate = totalBilled == 0 ? 0 : Math.Round(totalGlosaOpen / totalBilled * 100, 1);

        var sentGuides = guides.Where(g => g.Status == TissGuideStatus.Sent || g.Status == TissGuideStatus.Glosa).ToList();
        var over30 = sentGuides.Count(g => g.SentAt.HasValue && (now - g.SentAt.Value).TotalDays > 30);
        var over60 = sentGuides.Count(g => g.SentAt.HasValue && (now - g.SentAt.Value).TotalDays > 60);

        var byOperator = guides
            .GroupBy(g => g.Name)
            .Select(g => new TissOperatorStatDto(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var glosaByOperator = guides
            .Where(g => g.GlosaOpen > 0)
            .GroupBy(g => g.Name)
            .Select(g => new TissOperatorStatDto(g.Key, g.Count(), g.Sum(x => x.GlosaOpen)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new TissConvenioDashboardDto(
            totalBilled,
            totalPaid,
            totalGlosaOpen,
            glosaRate,
            over30,
            over60,
            byOperator,
            glosaByOperator);
    }

    public Task<TissXmlValidationResultDto> ValidateXmlAsync(string xmlContent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TissXmlValidator.Validate(xmlContent));
    }

    public async Task<TissXmlValidationResultDto?> ValidateBatchXmlAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.TissBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId && b.IsActive, cancellationToken);
        if (batch is null)
            return null;

        return TissXmlValidator.Validate(batch.XmlContent);
    }

    private static bool SupportsOperatorIntegration(HealthInsurance? insurer)
    {
        if (insurer is null)
            return false;

        var name = insurer.Name.ToUpperInvariant();
        if (name.Contains("SUS", StringComparison.Ordinal)
            || name.Contains("PARTICULAR", StringComparison.Ordinal)
            || name.Contains("AVULSO", StringComparison.Ordinal))
            return false;

        return !string.IsNullOrWhiteSpace(insurer.OperatorCode)
            || !string.IsNullOrWhiteSpace(insurer.WebServiceUrl)
            || insurer.UseMockIntegration;
    }

    private async Task<(InsuranceEligibilityCheck? Check, string? Source)> ResolveOperatorEligibilityForPrefillAsync(
        GuidePrefillRequest request,
        PatientInsurance patientInsurance,
        InsuranceEligibilityCheck? cached,
        CancellationToken cancellationToken)
    {
        var insurer = patientInsurance.HealthInsurance;
        if (!SupportsOperatorIntegration(insurer))
            return (cached, cached is null ? null : "cache");

        var cacheValid = cached is not null
            && DateTime.UtcNow - cached.CreatedAt < OperatorEligibilityCacheTtl;
        if (cacheValid && !request.RefreshOperatorData)
            return (cached, "cache");

        var cardNumber = patientInsurance.CardNumber?.Trim();
        if (string.IsNullOrWhiteSpace(cardNumber))
            return (cached, cached is null ? null : "cache");

        try
        {
            var check = await QueryOperatorEligibilityAsync(
                request.PatientId,
                patientInsurance.HealthInsuranceId,
                cardNumber,
                insurer,
                patientInsurance,
                cancellationToken);
            return (check, "live");
        }
        catch
        {
            return (cached, cached is null ? null : "cache");
        }
    }

    private async Task<InsuranceEligibilityCheck> QueryOperatorEligibilityAsync(
        Guid patientId,
        Guid healthInsuranceId,
        string cardNumber,
        HealthInsurance insurer,
        PatientInsurance? patientInsurance,
        CancellationToken cancellationToken)
    {
        var client = OperatorTissClientFactory.Resolve(insurer, mockOperatorClient, httpOperatorClient);
        var eligRequest = new EligibilityCheckRequest(patientId, healthInsuranceId, cardNumber);
        var sw = Stopwatch.StartNew();
        OperatorEligibilityResponse opResponse;
        try
        {
            opResponse = await client.CheckEligibilityAsync(insurer, eligRequest, patientInsurance, cancellationToken);
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.Eligibility,
                OperatorTransactionStatus.Success, cardNumber, JsonSerializer.Serialize(eligRequest),
                opResponse.RawJson, null, (int)sw.ElapsedMilliseconds, cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            await OperatorTransactionLogger.LogAsync(
                dbContext, insurer, OperatorTransactionType.Eligibility,
                OperatorTransactionStatus.Failure, cardNumber, JsonSerializer.Serialize(eligRequest),
                null, ex.Message, (int)sw.ElapsedMilliseconds, cancellationToken);
            throw;
        }

        var status = opResponse.IsEligible ? EligibilityStatus.Eligible : EligibilityStatus.Ineligible;
        var check = new InsuranceEligibilityCheck
        {
            PatientId = patientId,
            HealthInsuranceId = healthInsuranceId,
            CardNumber = cardNumber,
            Status = status,
            PlanName = opResponse.PlanName ?? patientInsurance?.PlanName,
            CoverageSummary = opResponse.CoverageSummary
                ?? (patientInsurance?.AccommodationType is not null
                    ? $"Acomodação: {patientInsurance.AccommodationType}. Plano: {patientInsurance.PlanName ?? "—"}."
                    : $"Plano: {patientInsurance?.PlanName ?? "—"}."),
            ValidUntil = opResponse.ValidUntil
                ?? (patientInsurance?.ValidUntil.HasValue == true
                    ? patientInsurance.ValidUntil.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                    : null),
            ResponseMessage = opResponse.Message,
            RawResponseJson = opResponse.RawJson,
        };

        dbContext.InsuranceEligibilityChecks.Add(check);
        await dbContext.SaveChangesAsync(cancellationToken);
        return check;
    }

    private async Task<EligibilityCheckDto?> MapEligibilityAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.InsuranceEligibilityChecks.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EligibilityCheckDto(
                e.Id,
                e.PatientId,
                e.Patient.FullName,
                e.HealthInsuranceId,
                e.HealthInsurance.Name,
                e.CardNumber,
                e.Status,
                e.PlanName,
                e.CoverageSummary,
                e.ValidUntil,
                e.ResponseMessage,
                e.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<InsuranceAuthorization, InsuranceAuthorizationDto>> MapAuthorization() =>
        a => new InsuranceAuthorizationDto(
            a.Id,
            a.PatientId,
            a.Patient.FullName,
            a.HealthInsuranceId,
            a.HealthInsurance.Name,
            a.AuthorizationType,
            a.Status,
            a.AuthorizationNumber,
            a.ValidFrom,
            a.ValidUntil,
            a.ProcedureSummary,
            a.TissGuideId,
            a.Notes,
            a.CreatedAt);
}
