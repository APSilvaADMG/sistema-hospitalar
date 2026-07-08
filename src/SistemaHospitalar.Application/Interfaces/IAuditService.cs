using SistemaHospitalar.Application.DTOs.Audit;

namespace SistemaHospitalar.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> GetLogsAsync(int limit, string? entityType, CancellationToken cancellationToken = default);
}
