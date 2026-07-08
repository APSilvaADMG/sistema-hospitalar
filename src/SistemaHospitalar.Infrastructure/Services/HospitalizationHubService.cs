using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HospitalizationHubService(AppDbContext dbContext) : IHospitalizationHubService
{
    public async Task<HospitalizationHubDashboardDto> GetDashboardAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(dateFrom, dateTo);

        var activeHospitalizations = await dbContext.Hospitalizations
            .AsNoTracking()
            .Where(h => h.IsActive && h.Status == HospitalizationStatus.Active)
            .Select(h => new
            {
                h.AdmittedAt,
                WardName = h.Bed.Ward.Name,
                Modality = h.Bed.Ward.CoverageModality,
                Professional = h.Professional.FullName,
            })
            .ToListAsync(cancellationToken);

        var dischargedInPeriod = await dbContext.Hospitalizations
            .AsNoTracking()
            .CountAsync(h =>
                h.IsActive
                && h.Status != HospitalizationStatus.Active
                && h.DischargedAt >= from
                && h.DischargedAt <= to,
                cancellationToken);

        var pendingRequests = await dbContext.HospitalizationRequests
            .AsNoTracking()
            .CountAsync(r =>
                r.IsActive
                && (r.Status == HospitalizationRequestStatus.Pending
                    || r.Status == HospitalizationRequestStatus.Approved),
                cancellationToken);

        var bedStats = await dbContext.Beds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Available = g.Count(b => b.Status == BedStatus.Available),
                Occupied = g.Count(b => b.Status == BedStatus.Occupied),
                Blocked = g.Count(b => b.Status == BedStatus.Maintenance
                    || b.Status == BedStatus.Reserved),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var stayDays = activeHospitalizations
            .Select(h => (now - h.AdmittedAt).TotalDays)
            .Where(d => d >= 0)
            .ToList();

        double? avgStay = stayDays.Count > 0 ? Math.Round(stayDays.Average(), 1) : null;

        return new HospitalizationHubDashboardDto(
            activeHospitalizations.Count,
            dischargedInPeriod,
            pendingRequests,
            bedStats?.Available ?? 0,
            bedStats?.Occupied ?? 0,
            bedStats?.Blocked ?? 0,
            avgStay,
            BuildSlices(activeHospitalizations, h => h.WardName),
            BuildSlices(activeHospitalizations, h => ModalityLabel(h.Modality)),
            BuildSlices(activeHospitalizations, h => h.Professional));
    }

    public async Task<HospitalizationHubListResultDto> SearchAsync(
        HospitalizationHubFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(filter.DateFrom, filter.DateTo);
        var includeRequests = string.IsNullOrWhiteSpace(filter.GroupId)
            || filter.GroupId is "solicitacoes" or "admissao";

        var items = new List<HospitalizationHubListItemDto>();

        if (filter.GroupId != "solicitacoes")
        {
            items.AddRange(await LoadHospitalizationsAsync(filter, from, to, cancellationToken));
        }

        if (includeRequests && filter.GroupId is "solicitacoes" or "admissao" or null or "")
        {
            if (filter.GroupId is "solicitacoes" or "admissao" or null or "")
            {
                items.AddRange(await LoadRequestsAsync(filter, from, to, cancellationToken));
            }
        }

        if (filter.GroupId == "solicitacoes")
        {
            items = items.Where(i => i.ItemType == "request").ToList();
        }

        var ordered = items.OrderByDescending(i => i.EventAt).ToList();
        var total = ordered.Count;
        var take = Math.Clamp(filter.Take, 1, 200);
        var skip = Math.Max(filter.Skip, 0);
        var page = ordered.Skip(skip).Take(take).ToList();

        return new HospitalizationHubListResultDto(total, page);
    }

    private async Task<List<HospitalizationHubListItemDto>> LoadHospitalizationsAsync(
        HospitalizationHubFilterDto filter,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Hospitalizations.AsNoTracking().Where(h => h.IsActive);

        query = filter.GroupId switch
        {
            "ativas" => query.Where(h => h.Status == HospitalizationStatus.Active),
            "altas" => query.Where(h =>
                h.Status != HospitalizationStatus.Active
                && h.DischargedAt >= from
                && h.DischargedAt <= to),
            "obitos" => query.Where(h => h.Patient.IsDeceased),
            "transferencias" => query.Where(h => h.Status == HospitalizationStatus.Transferred),
            "sus-aih" => query.Where(h =>
                h.Bed.Ward.CoverageModality == WardCoverageModality.Sus
                || h.AihNumber != null),
            "solicitacoes" => query.Where(_ => false),
            _ => query.Where(h =>
                (h.Status == HospitalizationStatus.Active && h.AdmittedAt <= to)
                || (h.Status != HospitalizationStatus.Active
                    && h.DischargedAt >= from
                    && h.DischargedAt <= to)),
        };

        if (filter.PatientId.HasValue)
        {
            query = query.Where(h => h.PatientId == filter.PatientId.Value);
        }

        if (filter.WardId.HasValue)
        {
            query = query.Where(h => h.Bed.WardId == filter.WardId.Value);
        }

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(h => h.ProfessionalId == filter.ProfessionalId.Value);
        }

        if (filter.Modality.HasValue)
        {
            query = query.Where(h =>
                h.Bed.Ward.CoverageModality == filter.Modality.Value
                || h.Bed.Ward.CoverageModality == WardCoverageModality.Mixed);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(h => h.Bed.Ward.Category == filter.Category.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(h => h.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(h =>
                h.Patient.FullName.ToLower().Contains(term)
                || h.Bed.Ward.Name.ToLower().Contains(term)
                || h.Bed.BedNumber.ToLower().Contains(term)
                || h.Professional.FullName.ToLower().Contains(term)
                || h.Reason.ToLower().Contains(term)
                || (h.Diagnosis != null && h.Diagnosis.ToLower().Contains(term)));
        }

        var rows = await query
            .OrderByDescending(h => h.AdmittedAt)
            .Select(h => new
            {
                h.Id,
                h.PatientId,
                PatientName = h.Patient.FullName,
                WardName = h.Bed.Ward.Name,
                h.Bed.BedNumber,
                ProfessionalName = h.Professional.FullName,
                h.AdmittedAt,
                h.DischargedAt,
                h.Status,
                h.Diagnosis,
                Modality = h.Bed.Ward.CoverageModality,
                HasSusAih = h.AihNumber != null,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return rows.Select(h =>
        {
            var days = h.Status == HospitalizationStatus.Active
                ? (int?)Math.Max(0, (now - h.AdmittedAt).Days)
                : h.DischargedAt.HasValue
                    ? (int?)Math.Max(0, (h.DischargedAt.Value - h.AdmittedAt).Days)
                    : null;

            return new HospitalizationHubListItemDto(
                h.Id,
                "hospitalization",
                h.PatientId,
                h.PatientName,
                h.WardName,
                h.BedNumber,
                h.ProfessionalName,
                h.AdmittedAt,
                (int)h.Status,
                HospitalizationStatusLabel(h.Status),
                ModalityLabel(h.Modality),
                h.Diagnosis,
                h.HasSusAih,
                days);
        }).ToList();
    }

    private async Task<List<HospitalizationHubListItemDto>> LoadRequestsAsync(
        HospitalizationHubFilterDto filter,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.HospitalizationRequests
            .AsNoTracking()
            .Where(r => r.IsActive && r.RequestedAt >= from && r.RequestedAt <= to);

        if (filter.GroupId == "admissao")
        {
            query = query.Where(r =>
                r.Status == HospitalizationRequestStatus.Pending
                || r.Status == HospitalizationRequestStatus.Approved);
        }
        else if (filter.GroupId == "solicitacoes")
        {
            query = query.Where(r =>
                r.Status == HospitalizationRequestStatus.Pending
                || r.Status == HospitalizationRequestStatus.Approved);
        }

        if (filter.PatientId.HasValue)
        {
            query = query.Where(r => r.PatientId == filter.PatientId.Value);
        }

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(r => r.RequestingProfessionalId == filter.ProfessionalId.Value);
        }

        if (filter.WardId.HasValue)
        {
            query = query.Where(r => r.PreferredWardId == filter.WardId.Value);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(r => r.PreferredWardCategory == filter.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(r =>
                r.Patient.FullName.ToLower().Contains(term)
                || r.RequestingProfessional.FullName.ToLower().Contains(term)
                || r.Reason.ToLower().Contains(term)
                || (r.Diagnosis != null && r.Diagnosis.ToLower().Contains(term)));
        }

        var rows = await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new
            {
                r.Id,
                r.PatientId,
                PatientName = r.Patient.FullName,
                PreferredWardName = r.PreferredWard != null ? r.PreferredWard.Name : null,
                ProfessionalName = r.RequestingProfessional.FullName,
                r.RequestedAt,
                r.Status,
                r.Diagnosis,
                r.PreferredWardCategory,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new HospitalizationHubListItemDto(
            r.Id,
            "request",
            r.PatientId,
            r.PatientName,
            r.PreferredWardName,
            null,
            r.ProfessionalName,
            r.RequestedAt,
            (int)r.Status,
            RequestStatusLabel(r.Status),
            r.PreferredWardCategory.HasValue ? CategoryLabel(r.PreferredWardCategory.Value) : null,
            r.Diagnosis,
            false,
            null)).ToList();
    }

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? dateFrom, DateTime? dateTo)
    {
        var to = dateTo?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;
        var from = dateFrom?.Date ?? to.Date.AddDays(-30);
        if (from > to)
        {
            (from, to) = (to.Date, from.Date.AddDays(1).AddTicks(-1));
        }

        return (from, to);
    }

    private static IReadOnlyList<HospitalizationHubSliceDto> BuildSlices<T>(
        IReadOnlyList<T> items,
        Func<T, string> labelSelector,
        int max = 8)
    {
        return items
            .GroupBy(labelSelector)
            .Select(g => new HospitalizationHubSliceDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .Take(max)
            .ToList();
    }

    private static string ModalityLabel(WardCoverageModality modality) => modality switch
    {
        WardCoverageModality.Particular => "Particular",
        WardCoverageModality.Convenio => "Convênio",
        WardCoverageModality.Sus => "SUS",
        WardCoverageModality.Mixed => "Mista",
        _ => "—",
    };

    private static string CategoryLabel(WardCategory category) => category switch
    {
        WardCategory.Enfermaria => "Enfermaria",
        WardCategory.Apartamento => "Apartamento",
        WardCategory.Uti => "UTI",
        WardCategory.Pediatrica => "Pediatria",
        WardCategory.Maternidade => "Maternidade",
        _ => "—",
    };

    private static string HospitalizationStatusLabel(HospitalizationStatus status) => status switch
    {
        HospitalizationStatus.Active => "Ativa",
        HospitalizationStatus.Discharged => "Alta",
        HospitalizationStatus.Transferred => "Transferida",
        _ => "—",
    };

    private static string RequestStatusLabel(HospitalizationRequestStatus status) => status switch
    {
        HospitalizationRequestStatus.Pending => "Pendente",
        HospitalizationRequestStatus.Approved => "Aprovada",
        HospitalizationRequestStatus.Rejected => "Rejeitada",
        HospitalizationRequestStatus.Admitted => "Admitida",
        HospitalizationRequestStatus.Cancelled => "Cancelada",
        _ => "—",
    };
}
