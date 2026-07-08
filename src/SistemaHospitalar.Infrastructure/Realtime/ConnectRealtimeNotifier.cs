using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Realtime;
public class ConnectRealtimeNotifier(
    IHubContext<ConnectHub> hubContext,
    ILogger<ConnectRealtimeNotifier> logger) : IConnectRealtimeNotifier
{
    public Task NotifyMessageReceivedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectMessageReceived", new { conversationId, messageId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyMessageSentAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectMessageSent", new { conversationId, messageId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyConversationUpdatedAsync(Guid conversationId, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectConversationUpdated", new { conversationId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyAwaitingHumanAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return BroadcastAsync("connectAwaitingHuman", new { conversationId, at = DateTime.UtcNow }, cancellationToken);
    }

    public Task NotifyInboxSummaryChangedAsync(CancellationToken cancellationToken = default)
        => BroadcastAsync("connectInboxSummaryChanged", new { at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyChatMessageAsync(Guid roomId, ChatMessageDto message, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectChatMessage", new { roomId, message }, cancellationToken);

    public Task NotifyConnectNotificationAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
        => BroadcastToUserAsync(userId, "connectNotification", new { notificationId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyCommSummaryChangedAsync(Guid userId, CancellationToken cancellationToken = default)
        => BroadcastToUserAsync(userId, "connectCommSummaryChanged", new { at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyMailUpdatedAsync(Guid userId, Guid? messageId = null, CancellationToken cancellationToken = default)
        => BroadcastToUserAsync(userId, "connectMailUpdated", new { messageId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyTicketUpdatedAsync(Guid? ticketId = null, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectTicketUpdated", new { ticketId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyTaskUpdatedAsync(Guid? taskId = null, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectTaskUpdated", new { taskId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyCalendarUpdatedAsync(Guid? eventId = null, CancellationToken cancellationToken = default)
        => BroadcastAsync("connectCalendarUpdated", new { eventId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifySlaAlertAsync(Guid ticketId, Guid targetUserId, CancellationToken cancellationToken = default)
        => BroadcastToUserAsync(targetUserId, "connectSlaAlert", new { ticketId, at = DateTime.UtcNow }, cancellationToken);

    public Task NotifyHubNotificationUpdatedAsync(Guid userId, CancellationToken cancellationToken = default)
        => BroadcastToUserAsync(userId, "hubNotificationUpdated", new { at = DateTime.UtcNow }, cancellationToken);

    private async Task BroadcastToUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients
                .Group($"connect-user-{userId}")
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar evento Connect SignalR {EventName} para usuário {UserId}", eventName, userId);
        }
    }

    private async Task BroadcastToUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken)
        => await BroadcastToUserAsync(userId.ToString(), eventName, payload, cancellationToken);

    private async Task BroadcastAsync(string eventName, object payload, CancellationToken cancellationToken)    {
        try
        {
            await hubContext.Clients
                .Group(ConnectHub.InboxGroup)
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar evento Connect SignalR {EventName}", eventName);
        }
    }
}
