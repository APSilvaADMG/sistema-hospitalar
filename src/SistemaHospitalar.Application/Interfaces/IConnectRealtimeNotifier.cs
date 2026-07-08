using SistemaHospitalar.Application.DTOs.Connect;

namespace SistemaHospitalar.Application.Interfaces;

public interface IConnectRealtimeNotifier
{
    Task NotifyMessageReceivedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);
    Task NotifyMessageSentAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);
    Task NotifyConversationUpdatedAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task NotifyAwaitingHumanAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task NotifyInboxSummaryChangedAsync(CancellationToken cancellationToken = default);
    Task NotifyChatMessageAsync(Guid roomId, ChatMessageDto message, CancellationToken cancellationToken = default);
    Task NotifyConnectNotificationAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task NotifyCommSummaryChangedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task NotifyMailUpdatedAsync(Guid userId, Guid? messageId = null, CancellationToken cancellationToken = default);
    Task NotifyTicketUpdatedAsync(Guid? ticketId = null, CancellationToken cancellationToken = default);
    Task NotifyTaskUpdatedAsync(Guid? taskId = null, CancellationToken cancellationToken = default);
    Task NotifyCalendarUpdatedAsync(Guid? eventId = null, CancellationToken cancellationToken = default);
    Task NotifySlaAlertAsync(Guid ticketId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task NotifyHubNotificationUpdatedAsync(Guid userId, CancellationToken cancellationToken = default);
}