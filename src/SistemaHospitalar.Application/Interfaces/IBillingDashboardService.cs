using SistemaHospitalar.Application.DTOs.Billing;

namespace SistemaHospitalar.Application.Interfaces;

public interface IBillingDashboardService
{
    Task<BillingDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
