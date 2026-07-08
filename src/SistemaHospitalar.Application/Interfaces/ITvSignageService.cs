using SistemaHospitalar.Application.DTOs.TvSignage;

namespace SistemaHospitalar.Application.Interfaces;

public interface ITvSignageService
{
    Task<TvMonitorSummaryDto> GetMonitorSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvDisplayDto>> ListDisplaysAsync(CancellationToken cancellationToken = default);
    Task<TvDisplayDto?> GetDisplayAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TvDisplayDto> CreateDisplayAsync(CreateTvDisplayRequest request, CancellationToken cancellationToken = default);
    Task<TvDisplayDto?> UpdateDisplayAsync(Guid id, UpdateTvDisplayRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDisplayAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string?> RegenerateDisplayTokenAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvLayoutDto>> ListLayoutsAsync(CancellationToken cancellationToken = default);
    Task<TvLayoutDto?> GetLayoutAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TvLayoutDto> CreateLayoutAsync(CreateTvLayoutRequest request, CancellationToken cancellationToken = default);
    Task<TvLayoutDto?> UpdateLayoutAsync(Guid id, UpdateTvLayoutRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvMediaDto>> ListMediaAsync(CancellationToken cancellationToken = default);
    Task<TvMediaDto> UploadMediaAsync(CreateTvMediaRequest request, string fileName, byte[] content, CancellationToken cancellationToken = default);
    Task<bool> DeleteMediaAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvNewsDto>> ListNewsAsync(CancellationToken cancellationToken = default);
    Task<TvNewsDto> CreateNewsAsync(CreateTvNewsRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteNewsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvAnnouncementDto>> ListAnnouncementsAsync(CancellationToken cancellationToken = default);
    Task<TvAnnouncementDto> CreateAnnouncementAsync(CreateTvAnnouncementRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvQueueCallDto>> ListRecentCallsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<TvQueueCallDto> CallQueueAsync(CallTvQueueRequest request, CancellationToken cancellationToken = default);
    Task<TvQueueCallDto?> CallKioskTicketAsync(Guid kioskTicketId, CallKioskTicketRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TvCampaignDto>> ListCampaignsAsync(CancellationToken cancellationToken = default);
    Task<TvCampaignDto> CreateCampaignAsync(CreateTvCampaignRequest request, CancellationToken cancellationToken = default);
    Task<TvCampaignDto?> UpdateCampaignAsync(Guid id, UpdateTvCampaignRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCampaignAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TvPlayerStateDto?> GetPlayerStateAsync(string slug, string token, CancellationToken cancellationToken = default);
    Task<bool> RegisterHeartbeatAsync(string slug, string token, TvHeartbeatRequest request, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCallSpeechAsync(string slug, string token, Guid callId, CancellationToken cancellationToken = default);
    string GetSpeechProvider();
}

public interface ITvSignageRealtimeNotifier
{
    Task NotifyDisplayUpdatedAsync(Guid displayId, CancellationToken cancellationToken = default);
    Task NotifyQueueCallAsync(Guid? displayId, CancellationToken cancellationToken = default);
}

public interface ITvSignageMediaStorage
{
    Task<string> SaveAsync(Guid mediaId, string fileName, byte[] content, CancellationToken cancellationToken = default);
    void DeleteIfExists(string? storagePath);
}
