using SistemaHospitalar.Application.DTOs.Notifications;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPendencyService
{
    Task SyncForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendencyDto>> ListForUserAsync(
        Guid userId,
        string? modulo = null,
        string? status = null,
        CancellationToken cancellationToken = default);
    Task<PendencySummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
