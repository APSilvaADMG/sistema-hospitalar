using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.HumanResources;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/hr")]
public class HrController(IHrService hrService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await hrService.GetDashboardAsync(cancellationToken));

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
        => Ok(await hrService.GetDepartmentsAsync(cancellationToken));

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken)
        => Ok(await hrService.GetEmployeesAsync(cancellationToken));

    [HttpGet("employees/{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = await hrService.GetEmployeeByIdAsync(id, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hrService.CreateEmployeeAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Não foi possível salvar o colaborador. Verifique CEP, CPF e demais campos." });
        }
    }

    [HttpPut("employees/{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await hrService.UpdateEmployeeAsync(id, request, cancellationToken);
            return employee is null ? NotFound() : Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Não foi possível atualizar o colaborador. Verifique CEP, CPF e demais campos." });
        }
    }

    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] ShiftType? shiftType,
        CancellationToken cancellationToken)
        => Ok(await hrService.GetShiftsAsync(date, from, to, shiftType, cancellationToken));

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hrService.CreateShiftAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("shifts/{id:guid}")]
    public async Task<IActionResult> DeleteShift(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await hrService.DeleteShiftAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetHrEvents([FromQuery] HrEventType? type, CancellationToken cancellationToken)
        => Ok(await hrService.GetHrEventsAsync(type, cancellationToken));

    [HttpPost("events")]
    public async Task<IActionResult> CreateHrEvent([FromBody] CreateEmployeeHrEventRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hrService.CreateHrEventAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
