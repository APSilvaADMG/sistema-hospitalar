using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Connect;

public record ConnectCalendarEventListItemDto(
    Guid Id,
    string Titulo,
    string? Descricao,
    DateTime Inicio,
    DateTime Fim,
    string? Local,
    ConnectCalendarEventType Tipo,
    bool AllDay,
    ConnectCalendarRecurrenceRule RecurrenceRule,
    string? Color,
    int? ReminderMinutes,
    string OrganizadorName,
    Guid? SetorId,
    string? SetorName,
    int ParticipantCount,
    bool IsOrganizer,
    ConnectCalendarParticipantResponse? MyResponse,
    bool IsRecurrenceInstance = false);

public record ConnectCalendarParticipantDto(
    Guid UserId,
    string UserName,
    ConnectCalendarParticipantResponse? Response);

public record ConnectCalendarEventDetailDto(
    Guid Id,
    string Titulo,
    string? Descricao,
    DateTime Inicio,
    DateTime Fim,
    string? Local,
    ConnectCalendarEventType Tipo,
    bool AllDay,
    ConnectCalendarRecurrenceRule RecurrenceRule,
    string? Color,
    int? ReminderMinutes,
    Guid OrganizadorId,
    string OrganizadorName,
    Guid? SetorId,
    string? SetorName,
    IReadOnlyList<ConnectCalendarParticipantDto> Participants,
    bool IsOrganizer,
    ConnectCalendarParticipantResponse? MyResponse,
    DateTime CreatedAt);

public record CreateConnectCalendarEventRequest(
    string Titulo,
    string? Descricao,
    DateTime Inicio,
    DateTime Fim,
    string? Local,
    ConnectCalendarEventType Tipo,
    bool AllDay,
    ConnectCalendarRecurrenceRule RecurrenceRule,
    string? Color,
    int? ReminderMinutes,
    Guid? SetorId,
    IReadOnlyList<Guid>? ParticipantUserIds);

public record UpdateConnectCalendarEventRequest(
    string Titulo,
    string? Descricao,
    DateTime Inicio,
    DateTime Fim,
    string? Local,
    ConnectCalendarEventType Tipo,
    bool AllDay,
    ConnectCalendarRecurrenceRule RecurrenceRule,
    string? Color,
    int? ReminderMinutes,
    Guid? SetorId,
    IReadOnlyList<Guid>? ParticipantUserIds);

public record RespondCalendarEventRequest(ConnectCalendarParticipantResponse Response);

public record ConnectContextMessageDto(
    Guid Id,
    string Subject,
    string Content,
    MessagePriority Priority,
    string SenderName,
    DateTime CreatedAt,
    ConnectContextType ContextType,
    Guid ContextId,
    string? ContextLabel);

public record MailContextInputDto(
    Guid? PatientId,
    Guid? TissGuideId,
    Guid? SusGuideId,
    Guid? AppointmentId,
    Guid? TicketId);

public record ConnectAiAskRequest(string Question);

public record ConnectAiAskResponse(
    string Question,
    string Answer,
    string Intent,
    IReadOnlyDictionary<string, object>? Data,
    bool UsedLlm = false);

public record ConnectAiQuickQueryDto(string Id, string Label, string Question);

public record ConnectAiStreamChunk(
    string Type,
    string? Text = null,
    string? Intent = null,
    bool? UsedLlm = null,
    IReadOnlyDictionary<string, object>? Data = null);
