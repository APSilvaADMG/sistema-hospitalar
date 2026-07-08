using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Security;

public record ComplianceDashboardDto(
    int ActiveConsents,
    int RevokedConsents,
    int OpenSubjectRequests,
    int OpenPrivacyIncidents,
    int FailedLogins24h,
    int ActiveSessions,
    int UsersWithMfa);

public record LoginAttemptDto(
    Guid Id,
    string Email,
    bool Success,
    string? FailureReason,
    string? IpAddress,
    DateTime CreatedAt);

public record UserSessionDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string? IpAddress,
    string? UserAgent,
    bool IsActive);

public record ConsentTermDto(
    Guid Id,
    string Version,
    string Title,
    string Content,
    IReadOnlyList<string> Purposes,
    DateTime EffectiveFrom,
    bool IsCurrent);

public record PatientConsentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string TermVersion,
    string TermTitle,
    IReadOnlyList<string> Purposes,
    DateTime? ReadAt,
    DateTime? AcknowledgedAt,
    string? SignerName,
    bool HasSignature,
    DateTime GrantedAt,
    DateTime? RevokedAt,
    string? IpAddress,
    string? RecordedByName);

public record CreateConsentTermRequest(
    string Version,
    string Title,
    string Content,
    IReadOnlyList<string> Purposes,
    bool SetAsCurrent);

public record RecordPatientConsentRequest(
    Guid PatientId,
    Guid ConsentTermId,
    IReadOnlyList<string> Purposes,
    DateTime ReadAt,
    DateTime AcknowledgedAt,
    string SignerName,
    string SignatureImage,
    string? Notes);

public record SignPatientConsentRequest(
    Guid ConsentTermId,
    IReadOnlyList<string> Purposes,
    DateTime ReadAt,
    DateTime AcknowledgedAt,
    string SignerName,
    string SignatureImage,
    string? Notes);

public record PatientConsentStatusDto(
    Guid PatientId,
    IReadOnlyList<ConsentTermDto> PendingTerms,
    IReadOnlyList<PatientConsentDto> ActiveConsents);

public record PatientConsentDetailDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string TermVersion,
    string TermTitle,
    string TermContent,
    IReadOnlyList<string> Purposes,
    DateTime? ReadAt,
    DateTime? AcknowledgedAt,
    string? SignerName,
    string? SignatureImage,
    string? SignatureHash,
    DateTime GrantedAt,
    DateTime? RevokedAt,
    string? IpAddress,
    string? RecordedByName,
    string? Notes);

public record DataSubjectRequestDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    DataSubjectRequestType RequestType,
    DataSubjectRequestStatus Status,
    string? Details,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    string? HandledByName,
    string? ResponseNotes);

public record CreateDataSubjectRequest(
    Guid PatientId,
    DataSubjectRequestType RequestType,
    string? Details);

public record UpdateDataSubjectRequestStatus(
    DataSubjectRequestStatus Status,
    string? ResponseNotes);

public record LgpdMedicalRecordEntryExportDto(
    Guid Id,
    string EntryType,
    string Content,
    string? Cid10Code,
    DateTime CreatedAt,
    string? ProfessionalName);

public record LgpdMedicalRecordExportDto(
    string RecordNumber,
    IReadOnlyList<LgpdMedicalRecordEntryExportDto> Entries);

public record LgpdPatientExportDto(
    Guid Id,
    string FullName,
    string? SocialName,
    string Cpf,
    string? Cns,
    string? Rg,
    DateOnly BirthDate,
    string Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? MotherName,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BloodType,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record LgpdSubjectDataExportDto(
    Guid RequestId,
    DataSubjectRequestType RequestType,
    Guid PatientId,
    DateTime ExportedAt,
    string ExportedBy,
    LgpdPatientExportDto Patient,
    IReadOnlyList<PatientConsentDto> Consents,
    LgpdMedicalRecordExportDto? MedicalRecord);

public record PrivacyIncidentDto(
    Guid Id,
    string Title,
    string IncidentType,
    PrivacyIncidentSeverity Severity,
    PrivacyIncidentStatus Status,
    string Description,
    DateTime DetectedAt,
    DateTime? ResolvedAt,
    string? ReportedByName,
    string? InvestigationNotes,
    string? NotificationNotes);

public record CreatePrivacyIncidentRequest(
    string Title,
    string IncidentType,
    PrivacyIncidentSeverity Severity,
    string Description);

public record UpdatePrivacyIncidentRequest(
    PrivacyIncidentStatus Status,
    string? InvestigationNotes,
    string? NotificationNotes);

public record MfaSetupResponse(string Secret, string QrCodeUri, string ManualEntryKey);

public record MfaVerifyRequest(string Code);

public record MfaLoginVerifyRequest(string MfaToken, string Code);
