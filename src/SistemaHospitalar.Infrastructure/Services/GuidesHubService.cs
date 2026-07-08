using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Guides;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class GuidesHubService(
    AppDbContext dbContext,
    ITissBillingService tissBillingService,
    ISusGuideService susGuideService) : IGuidesHubService
{
    private const string DefaultServiceUnit = "Unidade Principal";

    public async Task<GuidesHubDashboardDto> GetDashboardAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(dateFrom, dateTo);
        var query = dbContext.TissGuides.AsNoTracking()
            .Where(g => g.IsActive && g.CreatedAt >= from && g.CreatedAt <= to);

        var guides = await query
            .Select(g => new
            {
                g.Status,
                g.TotalAmount,
                g.CreatedAt,
                Insurance = g.HealthInsurance.Name,
                Professional = g.RequestingProfessionalName
                    ?? (g.RequestingProfessional != null ? g.RequestingProfessional.FullName : null),
                Specialty = g.RequestingProfessional != null && g.RequestingProfessional.Specialty != null
                    ? g.RequestingProfessional.Specialty.Name
                    : null,
                g.AuthorizationPassword,
            })
            .ToListAsync(cancellationToken);

        var issued = guides.Count;
        var pending = guides.Count(g => g.Status is TissGuideStatus.Draft or TissGuideStatus.Sent);
        var authorized = guides.Count(g =>
            g.Status is TissGuideStatus.Sent or TissGuideStatus.Paid
            || !string.IsNullOrWhiteSpace(g.AuthorizationPassword));
        var billed = guides.Count(g => g.Status is TissGuideStatus.Sent or TissGuideStatus.Paid);
        var glosa = guides.Count(g => g.Status == TissGuideStatus.Glosa);

        double? avgHours = null;
        var authDurations = guides
            .Where(g => !string.IsNullOrWhiteSpace(g.AuthorizationPassword))
            .Select(g =>
            {
                var hours = (g.CreatedAt - from).TotalHours;
                return hours >= 0 ? hours : (double?)null;
            })
            .Where(h => h.HasValue)
            .Select(h => h!.Value)
            .ToList();

        if (authDurations.Count > 0)
        {
            avgHours = Math.Round(authDurations.Average(), 1);
        }

        return new GuidesHubDashboardDto(
            issued,
            authorized,
            pending,
            billed,
            glosa,
            avgHours,
            BuildSlices(guides, g => g.Insurance, g => g.TotalAmount),
            BuildSlices(guides, g => g.Professional ?? "Não informado", g => g.TotalAmount),
            BuildSlices(guides, g => g.Specialty ?? "Não informado", g => g.TotalAmount));
    }

    public async Task<GuidesHubListResultDto> SearchAsync(
        GuidesHubFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var includeTiss = string.IsNullOrWhiteSpace(filter.GroupId) || filter.GroupId is not "sus";
        var includeSus = string.IsNullOrWhiteSpace(filter.GroupId) || filter.GroupId == "sus";

        if (filter.GroupId is "consultas" or "exames" or "procedimentos" or "internacao" or "tiss" or "autorizacoes")
        {
            includeSus = false;
        }

        if (filter.GroupId == "faturamento")
        {
            includeSus = true;
        }

        var items = new List<GuideHubListItemDto>();

        if (includeTiss)
        {
            items.AddRange(await LoadTissItemsAsync(filter, cancellationToken));
        }

        if (includeSus)
        {
            items.AddRange(await LoadSusItemsAsync(filter, cancellationToken));
        }

        var ordered = items.OrderByDescending(i => i.CreatedAt).ToList();
        var total = ordered.Count;
        var take = Math.Clamp(filter.Take, 1, 200);
        var skip = Math.Max(filter.Skip, 0);
        var page = ordered.Skip(skip).Take(take).ToList();

        return new GuidesHubListResultDto(total, page);
    }

    private async Task<List<GuideHubListItemDto>> LoadTissItemsAsync(
        GuidesHubFilterDto filter,
        CancellationToken cancellationToken)
    {
        var (from, to) = NormalizeRange(filter.DateFrom, filter.DateTo);
        var query = BuildFilteredQuery(filter, from, to);

        var rows = await query
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new
            {
                g.Id,
                g.GuideNumber,
                PatientName = g.Patient.FullName,
                InsuranceName = g.HealthInsurance.Name,
                RequestingName = g.RequestingProfessionalName
                    ?? (g.RequestingProfessional != null ? g.RequestingProfessional.FullName : null),
                SpecialtyName = g.RequestingProfessional != null && g.RequestingProfessional.Specialty != null
                    ? g.RequestingProfessional.Specialty.Name
                    : null,
                ProcedureSummary = g.Items.Where(i => i.IsActive)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.Description)
                    .FirstOrDefault(),
                g.Cid10Code,
                g.CreatedAt,
                g.Status,
                g.GuideType,
                g.TotalAmount,
                ServiceUnit = g.ServiceUnit != null
                    ? g.ServiceUnit.Name
                    : g.Hospitalization != null && g.Hospitalization.Bed != null && g.Hospitalization.Bed.Ward != null
                        ? g.Hospitalization.Bed.Ward.Name
                        : g.Appointment != null && g.Appointment.Room != null && g.Appointment.Room != ""
                            ? g.Appointment.Room!
                            : DefaultServiceUnit,
                g.AuthorizationPassword,
            })
            .ToListAsync(cancellationToken);

        var guideIds = rows.Select(r => r.Id).ToList();
        var authDates = guideIds.Count == 0
            ? new Dictionary<Guid, DateTime>()
            : await dbContext.InsuranceAuthorizations.AsNoTracking()
                .Where(a => a.IsActive
                    && a.TissGuideId != null
                    && guideIds.Contains(a.TissGuideId.Value)
                    && a.Status == InsuranceAuthorizationStatus.Approved)
                .GroupBy(a => a.TissGuideId!.Value)
                .Select(g => new { GuideId = g.Key, AuthorizedAt = g.Min(a => a.ValidFrom ?? a.CreatedAt) })
                .ToDictionaryAsync(x => x.GuideId, x => x.AuthorizedAt, cancellationToken);

        return rows.Select(r =>
        {
            DateTime? authorizedAt = authDates.TryGetValue(r.Id, out var authAt) ? authAt : null;
            if (!authorizedAt.HasValue && !string.IsNullOrWhiteSpace(r.AuthorizationPassword))
            {
                authorizedAt = r.CreatedAt;
            }

            return new GuideHubListItemDto(
                r.Id,
                r.GuideNumber,
                r.PatientName,
                r.InsuranceName,
                r.RequestingName,
                r.SpecialtyName,
                r.ProcedureSummary,
                r.Cid10Code,
                r.CreatedAt,
                authorizedAt,
                (int)r.Status,
                StatusLabel(r.Status),
                (int)r.GuideType,
                GuideTypeLabel(r.GuideType),
                r.ServiceUnit,
                r.TotalAmount,
                "tiss");
        }).ToList();
    }

    private async Task<List<GuideHubListItemDto>> LoadSusItemsAsync(
        GuidesHubFilterDto filter,
        CancellationToken cancellationToken)
    {
        var susFilter = new SusGuideFilterDto(
            filter.DateFrom,
            filter.DateTo,
            filter.PatientId,
            filter.ProfessionalId,
            filter.ServiceUnitId,
            MapSusGuideType(filter.GroupId),
            MapSusStatus(filter.Status),
            filter.GuideNumber,
            filter.ProcedureSearch,
            0,
            500);

        var guides = await susGuideService.SearchAsync(susFilter, cancellationToken);
        return guides.Select(g => new GuideHubListItemDto(
            g.Id,
            g.GuideNumber,
            g.PatientName,
            "SUS",
            g.ProfessionalName,
            null,
            g.ProcedureDescription,
            g.Cid10Code,
            g.CreatedAt,
            g.AuthorizedAt,
            g.Status,
            SusStatusLabel(g.Status),
            g.GuideType,
            SusTypeLabel(g.GuideType),
            g.ServiceUnitName ?? DefaultServiceUnit,
            g.TotalAmount ?? 0,
            "sus")).ToList();
    }

    private static int? MapSusGuideType(string? groupId) => groupId switch
    {
        "sus" => null,
        _ => null,
    };

    private static int? MapSusStatus(TissGuideStatus? status) =>
        status.HasValue ? status.Value switch
        {
            TissGuideStatus.Draft => 1,
            TissGuideStatus.Sent => 2,
            TissGuideStatus.Paid => 4,
            TissGuideStatus.Glosa => 5,
            TissGuideStatus.Cancelled => 6,
            _ => null,
        } : null;

    public async Task<IReadOnlyList<GuideHistoryEntryDto>> GetHistoryAsync(
        Guid guideId,
        string? source,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(source, "sus", StringComparison.OrdinalIgnoreCase))
        {
            return await GetSusHistoryAsync(guideId, cancellationToken);
        }

        return await GetTissHistoryAsync(guideId, cancellationToken);
    }

    private async Task<IReadOnlyList<GuideHistoryEntryDto>> GetTissHistoryAsync(
        Guid guideId,
        CancellationToken cancellationToken)
    {
        var guide = await dbContext.TissGuides.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guideId && g.IsActive, cancellationToken);

        if (guide is null)
        {
            return [];
        }

        var entries = new List<GuideHistoryEntryDto>();

        var audits = await dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.EntityId == guideId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new GuideHistoryEntryDto(
                a.CreatedAt,
                a.Action,
                a.UserEmail,
                a.Details,
                "audit"))
            .ToListAsync(cancellationToken);

        entries.AddRange(audits);

        var authorizations = await dbContext.InsuranceAuthorizations.AsNoTracking()
            .Where(a => a.IsActive && a.TissGuideId == guideId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new GuideHistoryEntryDto(
                a.CreatedAt,
                $"Autorização — {a.Status}",
                null,
                $"{a.AuthorizationNumber}: {a.ProcedureSummary ?? a.Notes ?? "Sem detalhes"}",
                "authorization"))
            .ToListAsync(cancellationToken);

        entries.AddRange(authorizations);

        entries.Add(new GuideHistoryEntryDto(
            guide.CreatedAt,
            "Emissão",
            null,
            $"Guia {guide.GuideNumber} criada.",
            "system"));

        if (guide.SentAt.HasValue)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.SentAt.Value,
                "Envio",
                null,
                "Guia enviada para operadora / faturamento.",
                "system"));
        }

        if (!string.IsNullOrWhiteSpace(guide.AuthorizationPassword))
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.SentAt ?? guide.CreatedAt,
                "Autorização",
                null,
                $"Senha/autorização: {guide.AuthorizationPassword}",
                "system"));
        }

        if (guide.Status == TissGuideStatus.Paid)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.AccountClosedAt ?? guide.UpdatedAt ?? guide.CreatedAt,
                "Faturamento",
                null,
                "Guia marcada como paga.",
                "system"));
        }

        if (guide.Status == TissGuideStatus.Glosa)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.UpdatedAt ?? guide.CreatedAt,
                "Glosa",
                null,
                "Guia com glosa registrada.",
                "system"));
        }

        if (guide.Status == TissGuideStatus.Cancelled)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.UpdatedAt ?? guide.CreatedAt,
                "Cancelamento",
                null,
                "Guia cancelada.",
                "system"));
        }

        return entries
            .OrderByDescending(e => e.OccurredAt)
            .ToList();
    }

    private async Task<IReadOnlyList<GuideHistoryEntryDto>> GetSusHistoryAsync(
        Guid guideId,
        CancellationToken cancellationToken)
    {
        var guide = await dbContext.SusGuides.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guideId && g.IsActive, cancellationToken);
        if (guide is null)
        {
            return [];
        }

        var audits = await dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.EntityId == guideId && a.EntityType == "SusGuide")
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new GuideHistoryEntryDto(
                a.CreatedAt, a.Action, a.UserEmail, a.Details, "audit"))
            .ToListAsync(cancellationToken);

        var entries = new List<GuideHistoryEntryDto>(audits);
        entries.Add(new GuideHistoryEntryDto(
            guide.CreatedAt, "Emissão", null, $"Guia SUS {guide.GuideNumber} criada.", "system"));

        if (guide.SubmittedAt.HasValue)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.SubmittedAt.Value, "Envio", null, "Guia enviada para autorização SUS.", "system"));
        }

        if (guide.AuthorizedAt.HasValue)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.AuthorizedAt.Value, "Autorização", null,
                $"Autorização: {guide.AuthorizationNumber ?? "—"}", "system"));
        }

        if (guide.Status == SusGuideStatus.Cancelled)
        {
            entries.Add(new GuideHistoryEntryDto(
                guide.UpdatedAt ?? guide.CreatedAt, "Cancelamento", null, "Guia SUS cancelada.", "system"));
        }

        return entries.OrderByDescending(e => e.OccurredAt).ToList();
    }

    public async Task<TissGuideDto?> DuplicateGuideAsync(
        Guid guideId,
        CancellationToken cancellationToken = default)
    {
        var source = await tissBillingService.GetGuideByIdAsync(guideId, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var clinical = new TissGuideClinicalRequest(
            source.Clinical.Cid10Code,
            source.Clinical.Cid10Secondary,
            source.Clinical.ClinicalJustification,
            source.Clinical.ServiceCharacter,
            source.Clinical.AccidentIndicator,
            source.Clinical.RequestingProfessionalId,
            source.Clinical.RequestingProfessionalName,
            source.Clinical.RequestingProfessionalCrm,
            source.Clinical.ExecutingProfessionalId,
            source.Clinical.ExecutingProfessionalName,
            source.Clinical.ExecutingProfessionalCrm,
            source.Clinical.AdmissionDate,
            source.Clinical.DischargeDate,
            source.Clinical.RequestedBedType,
            source.Clinical.ParentGuideId,
            source.Clinical.ProfessionalRole,
            source.Clinical.ParticipationPercent,
            source.Clinical.SurgeryId);

        var request = new CreateTissGuideRequest(
            source.PatientId,
            source.HealthInsuranceId,
            source.AppointmentId,
            source.HospitalizationId,
            source.GuideType,
            source.Items.Select(i => new TissGuideItemRequest(
                i.TussCode,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.PriceTableSource,
                i.Cid10Code,
                i.RelatedTussCode)).ToList(),
            source.Notes,
            clinical,
            $"dup-{guideId:N}-{Guid.NewGuid():N}");

        return await tissBillingService.CreateGuideAsync(request, cancellationToken);
    }

    private IQueryable<TissGuide> BuildFilteredQuery(
        GuidesHubFilterDto filter,
        DateTime from,
        DateTime to)
    {
        var query = dbContext.TissGuides.AsNoTracking().Where(g => g.IsActive);

        query = query.Where(g => g.CreatedAt >= from && g.CreatedAt <= to);

        if (filter.PatientId.HasValue)
        {
            query = query.Where(g => g.PatientId == filter.PatientId.Value);
        }

        if (filter.HealthInsuranceId.HasValue)
        {
            query = query.Where(g => g.HealthInsuranceId == filter.HealthInsuranceId.Value);
        }

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(g => g.RequestingProfessionalId == filter.ProfessionalId.Value);
        }

        if (filter.SpecialtyId.HasValue)
        {
            query = query.Where(g =>
                g.RequestingProfessional != null
                && g.RequestingProfessional.SpecialtyId == filter.SpecialtyId.Value);
        }

        if (filter.GuideType.HasValue)
        {
            query = query.Where(g => g.GuideType == filter.GuideType.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(g => g.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.GuideNumber))
        {
            var term = filter.GuideNumber.Trim();
            query = query.Where(g => g.GuideNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProcedureSearch))
        {
            var term = filter.ProcedureSearch.Trim();
            query = query.Where(g => g.Items.Any(i =>
                i.IsActive && (i.Description.Contains(term) || i.TussCode.Contains(term))));
        }

        if (!string.IsNullOrWhiteSpace(filter.ServiceUnit))
        {
            var unit = filter.ServiceUnit.Trim();
            query = query.Where(g =>
                (g.ServiceUnit != null && g.ServiceUnit.Name.Contains(unit))
                || (g.Hospitalization != null && g.Hospitalization.Bed != null
                    && g.Hospitalization.Bed.Ward != null
                    && g.Hospitalization.Bed.Ward.Name.Contains(unit))
                || (g.Appointment != null && g.Appointment.Room != null && g.Appointment.Room.Contains(unit))
                || (unit == DefaultServiceUnit
                    && g.ServiceUnitId == null
                    && g.HospitalizationId == null
                    && (g.Appointment == null || g.Appointment.Room == null || g.Appointment.Room == "")));
        }

        if (filter.ServiceUnitId.HasValue)
        {
            query = query.Where(g => g.ServiceUnitId == filter.ServiceUnitId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.GroupId))
        {
            query = ApplyGroupFilter(query, filter.GroupId.Trim());
        }

        return query;
    }

    private static IQueryable<TissGuide> ApplyGroupFilter(IQueryable<TissGuide> query, string groupId) =>
        groupId switch
        {
            "consultas" => query.Where(g => g.GuideType == TissGuideType.Consultation),
            "exames" => query.Where(g => g.GuideType == TissGuideType.SpSadt),
            "procedimentos" => query.Where(g =>
                g.GuideType == TissGuideType.SpSadt
                || g.GuideType == TissGuideType.Hospitalization
                || g.GuideType == TissGuideType.IndividualFees
                || g.GuideType == TissGuideType.OtherExpenses),
            "internacao" => query.Where(g =>
                g.GuideType == TissGuideType.HospitalizationRequest
                || g.GuideType == TissGuideType.DischargeSummary
                || g.GuideType == TissGuideType.ExtensionRequest
                || g.GuideType == TissGuideType.Hospitalization),
            "tiss" => query,
            "sus" => query.Where(g => g.HealthInsurance.Name.Contains("SUS")),
            "autorizacoes" => query.Where(g =>
                g.GuideType == TissGuideType.HospitalizationRequest
                || g.GuideType == TissGuideType.ExtensionRequest
                || g.Status == TissGuideStatus.Draft),
            "faturamento" => query.Where(g =>
                g.Status == TissGuideStatus.Sent
                || g.Status == TissGuideStatus.Paid
                || g.Status == TissGuideStatus.Glosa),
            "auditoria" => query,
            _ => query,
        };

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var end = to.HasValue
            ? AsUtcDateEnd(to.Value)
            : DateTime.UtcNow;
        var start = from.HasValue
            ? AsUtcDateStart(from.Value)
            : end.AddDays(-30);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return (start, end);
    }

    private static DateTime AsUtcDateStart(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime AsUtcDateEnd(DateTime value)
        => DateTime.SpecifyKind(value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

    private static IReadOnlyList<GuidesHubProductionSliceDto> BuildSlices<T>(
        IEnumerable<T> source,
        Func<T, string> labelSelector,
        Func<T, decimal> amountSelector)
    {
        return source
            .GroupBy(labelSelector)
            .Select(g => new GuidesHubProductionSliceDto(
                g.Key,
                g.Count(),
                g.Sum(amountSelector)))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();
    }

    private static string StatusLabel(TissGuideStatus status) => status switch
    {
        TissGuideStatus.Draft => "Rascunho",
        TissGuideStatus.Sent => "Enviada",
        TissGuideStatus.Paid => "Paga",
        TissGuideStatus.Glosa => "Glosa",
        TissGuideStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };

    private static string SusStatusLabel(int status) => status switch
    {
        1 => "Rascunho",
        2 => "Enviada",
        3 => "Autorizada",
        4 => "Faturada",
        5 => "Glosa",
        6 => "Cancelada",
        _ => status.ToString(),
    };

    private static string SusTypeLabel(int type) => type switch
    {
        1 => "BPA",
        2 => "APAC",
        3 => "AIH",
        _ => type.ToString(),
    };

    private static string GuideTypeLabel(TissGuideType type) => type switch
    {
        TissGuideType.Consultation => "Consulta",
        TissGuideType.SpSadt => "SP/SADT",
        TissGuideType.Hospitalization => "Internação",
        TissGuideType.DischargeSummary => "Resumo de internação",
        TissGuideType.IndividualFees => "Honorários",
        TissGuideType.HospitalizationRequest => "Solicitação de internação",
        TissGuideType.OtherExpenses => "Outras despesas",
        TissGuideType.DentalTreatment => "Odontológica",
        TissGuideType.ExtensionRequest => "Prorrogação",
        _ => type.ToString(),
    };
}
