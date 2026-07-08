using SistemaHospitalar.Application.DTOs.Notifications;

namespace SistemaHospitalar.Application.Interfaces;

public interface IUnifiedNotificationHubService
{
    Task<HubSummaryDto> GetHubSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task NotifyHubUpdatedAsync(Guid userId, CancellationToken cancellationToken = default);
}
