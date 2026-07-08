using SistemaHospitalar.Application.DTOs.Bi;

namespace SistemaHospitalar.Application.Interfaces;

public interface IBiService
{
    Task<BiDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
