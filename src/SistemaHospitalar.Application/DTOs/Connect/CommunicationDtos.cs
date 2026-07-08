using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Connect;

public record ConnectCommSummaryDto(
    int UnreadMailCount,
    int UnreadChatCount,
    int UnreadNotificationCount,
    int UnviewedBulletinCount);

public record MailListItemDto(
    Guid Id,
    string Subject,
    string Preview,
    MessagePriority Priority,
    string SenderName,
    DateTime CreatedAt,
    bool IsRead,
    int AttachmentCount);

public record MailRecipientInputDto(Guid UserId, MessageRecipientType Type);

public record MailAttachmentInputDto(
    string FileName,
    string? ContentBase64,
    string MimeType,
    long SizeBytes);

public record CreateMailRequest(
    string Subject,
    string Content,
    MessagePriority Priority,
    IReadOnlyList<MailRecipientInputDto> Recipients,
    IReadOnlyList<MailAttachmentInputDto>? Attachments,
    bool SendNow,
    MailContextInputDto? Context = null);

public record UpdateMailRequest(
    string Subject,
    string Content,
    MessagePriority Priority,
    IReadOnlyList<MailRecipientInputDto> Recipients,
    IReadOnlyList<MailAttachmentInputDto>? Attachments);

public record MailDetailDto(
    Guid Id,
    string Subject,
    string Content,
    MessagePriority Priority,
    InternalMessageStatus Status,
    Guid SenderId,
    string SenderName,
    DateTime CreatedAt,
    bool IsRead,
    DateTime? ReadAt,
    MailFolder Folder,
    IReadOnlyList<MailRecipientDto> Recipients,
    IReadOnlyList<MailAttachmentDto> Attachments);

public record MailRecipientDto(Guid UserId, string UserName, MessageRecipientType Type, bool IsRead, DateTime? ReadAt);

public record MailAttachmentDto(Guid Id, string FileName, string MimeType, long SizeBytes);

public record ChatRoomDto(
    Guid Id,
    string Name,
    ChatRoomType RoomType,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    int UnreadCount);

public record CreateChatRoomRequest(
    string Name,
    ChatRoomType RoomType,
    Guid? SectorId,
    IReadOnlyList<Guid> ParticipantUserIds);

public record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderId,
    string SenderName,
    string Content,
    DateTime CreatedAt,
    bool IsRead);

public record SendChatMessageRequest(string Content);

public record ConnectNotificationDto(
    Guid Id,
    string Title,
    string Message,
    ConnectNotificationCategory Category,
    bool IsRead,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    DateTime CreatedAt);

public record CreateConnectNotificationRequest(
    Guid UserId,
    string Title,
    string Message,
    ConnectNotificationCategory Category,
    string? RelatedEntityType,
    Guid? RelatedEntityId);

public record BulletinPostDto(
    Guid Id,
    string Title,
    string Content,
    string AuthorName,
    DateTime? PublishedAt,
    bool IsPinned,
    bool IsViewed,
    int ViewCount,
    DateTime CreatedAt);

public record CreateBulletinPostRequest(string Title, string Content, bool IsPinned, bool PublishNow);

public record UpdateBulletinPostRequest(string Title, string Content, bool IsPinned, bool PublishNow);
