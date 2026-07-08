using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Notifications;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class NotificationService(
    AppDbContext dbContext,
    IConnectRealtimeNotifier realtimeNotifier) : INotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        Guid userId, bool? unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.IsActive);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message, n.Type, n.IsRead,
                n.RelatedEntityType, n.RelatedEntityId, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .CountAsync(n => n.UserId == userId && n.IsActive && !n.IsRead, cancellationToken);
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = request.Type,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyHubNotificationUpdatedAsync(request.UserId, cancellationToken);

        return new NotificationDto(
            notification.Id, notification.Title, notification.Message, notification.Type,
            notification.IsRead, notification.RelatedEntityType, notification.RelatedEntityId,
            notification.CreatedAt);
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && n.IsActive, cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyHubNotificationUpdatedAsync(userId, cancellationToken);
        return true;
    }

    public async Task NotifyAdminsAsync(
        string title, string message, NotificationType type,
        string? entityType, Guid? entityId, CancellationToken cancellationToken = default)
    {
        var adminIds = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var adminId in adminIds)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = adminId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityType = entityType,
                RelatedEntityId = entityId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
