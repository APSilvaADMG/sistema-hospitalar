namespace SistemaHospitalar.Application.DTOs.Notifications;

public record HubNotificationItemDto(
    Guid Id,
    string Source,
    string Title,
    string Message,
    string Category,
    bool IsRead,
    string? LinkDestino,
    string Priority,
    DateTime CreatedAt);

public record HubSummaryDto(
    int UnreadNotifications,
    int UnreadMail,
    int UnreadChat,
    int PendingGuides,
    int PendingItemsCount,
    int CriticalCount,
    string Status,
    IReadOnlyList<HubNotificationItemDto> Items);

public record PendencyDto(
    Guid Id,
    string Titulo,
    string Descricao,
    string Modulo,
    string Tipo,
    string Status,
    string Prioridade,
    string? Responsavel,
    string? Setor,
    DateTime DataAbertura,
    DateTime? DataLimite,
    string? LinkDestino,
    Guid? UsuarioResponsavelId);

public record PendencySummaryDto(
    int Total,
    int Abertas,
    int Vencidas,
    int Criticas,
    IReadOnlyDictionary<string, int> PorModulo);
