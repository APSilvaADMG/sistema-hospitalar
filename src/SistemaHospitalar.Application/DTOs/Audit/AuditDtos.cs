namespace SistemaHospitalar.Application.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    string UserEmail,
    string Action,
    string EntityType,
    Guid? EntityId,
    string Details,
    string? IpAddress,
    string? UserAgent,
    string? ActionCategory,
    bool IsSensitive,
    DateTime CreatedAt);

public record CreateAuditLogRequest(
    Guid? UserId,
    string UserEmail,
    string Action,
    string EntityType,
    Guid? EntityId,
    string Details,
    string? IpAddress,
    string? UserAgent = null,
    string? DeviceId = null,
    string? ActionCategory = null,
    bool IsSensitive = false,
    string? BeforeSnapshot = null,
    string? AfterSnapshot = null);
