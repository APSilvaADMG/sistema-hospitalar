using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Notifications;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class UnifiedNotificationHubService(
    AppDbContext db,
    INotificationService notificationService,
    IConnectCommSummaryService commSummaryService,
    IPendencyService pendencyService,
    IConnectRealtimeNotifier realtimeNotifier) : IUnifiedNotificationHubService
{
    public async Task<HubSummaryDto> GetHubSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await pendencyService.SyncForUserAsync(userId, cancellationToken);

        var unreadSystem = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        var unreadConnect = await db.ConnectNotifications
            .CountAsync(n => n.UserId == userId && n.IsActive && !n.IsRead, cancellationToken);

        var unreadMail = 0;
        var unreadChat = 0;
        try
        {
            var comm = await commSummaryService.GetSummaryAsync(userId, cancellationToken);
            unreadMail = comm.UnreadMailCount;
            unreadChat = comm.UnreadChatCount;
            unreadConnect = Math.Max(unreadConnect, comm.UnreadNotificationCount);
        }
        catch
        {
            // Connect module may be unavailable for some users
        }

        var pendingGuides = await db.TissGuides
            .CountAsync(g => g.IsActive && g.Status == TissGuideStatus.Draft, cancellationToken);

        var pendencySummary = await pendencyService.GetSummaryAsync(userId, cancellationToken);
        var criticalCount = pendencySummary.Criticas
            + await CountCriticalNotificationsAsync(userId, cancellationToken);

        var status = criticalCount > 0 ? "red" : pendencySummary.Total > 0 || unreadSystem + unreadConnect > 0 ? "yellow" : "green";

        var items = await BuildMixedItemsAsync(userId, cancellationToken);

        return new HubSummaryDto(
            unreadSystem + unreadConnect,
            unreadMail,
            unreadChat,
            pendingGuides,
            pendencySummary.Total,
            criticalCount,
            status,
            items);
    }

    public Task NotifyHubUpdatedAsync(Guid userId, CancellationToken cancellationToken = default)
        => realtimeNotifier.NotifyHubNotificationUpdatedAsync(userId, cancellationToken);

    private async Task<int> CountCriticalNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var systemCritical = await db.Notifications
            .CountAsync(n => n.UserId == userId && n.IsActive && !n.IsRead
                             && (n.Type == NotificationType.Alert || n.Type == NotificationType.Warning),
                cancellationToken);

        var connectCritical = await db.ConnectNotifications
            .CountAsync(n => n.UserId == userId && n.IsActive && !n.IsRead
                             && n.Category == ConnectNotificationCategory.Alert,
                cancellationToken);

        return systemCritical + connectCritical;
    }

    private async Task<IReadOnlyList<HubNotificationItemDto>> BuildMixedItemsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var systemItems = await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.IsActive)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new HubNotificationItemDto(
                n.Id,
                "system",
                n.Title,
                n.Message,
                n.Type.ToString(),
                n.IsRead,
                n.RelatedEntityType != null ? $"/notificacoes" : "/notificacoes",
                MapNotificationPriority(n.Type),
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        var connectItems = await db.ConnectNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.IsActive)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new HubNotificationItemDto(
                n.Id,
                "connect",
                n.Title,
                n.Message,
                n.Category.ToString(),
                n.IsRead,
                "/connect",
                n.Category == ConnectNotificationCategory.Alert ? "critical" : "normal",
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return systemItems
            .Concat(connectItems)
            .OrderByDescending(i => i.CreatedAt)
            .Take(20)
            .ToList();
    }

    private static string MapNotificationPriority(NotificationType type) => type switch
    {
        NotificationType.Alert => "critical",
        NotificationType.Warning => "high",
        _ => "normal",
    };
}
