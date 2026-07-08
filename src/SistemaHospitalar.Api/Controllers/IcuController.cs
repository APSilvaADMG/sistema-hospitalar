using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Icu;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/icu")]
public class IcuController(IIcuService icuService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await icuService.GetDashboardAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]
    [HttpPost("vitals")]
    public async Task<IActionResult> RecordVitals([FromBody] RecordVitalSignsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await icuService.RecordVitalSignsAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("vitals/{hospitalizationId:guid}")]
    public async Task<IActionResult> GetVitalHistory(Guid hospitalizationId, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        => Ok(await icuService.GetVitalHistoryAsync(hospitalizationId, limit, cancellationToken));
}
