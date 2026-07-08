using SistemaHospitalar.Application.DTOs.Connect;

namespace SistemaHospitalar.Application.Interfaces;

public interface IConnectService
{
    Task<ConnectDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectConversationDto>> GetConversationsAsync(ConnectConversationQuery query, CancellationToken cancellationToken = default);
    Task<ConnectConversationDetailDto?> GetConversationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectWaitlistDto>> GetWaitlistAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectKnowledgeArticleDto>> GetKnowledgeArticlesAsync(CancellationToken cancellationToken = default);
    Task<ConnectSatisfactionStatsDto> GetSatisfactionStatsAsync(CancellationToken cancellationToken = default);
    Task<SimulateInboundResponse> SimulateInboundAsync(SimulateInboundRequest request, CancellationToken cancellationToken = default);
    Task<BlockProfessionalScheduleResult> BlockProfessionalScheduleAsync(BlockProfessionalScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ConnectWaitlistDto> JoinWaitlistAsync(JoinWaitlistRequest request, CancellationToken cancellationToken = default);
    Task<ConnectIntegrationStatusDto> GetIntegrationStatusAsync(CancellationToken cancellationToken = default);
    Task<ConnectInboxSummaryDto> GetInboxSummaryAsync(CancellationToken cancellationToken = default);
    Task<ConnectMessageDto> ReplyAsync(Guid conversationId, ConnectReplyRequest request, Guid? staffUserId, CancellationToken cancellationToken = default);
    Task<ConnectConversationDto?> AssignConversationAsync(Guid conversationId, ConnectAssignRequest request, CancellationToken cancellationToken = default);
    Task<ConnectConversationDto?> ResolveConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

public interface IConnectBotService
{
    Task<string> ProcessInboundAsync(
        string phone,
        string message,
        string? contactName,
        string? externalMessageId = null,
        CancellationToken cancellationToken = default);
}

public sealed record WhatsAppSendResult(
    bool Success,
    string? ExternalId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface IWhatsAppProvider
{
    string ProviderName { get; }
    bool IsMock { get; }
    Task<WhatsAppSendResult> SendTextAsync(string phone, string body, CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendTemplateAsync(
        string phone,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default);
}

public interface IConnectAppointmentIntegration
{
    Task OnAppointmentCreatedAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task OnAppointmentStatusChangedAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task OnAppointmentCancelledAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
