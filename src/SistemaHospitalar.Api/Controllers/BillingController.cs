using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/billing")]
public class BillingController(IBillingDashboardService billingDashboardService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.BillingRead, PermissionCodes.ReportsRead)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await billingDashboardService.GetDashboardAsync(cancellationToken));
}
