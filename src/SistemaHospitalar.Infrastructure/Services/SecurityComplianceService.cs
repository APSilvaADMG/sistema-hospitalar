using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

public class SecurityComplianceService(AppDbContext dbContext, IFieldEncryptionService encryption) : ISecurityComplianceService
{
    public async Task<ComplianceDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var activeConsents = await dbContext.PatientConsents.CountAsync(c => c.IsActive && c.RevokedAt == null, cancellationToken);
        var revokedConsents = await dbContext.PatientConsents.CountAsync(c => c.RevokedAt != null, cancellationToken);
        var openRequests = await dbContext.DataSubjectRequests.CountAsync(
            r => r.IsActive && r.Status != DataSubjectRequestStatus.Completed && r.Status != DataSubjectRequestStatus.Rejected, cancellationToken);
        var openIncidents = await dbContext.PrivacyIncidents.CountAsync(
            i => i.IsActive && i.Status != PrivacyIncidentStatus.Closed, cancellationToken);
        var failedLogins = await dbContext.LoginAttempts.CountAsync(
            l => !l.Success && l.CreatedAt >= since, cancellationToken);
        var activeSessions = await dbContext.UserSessions.CountAsync(
            s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow, cancellationToken);
        var mfaUsers = await dbContext.Users.CountAsync(u => u.IsActive && u.MfaEnabled, cancellationToken);

