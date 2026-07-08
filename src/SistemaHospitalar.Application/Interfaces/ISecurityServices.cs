using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDefinitionDto>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolePermissionDto>> GetRoleMatrixAsync(CancellationToken cancellationToken = default);
}

public record PermissionDefinitionDto(string Code, string Name, string Module, string? Description);
public record RolePermissionDto(UserRole Role, string PermissionCode);

public interface IFieldEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    bool IsEncrypted(string value);
    string HashForLookup(string plaintext);
}

public interface ISecurityComplianceService
{
    Task<ComplianceDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoginAttemptDto>> GetLoginAttemptsAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSessionDto>> GetSessionsAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsentTermDto>> GetConsentTermsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsentTermDto>> GetCurrentConsentTermsAsync(CancellationToken cancellationToken = default);
    Task<ConsentTermDto> CreateConsentTermAsync(CreateConsentTermRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientConsentDto>> GetPatientConsentsAsync(Guid? patientId, CancellationToken cancellationToken = default);
    Task<PatientConsentStatusDto> GetPatientConsentStatusAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientConsentDetailDto> GetPatientConsentDetailAsync(Guid consentId, CancellationToken cancellationToken = default);
    Task<PatientConsentDto> RecordConsentAsync(RecordPatientConsentRequest request, Guid? userId, string? ip, CancellationToken cancellationToken = default);
    Task<PatientConsentDto> SignPatientConsentAsync(Guid patientId, SignPatientConsentRequest request, Guid? userId, string? ip, CancellationToken cancellationToken = default);
    Task RevokeConsentAsync(Guid consentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSubjectRequestDto>> GetSubjectRequestsAsync(DataSubjectRequestStatus? status, CancellationToken cancellationToken = default);
    Task<DataSubjectRequestDto> CreateSubjectRequestAsync(CreateDataSubjectRequest request, CancellationToken cancellationToken = default);
    Task<DataSubjectRequestDto> UpdateSubjectRequestAsync(Guid id, UpdateDataSubjectRequestStatus request, Guid userId, CancellationToken cancellationToken = default);
    Task<LgpdSubjectDataExportDto> ExportSubjectRequestAsync(Guid id, Guid userId, string exportedByName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrivacyIncidentDto>> GetPrivacyIncidentsAsync(CancellationToken cancellationToken = default);
    Task<PrivacyIncidentDto> CreatePrivacyIncidentAsync(CreatePrivacyIncidentRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<PrivacyIncidentDto> UpdatePrivacyIncidentAsync(Guid id, UpdatePrivacyIncidentRequest request, CancellationToken cancellationToken = default);
}
