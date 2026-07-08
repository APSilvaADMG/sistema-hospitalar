using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Connect;

public record ConnectDashboardDto(
    int ActiveConversations,
    int MessagesToday,
    int PendingReminders,
    int WaitlistWaiting,
    int SurveysThisMonth,
    double AverageNps,
    int CheckInsToday);

public record ConnectIntegrationStatusDto(
    bool WhatsAppEnabled,
    bool UseMockProvider,
    string ProviderName,
    bool MetaConfigured,
    bool WebhookSecretConfigured,
    bool VerifyTokenConfigured,
    bool LiveMode,
    bool Ready,
    string WebhookPath,
    int OverdueAccounts,
    int CollectionRemindersSentToday,
    bool CollectionAgentEnabled,
    string? PublicWebhookUrl,
    string TemplateLanguageCode,
    string ReminderTemplateName,
    string BillingTemplateName,
    string ConfirmationTemplateName,
    int FailedMessagesToday,
    IReadOnlyList<string> HealthIssues);

public record ConnectInboxSummaryDto(
    int AwaitingHuman,
    int AssignedOpen,
    int MessagesToday,
    int FailedMessagesToday);

public record ConnectReplyRequest(string Body);

public record ConnectAssignRequest(Guid? UserId, ConnectInboxQueue? Queue);

public record ConnectConversationQuery(
    int Limit = 50,
    ConnectBotStep? BotStep = null,
    ConnectInboxQueue? Queue = null,
    bool AwaitingHumanOnly = false);

public record ConnectRoadmapItemDto(
    int Priority,
    string Title,
    string Status,
    string Description);

public record ConnectConversationDto(
    Guid Id,
    Guid? PatientId,
    string? PatientName,
    ConnectChannel Channel,
    string ContactPhone,
    ConnectBotStep BotStep,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    ConnectInboxQueue Queue,
    Guid? AssignedUserId,
    string? AssignedUserName,
    DateTime? HumanRequestedAt,
    DateTime? ResolvedAt);

public record ConnectMessageDto(
    Guid Id,
    Guid ConversationId,
    ConnectMessageDirection Direction,
    ConnectMessageStatus Status,
    string Body,
    DateTime CreatedAt,
    ConnectReminderType? ReminderType);

public record ConnectWaitlistDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string SpecialtyName,
    string? ProfessionalName,
    ConnectWaitlistStatus Status,
    int Priority,
    DateTime CreatedAt);

public record ConnectKnowledgeArticleDto(
    Guid Id,
    string Category,
    string Question,
    string Answer,
    string? Keywords);

public record ConnectSatisfactionStatsDto(
    double AverageScore,
    int TotalResponses,
    IReadOnlyList<ConnectSatisfactionByGroupDto> ByProfessional,
    IReadOnlyList<ConnectSatisfactionByGroupDto> BySpecialty);

public record ConnectSatisfactionByGroupDto(string Name, double AverageScore, int Count);

public record SimulateInboundRequest(string Phone, string Message, string? ContactName);

public record SimulateInboundResponse(string Reply, Guid ConversationId);

public record BlockProfessionalScheduleRequest(
    Guid ProfessionalId,
    DateOnly Date,
    string Reason);

public record BlockProfessionalScheduleResult(int AffectedAppointments, int NotificationsSent);

public record JoinWaitlistRequest(Guid PatientId, Guid SpecialtyId, Guid? ProfessionalId);

public record AvailableSlotDto(
    DateTime ScheduledAt,
    Guid ProfessionalId,
    string ProfessionalName,
    string SpecialtyName);

public record ConnectConversationDetailDto(
    ConnectConversationDto Conversation,
    IReadOnlyList<ConnectMessageDto> Messages);
