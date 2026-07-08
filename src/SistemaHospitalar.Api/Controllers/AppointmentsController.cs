using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Appointments;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet]
    public async Task<IActionResult> GetByDate(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? HospitalTime.TodayInBrazil;
        var appointments = await appointmentService.GetByDateAsync(targetDate, cancellationToken);
        return Ok(appointments);
    }

    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentService.GetByIdAsync(id, cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [RequirePermission(PermissionCodes.PatientsCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await appointmentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Appointment.Id }, result);
        }
        catch (ScheduleConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PatientsUpdate)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await appointmentService.UpdateAsync(id, request, cancellationToken);
            return appointment is null ? NotFound() : Ok(appointment);
        }
        catch (ScheduleConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentService.UpdateStatusAsync(id, request, cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }
}
