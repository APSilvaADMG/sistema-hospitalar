using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class InternalMessage : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public InternalMessageStatus Status { get; set; } = InternalMessageStatus.Draft;
    public DateTime? DeletedAt { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? TissGuideId { get; set; }
    public TissGuide? TissGuide { get; set; }
    public Guid? SusGuideId { get; set; }
    public SusGuide? SusGuide { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public ICollection<InternalMessageRecipient> Recipients { get; set; } = [];
    public ICollection<InternalMessageAttachment> Attachments { get; set; } = [];
}

public class InternalMessageRecipient : BaseEntity
{
    public Guid MessageId { get; set; }
    public InternalMessage Message { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public MessageRecipientType RecipientType { get; set; } = MessageRecipientType.To;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsArchived { get; set; }
    public MailFolder Folder { get; set; } = MailFolder.Inbox;
}

public class InternalMessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public InternalMessage Message { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string? StoragePath { get; set; }
    public string? ContentBase64 { get; set; }
    public string MimeType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
}

public class ChatRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ChatRoomType RoomType { get; set; } = ChatRoomType.Private;
    public Guid? SectorId { get; set; }
    public Department? Sector { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = [];
    public ICollection<ChatMessage> Messages { get; set; } = [];
}

public class ChatParticipant : BaseEntity
{
    public Guid RoomId { get; set; }
    public ChatRoom Room { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime? LastReadAt { get; set; }
}

public class ChatMessage : BaseEntity
{
    public Guid RoomId { get; set; }
    public ChatRoom Room { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}

public class ConnectNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ConnectNotificationCategory Category { get; set; } = ConnectNotificationCategory.Info;
    public bool IsRead { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public class BulletinPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public DateTime? PublishedAt { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<BulletinView> Views { get; set; } = [];
}

public class BulletinView : BaseEntity
{
    public Guid BulletinId { get; set; }
    public BulletinPost Bulletin { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}

public class CommunicationAuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
}
