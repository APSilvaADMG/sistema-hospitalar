using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    DateTime CreatedAt);

public record CreateNotificationRequest(
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    string? RelatedEntityType,
    Guid? RelatedEntityId);
