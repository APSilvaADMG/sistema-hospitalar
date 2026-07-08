using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Emergency;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/emergency")]
public class EmergencyController(IEmergencyService emergencyService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits([FromQuery] EmergencyVisitStatus? status, CancellationToken cancellationToken)
        => Ok(await emergencyService.GetVisitsAsync(status, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsCreate, PermissionCodes.PepWrite)]
    [HttpPost("visits")]
    public async Task<IActionResult> CreateVisit([FromBody] CreateEmergencyVisitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await emergencyService.CreateVisitAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.PatientsUpdate, PermissionCodes.PepWrite)]
    [HttpPatch("visits/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateEmergencyVisitStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await emergencyService.UpdateStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
