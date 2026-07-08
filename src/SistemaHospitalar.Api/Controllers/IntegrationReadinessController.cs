using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public class IntegrationReadinessController(IIntegrationReadinessService readinessService) : ControllerBase
{
    [HttpGet("readiness")]
    [RequireAnyPermission(
        PermissionCodes.IntegrationsManage,
        PermissionCodes.ConnectRead,
        PermissionCodes.BillingRead,
        PermissionCodes.SecurityManage,
        PermissionCodes.UsersManage)]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
        => Ok(await readinessService.GetReadinessAsync(cancellationToken));

    [HttpPost("test/whatsapp")]
    [RequireAnyPermission(PermissionCodes.IntegrationsManage, PermissionCodes.ConnectAdmin, PermissionCodes.SecurityManage)]
    public async Task<IActionResult> TestWhatsApp(CancellationToken cancellationToken)
        => Ok(await readinessService.TestWhatsAppAsync(cancellationToken));

    [HttpPost("test/pix")]
    [RequireAnyPermission(PermissionCodes.IntegrationsManage, PermissionCodes.BillingWrite, PermissionCodes.SecurityManage)]
    public async Task<IActionResult> TestPix(CancellationToken cancellationToken)
        => Ok(await readinessService.TestPixAsync(cancellationToken));

    [HttpPost("test/tiss")]
    [RequireAnyPermission(PermissionCodes.IntegrationsManage, PermissionCodes.BillingWrite, PermissionCodes.SecurityManage)]
    public async Task<IActionResult> TestTiss([FromQuery] Guid? operatorId, CancellationToken cancellationToken)
        => Ok(await readinessService.TestTissAsync(operatorId, cancellationToken));
}
