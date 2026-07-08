using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Tiss;

namespace SistemaHospitalar.Infrastructure.Services;

public class TissBillingService(AppDbContext dbContext, GuideAuditLogger auditLogger) : ITissBillingService
{
    public async Task<IReadOnlyList<TissGuideDto>> GetGuidesAsync(
        TissGuideStatus? status,
        Guid? patientId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TissGuides.AsNoTracking().Where(g => g.IsActive);

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(g => g.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(g =>
                g.GuideNumber.Contains(term)
                || g.Patient.FullName.Contains(term)
                || g.HealthInsurance.Name.Contains(term));
        }

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Select(MapGuide())
            .ToListAsync(cancellationToken);
    }

    public Task<TissGuideDto?> GetGuideByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public async Task<TissGuideDto> CreateGuideAsync(
        CreateTissGuideRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ClientRequestId))
        {
            var existing = await dbContext.TissGuides
                .AsNoTracking()
                .Where(g => g.ClientRequestId == request.ClientRequestId)
                .Select(MapGuide())
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        ValidateItems(
            request.Items.Select(i => (i.TussCode, i.Description, i.Quantity, i.UnitPrice)).ToList(),
            request.GuideType);

        var count = await dbContext.TissGuides.CountAsync(cancellationToken);
        var guideNumber = $"TISS-{DateTime.UtcNow:yyyy}-{(count + 1):D6}";

        var guide = new TissGuide
        {
            GuideNumber = guideNumber,
            PatientId = request.PatientId,
            HealthInsuranceId = request.HealthInsuranceId,
            AppointmentId = request.AppointmentId,
            HospitalizationId = request.HospitalizationId,
            GuideType = request.GuideType,
            ServiceUnitId = request.ServiceUnitId,
            Notes = request.Notes?.Trim(),
            ClientRequestId = request.ClientRequestId?.Trim()
        };

        await ApplyBeneficiarySnapshotAsync(guide, cancellationToken);
        await ApplyClinicalContextAsync(guide, cancellationToken);
        TissGuideClinicalMapper.Apply(guide, request.Clinical);

        foreach (var item in request.Items)
        {
            guide.Items.Add(MapNewItem(item));
        }

        guide.TotalAmount = CalculateTotal(guide.Items);
        dbContext.TissGuides.Add(guide);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogTissGuideAsync(
            guide.Id,
            "Emissão",
            $"Guia {guide.GuideNumber} criada.",
            cancellationToken: cancellationToken);

        return (await GetByIdAsync(guide.Id, cancellationToken))!;
    }

    public async Task<TissGuideDto?> UpdateGuideAsync(
        Guid id, UpdateTissGuideRequest request, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);

        if (guide is null || guide.Status != TissGuideStatus.Draft)
        {
            return null;
        }

        ValidateItems(
            request.Items.Select(i => (i.TussCode, i.Description, i.Quantity, i.UnitPrice)).ToList(),
            request.GuideType);

        guide.HealthInsuranceId = request.HealthInsuranceId;
        guide.AppointmentId = request.AppointmentId;
        guide.HospitalizationId = request.HospitalizationId;
        guide.GuideType = request.GuideType;
        guide.ServiceUnitId = request.ServiceUnitId;
        guide.Notes = request.Notes?.Trim();
        guide.UpdatedAt = DateTime.UtcNow;
        TissGuideClinicalMapper.Apply(guide, request.Clinical);

        SyncItems(guide, request.Items);
        guide.AccountClosedAt = null;
        guide.TotalAmount = CalculateTotal(guide.Items.Where(i => i.IsActive));

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogTissGuideAsync(
            id, "Atualização", $"Guia {guide.GuideNumber} atualizada.", cancellationToken: cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteGuideAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides
            .Include(g => g.Items)
            .Include(g => g.Glosas)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);

        if (guide is null || guide.Status != TissGuideStatus.Draft)
        {
            return false;
        }

        guide.IsActive = false;
        guide.UpdatedAt = DateTime.UtcNow;
        foreach (var item in guide.Items)
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var glosa in guide.Glosas)
        {
            glosa.IsActive = false;
            glosa.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TissGuideDto?> CloseGuideAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);

        if (guide is null || guide.Status != TissGuideStatus.Draft)
        {
            return null;
        }

        var activeItems = guide.Items.Where(i => i.IsActive).ToList();
        if (activeItems.Count == 0)
        {
            throw new InvalidOperationException("Guia sem itens não pode ter conta fechada.");
        }

        foreach (var item in activeItems)
        {
            item.IsAudited = true;
            item.UpdatedAt = DateTime.UtcNow;
        }

        guide.AccountClosedAt = DateTime.UtcNow;
        guide.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogTissGuideAsync(
            id, "Fechamento de conta", "Conta da guia fechada para faturamento.", cancellationToken: cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<TissGuideDto?> SendGuideAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status != TissGuideStatus.Draft)
        {
            return null;
        }

        var activeItems = guide.Items.Where(i => i.IsActive).ToList();
        HospitalBusinessRules.ValidateTissGuideReadyForBilling(
            guide.AccountClosedAt,
            activeItems.Select(i => i.IsAudited),
            activeItems.Count);

        var beforeStatus = guide.Status;
        guide.Status = TissGuideStatus.Sent;
        guide.SentAt = DateTime.UtcNow;
        guide.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogTissGuideAsync(
            id, "Envio", "Guia enviada para operadora.", beforeStatus, TissGuideStatus.Sent, cancellationToken);
        await TissFinancialReconciliation.EnsureReceivableForGuideAsync(dbContext, guide, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<TissGuideDto?> CancelGuideAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status is TissGuideStatus.Paid or TissGuideStatus.Cancelled)
        {
            return null;
        }

        var beforeStatus = guide.Status;
        guide.Status = TissGuideStatus.Cancelled;
        guide.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogTissGuideAsync(
            id, "Cancelamento", "Guia cancelada.", beforeStatus, TissGuideStatus.Cancelled, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<TissGuideDto?> MarkGuidePaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status is not (TissGuideStatus.Sent or TissGuideStatus.Glosa))
        {
            return null;
        }

        var beforeStatus = guide.Status;
        guide.Status = TissGuideStatus.Paid;
        guide.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogTissGuideAsync(
            id, "Faturamento", "Guia marcada como paga.", beforeStatus, TissGuideStatus.Paid, cancellationToken);
        await TissFinancialReconciliation.MarkGuidePaidFinancialAsync(dbContext, guide, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<TissGlosaDto?> RegisterGlosaAsync(
        Guid guideId, RegisterGlosaRequest request, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.TissGuides.FirstOrDefaultAsync(g => g.Id == guideId && g.IsActive, cancellationToken);
        if (guide is null || guide.Status is TissGuideStatus.Draft or TissGuideStatus.Cancelled or TissGuideStatus.Paid)
        {
            return null;
        }

        if (request.GlosaAmount <= 0 || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Informe motivo e valor da glosa.");
        }

        var glosa = new TissGlosa
        {
            TissGuideId = guideId,
            TissGuideItemId = request.TissGuideItemId,
            Reason = request.Reason.Trim(),
            AnsGlosaCode = request.AnsGlosaCode?.Trim(),
            GlosaAmount = request.GlosaAmount
        };

        var beforeStatus = guide.Status;
        guide.Status = TissGuideStatus.Glosa;
        guide.UpdatedAt = DateTime.UtcNow;

        dbContext.TissGlosas.Add(glosa);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.LogTissGuideAsync(
            guideId, "Glosa", $"Glosa registrada: {request.Reason.Trim()}", beforeStatus, TissGuideStatus.Glosa, cancellationToken);

        return await MapGlosaAsync(glosa.Id, cancellationToken);
    }

    public async Task<TissGlosaDto?> UpdateGlosaAsync(
        Guid glosaId, UpdateGlosaRequest request, CancellationToken cancellationToken = default)
    {
        var glosa = await dbContext.TissGlosas
            .Include(g => g.TissGuide)
            .FirstOrDefaultAsync(g => g.Id == glosaId && g.IsActive, cancellationToken);

        if (glosa is null || glosa.IsResolved)
        {
            return null;
        }

        if (request.GlosaAmount <= 0 || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Informe motivo e valor da glosa.");
        }

        glosa.TissGuideItemId = request.TissGuideItemId;
        glosa.Reason = request.Reason.Trim();
        glosa.AnsGlosaCode = request.AnsGlosaCode?.Trim();
        glosa.GlosaAmount = request.GlosaAmount;
        glosa.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapGlosaAsync(glosaId, cancellationToken);
    }

    public async Task<bool> DeleteGlosaAsync(Guid glosaId, CancellationToken cancellationToken = default)
    {
        var glosa = await dbContext.TissGlosas
            .Include(g => g.TissGuide)
            .FirstOrDefaultAsync(g => g.Id == glosaId && g.IsActive, cancellationToken);

        if (glosa is null)
        {
            return false;
        }

        glosa.IsActive = false;
        glosa.UpdatedAt = DateTime.UtcNow;

        var guide = glosa.TissGuide;
        var hasActiveGlosas = await dbContext.TissGlosas
            .AnyAsync(g => g.TissGuideId == guide.Id && g.IsActive && g.Id != glosaId, cancellationToken);

        if (!hasActiveGlosas && guide.Status == TissGuideStatus.Glosa)
        {
            guide.Status = TissGuideStatus.Sent;
            guide.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TissGlosaDto?> ResolveGlosaAsync(Guid glosaId, CancellationToken cancellationToken = default)
    {
        var glosa = await dbContext.TissGlosas
            .Include(g => g.TissGuide)
            .FirstOrDefaultAsync(g => g.Id == glosaId && g.IsActive, cancellationToken);

        if (glosa is null)
        {
            return null;
        }

        glosa.IsResolved = true;
        glosa.UpdatedAt = DateTime.UtcNow;

        var guide = glosa.TissGuide;
        var unresolved = await dbContext.TissGlosas
            .AnyAsync(g => g.TissGuideId == guide.Id && g.IsActive && !g.IsResolved, cancellationToken);

        if (!unresolved && guide.Status == TissGuideStatus.Glosa)
        {
            guide.Status = TissGuideStatus.Sent;
            guide.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapGlosaAsync(glosaId, cancellationToken);
    }

    public async Task<TissGlosaDto?> ContestGlosaAsync(
        Guid glosaId,
        ContestGlosaRequest request,
        CancellationToken cancellationToken = default)
    {
        var glosa = await dbContext.TissGlosas
            .FirstOrDefaultAsync(g => g.Id == glosaId && g.IsActive, cancellationToken);

        if (glosa is null || glosa.IsResolved)
            return null;

        if (string.IsNullOrWhiteSpace(request.ContestationNotes))
            throw new InvalidOperationException("Informe a justificativa do recurso de glosa.");

        glosa.ContestationStatus = GlosaContestationStatus.Submitted;
        glosa.ContestationNotes = request.ContestationNotes.Trim();
        if (!string.IsNullOrWhiteSpace(request.AnsGlosaCode))
            glosa.AnsGlosaCode = request.AnsGlosaCode.Trim();
        glosa.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapGlosaAsync(glosaId, cancellationToken);
    }

    private async Task ApplyBeneficiarySnapshotAsync(TissGuide guide, CancellationToken cancellationToken)
    {
        var insurance = await dbContext.PatientInsurances.AsNoTracking()
            .Where(pi => pi.PatientId == guide.PatientId
                && pi.HealthInsuranceId == guide.HealthInsuranceId
                && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (insurance is null)
            return;

        guide.BeneficiaryCardNumber = insurance.CardNumber;
        guide.BeneficiaryPlanName = insurance.PlanName;
        guide.BeneficiaryCns = insurance.CnsNumber;
        guide.BeneficiaryAccommodation = insurance.AccommodationType;

        var auth = await dbContext.InsuranceAuthorizations.AsNoTracking()
            .Where(a => a.PatientId == guide.PatientId
                && a.HealthInsuranceId == guide.HealthInsuranceId
                && a.IsActive
                && a.Status == InsuranceAuthorizationStatus.Approved
                && (!a.ValidUntil.HasValue || a.ValidUntil.Value >= DateTime.UtcNow))
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (auth is not null)
            guide.AuthorizationPassword = auth.AuthorizationNumber;
    }

    private async Task ApplyClinicalContextAsync(TissGuide guide, CancellationToken cancellationToken)
    {
        if (guide.AppointmentId.HasValue)
        {
            var appt = await dbContext.Appointments.AsNoTracking()
                .Include(a => a.Professional).ThenInclude(p => p.Specialty)
                .FirstOrDefaultAsync(a => a.Id == guide.AppointmentId.Value, cancellationToken);

            if (appt is not null)
            {
                guide.RequestingProfessionalId ??= appt.ProfessionalId;
                guide.RequestingProfessionalName ??= appt.Professional.FullName;
                guide.RequestingProfessionalCrm ??= appt.Professional.Crm;
                guide.ExecutingProfessionalId ??= appt.ProfessionalId;
                guide.ExecutingProfessionalName ??= appt.Professional.FullName;
                guide.ExecutingProfessionalCrm ??= appt.Professional.Crm;
                guide.ServiceCharacter ??= TissServiceCharacter.Elective;
            }
        }

        if (guide.HospitalizationId.HasValue)
        {
            var hosp = await dbContext.Hospitalizations.AsNoTracking()
                .Include(h => h.Professional)
                .Include(h => h.Bed).ThenInclude(b => b.Ward)
                .FirstOrDefaultAsync(h => h.Id == guide.HospitalizationId.Value, cancellationToken);

            if (hosp is not null)
            {
                guide.AdmissionDate ??= hosp.AdmittedAt;
                guide.DischargeDate ??= hosp.DischargedAt;
                guide.RequestedBedType ??= hosp.Bed?.Ward?.Name;
                guide.RequestingProfessionalId ??= hosp.ProfessionalId;
                guide.RequestingProfessionalName ??= hosp.Professional.FullName;
                guide.RequestingProfessionalCrm ??= hosp.Professional.Crm;
                guide.ClinicalJustification ??= hosp.Reason;
            }
        }

        var latestCid = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.IsActive
                && e.MedicalRecord.PatientId == guide.PatientId
                && e.Cid10Code != null)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Cid10Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(latestCid))
            guide.Cid10Code ??= latestCid;
    }

    private static void SyncItems(TissGuide guide, IReadOnlyList<UpdateTissGuideItemRequest> items)
    {
        var incomingIds = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();

        foreach (var existing in guide.Items.Where(i => i.IsActive))
        {
            if (!incomingIds.Contains(existing.Id))
            {
                existing.IsActive = false;
                existing.IsAudited = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        foreach (var item in items)
        {
            if (item.Id.HasValue)
            {
                var existing = guide.Items.FirstOrDefault(i => i.Id == item.Id.Value);
                if (existing is null) continue;

                var changed = existing.TussCode != item.TussCode.Trim()
                    || existing.Description != item.Description.Trim()
                    || existing.Quantity != item.Quantity
                    || existing.UnitPrice != item.UnitPrice;

                existing.TussCode = item.TussCode.Trim();
                existing.Description = item.Description.Trim();
                existing.Quantity = item.Quantity;
                existing.UnitPrice = item.UnitPrice;
                existing.PriceTableSource = item.PriceTableSource;
                existing.Cid10Code = item.Cid10Code?.Trim();
                existing.RelatedTussCode = item.RelatedTussCode?.Trim();
                existing.IsActive = true;
                if (changed) existing.IsAudited = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                guide.Items.Add(MapNewItem(new TissGuideItemRequest(
                    item.TussCode,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.PriceTableSource,
                    item.Cid10Code,
                    item.RelatedTussCode)));
            }
        }
    }

    private static TissGuideItem MapNewItem(TissGuideItemRequest item) => new()
    {
        TussCode = item.TussCode.Trim(),
        Description = item.Description.Trim(),
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        PriceTableSource = item.PriceTableSource,
        Cid10Code = item.Cid10Code?.Trim(),
        RelatedTussCode = item.RelatedTussCode?.Trim(),
    };

    private static decimal CalculateTotal(IEnumerable<TissGuideItem> items) =>
        items.Where(i => i.IsActive).Sum(i => i.Quantity * i.UnitPrice);

    private static bool AllowsEmptyItems(TissGuideType guideType) =>
        guideType is TissGuideType.ChemotherapyAnnex
            or TissGuideType.RadiotherapyAnnex
            or TissGuideType.OpmeAnnex
            or TissGuideType.DentalInitialAnnex
            or TissGuideType.ExtensionRequest
            or TissGuideType.PresenceProof
            or TissGuideType.GlosaAppeal
            or TissGuideType.PaymentStatement
            or TissGuideType.MonitoringReport
            or TissGuideType.DentalGlosaAppeal
            or TissGuideType.DentalPaymentStatement;

    private static void ValidateItems(
        IReadOnlyList<(string TussCode, string Description, int Quantity, decimal UnitPrice)> items,
        TissGuideType guideType)
    {
        if (items.Count == 0 && !AllowsEmptyItems(guideType))
            throw new InvalidOperationException("Adicione ao menos um procedimento TUSS.");

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.TussCode) || string.IsNullOrWhiteSpace(item.Description))
            {
                throw new InvalidOperationException("Preencha código TUSS e descrição em todos os itens.");
            }

            if (item.Quantity < 1)
            {
                throw new InvalidOperationException("Quantidade deve ser pelo menos 1.");
            }

            if (item.UnitPrice < 0)
            {
                throw new InvalidOperationException("Valor unitário não pode ser negativo.");
            }
        }
    }

    private async Task<TissGuideDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.TissGuides
            .AsNoTracking()
            .Where(g => g.Id == id && g.IsActive)
            .Select(MapGuide())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TissGlosaDto?> MapGlosaAsync(Guid glosaId, CancellationToken cancellationToken)
    {
        return await dbContext.TissGlosas
            .AsNoTracking()
            .Where(g => g.Id == glosaId && g.IsActive)
            .Select(g => new TissGlosaDto(
                g.Id,
                g.TissGuideItemId,
                g.Reason,
                g.AnsGlosaCode,
                g.GlosaAmount,
                g.IsResolved,
                g.ContestationStatus,
                g.ContestationNotes,
                g.TissGuideItem != null ? g.TissGuideItem.Description : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<TissGuide, TissGuideDto>> MapGuide() =>
        g => new TissGuideDto(
            g.Id,
            g.GuideNumber,
            g.PatientId,
            g.Patient.FullName,
            g.HealthInsuranceId,
            g.HealthInsurance.Name,
            g.AppointmentId,
            g.HospitalizationId,
            g.GuideType,
            g.Status,
            g.TotalAmount,
            g.SentAt,
            g.AccountClosedAt,
            g.Notes,
            g.BeneficiaryCardNumber,
            g.BeneficiaryPlanName,
            g.BeneficiaryCns,
            g.AuthorizationPassword,
            new TissGuideClinicalDto(
                g.Cid10Code,
                g.Cid10Secondary,
                g.ClinicalJustification,
                g.ServiceCharacter,
                g.AccidentIndicator,
                g.RequestingProfessionalId,
                g.RequestingProfessionalName,
                g.RequestingProfessionalCrm,
                g.ExecutingProfessionalId,
                g.ExecutingProfessionalName,
                g.ExecutingProfessionalCrm,
                g.AdmissionDate,
                g.DischargeDate,
                g.RequestedBedType,
                g.ParentGuideId,
                g.ProfessionalRole,
                g.ParticipationPercent,
                g.SurgeryId),
            g.ServiceUnitId,
            g.ServiceUnit != null ? g.ServiceUnit.Name : null,
            g.CreatedAt,
            g.Items.Where(i => i.IsActive).Select(i => new TissGuideItemDto(
                i.Id,
                i.TussCode,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.PriceTableSource,
                i.Cid10Code,
                i.RelatedTussCode,
                i.IsAudited)).ToList(),
            g.Glosas.Where(gl => gl.IsActive).Select(gl => new TissGlosaDto(
                gl.Id,
                gl.TissGuideItemId,
                gl.Reason,
                gl.AnsGlosaCode,
                gl.GlosaAmount,
                gl.IsResolved,
                gl.ContestationStatus,
                gl.ContestationNotes,
                gl.TissGuideItem != null ? gl.TissGuideItem.Description : null)).ToList());
}
