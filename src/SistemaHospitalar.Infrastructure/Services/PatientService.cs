using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Application.DTOs.Patients;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

public class PatientService(
    AppDbContext dbContext,
    IHospitalizationService hospitalizationService,
    IFieldEncryptionService encryption,
    ClinicalStatusAuditLogger clinicalStatusAuditLogger) : IPatientService
{
    public async Task<PagedResult<PatientDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        bool? isActive = true,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Patients.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var cpfDigits = new string(term.Where(char.IsDigit).ToArray());
            if (cpfDigits.Length == 11)
            {
                var cpfHash = encryption.HashForLookup(cpfDigits);
                query = query.Where(p =>
                    p.CpfHash == cpfHash ||
                    p.FullName.Contains(term) ||
                    (p.SocialName != null && p.SocialName.Contains(term)));
            }
            else
            {
                query = query.Where(p =>
                    p.FullName.Contains(term) ||
                    (p.SocialName != null && p.SocialName.Contains(term)));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var patients = await query
            .Include(p => p.Insurances.Where(i => i.IsActive))
                .ThenInclude(i => i.HealthInsurance)
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var enrichment = await LoadListEnrichmentAsync(
            patients.Select(p => p.Id).ToList(),
            cancellationToken);

        var items = patients.Select(p =>
        {
            PatientFieldProtection.Decrypt(p, encryption);
            enrichment.TryGetValue(p.Id, out var extra);
            return new PatientDto(
                p.Id,
                p.FullName,
                p.SocialName,
                p.Cpf,
                p.Cns,
                p.BirthDate,
                p.Gender,
                p.Email,
                p.Phone,
                p.MobilePhone,
                p.AddressCity,
                p.AddressState,
                p.Insurances.Where(i => i.IsActive && i.IsPrimary)
                    .Select(i => i.HealthInsurance.Name)
                    .FirstOrDefault(),
                p.PhotoData != null,
                p.IsActive,
                p.EmergencyContactName,
                p.EmergencyContactPhone,
                p.MotherName,
                p.EmergencyContactRelationship,
                p.CreatedAt,
                p.UsesResponsibleCpf,
                MapLegalResponsibleDto(p),
                extra?.OpenReceivableCount ?? 0,
                extra?.LastAppointmentAt,
                extra?.NextAppointmentAt);
        }).ToList();

        return new PagedResult<PatientDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<PatientDto>> QuickSearchAsync(
        string? search,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 30);
        var query = dbContext.Patients.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            if (digits.Length == 11)
            {
                var cpfHash = encryption.HashForLookup(digits);
                query = query.Where(p =>
                    p.CpfHash == cpfHash ||
                    p.FullName.Contains(term) ||
                    (p.SocialName != null && p.SocialName.Contains(term)));
            }
            else
            {
                query = query.Where(p =>
                    p.FullName.Contains(term) ||
                    (p.SocialName != null && p.SocialName.Contains(term)));
            }
        }

        var patients = await query
            .Include(p => p.Insurances.Where(i => i.IsActive))
                .ThenInclude(i => i.HealthInsurance)
            .OrderBy(p => p.FullName)
            .Take(take)
            .ToListAsync(cancellationToken);

        var enrichment = await LoadListEnrichmentAsync(
            patients.Select(p => p.Id).ToList(),
            cancellationToken);

        return patients.Select(p =>
        {
            PatientFieldProtection.Decrypt(p, encryption);
            enrichment.TryGetValue(p.Id, out var extra);
            return new PatientDto(
                p.Id,
                p.FullName,
                p.SocialName,
                p.Cpf,
                p.Cns,
                p.BirthDate,
                p.Gender,
                p.Email,
                p.Phone,
                p.MobilePhone,
                p.AddressCity,
                p.AddressState,
                p.Insurances.Where(i => i.IsActive && i.IsPrimary)
                    .Select(i => i.HealthInsurance.Name)
                    .FirstOrDefault(),
                p.PhotoData != null,
                p.IsActive,
                p.EmergencyContactName,
                p.EmergencyContactPhone,
                p.MotherName,
                p.EmergencyContactRelationship,
                p.CreatedAt,
                p.UsesResponsibleCpf,
                MapLegalResponsibleDto(p),
                extra?.OpenReceivableCount ?? 0,
                extra?.LastAppointmentAt,
                extra?.NextAppointmentAt);
        }).ToList();
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
            .Include(p => p.Insurances.Where(i => i.IsActive))
                .ThenInclude(i => i.HealthInsurance)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (patient is null)
        {
            return null;
        }

        PatientFieldProtection.Decrypt(patient, encryption);
        return MapDetail(patient);
    }

    public async Task<CreatePatientResult> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        ValidateInitialAdmission(request.InitialAdmission);

        var legalResponsible = MapLegalResponsibleData(request.LegalResponsible);
        var registrationCpf = PatientRegistrationRules.ResolveRegistrationCpf(
            request.UsesResponsibleCpf,
            request.Cpf,
            legalResponsible);

        if (!request.UsesResponsibleCpf)
        {
            PatientCpfRules.ValidateForRegistration(
                registrationCpf,
                await CpfIsRegisteredAsOwnAsync(registrationCpf, excludePatientId: null, cancellationToken));
        }

        HospitalBusinessRules.ValidateMinorHasResponsible(
            request.BirthDate,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.MotherName,
            request.UsesResponsibleCpf);

        var patient = new Patient
        {
            FullName = request.FullName.Trim(),
            SocialName = request.SocialName?.Trim(),
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            MobilePhone = request.MobilePhone?.Trim(),
            AddressStreet = request.AddressStreet?.Trim(),
            AddressNumber = request.AddressNumber?.Trim(),
            AddressComplement = request.AddressComplement?.Trim(),
            AddressNeighborhood = request.AddressNeighborhood?.Trim(),
            AddressCity = request.AddressCity?.Trim(),
            AddressState = request.AddressState?.Trim()?.ToUpperInvariant(),
            AddressZipCode = request.AddressZipCode?.Trim(),
            MotherName = request.MotherName?.Trim(),
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            EmergencyContactRelationship = request.EmergencyContactRelationship?.Trim(),
            Notes = request.Notes?.Trim(),
            PhotoData = NormalizePhoto(request.PhotoData),
            Rg = request.Rg?.Trim(),
            Nationality = request.Nationality?.Trim(),
            BloodType = request.BloodType?.Trim(),
            Occupation = request.Occupation?.Trim(),
            MaritalStatus = request.MaritalStatus?.Trim(),
            BirthPlace = request.BirthPlace?.Trim(),
            UsesResponsibleCpf = request.UsesResponsibleCpf,
        };

        if (request.UsesResponsibleCpf && legalResponsible is not null)
        {
            patient.LegalResponsibleName = legalResponsible.Name.Trim();
            patient.LegalResponsibleBirthDate = legalResponsible.BirthDate;
            patient.LegalResponsibleRelationship = legalResponsible.Relationship;
            patient.LegalResponsibleRg = legalResponsible.Rg.Trim();
            patient.LegalAuthorizationDocumentType = legalResponsible.AuthorizationDocumentType;
            patient.LegalAuthorizationDocumentReference = legalResponsible.AuthorizationDocumentReference?.Trim();
        }

        PatientFieldProtection.Protect(patient, encryption, registrationCpf);

        var recordNumber = await GenerateRecordNumberAsync(cancellationToken);

        patient.MedicalRecord = new MedicalRecord
        {
            RecordNumber = recordNumber
        };

        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncInsurancesAsync(patient.Id, request.Insurances, cancellationToken);

        HospitalizationDto? initialHospitalization = null;
        if (request.InitialAdmission is not null)
        {
            var admission = request.InitialAdmission;
            initialHospitalization = await hospitalizationService.AdmitAsync(
                new AdmitPatientRequest(
                    patient.Id,
                    admission.BedId,
                    admission.ProfessionalId,
                    admission.Reason,
                    admission.Diagnosis,
                    admission.Notes),
                cancellationToken);
        }

        return new CreatePatientResult(
            (await GetByIdAsync(patient.Id, cancellationToken))!,
            initialHospitalization);
    }

    private static void ValidateInitialAdmission(PatientInitialAdmissionInput? admission)
    {
        if (admission is null)
        {
            return;
        }

        if (admission.BedId == Guid.Empty
            || admission.ProfessionalId == Guid.Empty
            || string.IsNullOrWhiteSpace(admission.Reason))
        {
            throw new InvalidOperationException(
                "Para internar no cadastro, informe leito, médico responsável e motivo da internação.");
        }
    }

    public async Task<PatientDetailDto?> UpdateAsync(
        Guid id,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient is null)
        {
            return null;
        }

        PatientFieldProtection.Decrypt(patient, encryption);

        HospitalBusinessRules.ValidateMinorHasResponsible(
            request.BirthDate,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.MotherName,
            patient.UsesResponsibleCpf);

        var wasActive = patient.IsActive;
        HospitalBusinessRules.ValidateInactivationReason(wasActive, request.IsActive, request.InactivationReason);

        patient.FullName = request.FullName.Trim();
        patient.SocialName = request.SocialName?.Trim();
        patient.BirthDate = request.BirthDate;
        patient.Gender = request.Gender;
        patient.Email = request.Email?.Trim();
        patient.Phone = request.Phone?.Trim();
        patient.MobilePhone = request.MobilePhone?.Trim();
        patient.AddressStreet = request.AddressStreet?.Trim();
        patient.AddressNumber = request.AddressNumber?.Trim();
        patient.AddressComplement = request.AddressComplement?.Trim();
        patient.AddressNeighborhood = request.AddressNeighborhood?.Trim();
        patient.AddressCity = request.AddressCity?.Trim();
        patient.AddressState = request.AddressState?.Trim()?.ToUpperInvariant();
        patient.AddressZipCode = request.AddressZipCode?.Trim();
        patient.MotherName = request.MotherName?.Trim();
        patient.EmergencyContactName = request.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        patient.EmergencyContactRelationship = request.EmergencyContactRelationship?.Trim();
        patient.Notes = request.Notes?.Trim();
        patient.Rg = request.Rg?.Trim();
        patient.Nationality = request.Nationality?.Trim();
        patient.BloodType = request.BloodType?.Trim();
        patient.Occupation = request.Occupation?.Trim();
        patient.MaritalStatus = request.MaritalStatus?.Trim();
        patient.BirthPlace = request.BirthPlace?.Trim();
        patient.IsActive = request.IsActive;
        patient.UpdatedAt = DateTime.UtcNow;

        if (request.PhotoData is not null)
        {
            patient.PhotoData = NormalizePhoto(request.PhotoData);
        }

        PatientFieldProtection.ReprotectCpf(patient, encryption);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (wasActive && !request.IsActive)
        {
            await clinicalStatusAuditLogger.LogPatientInactivationAsync(
                id,
                request.InactivationReason!.Trim(),
                cancellationToken);
        }

        await SyncInsurancesAsync(id, request.Insurances, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CpfAvailabilityResult> CheckCpfAvailabilityAsync(
        string cpf,
        Guid? excludePatientId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = PatientCpfRules.Normalize(cpf);
        if (PatientCpfRules.IsMissing(normalized))
        {
            return new CpfAvailabilityResult(true, null);
        }

        if (!PatientCpfRules.IsValidChecksum(normalized))
        {
            return new CpfAvailabilityResult(false, "CPF inválido.");
        }

        var exists = await CpfIsRegisteredAsOwnAsync(normalized, excludePatientId, cancellationToken);
        return exists
            ? new CpfAvailabilityResult(false, "Já existe um prontuário cadastrado com este CPF.")
            : new CpfAvailabilityResult(true, null);
    }

    public async Task<IReadOnlyList<PotentialDuplicatePatientDto>> FindPotentialDuplicatesAsync(
        PatientDuplicateCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PotentialDuplicatePatientDto>();
        var normalizedName = request.FullName.Trim();

        var nameBirthMatches = await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
            .Where(p => p.IsActive
                && p.BirthDate == request.BirthDate
                && (p.FullName == normalizedName
                    || (p.SocialName != null && p.SocialName == normalizedName))
                && (request.ExcludePatientId == null || p.Id != request.ExcludePatientId))
            .ToListAsync(cancellationToken);

        foreach (var match in nameBirthMatches)
        {
            PatientFieldProtection.Decrypt(match, encryption);
            results.Add(new PotentialDuplicatePatientDto(
                match.Id,
                match.FullName,
                match.BirthDate,
                match.Cpf,
                match.MedicalRecord?.RecordNumber,
                "Nome e data de nascimento coincidentes"));
        }

        var cpf = PatientCpfRules.Normalize(request.Cpf);
        if (!PatientCpfRules.IsMissing(cpf) && PatientCpfRules.IsValidChecksum(cpf))
        {
            var cpfHash = encryption.HashForLookup(cpf);
            var cpfMatches = await dbContext.Patients
                .AsNoTracking()
                .Include(p => p.MedicalRecord)
                .Where(p => p.IsActive
                    && !p.UsesResponsibleCpf
                    && p.CpfHash == cpfHash
                    && (request.ExcludePatientId == null || p.Id != request.ExcludePatientId))
                .ToListAsync(cancellationToken);

            foreach (var match in cpfMatches.Where(m => results.All(r => r.Id != m.Id)))
            {
                PatientFieldProtection.Decrypt(match, encryption);
                results.Add(new PotentialDuplicatePatientDto(
                    match.Id,
                    match.FullName,
                    match.BirthDate,
                    match.Cpf,
                    match.MedicalRecord?.RecordNumber,
                    "CPF coincidente"));
            }
        }

        return results;
    }

    private async Task<bool> CpfIsRegisteredAsOwnAsync(
        string normalizedCpf,
        Guid? excludePatientId,
        CancellationToken cancellationToken)
    {
        var cpfHash = encryption.HashForLookup(normalizedCpf);
        return await dbContext.Patients.AnyAsync(
            p => !p.UsesResponsibleCpf
                 && p.CpfHash == cpfHash
                 && (excludePatientId == null || p.Id != excludePatientId),
            cancellationToken);
    }

    private async Task<Dictionary<Guid, PatientListEnrichment>> LoadListEnrichmentAsync(
        IReadOnlyList<Guid> patientIds,
        CancellationToken cancellationToken)
    {
        if (patientIds.Count == 0)
        {
            return new Dictionary<Guid, PatientListEnrichment>();
        }

        var now = DateTime.UtcNow;

        var openReceivables = await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(f => f.PatientId != null
                && patientIds.Contains(f.PatientId.Value)
                && f.IsActive
                && f.Direction == FinancialAccountDirection.Receivable
                && (f.Status == FinancialAccountStatus.Open || f.Status == FinancialAccountStatus.PartiallyPaid))
            .GroupBy(f => f.PatientId!.Value)
            .Select(g => new { PatientId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var appointmentStats = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => patientIds.Contains(a.PatientId)
                && a.IsActive
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .GroupBy(a => a.PatientId)
            .Select(g => new
            {
                PatientId = g.Key,
                LastPast = g.Where(a => a.ScheduledAt < now).Max(a => (DateTime?)a.ScheduledAt),
                NextFuture = g.Where(a => a.ScheduledAt >= now).Min(a => (DateTime?)a.ScheduledAt)
            })
            .ToListAsync(cancellationToken);

        var result = patientIds.ToDictionary(
            id => id,
            id => new PatientListEnrichment(0, null, null));

        foreach (var row in openReceivables)
        {
            result[row.PatientId] = result[row.PatientId] with { OpenReceivableCount = row.Count };
        }

        foreach (var row in appointmentStats)
        {
            result[row.PatientId] = result[row.PatientId] with
            {
                LastAppointmentAt = row.LastPast,
                NextAppointmentAt = row.NextFuture
            };
        }

        return result;
    }

    private sealed record PatientListEnrichment(
        int OpenReceivableCount,
        DateTime? LastAppointmentAt,
        DateTime? NextAppointmentAt);

    private static LegalResponsibleData? MapLegalResponsibleData(LegalResponsibleInput? input)
    {
        if (input is null)
        {
            return null;
        }

        return new LegalResponsibleData(
            input.Name,
            input.Cpf,
            input.BirthDate,
            input.Relationship,
            input.Rg,
            input.AuthorizationDocumentType,
            input.AuthorizationDocumentReference);
    }

    private static LegalResponsibleDto? MapLegalResponsibleDto(Patient patient)
    {
        if (!patient.UsesResponsibleCpf || string.IsNullOrWhiteSpace(patient.LegalResponsibleName))
        {
            return null;
        }

        return new LegalResponsibleDto(
            patient.LegalResponsibleName,
            patient.LegalResponsibleBirthDate ?? default,
            patient.LegalResponsibleRelationship ?? LegalResponsibleRelationship.NotInformed,
            patient.LegalResponsibleRg ?? string.Empty,
            patient.LegalAuthorizationDocumentType,
            patient.LegalAuthorizationDocumentReference);
    }

    private async Task SyncInsurancesAsync(
        Guid patientId,
        IReadOnlyList<PatientInsuranceInput>? insurances,
        CancellationToken cancellationToken)
    {
        if (insurances is null) return;

        var existing = await dbContext.PatientInsurances
            .Where(pi => pi.PatientId == patientId)
            .ToListAsync(cancellationToken);

        dbContext.PatientInsurances.RemoveRange(existing);

        var hasPrimary = insurances.Any(i => i.IsPrimary);
        var index = 0;

        foreach (var ins in insurances)
        {
            if (ins.HealthInsuranceId == Guid.Empty) continue;

            if (!string.IsNullOrWhiteSpace(ins.CnsNumber))
            {
                PatientCnsRules.ValidateForRegistration(ins.CnsNumber);
                var normalizedCns = PatientCnsRules.Normalize(ins.CnsNumber);
                var cnsHash = encryption.HashForLookup(normalizedCns);
                var cnsExists = await dbContext.Patients.AnyAsync(
                    p => p.IsActive && p.CnsHash == cnsHash && p.Id != patientId,
                    cancellationToken);
                HospitalBusinessRules.ValidateUniqueCns(cnsExists);
            }

            dbContext.PatientInsurances.Add(new PatientInsurance
            {
                PatientId = patientId,
                HealthInsuranceId = ins.HealthInsuranceId,
                CardNumber = ins.CardNumber.Trim(),
                PlanName = ins.PlanName?.Trim(),
                CardHolderName = ins.CardHolderName?.Trim(),
                ProductCode = ins.ProductCode?.Trim(),
                CnsNumber = ins.CnsNumber?.Trim(),
                AccommodationType = ins.AccommodationType?.Trim(),
                ValidFrom = ins.ValidFrom,
                ValidUntil = ins.ValidUntil,
                IsPrimary = ins.IsPrimary || (!hasPrimary && index == 0)
            });
            index++;
        }

        await ApplyPrimaryCnsToPatientAsync(patientId, insurances, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyPrimaryCnsToPatientAsync(
        Guid patientId,
        IReadOnlyList<PatientInsuranceInput> insurances,
        CancellationToken cancellationToken)
    {
        var primaryCns = insurances
            .FirstOrDefault(i => i.IsPrimary && !string.IsNullOrWhiteSpace(i.CnsNumber))?.CnsNumber
            ?? insurances.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.CnsNumber))?.CnsNumber;

        if (string.IsNullOrWhiteSpace(primaryCns))
        {
            return;
        }

        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return;
        }

        PatientFieldProtection.Decrypt(patient, encryption);
        patient.Cns = PatientCnsRules.Normalize(primaryCns);
        PatientFieldProtection.ApplyCnsProtection(patient, encryption);
    }

    private static PatientDetailDto MapDetail(Patient p) => new(
        p.Id,
        p.FullName,
        p.SocialName,
        p.Cpf,
        p.Cns,
        p.BirthDate,
        p.Gender,
        p.Email,
        p.Phone,
        p.MobilePhone,
        p.AddressStreet,
        p.AddressNumber,
        p.AddressComplement,
        p.AddressNeighborhood,
        p.AddressCity,
        p.AddressState,
        p.AddressZipCode,
        p.MotherName,
        p.EmergencyContactName,
        p.EmergencyContactPhone,
        p.EmergencyContactRelationship,
        p.Notes,
        p.PhotoData,
        p.Rg,
        p.Nationality,
        p.BloodType,
        p.Occupation,
        p.MaritalStatus,
        p.BirthPlace,
        p.IsActive,
        p.CreatedAt,
        p.MedicalRecord?.Id,
        p.MedicalRecord?.RecordNumber,
        p.Insurances
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.HealthInsurance.Name)
            .Select(i => new PatientInsuranceDto(
                i.Id,
                i.HealthInsuranceId,
                i.HealthInsurance.Name,
                i.CardNumber,
                i.PlanName,
                i.CardHolderName,
                i.ProductCode,
                i.CnsNumber,
                i.AccommodationType,
                i.ValidFrom,
                i.ValidUntil,
                i.IsPrimary))
            .ToList(),
        p.UsesResponsibleCpf,
        MapLegalResponsibleDto(p));

    private async Task<string> GenerateRecordNumberAsync(CancellationToken cancellationToken)
    {
        var recordNumbers = await dbContext.MedicalRecords
            .AsNoTracking()
            .Select(m => m.RecordNumber)
            .ToListAsync(cancellationToken);

        long nextSequence = 1;
        foreach (var recordNumber in recordNumbers)
        {
            if (PatientRecordNumberRules.TryParseSequence(recordNumber, out var sequence)
                && sequence >= nextSequence)
            {
                nextSequence = sequence + 1;
            }
        }

        return PatientRecordNumberRules.Format(nextSequence);
    }

    private static string? NormalizePhoto(string? photoData)
    {
        if (string.IsNullOrWhiteSpace(photoData))
        {
            return null;
        }

        if (photoData.Length > 500_000)
        {
            throw new InvalidOperationException("A foto excede o tamanho máximo permitido.");
        }

        return photoData;
    }
}
