using SistemaHospitalar.Application.DTOs.Connect;

namespace SistemaHospitalar.Application.Interfaces;

public interface IConnectCalendarService
{
    Task<IReadOnlyList<ConnectCalendarEventListItemDto>> ListByRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        string scope,
        CancellationToken cancellationToken = default);

    Task<ConnectCalendarEventDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<ConnectCalendarEventDetailDto> CreateAsync(
        Guid userId,
        CreateConnectCalendarEventRequest request,
        CancellationToken cancellationToken = default);

    Task<ConnectCalendarEventDetailDto?> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateConnectCalendarEventRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<ConnectCalendarEventDetailDto?> RespondAsync(
        Guid userId,
        Guid id,
        RespondCalendarEventRequest request,
        CancellationToken cancellationToken = default);
}

public interface IConnectContextService
{
    Task<IReadOnlyList<ConnectContextMessageDto>> ListPatientMessagesAsync(
        Guid userId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectContextMessageDto>> ListGuideMessagesAsync(
        Guid userId,
        Guid guideId,
        string guideType,
        CancellationToken cancellationToken = default);

    Task LinkMessageContextAsync(
        Guid messageId,
        MailContextInputDto context,
        CancellationToken cancellationToken = default);
}

public interface IConnectAiAssistantService
{
    Task<IReadOnlyList<ConnectAiQuickQueryDto>> GetQuickQueriesAsync(CancellationToken cancellationToken = default);

    Task<ConnectAiAskResponse> AskAsync(Guid userId, ConnectAiAskRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ConnectAiStreamChunk> AskStreamAsync(
        Guid userId,
        ConnectAiAskRequest request,
        CancellationToken cancellationToken = default);
}
