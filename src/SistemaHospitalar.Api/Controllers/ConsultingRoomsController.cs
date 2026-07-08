using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.ConsultingRooms;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/consulting-rooms")]
public class ConsultingRoomsController(IConsultingRoomService consultingRoomService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRooms(CancellationToken cancellationToken)
        => Ok(await consultingRoomService.GetRoomsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateConsultingRoomRequest request, CancellationToken cancellationToken)
        => Ok(await consultingRoomService.CreateRoomAsync(request, cancellationToken));

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] Guid? roomId, CancellationToken cancellationToken)
        => Ok(await consultingRoomService.GetSchedulesAsync(roomId, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateRoomScheduleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await consultingRoomService.CreateScheduleAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
