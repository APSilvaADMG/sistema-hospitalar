using SistemaHospitalar.Application.DTOs.OfficialUpdates;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IOfficialUpdatesService
{
    Task<OfficialUpdatesDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<OfficialUpdatesDashboardDto> CheckAllAsync(string? triggeredBy, CancellationToken cancellationToken = default);

    Task<OfficialUpdateActionResultDto> UpdateSourceAsync(
        OfficialSourceType sourceType,
        string? triggeredBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntegrationLogDto>> GetLogsAsync(
        int take = 50,
        OfficialSourceType? sourceType = null,
        CancellationToken cancellationToken = default);
}
