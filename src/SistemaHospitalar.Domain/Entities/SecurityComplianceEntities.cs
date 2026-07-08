using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class PermissionDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public UserRole Role { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public PermissionDefinition Permission { get; set; } = null!;
}

public class LoginAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Email { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? UserId { get; set; }
}

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceId { get; set; }
}

public class ConsentTerm : BaseEntity
{
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PurposesJson { get; set; } = "[]";
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; }
}

public class PatientConsent : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid ConsentTermId { get; set; }
    public ConsentTerm ConsentTerm { get; set; } = null!;
    public string PurposesJson { get; set; } = "[]";
    public DateTime? ReadAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? SignerName { get; set; }
    public string? SignatureImage { get; set; }
    public string? SignatureHash { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public string? Notes { get; set; }
}

public enum DataSubjectRequestType
{
    Access = 1,
    Rectification = 2,
    Portability = 3,
    Anonymization = 4,
    Revocation = 5,
    Erasure = 6,
}

public enum DataSubjectRequestStatus
{
    Open = 1,
    InReview = 2,
    Completed = 3,
    Rejected = 4,
}

public class DataSubjectRequest : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DataSubjectRequestType RequestType { get; set; }
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Open;
    public string? Details { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? HandledByUserId { get; set; }
    public User? HandledByUser { get; set; }
    public string? ResponseNotes { get; set; }
    public string? ExportFilePath { get; set; }
}

public enum PrivacyIncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

public enum PrivacyIncidentStatus
{
    Detected = 1,
    Investigating = 2,
    Contained = 3,
    Notified = 4,
    Closed = 5,
}

public class PrivacyIncident : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string IncidentType { get; set; } = string.Empty;
    public PrivacyIncidentSeverity Severity { get; set; } = PrivacyIncidentSeverity.Medium;
    public PrivacyIncidentStatus Status { get; set; } = PrivacyIncidentStatus.Detected;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ReportedByUserId { get; set; }
    public User? ReportedByUser { get; set; }
    public string? InvestigationNotes { get; set; }
    public string? NotificationNotes { get; set; }
}
