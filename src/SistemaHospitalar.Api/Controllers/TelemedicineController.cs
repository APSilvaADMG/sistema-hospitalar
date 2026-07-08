using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Telemedicine;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/telemedicine")]
public class TelemedicineController(ITelemedicineService telemedicineService) : ControllerBase
{
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments(CancellationToken cancellationToken)
        => Ok(await telemedicineService.GetAppointmentsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateTelemedicineAppointmentRequest request, CancellationToken cancellationToken)
        => Ok(await telemedicineService.CreateAppointmentAsync(request, cancellationToken));

    [Authorize(Roles = "Admin,Doctor,Reception,Patient")]
    [HttpPatch("appointments/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTelemedicineStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await telemedicineService.UpdateAppointmentStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