        return new ComplianceDashboardDto(
            activeConsents, revokedConsents, openRequests, openIncidents,
            failedLogins, activeSessions, mfaUsers);
    }

    public async Task<IReadOnlyList<LoginAttemptDto>> GetLoginAttemptsAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        return await dbContext.LoginAttempts.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .Select(l => new LoginAttemptDto(l.Id, l.Email, l.Success, l.FailureReason, l.IpAddress, l.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetSessionsAsync(
        bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserSessions.AsNoTracking().Include(s => s.User).AsQueryable();
        if (activeOnly)
        {
            query = query.Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(200)
            .Select(s => new UserSessionDto(
                s.Id, s.UserId, s.User.Email, s.User.FullName,
                s.CreatedAt, s.ExpiresAt, s.RevokedAt, s.IpAddress, s.UserAgent,
                s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Sessão não encontrada.");

        session.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConsentTermDto>> GetConsentTermsAsync(CancellationToken cancellationToken = default)
    {
        var terms = await dbContext.ConsentTerms.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return terms.Select(MapTerm).ToList();
    }

    public async Task<IReadOnlyList<ConsentTermDto>> GetCurrentConsentTermsAsync(CancellationToken cancellationToken = default)
    {
        var terms = await dbContext.ConsentTerms.AsNoTracking()
            .Where(t => t.IsActive && t.IsCurrent)
            .OrderBy(t => t.Title)
            .ToListAsync(cancellationToken);

        return terms.Select(MapTerm).ToList();
    }

    public async Task<ConsentTermDto> CreateConsentTermAsync(
        CreateConsentTermRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SetAsCurrent)
        {
            var current = await dbContext.ConsentTerms.Where(t => t.IsCurrent && t.IsActive).ToListAsync(cancellationToken);
            foreach (var term in current)
            {
                term.IsCurrent = false;
            }
        }

        var entity = new ConsentTerm
        {
            Version = request.Version.Trim(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            PurposesJson = JsonSerializer.Serialize(request.Purposes),
            EffectiveFrom = DateTime.UtcNow,
            IsCurrent = request.SetAsCurrent,
        };

        dbContext.ConsentTerms.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTerm(entity);
    }

    public async Task<IReadOnlyList<PatientConsentDto>> GetPatientConsentsAsync(
        Guid? patientId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PatientConsents.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.ConsentTerm)
            .Include(c => c.RecordedByUser)
            .Where(c => c.IsActive);

        if (patientId.HasValue)
        {
            query = query.Where(c => c.PatientId == patientId.Value);
        }

        var items = await query.OrderByDescending(c => c.GrantedAt).Take(200).ToListAsync(cancellationToken);
        return items.Select(MapConsent).ToList();
    }

    public async Task<PatientConsentStatusDto> GetPatientConsentStatusAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var currentTerms = await dbContext.ConsentTerms.AsNoTracking()
            .Where(t => t.IsActive && t.IsCurrent)
            .OrderBy(t => t.Title)
            .ToListAsync(cancellationToken);

        var activeConsents = await dbContext.PatientConsents.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.ConsentTerm)
            .Include(c => c.RecordedByUser)
            .Where(c => c.IsActive && c.PatientId == patientId && c.RevokedAt == null)
            .OrderByDescending(c => c.GrantedAt)
            .ToListAsync(cancellationToken);

        var signedTermIds = activeConsents
            .Where(c => c.AcknowledgedAt != null && !string.IsNullOrWhiteSpace(c.SignatureImage))
            .Select(c => c.ConsentTermId)
            .ToHashSet();

        var pendingTerms = currentTerms
            .Where(t => !signedTermIds.Contains(t.Id))
            .Select(MapTerm)
            .ToList();

        return new PatientConsentStatusDto(
            patientId,
            pendingTerms,
            activeConsents.Select(MapConsent).ToList());
    }

    public async Task<PatientConsentDetailDto> GetPatientConsentDetailAsync(
        Guid consentId, CancellationToken cancellationToken = default)
    {
        var consent = await dbContext.PatientConsents.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.ConsentTerm)
            .Include(c => c.RecordedByUser)
            .FirstOrDefaultAsync(c => c.Id == consentId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Consentimento não encontrado.");

        return MapConsentDetail(consent);
    }

    public async Task<PatientConsentDto> RecordConsentAsync(
        RecordPatientConsentRequest request, Guid? userId, string? ip, CancellationToken cancellationToken = default)
        => await CreatePatientConsentAsync(
            request.PatientId,
            request.ConsentTermId,
            request.Purposes,
            request.ReadAt,
            request.AcknowledgedAt,
            request.SignerName,
            request.SignatureImage,
            request.Notes,
            userId,
            ip,
            cancellationToken);

    public async Task<PatientConsentDto> SignPatientConsentAsync(
        Guid patientId,
        SignPatientConsentRequest request,
        Guid? userId,
        string? ip,
        CancellationToken cancellationToken = default)
        => await CreatePatientConsentAsync(
            patientId,
            request.ConsentTermId,
            request.Purposes,
            request.ReadAt,
            request.AcknowledgedAt,
            request.SignerName,
            request.SignatureImage,
            request.Notes,
            userId,
            ip,
            cancellationToken);

    private async Task<PatientConsentDto> CreatePatientConsentAsync(
        Guid patientId,
        Guid consentTermId,
        IReadOnlyList<string> purposes,
        DateTime readAt,
        DateTime acknowledgedAt,
        string signerName,
        string signatureImage,
        string? notes,
        Guid? userId,
        string? ip,
        CancellationToken cancellationToken)
    {
        ValidateConsentSignature(readAt, acknowledgedAt, signerName, signatureImage);

        var term = await dbContext.ConsentTerms.FirstOrDefaultAsync(
            t => t.Id == consentTermId && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Termo de consentimento não encontrado.");

        var alreadyActive = await dbContext.PatientConsents.AnyAsync(
            c => c.IsActive && c.RevokedAt == null && c.PatientId == patientId && c.ConsentTermId == term.Id,
            cancellationToken);
        if (alreadyActive)
        {
            throw new InvalidOperationException("Este paciente já possui consentimento ativo para este termo.");
        }

        var grantedAt = DateTime.UtcNow;
        var entity = new PatientConsent
        {
            PatientId = patientId,
            ConsentTermId = term.Id,
            PurposesJson = JsonSerializer.Serialize(purposes),
            ReadAt = readAt.ToUniversalTime(),
            AcknowledgedAt = acknowledgedAt.ToUniversalTime(),
            SignerName = signerName.Trim(),
            SignatureImage = signatureImage.Trim(),
            SignatureHash = ComputeConsentSignatureHash(patientId, term.Id, signerName, grantedAt),
            GrantedAt = grantedAt,
            IpAddress = ip,
            RecordedByUserId = userId,
            Notes = notes?.Trim(),
        };

        dbContext.PatientConsents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(entity).Reference(c => c.Patient).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(c => c.ConsentTerm).LoadAsync(cancellationToken);
        await dbContext.Entry(entity).Reference(c => c.RecordedByUser).LoadAsync(cancellationToken);

        return MapConsent(entity);
    }

    private static void ValidateConsentSignature(
        DateTime readAt, DateTime acknowledgedAt, string signerName, string signatureImage)
    {
        if (string.IsNullOrWhiteSpace(signerName))
        {
            throw new InvalidOperationException("Informe o nome de quem assina o consentimento.");
        }

        if (string.IsNullOrWhiteSpace(signatureImage) || !signatureImage.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A assinatura digital é obrigatória.");
        }

        if (acknowledgedAt < readAt)
        {
            throw new InvalidOperationException("A ciência deve ser registrada após a leitura do termo.");
        }
    }

    private string ComputeConsentSignatureHash(Guid patientId, Guid termId, string signerName, DateTime grantedAt)
        => encryption.HashForLookup($"{patientId:N}|{termId:N}|{signerName.Trim()}|{grantedAt:O}");

    public async Task RevokeConsentAsync(Guid consentId, CancellationToken cancellationToken = default)
    {
        var consent = await dbContext.PatientConsents.FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken)
            ?? throw new InvalidOperationException("Consentimento não encontrado.");

        consent.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataSubjectRequestDto>> GetSubjectRequestsAsync(
        DataSubjectRequestStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.DataSubjectRequests.AsNoTracking()
            .Include(r => r.Patient)
            .Include(r => r.HandledByUser)
            .Where(r => r.IsActive);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var items = await query.OrderByDescending(r => r.RequestedAt).Take(200).ToListAsync(cancellationToken);
        return items.Select(MapSubjectRequest).ToList();
    }

    public async Task<DataSubjectRequestDto> CreateSubjectRequestAsync(
        CreateDataSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new DataSubjectRequest
        {
            PatientId = request.PatientId,
            RequestType = request.RequestType,
            Details = request.Details?.Trim(),
            RequestedAt = DateTime.UtcNow,
        };

        dbContext.DataSubjectRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(entity).Reference(r => r.Patient).LoadAsync(cancellationToken);
        return MapSubjectRequest(entity);
    }

    public async Task<DataSubjectRequestDto> UpdateSubjectRequestAsync(
        Guid id, UpdateDataSubjectRequestStatus request, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DataSubjectRequests
            .Include(r => r.Patient)
            .Include(r => r.HandledByUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Solicitação não encontrada.");

        entity.Status = request.Status;
        entity.ResponseNotes = request.ResponseNotes?.Trim();
        entity.HandledByUserId = userId;
        if (request.Status is DataSubjectRequestStatus.Completed or DataSubjectRequestStatus.Rejected)
        {
            entity.CompletedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSubjectRequest(entity);
    }

    public async Task<LgpdSubjectDataExportDto> ExportSubjectRequestAsync(
        Guid id, Guid userId, string exportedByName, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.DataSubjectRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Solicitação não encontrada.");

        var patient = await dbContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        PatientFieldProtection.Decrypt(patient, encryption);

        var consents = await GetPatientConsentsAsync(request.PatientId, cancellationToken);

        var record = await dbContext.MedicalRecords.AsNoTracking()
            .Include(r => r.Entries.OrderByDescending(e => e.CreatedAt))
                .ThenInclude(e => e.Professional)
            .FirstOrDefaultAsync(r => r.PatientId == request.PatientId, cancellationToken);

        LgpdMedicalRecordExportDto? medicalExport = null;
        if (record is not null)
        {
            var entries = record.Entries
                .Take(500)
                .Select(e => new LgpdMedicalRecordEntryExportDto(
                    e.Id,
                    e.EntryType.ToString(),
                    e.Content,
                    e.Cid10Code,
                    e.CreatedAt,
                    e.Professional?.FullName))
                .ToList();

            medicalExport = new LgpdMedicalRecordExportDto(record.RecordNumber, entries);
        }

        return new LgpdSubjectDataExportDto(
            request.Id,
            request.RequestType,
            request.PatientId,
            DateTime.UtcNow,
            exportedByName,
            MapPatientExport(patient),
            consents,
            medicalExport);
    }

    public async Task<IReadOnlyList<PrivacyIncidentDto>> GetPrivacyIncidentsAsync(CancellationToken cancellationToken = default)
    {
        var items = await dbContext.PrivacyIncidents.AsNoTracking()
            .Include(i => i.ReportedByUser)
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.DetectedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items.Select(MapIncident).ToList();
    }

    public async Task<PrivacyIncidentDto> CreatePrivacyIncidentAsync(
        CreatePrivacyIncidentRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        var entity = new PrivacyIncident
        {
            Title = request.Title.Trim(),
            IncidentType = request.IncidentType.Trim(),
            Severity = request.Severity,
            Description = request.Description.Trim(),
            ReportedByUserId = userId,
            DetectedAt = DateTime.UtcNow,
        };

        dbContext.PrivacyIncidents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(entity).Reference(i => i.ReportedByUser).LoadAsync(cancellationToken);
        return MapIncident(entity);
    }

    public async Task<PrivacyIncidentDto> UpdatePrivacyIncidentAsync(
        Guid id, UpdatePrivacyIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivacyIncidents
            .Include(i => i.ReportedByUser)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Incidente não encontrado.");

        entity.Status = request.Status;
        entity.InvestigationNotes = request.InvestigationNotes?.Trim();
        entity.NotificationNotes = request.NotificationNotes?.Trim();
        if (request.Status == PrivacyIncidentStatus.Closed)
        {
            entity.ResolvedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapIncident(entity);
    }

    private static ConsentTermDto MapTerm(ConsentTerm term) => new(
        term.Id, term.Version, term.Title, term.Content,
        ParsePurposes(term.PurposesJson), term.EffectiveFrom, term.IsCurrent);

    private static PatientConsentDto MapConsent(PatientConsent consent) => new(
        consent.Id, consent.PatientId, consent.Patient.FullName,
        consent.ConsentTerm.Version, consent.ConsentTerm.Title,
        ParsePurposes(consent.PurposesJson),
        consent.ReadAt, consent.AcknowledgedAt, consent.SignerName,
        !string.IsNullOrWhiteSpace(consent.SignatureImage),
        consent.GrantedAt, consent.RevokedAt,
        consent.IpAddress, consent.RecordedByUser?.FullName);

    private static PatientConsentDetailDto MapConsentDetail(PatientConsent consent) => new(
        consent.Id, consent.PatientId, consent.Patient.FullName,
        consent.ConsentTerm.Version, consent.ConsentTerm.Title, consent.ConsentTerm.Content,
        ParsePurposes(consent.PurposesJson),
        consent.ReadAt, consent.AcknowledgedAt, consent.SignerName,
        consent.SignatureImage, consent.SignatureHash,
        consent.GrantedAt, consent.RevokedAt,
        consent.IpAddress, consent.RecordedByUser?.FullName, consent.Notes);

    private static DataSubjectRequestDto MapSubjectRequest(DataSubjectRequest request) => new(
        request.Id, request.PatientId, request.Patient.FullName,
        request.RequestType, request.Status, request.Details,
        request.RequestedAt, request.CompletedAt,
        request.HandledByUser?.FullName, request.ResponseNotes);

    private static PrivacyIncidentDto MapIncident(PrivacyIncident incident) => new(
        incident.Id, incident.Title, incident.IncidentType, incident.Severity, incident.Status,
        incident.Description, incident.DetectedAt, incident.ResolvedAt,
        incident.ReportedByUser?.FullName, incident.InvestigationNotes, incident.NotificationNotes);

    private static LgpdPatientExportDto MapPatientExport(Patient patient) => new(
        patient.Id,
        patient.FullName,
        patient.SocialName,
        patient.Cpf,
        patient.Cns,
        patient.Rg,
        patient.BirthDate,
        patient.Gender.ToString(),
        patient.Email,
        patient.Phone,
        patient.MobilePhone,
        patient.AddressStreet,
        patient.AddressNumber,
        patient.AddressComplement,
        patient.AddressNeighborhood,
        patient.AddressCity,
        patient.AddressState,
        patient.AddressZipCode,
        patient.MotherName,
        patient.EmergencyContactName,
        patient.EmergencyContactPhone,
        patient.BloodType,
        patient.CreatedAt,
        patient.UpdatedAt);

    private static IReadOnlyList<string> ParsePurposes(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
