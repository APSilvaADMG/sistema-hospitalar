using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/command-center")]
public class CommandCenterController(ICommandCenterService commandCenterService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage, PermissionCodes.PatientsRead)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await commandCenterService.GetDashboardAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage, PermissionCodes.PatientsRead)]
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(CancellationToken cancellationToken)
        => Ok(await commandCenterService.GetQueueSnapshotAsync(cancellationToken));
}
