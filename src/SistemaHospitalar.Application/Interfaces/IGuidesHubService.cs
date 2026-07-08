using SistemaHospitalar.Application.DTOs.Guides;
using SistemaHospitalar.Application.DTOs.Tiss;

namespace SistemaHospitalar.Application.Interfaces;

public interface IGuidesHubService
{
    Task<GuidesHubDashboardDto> GetDashboardAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<GuidesHubListResultDto> SearchAsync(
        GuidesHubFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuideHistoryEntryDto>> GetHistoryAsync(
        Guid guideId,
        string? source = null,
        CancellationToken cancellationToken = default);

    Task<TissGuideDto?> DuplicateGuideAsync(
        Guid guideId,
        CancellationToken cancellationToken = default);
}
