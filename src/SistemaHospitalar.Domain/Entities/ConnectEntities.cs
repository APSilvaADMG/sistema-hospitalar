using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ConnectConversation : BaseEntity
{
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public ConnectChannel Channel { get; set; } = ConnectChannel.WhatsApp;
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public ConnectBotStep BotStep { get; set; } = ConnectBotStep.MainMenu;
    public string? BotContextJson { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public ConnectInboxQueue Queue { get; set; } = ConnectInboxQueue.None;
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public DateTime? HumanRequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    /// <summary>LGPD: paciente solicitou não receber mensagens proativas via WhatsApp.</summary>
    public bool WhatsAppOptOut { get; set; }
    public DateTime? WhatsAppOptOutAt { get; set; }
    /// <summary>Primeiro contato inbound — base de consentimento implícito para atendimento.</summary>
    public DateTime? WhatsAppConsentAt { get; set; }
    public ICollection<ConnectMessage> Messages { get; set; } = [];
}

public class ConnectMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public ConnectConversation Conversation { get; set; } = null!;
    public ConnectMessageDirection Direction { get; set; }
    public ConnectMessageStatus Status { get; set; } = ConnectMessageStatus.Pending;
    public string Body { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? FailureReason { get; set; }
    public Guid? SentByUserId { get; set; }
    public User? SentByUser { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public ConnectReminderType? ReminderType { get; set; }
}

public class ConnectScheduledMessage : BaseEntity
{
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public ConnectChannel Channel { get; set; } = ConnectChannel.WhatsApp;
    public ConnectReminderType ReminderType { get; set; }
    public DateTime ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsSent { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public class ConnectWaitlistEntry : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }
    public ConnectWaitlistStatus Status { get; set; } = ConnectWaitlistStatus.Waiting;
    public int Priority { get; set; }
    public DateTime? OfferedAt { get; set; }
    public DateTime? OfferedSlotAt { get; set; }
    public Guid? OfferedProfessionalId { get; set; }
}

public class ConnectCheckIn : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateTime CheckedInAt { get; set; }
    public string? UpdatedPhone { get; set; }
    public string? UpdatedAddress { get; set; }
    public string? UpdatedInsuranceNotes { get; set; }
}

public class ConnectSatisfactionSurvey : BaseEntity
{
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
}

public class ConnectKnowledgeArticle : BaseEntity
{
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Keywords { get; set; }
}
