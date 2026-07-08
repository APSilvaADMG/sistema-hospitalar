using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IConnectMailService
{
    Task<IReadOnlyList<MailListItemDto>> ListAsync(Guid userId, MailFolder folder, string? search, CancellationToken cancellationToken = default);
    Task<MailDetailDto?> GetAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
    Task<MailDetailDto> CreateAsync(Guid userId, CreateMailRequest request, CancellationToken cancellationToken = default);
    Task<MailDetailDto?> UpdateDraftAsync(Guid userId, Guid messageId, UpdateMailRequest request, CancellationToken cancellationToken = default);
    Task<MailDetailDto?> SendDraftAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
    Task<bool> TrashAsync(Guid userId, Guid messageId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string FileName, string MimeType)?> GetAttachmentAsync(
        Guid userId, Guid messageId, Guid attachmentId, CancellationToken cancellationToken = default);
}

public interface IConnectChatService
{
    Task<IReadOnlyList<ChatRoomDto>> ListRoomsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatRoomDto?> CreateRoomAsync(Guid userId, CreateChatRoomRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid userId, Guid roomId, int limit = 50, CancellationToken cancellationToken = default);
    Task<ChatMessageDto?> SendMessageAsync(Guid userId, Guid roomId, SendChatMessageRequest request, CancellationToken cancellationToken = default);
    Task<bool> MarkRoomReadAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IConnectNotificationService
{
    Task<IReadOnlyList<ConnectNotificationDto>> ListAsync(Guid userId, bool? unreadOnly, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<ConnectNotificationDto> CreateAsync(CreateConnectNotificationRequest request, CancellationToken cancellationToken = default);
}

public interface IBulletinService
{
    Task<IReadOnlyList<BulletinPostDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BulletinPostDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<BulletinPostDto> CreateAsync(Guid userId, CreateBulletinPostRequest request, CancellationToken cancellationToken = default);
    Task<BulletinPostDto?> UpdateAsync(Guid userId, Guid id, UpdateBulletinPostRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkViewedAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
    Task<int> GetUnviewedCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IConnectCommSummaryService
{
    Task<ConnectCommSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
