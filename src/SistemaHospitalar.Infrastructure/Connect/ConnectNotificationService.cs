using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectNotificationService(
    AppDbContext db,
    IConnectRealtimeNotifier realtimeNotifier) : IConnectNotificationService
{
    public async Task<IReadOnlyList<ConnectNotificationDto>> ListAsync(
        Guid userId, bool? unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = db.ConnectNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.IsActive);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new ConnectNotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Category,
                n.IsRead,
                n.RelatedEntityType,
                n.RelatedEntityId,
                n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => db.ConnectNotifications.CountAsync(
            n => n.UserId == userId && n.IsActive && !n.IsRead,
            cancellationToken);

    public async Task<bool> MarkReadAsync(
        Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await db.ConnectNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.IsActive, cancellationToken);

        if (notification is null) return false;

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyCommSummaryChangedAsync(userId, cancellationToken);
        await realtimeNotifier.NotifyHubNotificationUpdatedAsync(userId, cancellationToken);
        return true;
    }

    public async Task<ConnectNotificationDto> CreateAsync(
        CreateConnectNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new ConnectNotification
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Category = request.Category,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
        };

        db.ConnectNotifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyConnectNotificationAsync(request.UserId, notification.Id, cancellationToken);
        await realtimeNotifier.NotifyCommSummaryChangedAsync(request.UserId, cancellationToken);
        await realtimeNotifier.NotifyHubNotificationUpdatedAsync(request.UserId, cancellationToken);

        return new ConnectNotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Category,
            notification.IsRead,
            notification.RelatedEntityType,
            notification.RelatedEntityId,
            notification.CreatedAt);
    }
}
