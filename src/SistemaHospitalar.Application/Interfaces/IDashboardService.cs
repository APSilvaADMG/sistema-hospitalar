using SistemaHospitalar.Application.DTOs.Dashboard;

namespace SistemaHospitalar.Application.Interfaces;

public interface IDashboardService
{
    Task<OperationalDashboardDto> GetOperationalDashboardAsync(
        Guid? userId,
        DateOnly? date = null,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default);
}
