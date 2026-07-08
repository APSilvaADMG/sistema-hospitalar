using SistemaHospitalar.Application.DTOs.Hospitalization;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHospitalizationHubService
{
    Task<HospitalizationHubDashboardDto> GetDashboardAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<HospitalizationHubListResultDto> SearchAsync(
        HospitalizationHubFilterDto filter,
        CancellationToken cancellationToken = default);
}
