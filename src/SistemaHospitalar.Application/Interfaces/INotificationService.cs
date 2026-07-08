using SistemaHospitalar.Application.DTOs.Notifications;

namespace SistemaHospitalar.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, bool? unreadOnly, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task NotifyAdminsAsync(string title, string message, Domain.Enums.NotificationType type, string? entityType, Guid? entityId, CancellationToken cancellationToken = default);
}
