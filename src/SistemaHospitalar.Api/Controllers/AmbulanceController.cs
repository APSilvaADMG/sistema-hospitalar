using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Ambulance;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
[ApiController]
[Route("api/ambulance")]
public class AmbulanceController(IAmbulanceService ambulanceService) : ControllerBase
{
    [HttpGet("fleet")]
    public async Task<IActionResult> GetFleet(CancellationToken cancellationToken)
        => Ok(await ambulanceService.GetAmbulancesAsync(cancellationToken));

    [HttpGet("dispatches")]
    public async Task<IActionResult> GetDispatches([FromQuery] AmbulanceDispatchStatus? status, CancellationToken cancellationToken)
        => Ok(await ambulanceService.GetDispatchesAsync(status, cancellationToken));

    [HttpPost("dispatches")]
    public async Task<IActionResult> CreateDispatch([FromBody] CreateAmbulanceDispatchRequest request, CancellationToken cancellationToken)
        => Ok(await ambulanceService.CreateDispatchAsync(request, cancellationToken));

    [HttpPost("dispatches/{id:guid}/assign")]
    public async Task<IActionResult> AssignAmbulance(Guid id, [FromBody] AssignAmbulanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ambulanceService.AssignAmbulanceAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("dispatches/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDispatchStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await ambulanceService.UpdateDispatchStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
