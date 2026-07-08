using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HospitalizationService(
    AppDbContext dbContext,
    IHotelariaHospitalarService hotelariaHospitalarService,
    IHospitalEventEngine eventEngine,
    ClinicalStatusAuditLogger clinicalStatusAuditLogger) : IHospitalizationService
{
    public async Task<IReadOnlyList<WardDto>> GetWardsAsync(
        WardCoverageModality? modality = null,
        WardCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureBedStatusConsistencyAsync(cancellationToken);

        var query = dbContext.Wards.AsNoTracking().Where(w => w.IsActive);

        if (modality.HasValue)
        {
            query = query.Where(w => w.CoverageModality == modality.Value || w.CoverageModality == WardCoverageModality.Mixed);
        }

        if (category.HasValue)
        {
            query = query.Where(w => w.Category == category.Value);
        }

        return await query
            .OrderBy(w => w.CoverageModality)
            .ThenBy(w => w.Category)
            .ThenBy(w => w.Name)
            .Select(w => new WardDto(
                w.Id,
                w.Name,
                w.Code,
                w.Floor,
                w.Description,
                w.CoverageModality,
                w.Category,
                w.Beds.Count(b => b.IsActive),
                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Maintenance)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BedDto>> GetBedsAsync(
        Guid? wardId = null,
        WardCoverageModality? modality = null,
        WardCategory? category = null,
        BedStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureBedStatusConsistencyAsync(cancellationToken);

        var query = dbContext.Beds.AsNoTracking().Where(b => b.IsActive);

        if (wardId.HasValue)
        {
            query = query.Where(b => b.WardId == wardId.Value);
        }

        if (modality.HasValue)
        {
            query = query.Where(b =>
                b.Ward.CoverageModality == modality.Value
                || b.Ward.CoverageModality == WardCoverageModality.Mixed);
        }

        if (category.HasValue)
        {
            query = query.Where(b => b.Ward.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);

            if (status.Value == BedStatus.Available)
            {
                query = query.Where(b => !dbContext.Hospitalizations.Any(h =>
                    h.BedId == b.Id
                    && h.Status == HospitalizationStatus.Active
                    && h.IsActive));
            }
        }

        return await ProjectBedsAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<BedDto>> GetAvailableBedsForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        await EnsureBedStatusConsistencyAsync(cancellationToken);

        var patient = await dbContext.Patients
            .Include(p => p.Insurances.Where(i => i.IsActive))
                .ThenInclude(i => i.HealthInsurance)
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        var primaryInsurance = patient.Insurances
            .OrderByDescending(i => i.IsPrimary)
            .FirstOrDefault();

        var patientModality = WardInsuranceMatcher.ResolvePatientModality(
            primaryInsurance?.HealthInsurance.Name);

        var query = dbContext.Beds.AsNoTracking()
            .Where(b => b.IsActive
                && !dbContext.Hospitalizations.Any(h =>
                    h.BedId == b.Id
                    && h.Status == HospitalizationStatus.Active
                    && h.IsActive)
                && (b.Status == BedStatus.Available
                    || (b.Status == BedStatus.Reserved
                        && dbContext.BedEvents.Any(e =>
                            e.BedId == b.Id
                            && e.EventType == BedEventType.Reserve
                            && e.EndAt == null
                            && e.IsActive
                            && e.PatientId == patientId)))
                && (b.Ward.CoverageModality == patientModality
                    || b.Ward.CoverageModality == WardCoverageModality.Mixed));

        return await ProjectBedsAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<HospitalizationDto>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await GetListAsync(HospitalizationListScope.Active, cancellationToken);

    public async Task<IReadOnlyList<HospitalizationDto>> GetListAsync(
        HospitalizationListScope scope = HospitalizationListScope.Active,
        CancellationToken cancellationToken = default)
    {
        if (scope == HospitalizationListScope.Active)
        {
            await EnsureBedStatusConsistencyAsync(cancellationToken);
        }

        var query = dbContext.Hospitalizations.AsNoTracking().Where(h => h.IsActive);

        query = scope switch
        {
            HospitalizationListScope.Active => query.Where(h => h.Status == HospitalizationStatus.Active),
            HospitalizationListScope.Discharged => query.Where(h => h.Status != HospitalizationStatus.Active),
            HospitalizationListScope.Deceased => query.Where(h => h.Patient.IsDeceased),
            HospitalizationListScope.All => query,
            _ => query.Where(h => h.Status == HospitalizationStatus.Active),
        };

        return await query
            .OrderByDescending(h => h.AdmittedAt)
            .Select(MapHospitalization())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HospitalizationDto>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Hospitalizations
            .AsNoTracking()
            .Where(h => h.PatientId == patientId && h.IsActive)
            .OrderByDescending(h => h.AdmittedAt)
            .Select(MapHospitalization())
            .ToListAsync(cancellationToken);
    }

    public async Task<HospitalizationDto> AdmitAsync(
        AdmitPatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await PatientCareValidation.RequireEligibleForCareAsync(
            dbContext, request.PatientId, encryption: null, validateSusCns: true, cancellationToken);

        var alreadyAdmitted = await dbContext.Hospitalizations.AnyAsync(
            h => h.PatientId == request.PatientId && h.Status == HospitalizationStatus.Active,
            cancellationToken);

        if (alreadyAdmitted)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.OneBedPerPatient}] Paciente já possui internação ativa.");
        }

        var bed = await dbContext.Beds
            .Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.Id == request.BedId && b.IsActive, cancellationToken);

        if (bed is null)
        {
            throw new InvalidOperationException("Leito não encontrado.");
        }

        HospitalBusinessRules.ValidateBedAvailableForAdmission(bed.Status);

        if (bed.Status == BedStatus.Reserved)
        {
            var reservedForPatient = await dbContext.BedEvents.AnyAsync(
                e => e.BedId == bed.Id
                    && e.EventType == BedEventType.Reserve
                    && e.EndAt == null
                    && e.IsActive
                    && e.PatientId == request.PatientId,
                cancellationToken);

            if (!reservedForPatient)
            {
                throw new InvalidOperationException("Leito reservado para outro paciente.");
            }
        }

        var bedAlreadyOccupied = await dbContext.Hospitalizations.AnyAsync(
            h => h.BedId == request.BedId
                && h.Status == HospitalizationStatus.Active
                && h.IsActive,
            cancellationToken);

        if (bedAlreadyOccupied)
        {
            throw new InvalidOperationException("Leito já possui internação ativa.");
        }

        var primaryInsurance = patient.Insurances
            .OrderByDescending(i => i.IsPrimary)
            .FirstOrDefault();

        var patientModality = WardInsuranceMatcher.ResolvePatientModality(
            primaryInsurance?.HealthInsurance.Name);

        if (!WardInsuranceMatcher.IsCompatible(bed.Ward.CoverageModality, patientModality))
        {
            throw new InvalidOperationException(
                $"O leito pertence à ala {WardInsuranceMatcher.ModalityLabel(bed.Ward.CoverageModality)} " +
                $"({bed.Ward.Name}), incompatível com a cobertura do paciente " +
                $"({WardInsuranceMatcher.ModalityLabel(patientModality)}).");
        }

        var reason = request.Reason.Trim();
        var diagnosis = request.Diagnosis?.Trim();
        var notes = request.Notes?.Trim();
        Guid? aiTriageLogId = request.AiTriageLogId;
        AiTriageLog? triageLog = null;

        if (aiTriageLogId.HasValue || string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(diagnosis))
        {
            triageLog = await FindTriageForAdmissionAsync(
                request.PatientId, request.AiTriageLogId, cancellationToken);

            if (triageLog is not null)
            {
                var fields = TriageAdmissionHelper.BuildAdmissionFields(triageLog);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = fields.Reason;
                }

                if (string.IsNullOrWhiteSpace(diagnosis))
                {
                    diagnosis = fields.Diagnosis;
                }

                aiTriageLogId = triageLog.Id;
                notes = TriageAdmissionHelper.AppendTriageNotes(notes, triageLog);
            }
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Informe o motivo da internação.");
        }

        HospitalizationRequest? linkedRequest = null;
        if (request.HospitalizationRequestId.HasValue)
        {
            linkedRequest = await dbContext.HospitalizationRequests
                .FirstOrDefaultAsync(
                    r => r.Id == request.HospitalizationRequestId.Value && r.IsActive,
                    cancellationToken);
        }

        var hospitalization = new Hospitalization
        {
            PatientId = request.PatientId,
            BedId = request.BedId,
            ProfessionalId = request.ProfessionalId,
            Reason = reason,
            Diagnosis = diagnosis,
            Notes = notes,
            AiTriageLogId = aiTriageLogId
        };

        ApplySusFields(
            hospitalization,
            request.SusData,
            linkedRequest,
            bed.Ward.CoverageModality,
            patientModality);

        bed.Status = BedStatus.Occupied;
        bed.UpdatedAt = DateTime.UtcNow;

        dbContext.Hospitalizations.Add(hospitalization);
        await dbContext.SaveChangesAsync(cancellationToken);

        await CloseActiveBedEventsAsync(bed.Id, BedEventType.Reserve, cancellationToken);
        await CloseActiveBedEventsAsync(bed.Id, BedEventType.Block, cancellationToken);
        await CreateBedEventAsync(
            bed.Id,
            BedEventType.Occupy,
            request.PatientId,
            hospitalization.Id,
            reason,
            cancellationToken);

        if (RequiresSusDocumentation(bed.Ward.CoverageModality, patientModality))
        {
            EnsureAihNumber(hospitalization);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (request.HospitalizationRequestId.HasValue)
        {
            await CompleteHospitalizationRequestAsync(
                request.HospitalizationRequestId.Value,
                hospitalization.Id,
                cancellationToken);
        }

        await RegisterSnippetUsageAsync(HospitalizationSnippetType.Reason, reason, null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(diagnosis))
        {
            await RegisterSnippetUsageAsync(HospitalizationSnippetType.Diagnosis, diagnosis, null, cancellationToken);
        }

        var modalityLabel = WardInsuranceMatcher.ModalityLabel(bed.Ward.CoverageModality);

        await AddRecordEntryAsync(
            request.PatientId,
            hospitalization.Id,
            request.ProfessionalId,
            MedicalRecordEntryType.Anamnesis,
            $"INTERNAÇÃO — Admissão\n" +
            $"Ala: {bed.Ward.Name} ({modalityLabel}) · Leito {bed.BedNumber}\n" +
            $"Motivo: {hospitalization.Reason}" +
            (string.IsNullOrWhiteSpace(hospitalization.Diagnosis) ? "" : $"\nDiagnóstico: {hospitalization.Diagnosis}"),
            triageLog?.SuggestedCid10?.Trim() ?? diagnosis,
            cancellationToken);

        return (await GetByIdAsync(hospitalization.Id, cancellationToken))!;
    }

    public async Task<HospitalizationDto?> DischargeAsync(
        Guid id,
        DischargePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalization = await dbContext.Hospitalizations
            .Include(h => h.Patient)
            .Include(h => h.Bed)
            .ThenInclude(b => b.Ward)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hospitalization is null || hospitalization.Status != HospitalizationStatus.Active)
        {
            return null;
        }

        var hasOpenPrescriptions = await dbContext.MedicalRecordEntries.AnyAsync(
            e => e.MedicalRecord.PatientId == hospitalization.PatientId
                && e.IsActive
                && e.EntryType == MedicalRecordEntryType.Prescription
                && e.HospitalizationId == hospitalization.Id
                && !e.IsSigned,
            cancellationToken);

        var hasCriticalLabs = await dbContext.LabOrders.AnyAsync(
            o => o.PatientId == hospitalization.PatientId
                && o.IsActive
                && o.Status != LabOrderStatus.Completed
                && o.Status != LabOrderStatus.Cancelled
                && (EF.Functions.ILike(o.Notes ?? "", "%urgente%")
                    || EF.Functions.ILike(o.Notes ?? "", "%stat%")
                    || o.Items.Any(i => i.Result != null && i.Result.IsAbnormal)),
            cancellationToken);

        DischargeRules.ValidateDischargePendingChecks(hasOpenPrescriptions, hasCriticalLabs);

        hospitalization.Status = HospitalizationStatus.Discharged;
        hospitalization.DischargedAt = DateTime.UtcNow;
        hospitalization.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            hospitalization.Notes = request.Notes.Trim();
        }

        var dischargedBedId = hospitalization.BedId;
        var dischargedHospitalizationId = hospitalization.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        await clinicalStatusAuditLogger.LogStatusChangeAsync(
            "Hospitalization",
            hospitalization.Id,
            "AltaHospitalar",
            HospitalizationStatus.Active.ToString(),
            HospitalizationStatus.Discharged.ToString(),
            string.IsNullOrWhiteSpace(request.Notes)
                ? $"Paciente {hospitalization.Patient?.FullName} — leito {hospitalization.Bed.BedNumber}"
                : $"Paciente {hospitalization.Patient?.FullName} — {request.Notes.Trim()}",
            cancellationToken);

        await eventEngine.PublishAndProcessAsync(
            HospitalEventTypes.PatientDischarged,
            new
            {
                bedId = dischargedBedId,
                hospitalizationId = dischargedHospitalizationId,
                patientId = hospitalization.PatientId,
                patientName = hospitalization.Patient?.FullName,
                wardName = hospitalization.Bed.Ward.Name,
                bedNumber = hospitalization.Bed.BedNumber,
                requestTransport = true,
            },
            dischargedHospitalizationId,
            "Hospitalization",
            cancellationToken);

        var modalityLabel = WardInsuranceMatcher.ModalityLabel(hospitalization.Bed.Ward.CoverageModality);
        var dischargeContent = "ALTA HOSPITALAR\n" +
            $"Ala: {hospitalization.Bed.Ward.Name} ({modalityLabel}) · Leito {hospitalization.Bed.BedNumber}\n" +
            $"Permanência desde {hospitalization.AdmittedAt:dd/MM/yyyy HH:mm}" +
            (string.IsNullOrWhiteSpace(hospitalization.Notes) ? "" : $"\nObservações: {hospitalization.Notes}");

        await AddRecordEntryAsync(
            hospitalization.PatientId,
            hospitalization.Id,
            hospitalization.ProfessionalId,
            MedicalRecordEntryType.Evolution,
            dischargeContent,
            hospitalization.Diagnosis,
            cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<HospitalizationDto?> RegisterPatientDeathAsync(
        Guid id,
        RegisterPatientDeathRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalization = await dbContext.Hospitalizations
            .Include(h => h.Patient)
            .Include(h => h.Bed)
            .ThenInclude(b => b.Ward)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hospitalization is null)
        {
            return null;
        }

        if (hospitalization.Patient.IsDeceased)
        {
            throw new InvalidOperationException("Óbito já registrado para este paciente.");
        }

        if (hospitalization.Status == HospitalizationStatus.Active)
        {
            await DischargeAsync(
                id,
                new DischargePatientRequest(
                    string.IsNullOrWhiteSpace(request.Notes)
                        ? "Óbito hospitalar registrado."
                        : request.Notes.Trim()),
                cancellationToken);
        }

        hospitalization = await dbContext.Hospitalizations
            .FirstAsync(h => h.Id == id, cancellationToken);

        var patient = await dbContext.Patients.FirstAsync(p => p.Id == hospitalization.PatientId, cancellationToken);
        patient.IsDeceased = true;
        patient.DeceasedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.PrimaryCid10Code))
        {
            hospitalization.Diagnosis = request.PrimaryCid10Code.Trim();
        }

        hospitalization.Reason = "Óbito";
        hospitalization.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var deathContent = "REGISTRO DE ÓBITO\n" +
            $"Data/Hora: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC\n" +
            (string.IsNullOrWhiteSpace(request.PrimaryCid10Code) ? "" : $"CID-10: {request.PrimaryCid10Code.Trim()}\n") +
            (string.IsNullOrWhiteSpace(request.Notes) ? "" : $"Observações: {request.Notes.Trim()}");

        await AddRecordEntryAsync(
            hospitalization.PatientId,
            hospitalization.Id,
            hospitalization.ProfessionalId,
            MedicalRecordEntryType.Evolution,
            deathContent,
            request.PrimaryCid10Code?.Trim(),
            cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<HospitalizationDto?> CloseBillingAccountAsync(
        Guid hospitalizationId,
        CancellationToken cancellationToken = default)
    {
        var hospitalization = await dbContext.Hospitalizations
            .FirstOrDefaultAsync(h => h.Id == hospitalizationId && h.IsActive, cancellationToken);

        if (hospitalization is null)
        {
            return null;
        }

        if (hospitalization.BillingAccountClosedAt.HasValue)
        {
            throw new InvalidOperationException("Conta hospitalar já foi fechada para faturamento.");
        }

        hospitalization.BillingAccountClosedAt = DateTime.UtcNow;
        hospitalization.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(hospitalizationId, cancellationToken);
    }

    public async Task<HospitalizationDto> TransferBedAsync(
        Guid hospitalizationId,
        TransferBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalization = await dbContext.Hospitalizations
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .Include(h => h.Patient)
            .FirstOrDefaultAsync(h => h.Id == hospitalizationId && h.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Internação não encontrada.");

        if (hospitalization.Status != HospitalizationStatus.Active)
        {
            throw new InvalidOperationException("Somente internações ativas podem ser transferidas.");
        }

        if (hospitalization.BedId == request.TargetBedId)
        {
            throw new InvalidOperationException("O paciente já está neste leito.");
        }

        var targetBed = await dbContext.Beds
            .Include(b => b.Ward)
            .FirstOrDefaultAsync(b => b.Id == request.TargetBedId && b.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Leito de destino não encontrado.");

        HospitalBusinessRules.ValidateBedAvailableForAdmission(targetBed.Status);

        var targetOccupied = await dbContext.Hospitalizations.AnyAsync(
            h => h.BedId == targetBed.Id && h.Status == HospitalizationStatus.Active && h.IsActive,
            cancellationToken);

        if (targetOccupied)
        {
            throw new InvalidOperationException("Leito de destino já possui internação ativa.");
        }

        var patient = await dbContext.Patients
            .Include(p => p.Insurances.Where(i => i.IsActive)).ThenInclude(i => i.HealthInsurance)
            .FirstAsync(p => p.Id == hospitalization.PatientId, cancellationToken);

        var primaryInsurance = patient.Insurances.OrderByDescending(i => i.IsPrimary).FirstOrDefault();
        var patientModality = WardInsuranceMatcher.ResolvePatientModality(primaryInsurance?.HealthInsurance.Name);

        if (!WardInsuranceMatcher.IsCompatible(targetBed.Ward.CoverageModality, patientModality))
        {
            throw new InvalidOperationException(
                $"Leito incompatível com a cobertura do paciente ({WardInsuranceMatcher.ModalityLabel(patientModality)}).");
        }

        var fromBed = hospitalization.Bed;
        var transfer = new BedTransfer
        {
            HospitalizationId = hospitalization.Id,
            FromBedId = fromBed.Id,
            ToBedId = targetBed.Id,
            ProfessionalId = request.ProfessionalId ?? hospitalization.ProfessionalId,
            Reason = request.Reason?.Trim(),
            TransferredAt = DateTime.UtcNow
        };

        var fromBedId = fromBed.Id;
        targetBed.Status = BedStatus.Occupied;
        targetBed.StatusReason = null;
        targetBed.BlockedUntil = null;
        targetBed.UpdatedAt = DateTime.UtcNow;
        hospitalization.BedId = targetBed.Id;
        hospitalization.UpdatedAt = DateTime.UtcNow;

        dbContext.BedTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        await hotelariaHospitalarService.RequestBedCleaningAsync(
            fromBedId,
            hospitalization.Id,
            CleaningType.Terminal,
            CleaningTriggerReason.Transfer,
            cancellationToken);

        var fromLabel = $"{fromBed.Ward.Name} · Leito {fromBed.BedNumber}";
        var toLabel = $"{targetBed.Ward.Name} · Leito {targetBed.BedNumber}";

        await AddRecordEntryAsync(
            hospitalization.PatientId,
            hospitalization.Id,
            request.ProfessionalId ?? hospitalization.ProfessionalId,
            MedicalRecordEntryType.Evolution,
            $"TRANSFERÊNCIA DE LEITO\nDe: {fromLabel}\nPara: {toLabel}" +
            (string.IsNullOrWhiteSpace(transfer.Reason) ? "" : $"\nMotivo: {transfer.Reason}"),
            hospitalization.Diagnosis,
            cancellationToken);

        return (await GetByIdAsync(hospitalization.Id, cancellationToken))!;
    }

    public async Task<WardDto> CreateWardAsync(
        CreateWardRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("O nome da ala é obrigatório.");
        }

        var code = NormalizeOptionalCode(request.Code);
        await EnsureWardCodeAvailableAsync(code, excludeWardId: null, cancellationToken);

        var ward = new Ward
        {
            Name = name,
            Code = code,
            Floor = request.Floor?.Trim(),
            Description = request.Description?.Trim(),
            CoverageModality = request.CoverageModality,
            Category = request.Category
        };

        dbContext.Wards.Add(ward);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetWardDtoByIdAsync(ward.Id, cancellationToken)
            ?? throw new InvalidOperationException("Não foi possível carregar a ala criada.");
    }

    public async Task<WardDto?> UpdateWardAsync(
        Guid id,
        UpdateWardRequest request,
        CancellationToken cancellationToken = default)
    {
        var ward = await dbContext.Wards.FirstOrDefaultAsync(w => w.Id == id && w.IsActive, cancellationToken);
        if (ward is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("O nome da ala é obrigatório.");
        }

        var code = NormalizeOptionalCode(request.Code);
        await EnsureWardCodeAvailableAsync(code, ward.Id, cancellationToken);

        ward.Name = name;
        ward.Code = code;
        ward.Floor = request.Floor?.Trim();
        ward.Description = request.Description?.Trim();
        ward.CoverageModality = request.CoverageModality;
        ward.Category = request.Category;
        ward.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetWardDtoByIdAsync(ward.Id, cancellationToken);
    }

    public async Task<bool> DeactivateWardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ward = await dbContext.Wards
            .Include(w => w.Beds)
            .FirstOrDefaultAsync(w => w.Id == id && w.IsActive, cancellationToken);

        if (ward is null)
        {
            return false;
        }

        var hasActiveHospitalization = await dbContext.Hospitalizations.AnyAsync(
            h => h.Status == HospitalizationStatus.Active
                && h.IsActive
                && ward.Beds.Select(b => b.Id).Contains(h.BedId),
            cancellationToken);

        if (hasActiveHospitalization)
        {
            throw new InvalidOperationException("Não é possível desativar uma ala com internações ativas.");
        }

        ward.IsActive = false;
        ward.UpdatedAt = DateTime.UtcNow;

        foreach (var bed in ward.Beds.Where(b => b.IsActive))
        {
            bed.IsActive = false;
            bed.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BedDto> CreateBedAsync(
        CreateBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var ward = await dbContext.Wards.FirstOrDefaultAsync(w => w.Id == request.WardId && w.IsActive, cancellationToken);
        if (ward is null)
        {
            throw new InvalidOperationException("Ala não encontrada.");
        }

        var bedNumber = request.BedNumber.Trim();
        if (string.IsNullOrWhiteSpace(bedNumber))
        {
            throw new InvalidOperationException("O número do leito é obrigatório.");
        }

        await EnsureBedNumberAvailableAsync(request.WardId, bedNumber, excludeBedId: null, cancellationToken);

        var bed = new Bed
        {
            WardId = request.WardId,
            BedNumber = bedNumber,
            Status = BedStatus.Available
        };

        dbContext.Beds.Add(bed);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetBedDtoByIdAsync(bed.Id, cancellationToken)
            ?? throw new InvalidOperationException("Não foi possível carregar o leito criado.");
    }

    public async Task<BedDto?> UpdateBedAsync(
        Guid id,
        UpdateBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var bed = await dbContext.Beds.FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        if (bed is null)
        {
            return null;
        }

        var bedNumber = request.BedNumber.Trim();
        if (string.IsNullOrWhiteSpace(bedNumber))
        {
            throw new InvalidOperationException("O número do leito é obrigatório.");
        }

        await EnsureBedNumberAvailableAsync(bed.WardId, bedNumber, bed.Id, cancellationToken);

        bed.BedNumber = bedNumber;
        bed.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetBedDtoByIdAsync(bed.Id, cancellationToken);
    }

    public async Task<BedDto> UpdateBedStatusAsync(
        Guid id,
        UpdateBedStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("O status ocupado é definido automaticamente pela internação.");
        }

        var bed = await dbContext.Beds.FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        if (bed is null)
        {
            throw new InvalidOperationException("Leito não encontrado.");
        }

        var isOccupied = await dbContext.Hospitalizations.AnyAsync(
            h => h.BedId == bed.Id && h.Status == HospitalizationStatus.Active && h.IsActive,
            cancellationToken);

        if (isOccupied)
        {
            throw new InvalidOperationException("Não é possível alterar o status de um leito ocupado.");
        }

        if (request.Status == BedStatus.Maintenance)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Informe o motivo do bloqueio ou manutenção.");
            }

            bed.Status = BedStatus.Maintenance;
            bed.StatusReason = request.Reason.Trim();
            bed.BlockedUntil = request.BlockedUntil;
        }
        else if (request.Status == BedStatus.Available)
        {
            bed.Status = BedStatus.Available;
            bed.StatusReason = null;
            bed.BlockedUntil = null;
        }
        else
        {
            throw new InvalidOperationException("Status de leito inválido.");
        }

        bed.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetBedDtoByIdAsync(bed.Id, cancellationToken)
            ?? throw new InvalidOperationException("Não foi possível carregar o leito atualizado.");
    }

    public async Task<IReadOnlyList<HospitalizationRequestDto>> GetHospitalizationRequestsAsync(
        HospitalizationRequestStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HospitalizationRequests
            .AsNoTracking()
            .Where(r => r.IsActive);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(r => r.PatientId == patientId.Value);
        }

        return await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.RequestedAt)
            .Select(MapHospitalizationRequest())
            .ToListAsync(cancellationToken);
    }

    public async Task<HospitalizationRequestDto> CreateHospitalizationRequestAsync(
        CreateHospitalizationRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        await EnsurePatientCanRequestHospitalizationAsync(request.PatientId, cancellationToken);

        var professional = await dbContext.Professionals
            .FirstOrDefaultAsync(p => p.Id == request.RequestingProfessionalId && p.IsActive, cancellationToken);

        if (professional is null)
        {
            throw new InvalidOperationException("Profissional solicitante não encontrado.");
        }

        var reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Informe o motivo da solicitação.");
        }

        if (request.PreferredWardId.HasValue)
        {
            var wardExists = await dbContext.Wards.AnyAsync(
                w => w.Id == request.PreferredWardId.Value && w.IsActive,
                cancellationToken);

            if (!wardExists)
            {
                throw new InvalidOperationException("Ala preferencial não encontrada.");
            }
        }

        var admissionRequest = new HospitalizationRequest
        {
            PatientId = request.PatientId,
            RequestingProfessionalId = request.RequestingProfessionalId,
            PreferredWardId = request.PreferredWardId,
            PreferredWardCategory = request.PreferredWardCategory,
            Reason = reason,
            Diagnosis = request.Diagnosis?.Trim(),
            Cid10Code = request.Cid10Code?.Trim(),
            Notes = request.Notes?.Trim(),
            Priority = request.Priority,
            AiTriageLogId = request.AiTriageLogId,
            Status = HospitalizationRequestStatus.Pending
        };

        dbContext.HospitalizationRequests.Add(admissionRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RegisterSnippetUsageAsync(HospitalizationSnippetType.Reason, reason, null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(admissionRequest.Diagnosis))
        {
            await RegisterSnippetUsageAsync(
                HospitalizationSnippetType.Diagnosis,
                admissionRequest.Diagnosis,
                null,
                cancellationToken);
        }

        return (await GetHospitalizationRequestByIdAsync(admissionRequest.Id, cancellationToken))!;
    }

    public async Task<HospitalizationRequestDto> ReviewHospitalizationRequestAsync(
        Guid id,
        ReviewHospitalizationRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var admissionRequest = await dbContext.HospitalizationRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (admissionRequest is null)
        {
            throw new InvalidOperationException("Solicitação não encontrada.");
        }

        if (admissionRequest.Status != HospitalizationRequestStatus.Pending)
        {
            throw new InvalidOperationException("Somente solicitações pendentes podem ser analisadas.");
        }

        var reviewer = await dbContext.Professionals
            .FirstOrDefaultAsync(p => p.Id == request.ReviewedByProfessionalId && p.IsActive, cancellationToken);

        if (reviewer is null)
        {
            throw new InvalidOperationException("Profissional revisor não encontrado.");
        }

        admissionRequest.Status = request.Approve
            ? HospitalizationRequestStatus.Approved
            : HospitalizationRequestStatus.Rejected;
        admissionRequest.ReviewedByProfessionalId = request.ReviewedByProfessionalId;
        admissionRequest.ReviewedAt = DateTime.UtcNow;
        admissionRequest.ReviewNotes = request.ReviewNotes?.Trim();
        admissionRequest.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetHospitalizationRequestByIdAsync(admissionRequest.Id, cancellationToken))!;
    }

    public async Task<HospitalizationRequestDto> CancelHospitalizationRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var admissionRequest = await dbContext.HospitalizationRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (admissionRequest is null)
        {
            throw new InvalidOperationException("Solicitação não encontrada.");
        }

        if (admissionRequest.Status is HospitalizationRequestStatus.Admitted
            or HospitalizationRequestStatus.Rejected
            or HospitalizationRequestStatus.Cancelled)
        {
            throw new InvalidOperationException("Esta solicitação não pode ser cancelada.");
        }

        admissionRequest.Status = HospitalizationRequestStatus.Cancelled;
        admissionRequest.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetHospitalizationRequestByIdAsync(admissionRequest.Id, cancellationToken))!;
    }

    public async Task<HospitalizationDto> AdmitFromHospitalizationRequestAsync(
        Guid requestId,
        AdmitFromHospitalizationRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var admissionRequest = await dbContext.HospitalizationRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive, cancellationToken);

        if (admissionRequest is null)
        {
            throw new InvalidOperationException("Solicitação não encontrada.");
        }

        if (admissionRequest.Status != HospitalizationRequestStatus.Approved)
        {
            throw new InvalidOperationException("Somente solicitações aprovadas podem ser convertidas em internação.");
        }

        return await AdmitAsync(
            new AdmitPatientRequest(
                admissionRequest.PatientId,
                request.BedId,
                request.ProfessionalId,
                admissionRequest.Reason,
                admissionRequest.Diagnosis,
                request.Notes ?? admissionRequest.Notes,
                admissionRequest.AiTriageLogId,
                requestId,
                request.SusData),
            cancellationToken);
    }

    public async Task<HospitalizationDto> UpdateSusDataAsync(
        Guid hospitalizationId,
        UpdateHospitalizationSusDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalization = await dbContext.Hospitalizations
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .Include(h => h.Patient).ThenInclude(p => p.Insurances).ThenInclude(i => i.HealthInsurance)
            .FirstOrDefaultAsync(h => h.Id == hospitalizationId && h.IsActive, cancellationToken);

        if (hospitalization is null)
        {
            throw new InvalidOperationException("Internação não encontrada.");
        }

        var patientModality = WardInsuranceMatcher.ResolvePatientModality(
            hospitalization.Patient.Insurances.OrderByDescending(i => i.IsPrimary).FirstOrDefault()?.HealthInsurance.Name);

        if (!RequiresSusDocumentation(hospitalization.Bed.Ward.CoverageModality, patientModality))
        {
            throw new InvalidOperationException("Campos SUS/AIH aplicam-se a internações em alas SUS ou pacientes SUS.");
        }

        hospitalization.AihNumber = NormalizeOptional(request.AihNumber) ?? hospitalization.AihNumber;
        hospitalization.SusCompetence = NormalizeCompetence(request.SusCompetence) ?? hospitalization.SusCompetence;
        hospitalization.PrimaryCid10Code = NormalizeOptional(request.PrimaryCid10Code);
        hospitalization.SecondaryCid10Code = NormalizeOptional(request.SecondaryCid10Code);
        hospitalization.PrimarySigtapProcedureCode = NormalizeOptional(request.PrimarySigtapProcedureCode);
        hospitalization.SecondarySigtapProcedureCode = NormalizeOptional(request.SecondarySigtapProcedureCode);
        hospitalization.SusCharacter = request.SusCharacter ?? hospitalization.SusCharacter;
        hospitalization.SusModality = request.SusModality ?? hospitalization.SusModality;
        hospitalization.CnesCode = NormalizeCnes(request.CnesCode) ?? hospitalization.CnesCode;
        hospitalization.SusAuthorizationNumber = NormalizeOptional(request.SusAuthorizationNumber);
        hospitalization.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(hospitalization.SusCompetence))
        {
            hospitalization.SusCompetence = DateTime.UtcNow.ToString("yyyyMM");
        }

        EnsureAihNumber(hospitalization);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(hospitalization.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<HospitalizationSnippetDto>> GetSnippetsAsync(
        HospitalizationSnippetType type,
        CancellationToken cancellationToken = default) =>
        await dbContext.HospitalizationSnippets
            .AsNoTracking()
            .Where(s => s.IsActive && s.Type == type)
            .OrderByDescending(s => s.UsageCount)
            .ThenBy(s => s.Text)
            .Select(s => new HospitalizationSnippetDto(s.Id, s.Text, s.UsageCount))
            .ToListAsync(cancellationToken);

    public async Task<HospitalizationSnippetDto> RegisterSnippetAsync(
        RegisterHospitalizationSnippetRequest request,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var text = request.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Informe o texto do item.");
        }

        return await RegisterSnippetUsageAsync(request.Type, text, userId, cancellationToken);
    }

    public async Task<bool> DeactivateBedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bed = await dbContext.Beds.FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        if (bed is null)
        {
            return false;
        }

        var isOccupied = await dbContext.Hospitalizations.AnyAsync(
            h => h.BedId == bed.Id && h.Status == HospitalizationStatus.Active && h.IsActive,
            cancellationToken);

        if (isOccupied)
        {
            throw new InvalidOperationException("Não é possível desativar um leito ocupado.");
        }

        bed.IsActive = false;
        bed.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<BedTransferDto>> GetBedTransfersAsync(
        int? limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit ?? 100, 1, 500);

        return await dbContext.BedTransfers
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.TransferredAt)
            .Take(take)
            .Select(t => new BedTransferDto(
                t.Id,
                t.HospitalizationId,
                t.Hospitalization.Patient.FullName,
                t.FromBed.Ward.Name,
                t.FromBed.BedNumber,
                t.ToBed.Ward.Name,
                t.ToBed.BedNumber,
                t.Professional != null ? t.Professional.FullName : null,
                t.TransferredAt,
                t.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task<BedEventDto> ReserveBedAsync(
        Guid bedId,
        ReserveBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var bed = await RequireBedForEventAsync(bedId, cancellationToken);
        await EnsureNoActiveBedEventsAsync(bedId, cancellationToken);

        var patientExists = await dbContext.Patients.AnyAsync(
            p => p.Id == request.PatientId && p.IsActive, cancellationToken);
        if (!patientExists)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        bed.Status = BedStatus.Reserved;
        bed.StatusReason = request.Reason?.Trim();
        bed.BlockedUntil = request.Until;
        bed.UpdatedAt = DateTime.UtcNow;

        var bedEvent = await CreateBedEventAsync(
            bedId,
            BedEventType.Reserve,
            request.PatientId,
            null,
            request.Reason,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBedEventDtoByIdAsync(bedEvent.Id, cancellationToken))!;
    }

    public async Task<BedEventDto> BlockBedAsync(
        Guid bedId,
        BlockBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var bed = await RequireBedForEventAsync(bedId, cancellationToken);
        await EnsureNoActiveBedEventsAsync(bedId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Informe o motivo do bloqueio.");
        }

        bed.Status = BedStatus.Maintenance;
        bed.StatusReason = request.Reason.Trim();
        bed.BlockedUntil = request.Until;
        bed.UpdatedAt = DateTime.UtcNow;

        var bedEvent = await CreateBedEventAsync(
            bedId,
            BedEventType.Block,
            null,
            null,
            request.Reason,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBedEventDtoByIdAsync(bedEvent.Id, cancellationToken))!;
    }

    public async Task<BedEventDto> ReleaseBedAsync(
        Guid bedId,
        ReleaseBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var bed = await RequireBedForEventAsync(bedId, cancellationToken);

        var isOccupied = await dbContext.Hospitalizations.AnyAsync(
            h => h.BedId == bedId && h.Status == HospitalizationStatus.Active && h.IsActive,
            cancellationToken);
        if (isOccupied)
        {
            throw new InvalidOperationException("Não é possível liberar leito com internação ativa.");
        }

        await CloseActiveBedEventsAsync(bedId, BedEventType.Reserve, cancellationToken);
        await CloseActiveBedEventsAsync(bedId, BedEventType.Block, cancellationToken);
        await CloseActiveBedEventsAsync(bedId, BedEventType.Occupy, cancellationToken);

        bed.Status = BedStatus.Available;
        bed.StatusReason = null;
        bed.BlockedUntil = null;
        bed.UpdatedAt = DateTime.UtcNow;

        var bedEvent = await CreateBedEventAsync(
            bedId,
            BedEventType.Release,
            null,
            null,
            request.Reason,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBedEventDtoByIdAsync(bedEvent.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<BedEventDto>> GetBedEventsAsync(
        Guid? bedId,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BedEvents.AsNoTracking().Where(e => e.IsActive);
        if (bedId.HasValue)
        {
            query = query.Where(e => e.BedId == bedId.Value);
        }

        if (activeOnly)
        {
            query = query.Where(e => e.EndAt == null);
        }

        return await query
            .OrderByDescending(e => e.StartAt)
            .Take(200)
            .Select(e => new BedEventDto(
                e.Id,
                e.BedId,
                e.Bed.BedNumber,
                e.Bed.Ward.Name,
                e.EventType,
                e.PatientId,
                e.Patient != null ? e.Patient.FullName : null,
                e.HospitalizationId,
                e.Reason,
                e.StartAt,
                e.EndAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<AiTriageLog?> FindTriageForAdmissionAsync(
        Guid patientId,
        Guid? triageLogId,
        CancellationToken cancellationToken)
    {
        if (triageLogId.HasValue)
        {
            return await dbContext.AiTriageLogs
                .FirstOrDefaultAsync(
                    t => t.Id == triageLogId.Value
                        && t.PatientId == patientId
                        && t.IsActive,
                    cancellationToken);
        }

        var cutoff = DateTime.UtcNow.AddHours(-TriageAdmissionHelper.WindowHours);
        var usedTriageIds = dbContext.Hospitalizations
            .Where(h => h.AiTriageLogId != null)
            .Select(h => h.AiTriageLogId!.Value);

        return await dbContext.AiTriageLogs
            .Where(t => t.PatientId == patientId
                && t.IsActive
                && t.CreatedAt >= cutoff
                && !usedTriageIds.Contains(t.Id))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task AddRecordEntryAsync(
        Guid patientId,
        Guid hospitalizationId,
        Guid professionalId,
        MedicalRecordEntryType entryType,
        string content,
        string? cid10,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.MedicalRecords
            .FirstOrDefaultAsync(m => m.PatientId == patientId, cancellationToken);

        if (record is null) return;

        dbContext.MedicalRecordEntries.Add(new MedicalRecordEntry
        {
            MedicalRecordId = record.Id,
            HospitalizationId = hospitalizationId,
            ProfessionalId = professionalId,
            EntryType = entryType,
            Content = content,
            Cid10Code = cid10?.Trim()
        });
        record.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<HospitalizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Hospitalizations
            .AsNoTracking()
            .Where(h => h.Id == id)
            .Select(MapHospitalization())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureBedStatusConsistencyAsync(CancellationToken cancellationToken)
    {
        var activeHospitalizations = await dbContext.Hospitalizations
            .Include(h => h.Bed)
            .Where(h => h.Status == HospitalizationStatus.Active && h.IsActive)
            .ToListAsync(cancellationToken);

        var changed = false;

        foreach (var hospitalization in activeHospitalizations)
        {
            if (hospitalization.Bed.Status != BedStatus.Occupied)
            {
                hospitalization.Bed.Status = BedStatus.Occupied;
                hospitalization.Bed.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
        }

        var activeBedIds = activeHospitalizations.Select(h => h.BedId).ToHashSet();
        var orphanedOccupied = await dbContext.Beds
            .Where(b => b.IsActive
                && b.Status == BedStatus.Occupied
                && !activeBedIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var bed in orphanedOccupied)
        {
            await hotelariaHospitalarService.RequestBedCleaningAsync(
                bed.Id,
                null,
                CleaningType.Terminal,
                CleaningTriggerReason.Transfer,
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<BedDto>> ProjectBedsAsync(
        IQueryable<Bed> query,
        CancellationToken cancellationToken)
    {
        return await query
            .OrderBy(b => b.Ward.CoverageModality)
            .ThenBy(b => b.Ward.Name)
            .ThenBy(b => b.BedNumber)
            .Select(b => new BedDto(
                b.Id,
                b.WardId,
                b.Ward.Name,
                b.Ward.Code,
                b.Ward.CoverageModality,
                b.Ward.Category,
                b.BedNumber,
                b.Status,
                b.StatusReason,
                b.BlockedUntil,
                dbContext.Hospitalizations
                    .Where(h => h.BedId == b.Id
                        && h.Status == HospitalizationStatus.Active
                        && h.IsActive)
                    .Select(h => (Guid?)h.PatientId)
                    .FirstOrDefault(),
                dbContext.Hospitalizations
                    .Where(h => h.BedId == b.Id
                        && h.Status == HospitalizationStatus.Active
                        && h.IsActive)
                    .Select(h => h.Patient.FullName)
                    .FirstOrDefault(),
                dbContext.Hospitalizations
                    .Where(h => h.BedId == b.Id
                        && h.Status == HospitalizationStatus.Active
                        && h.IsActive)
                    .Select(h => h.Professional.FullName)
                    .FirstOrDefault(),
                dbContext.Hospitalizations
                    .Where(h => h.BedId == b.Id
                        && h.Status == HospitalizationStatus.Active
                        && h.IsActive)
                    .Select(h => (DateTime?)h.AdmittedAt)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    private async Task<WardDto?> GetWardDtoByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Wards
            .AsNoTracking()
            .Where(w => w.Id == id && w.IsActive)
            .Select(w => new WardDto(
                w.Id,
                w.Name,
                w.Code,
                w.Floor,
                w.Description,
                w.CoverageModality,
                w.Category,
                w.Beds.Count(b => b.IsActive),
                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
                w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Maintenance)))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<BedDto?> GetBedDtoByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var beds = await ProjectBedsAsync(
            dbContext.Beds.AsNoTracking().Where(b => b.Id == id && b.IsActive),
            cancellationToken);
        return beds.FirstOrDefault();
    }

    private static string? NormalizeOptionalCode(string? code)
    {
        var trimmed = code?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async Task EnsureWardCodeAvailableAsync(
        string? code,
        Guid? excludeWardId,
        CancellationToken cancellationToken)
    {
        if (code is null)
        {
            return;
        }

        var exists = await dbContext.Wards.AnyAsync(
            w => w.IsActive && w.Code == code && (!excludeWardId.HasValue || w.Id != excludeWardId.Value),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Já existe uma ala com o código \"{code}\".");
        }
    }

    private async Task EnsureBedNumberAvailableAsync(
        Guid wardId,
        string bedNumber,
        Guid? excludeBedId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Beds.AnyAsync(
            b => b.IsActive
                && b.WardId == wardId
                && b.BedNumber == bedNumber
                && (!excludeBedId.HasValue || b.Id != excludeBedId.Value),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Já existe o leito \"{bedNumber}\" nesta ala.");
        }
    }

    private async Task EnsurePatientCanRequestHospitalizationAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var alreadyAdmitted = await dbContext.Hospitalizations.AnyAsync(
            h => h.PatientId == patientId && h.Status == HospitalizationStatus.Active && h.IsActive,
            cancellationToken);

        if (alreadyAdmitted)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.OneBedPerPatient}] Paciente já possui internação ativa.");
        }

        var openRequest = await dbContext.HospitalizationRequests.AnyAsync(
            r => r.PatientId == patientId
                && r.IsActive
                && (r.Status == HospitalizationRequestStatus.Pending
                    || r.Status == HospitalizationRequestStatus.Approved),
            cancellationToken);

        if (openRequest)
        {
            throw new InvalidOperationException("Paciente já possui solicitação de internação em aberto.");
        }
    }

    private async Task CompleteHospitalizationRequestAsync(
        Guid requestId,
        Guid hospitalizationId,
        CancellationToken cancellationToken)
    {
        var admissionRequest = await dbContext.HospitalizationRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive, cancellationToken);

        if (admissionRequest is null)
        {
            return;
        }

        if (admissionRequest.Status is not (
            HospitalizationRequestStatus.Pending
            or HospitalizationRequestStatus.Approved))
        {
            throw new InvalidOperationException("A solicitação vinculada não está apta para admissão.");
        }

        admissionRequest.Status = HospitalizationRequestStatus.Admitted;
        admissionRequest.HospitalizationId = hospitalizationId;
        admissionRequest.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<HospitalizationRequestDto?> GetHospitalizationRequestByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await dbContext.HospitalizationRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(MapHospitalizationRequest())
            .FirstOrDefaultAsync(cancellationToken);

    private static System.Linq.Expressions.Expression<Func<HospitalizationRequest, HospitalizationRequestDto>> MapHospitalizationRequest() =>
        r => new HospitalizationRequestDto(
            r.Id,
            r.PatientId,
            r.Patient.FullName,
            r.RequestingProfessionalId,
            r.RequestingProfessional.FullName,
            r.PreferredWardId,
            r.PreferredWard != null ? r.PreferredWard.Name : null,
            r.PreferredWardCategory,
            r.Reason,
            r.Diagnosis,
            r.Cid10Code,
            r.Notes,
            r.Priority,
            r.Status,
            r.RequestedAt,
            r.ReviewedByProfessionalId,
            r.ReviewedByProfessional != null ? r.ReviewedByProfessional.FullName : null,
            r.ReviewedAt,
            r.ReviewNotes,
            r.HospitalizationId);

    private static void ApplySusFields(
        Hospitalization hospitalization,
        HospitalizationSusDataInput? susData,
        HospitalizationRequest? admissionRequest,
        WardCoverageModality wardModality,
        WardCoverageModality patientModality)
    {
        if (!RequiresSusDocumentation(wardModality, patientModality))
        {
            return;
        }

        hospitalization.SusCompetence = DateTime.UtcNow.ToString("yyyyMM");
        hospitalization.CnesCode ??= DefaultCnesCode;

        if (susData is not null)
        {
            hospitalization.PrimaryCid10Code = NormalizeOptional(susData.PrimaryCid10Code);
            hospitalization.SecondaryCid10Code = NormalizeOptional(susData.SecondaryCid10Code);
            hospitalization.PrimarySigtapProcedureCode = NormalizeOptional(susData.PrimarySigtapProcedureCode);
            hospitalization.SecondarySigtapProcedureCode = NormalizeOptional(susData.SecondarySigtapProcedureCode);
            hospitalization.SusCharacter = susData.SusCharacter ?? hospitalization.SusCharacter;
            hospitalization.SusModality = susData.SusModality ?? hospitalization.SusModality;
            hospitalization.CnesCode = NormalizeCnes(susData.CnesCode) ?? hospitalization.CnesCode;
            hospitalization.SusAuthorizationNumber = NormalizeOptional(susData.SusAuthorizationNumber);
        }

        if (admissionRequest is not null)
        {
            hospitalization.PrimaryCid10Code ??= NormalizeOptional(admissionRequest.Cid10Code);
            hospitalization.SusCharacter ??= MapPriorityToSusCharacter(admissionRequest.Priority);
        }

        hospitalization.SusModality ??= SusHospitalizationModality.Clinical;
        hospitalization.SusCharacter ??= SusHospitalizationCharacter.Elective;
    }

    private static void EnsureAihNumber(Hospitalization hospitalization)
    {
        if (!string.IsNullOrWhiteSpace(hospitalization.AihNumber))
        {
            return;
        }

        var competence = hospitalization.SusCompetence ?? DateTime.UtcNow.ToString("yyyyMM");
        hospitalization.SusCompetence ??= competence;
        hospitalization.AihNumber = $"AIH{competence}{hospitalization.Id.ToString("N")[..6].ToUpperInvariant()}";
    }

    private static bool RequiresSusDocumentation(
        WardCoverageModality wardModality,
        WardCoverageModality patientModality) =>
        wardModality == WardCoverageModality.Sus
        || wardModality == WardCoverageModality.Mixed
        || patientModality == WardCoverageModality.Sus;

    private static SusHospitalizationCharacter MapPriorityToSusCharacter(
        HospitalizationRequestPriority priority) =>
        priority switch
        {
            HospitalizationRequestPriority.Emergency => SusHospitalizationCharacter.Emergency,
            HospitalizationRequestPriority.Urgent => SusHospitalizationCharacter.Urgent,
            _ => SusHospitalizationCharacter.Elective
        };

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeCompetence(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return trimmed.Length == 6 && trimmed.All(char.IsDigit) ? trimmed : null;
    }

    private static string? NormalizeCnes(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return trimmed.PadLeft(7, '0')[..7];
    }

    private const string DefaultCnesCode = "2277185";

    private async Task<HospitalizationSnippetDto> RegisterSnippetUsageAsync(
        HospitalizationSnippetType type,
        string text,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var trimmed = text.Trim();
        var normalized = HospitalizationSnippetSeed.Normalize(trimmed);

        var snippet = await dbContext.HospitalizationSnippets
            .FirstOrDefaultAsync(
                s => s.Type == type && s.NormalizedText == normalized,
                cancellationToken);

        if (snippet is null)
        {
            snippet = new HospitalizationSnippet
            {
                Type = type,
                Text = trimmed,
                NormalizedText = normalized,
                UsageCount = 1,
                CreatedByUserId = userId
            };
            dbContext.HospitalizationSnippets.Add(snippet);
        }
        else
        {
            snippet.UsageCount += 1;
            snippet.Text = trimmed;
            snippet.UpdatedAt = DateTime.UtcNow;
            if (!snippet.IsActive)
            {
                snippet.IsActive = true;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new HospitalizationSnippetDto(snippet.Id, snippet.Text, snippet.UsageCount);
    }

    private async Task<Bed> RequireBedForEventAsync(Guid bedId, CancellationToken cancellationToken)
    {
        var bed = await dbContext.Beds
            .FirstOrDefaultAsync(b => b.Id == bedId && b.IsActive, cancellationToken);

        if (bed is null)
        {
            throw new InvalidOperationException("Leito não encontrado.");
        }

        if (bed.Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("Leito ocupado.");
        }

        return bed;
    }

    private async Task EnsureNoActiveBedEventsAsync(Guid bedId, CancellationToken cancellationToken)
    {
        var hasActive = await dbContext.BedEvents.AnyAsync(
            e => e.BedId == bedId && e.EndAt == null && e.IsActive,
            cancellationToken);

        if (hasActive)
        {
            throw new InvalidOperationException("Leito possui evento ativo (reserva ou bloqueio).");
        }
    }

    private async Task CloseActiveBedEventsAsync(
        Guid bedId,
        BedEventType eventType,
        CancellationToken cancellationToken)
    {
        var events = await dbContext.BedEvents
            .Where(e => e.BedId == bedId && e.EventType == eventType && e.EndAt == null && e.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var bedEvent in events)
        {
            bedEvent.EndAt = DateTime.UtcNow;
            bedEvent.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<BedEvent> CreateBedEventAsync(
        Guid bedId,
        BedEventType eventType,
        Guid? patientId,
        Guid? hospitalizationId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var bedEvent = new BedEvent
        {
            BedId = bedId,
            EventType = eventType,
            PatientId = patientId,
            HospitalizationId = hospitalizationId,
            Reason = reason?.Trim(),
            StartAt = DateTime.UtcNow,
        };

        dbContext.BedEvents.Add(bedEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return bedEvent;
    }

    private async Task<BedEventDto?> GetBedEventDtoByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.BedEvents
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new BedEventDto(
                e.Id,
                e.BedId,
                e.Bed.BedNumber,
                e.Bed.Ward.Name,
                e.EventType,
                e.PatientId,
                e.Patient != null ? e.Patient.FullName : null,
                e.HospitalizationId,
                e.Reason,
                e.StartAt,
                e.EndAt))
            .FirstOrDefaultAsync(cancellationToken);

    private static System.Linq.Expressions.Expression<Func<Hospitalization, HospitalizationDto>> MapHospitalization() =>
        h => new HospitalizationDto(
            h.Id,
            h.PatientId,
            h.Patient.FullName,
            h.Patient.IsDeceased,
            h.Patient.Cns ?? h.Patient.Insurances
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.IsPrimary)
                .Select(i => i.CnsNumber)
                .FirstOrDefault(),
            h.BedId,
            h.Bed.BedNumber,
            h.Bed.Ward.Name,
            h.Bed.Ward.Code,
            h.Bed.Ward.CoverageModality,
            h.Bed.Ward.Category,
            h.ProfessionalId,
            h.Professional.FullName,
            h.AdmittedAt,
            h.DischargedAt,
            h.Status,
            h.Reason,
            h.Diagnosis,
            h.BillingAccountClosedAt,
            new HospitalizationSusDataDto(
                h.AihNumber,
                h.SusCompetence,
                h.PrimaryCid10Code,
                h.SecondaryCid10Code,
                h.PrimarySigtapProcedureCode,
                h.SecondarySigtapProcedureCode,
                h.SusCharacter,
                h.SusModality,
                h.CnesCode,
                h.SusAuthorizationNumber,
                h.AihExportedAt));
}
