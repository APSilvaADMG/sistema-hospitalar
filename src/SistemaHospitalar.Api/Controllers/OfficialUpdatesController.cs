using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(
    PermissionCodes.IntegrationsManage,
    PermissionCodes.UsersManage,
    PermissionCodes.BillingWrite)]
[ApiController]
[Route("api/official-updates")]
public class OfficialUpdatesController(IOfficialUpdatesService officialUpdatesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await officialUpdatesService.GetDashboardAsync(cancellationToken));

    [HttpPost("check-all")]
    public async Task<IActionResult> CheckAll(CancellationToken cancellationToken)
        => Ok(await officialUpdatesService.CheckAllAsync(GetUserLabel(), cancellationToken));

    [HttpPost("update/{sourceType}")]
    public async Task<IActionResult> UpdateSource(string sourceType, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OfficialSourceType>(sourceType, ignoreCase: true, out var parsed))
            return BadRequest(new { message = $"Fonte inválida: {sourceType}" });

        return Ok(await officialUpdatesService.UpdateSourceAsync(parsed, GetUserLabel(), cancellationToken));
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int take = 50,
        [FromQuery] OfficialSourceType? sourceType = null,
        CancellationToken cancellationToken = default)
        => Ok(await officialUpdatesService.GetLogsAsync(take, sourceType, cancellationToken));

    private string GetUserLabel()
    {
        var name = User.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? "manual" : name;
    }
}
