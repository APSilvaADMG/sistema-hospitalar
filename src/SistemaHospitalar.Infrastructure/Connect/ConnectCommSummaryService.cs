using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Connect;

public class ConnectCommSummaryService(
    IConnectMailService mailService,
    IConnectChatService chatService,
    IConnectNotificationService notificationService,
    IBulletinService bulletinService) : IConnectCommSummaryService
{
    public async Task<ConnectCommSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadMail = await mailService.GetUnreadCountAsync(userId, cancellationToken);
        var unreadChat = await chatService.GetUnreadCountAsync(userId, cancellationToken);
        var unreadNotifications = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        var unviewedBulletin = await bulletinService.GetUnviewedCountAsync(userId, cancellationToken);

        return new ConnectCommSummaryDto(
            unreadMail,
            unreadChat,
            unreadNotifications,
            unviewedBulletin);
    }
}
