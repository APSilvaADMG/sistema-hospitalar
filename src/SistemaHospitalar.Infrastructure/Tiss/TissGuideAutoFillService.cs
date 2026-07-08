using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissGuideAutoFillService
{
    public static TissGuideType? SuggestGuideType(string source, TussTableType? tableType)
    {
        if (source is "CBHPM" or "Laboratório" or "Imagem")
            return TissGuideType.SpSadt;
        if (source is "Brasíndice")
            return TissGuideType.OtherExpenses;
        if (source is "SIMPRO")
            return TissGuideType.OtherExpenses;
        if (tableType is TussTableType.Daily)
            return TissGuideType.DischargeSummary;
        if (tableType is TussTableType.Fee)
            return TissGuideType.IndividualFees;
        return TissGuideType.Consultation;
    }

    public static async Task<List<TissGuideItemRequest>> BuildItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = new List<TissGuideItemRequest>();
        var guideType = request.GuideType ?? TissGuideType.Consultation;

        switch (guideType)
        {
            case TissGuideType.Consultation:
                await AddConsultationItemsAsync(db, request, items, cancellationToken);
                break;
            case TissGuideType.SpSadt:
                await AddSpSadtItemsAsync(db, request, items, cancellationToken);
                break;
            case TissGuideType.HospitalizationRequest:
                await AddHospitalizationRequestItemsAsync(db, request, items, cancellationToken);
                break;
            case TissGuideType.DischargeSummary:
            case TissGuideType.Hospitalization:
                await AddDischargeItemsAsync(db, request, items, cancellationToken);
                break;
            case TissGuideType.IndividualFees:
                await AddIndividualFeesItemsAsync(db, request, items, cancellationToken);
                break;
            case TissGuideType.OtherExpenses:
                await AddOtherExpensesItemsAsync(db, request, items, cancellationToken);
                break;
            case TissGuideType.DentalTreatment:
                items.Add(await ResolveTussItemAsync(db, "81000030", "Consulta odontológica inicial", 1, 120m, cancellationToken));
                break;
            default:
                await AddConsultationItemsAsync(db, request, items, cancellationToken);
                await AddSpSadtItemsAsync(db, request, items, cancellationToken);
                break;
        }

        return items;
    }

    private static async Task AddConsultationItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        List<TissGuideItemRequest> items,
        CancellationToken ct)
    {
        if (!request.AppointmentId.HasValue)
            return;

        var appt = await db.Appointments.AsNoTracking()
            .Include(a => a.Professional).ThenInclude(p => p.Specialty)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value && a.IsActive, ct);

        if (appt is null)
            return;

        var desc = $"Consulta — {appt.Professional.Specialty.Name}";
        items.Add(await ResolveTussItemAsync(db, "10101012", desc, 1, 250m, ct));
    }

    private static async Task AddSpSadtItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        List<TissGuideItemRequest> items,
        CancellationToken ct)
    {
        var pendingLabs = await db.LabOrderItems.AsNoTracking()
            .Where(i => i.IsActive
                && i.LabOrder.IsActive
                && i.LabOrder.PatientId == request.PatientId
                && i.LabOrder.Status != LabOrderStatus.Completed
                && i.LabOrder.Status != LabOrderStatus.Cancelled
                && i.LabExamCatalog.TussCode != null)
            .Take(10)
            .Select(i => new { i.LabExamCatalog.TussCode, i.LabExamCatalog.Name })
            .ToListAsync(ct);

        foreach (var lab in pendingLabs)
        {
            items.Add(await ResolveTussItemAsync(db, lab.TussCode!, lab.Name, 1, 45m, ct));
        }

        var imaging = await db.ImagingStudies.AsNoTracking()
            .Where(s => s.IsActive
                && s.PatientId == request.PatientId
                && s.Status != ImagingStudyStatus.Completed
                && s.Status != ImagingStudyStatus.Cancelled)
            .Take(10)
            .ToListAsync(ct);

        foreach (var study in imaging)
        {
            var catalog = await db.ImagingProcedureCatalogs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == study.StudyDescription && c.IsActive, ct);

            var code = catalog?.TussCode ?? "40801144";
            items.Add(await ResolveTussItemAsync(db, code, study.StudyDescription, 1, 120m, ct));
        }

        var physio = await db.PhysiotherapySessions.AsNoTracking()
            .Where(s => s.IsActive && s.PatientId == request.PatientId)
            .Take(5)
            .ToListAsync(ct);

        foreach (var _ in physio)
        {
            items.Add(await ResolveTussItemAsync(db, "20101015", "Sessão fisioterapia", 1, 95m, ct));
        }
    }

    private static async Task AddHospitalizationRequestItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        List<TissGuideItemRequest> items,
        CancellationToken ct)
    {
        if (!request.HospitalizationId.HasValue)
            return;

        var hosp = await db.Hospitalizations.AsNoTracking()
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .FirstOrDefaultAsync(h => h.Id == request.HospitalizationId.Value, ct);

        if (hosp is null)
            return;

        items.Add(await ResolveTussItemAsync(
            db,
            "60000732",
            $"Internação — {hosp.Bed?.Ward?.Name ?? "Enfermaria"}",
            1,
            450m,
            ct));
    }

    private static async Task AddDischargeItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        List<TissGuideItemRequest> items,
        CancellationToken ct)
    {
        if (!request.HospitalizationId.HasValue)
            return;

        var hosp = await db.Hospitalizations.AsNoTracking()
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .FirstOrDefaultAsync(h => h.Id == request.HospitalizationId.Value, ct);

        if (hosp is null)
            return;

        var days = Math.Max(1, (int)Math.Ceiling(((hosp.DischargedAt ?? DateTime.UtcNow) - hosp.AdmittedAt).TotalDays));
        var dailyCode = hosp.Bed?.Ward?.Name?.Contains("UTI", StringComparison.OrdinalIgnoreCase) == true
            ? "60000651"
            : "60000732";
        var dailyPrice = dailyCode == "60000651" ? 800m : 450m;

        items.Add(await ResolveTussItemAsync(
            db,
            dailyCode,
            $"Diária — {hosp.Bed?.Ward?.Name ?? "Internação"}",
            days,
            dailyPrice,
            ct));

        var dispensings = await db.PharmacyDispensings.AsNoTracking()
            .Include(d => d.Product)
            .Where(d => d.IsActive && d.PatientId == request.PatientId)
            .OrderByDescending(d => d.DispensedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var disp in dispensings)
        {
            var bras = await db.BrasindiceItems.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Description.Contains(disp.Product.Name) && b.IsActive, ct);

            if (bras is not null)
            {
                items.Add(new TissGuideItemRequest(
                    bras.Code,
                    bras.Description,
                    Math.Max(1, (int)Math.Ceiling(disp.Quantity)),
                    bras.ReferencePrice ?? 0m,
                    TissPriceTableSource.Brasindice));
            }
        }
    }

    private static async Task AddIndividualFeesItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        List<TissGuideItemRequest> items,
        CancellationToken ct)
    {
        if (!request.SurgeryId.HasValue)
            return;

        var surgery = await db.Surgeries.AsNoTracking()
            .Include(s => s.Surgeon)
            .FirstOrDefaultAsync(s => s.Id == request.SurgeryId.Value && s.IsActive, ct);

        if (surgery is null)
            return;

        var cbhpm = await db.CbhpmProcedures.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.ReferencePrice)
            .FirstOrDefaultAsync(ct);

        var code = cbhpm?.Code ?? "31001017";
        var price = cbhpm?.ReferencePrice ?? 1850m;

        items.Add(new TissGuideItemRequest(
            code,
            $"Honorário cirúrgico — {surgery.ProcedureName}",
            1,
            price,
            TissPriceTableSource.Cbhpm,
            RelatedTussCode: code));
    }

    private static async Task AddOtherExpensesItemsAsync(
        AppDbContext db,
        SuggestedGuideItemsRequest request,
        List<TissGuideItemRequest> items,
        CancellationToken ct)
    {
        var materials = await db.SimproItems.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .Take(3)
            .ToListAsync(ct);

        foreach (var material in materials)
        {
            items.Add(new TissGuideItemRequest(
                material.Code,
                material.Description,
                1,
                material.ReferencePrice ?? 0m,
                TissPriceTableSource.Simpro));
        }
    }

    private static async Task<TissGuideItemRequest> ResolveTussItemAsync(
        AppDbContext db,
        string code,
        string fallbackDescription,
        int quantity,
        decimal fallbackPrice,
        CancellationToken ct)
    {
        var tuss = await db.TussCatalogs.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive, ct);

        return new TissGuideItemRequest(
            code,
            tuss?.Description ?? fallbackDescription,
            quantity,
            tuss?.ReferencePrice ?? fallbackPrice,
            TissPriceTableSource.Tuss);
    }
}
