using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ConnectCalendarEvent : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string? Local { get; set; }
    public Guid OrganizadorId { get; set; }
    public User Organizador { get; set; } = null!;
    public ConnectCalendarEventType Tipo { get; set; } = ConnectCalendarEventType.Reuniao;
    public bool AllDay { get; set; }
    public ConnectCalendarRecurrenceRule RecurrenceRule { get; set; } = ConnectCalendarRecurrenceRule.None;
    public string? Color { get; set; }
    public int? ReminderMinutes { get; set; }
    public DateTime? LastReminderSentAt { get; set; }
    public DateTime? LastReminderOccurrenceStart { get; set; }
    public Guid? SetorId { get; set; }
    public Department? Setor { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ConnectCalendarParticipant> Participants { get; set; } = [];
}

public class ConnectCalendarParticipant : BaseEntity
{
    public Guid EventId { get; set; }
    public ConnectCalendarEvent Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ConnectCalendarParticipantResponse? Response { get; set; }
}

public class ConnectContextLink : BaseEntity
{
    public Guid? MessageId { get; set; }
    public InternalMessage? Message { get; set; }
    public Guid? ChatRoomId { get; set; }
    public ChatRoom? ChatRoom { get; set; }
    public Guid? TicketId { get; set; }
    public ConnectTicket? Ticket { get; set; }
    public ConnectContextType ContextType { get; set; }
    public Guid ContextId { get; set; }
}
