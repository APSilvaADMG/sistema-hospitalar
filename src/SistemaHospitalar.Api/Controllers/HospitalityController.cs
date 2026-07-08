using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Hospitality;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/hospitality")]
public class HospitalityController(IHospitalityService hospitalityService) : ControllerBase
{
    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms(CancellationToken cancellationToken)
        => Ok(await hospitalityService.GetRoomsAsync(cancellationToken));

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(CancellationToken cancellationToken)
        => Ok(await hospitalityService.GetBookingsAsync(cancellationToken));

    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateHospitalityBookingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalityService.CreateBookingAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("bookings/{id:guid}/check-in")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        var result = await hospitalityService.CheckInAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("bookings/{id:guid}/check-out")]
    public async Task<IActionResult> CheckOut(Guid id, CancellationToken cancellationToken)
    {
        var result = await hospitalityService.CheckOutAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
