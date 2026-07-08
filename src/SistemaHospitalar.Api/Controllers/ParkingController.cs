using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Parking;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/parking")]
public class ParkingController(IParkingService parkingService) : ControllerBase
{
    [HttpGet("zones")]
    public async Task<IActionResult> GetZones(CancellationToken cancellationToken)
        => Ok(await parkingService.GetZonesAsync(cancellationToken));

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
        => Ok(await parkingService.GetSessionsAsync(activeOnly, cancellationToken));

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInParkingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await parkingService.CheckInAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("pay")]
    public async Task<IActionResult> Pay([FromBody] PayParkingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await parkingService.PaySessionAsync(request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutParkingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await parkingService.CheckOutAsync(request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("gate/exit")]
    public async Task<IActionResult> GateExit([FromBody] ParkingGateExitRequest request, CancellationToken cancellationToken)
        => Ok(await parkingService.ProcessGateExitAsync(request, cancellationToken));
}
