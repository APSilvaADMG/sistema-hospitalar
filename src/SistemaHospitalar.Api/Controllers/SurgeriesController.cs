using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Surgery;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
[ApiController]
[Route("api/surgeries")]
public class SurgeriesController(ISurgeryService surgeryService) : ControllerBase
{
    [HttpGet("operating-rooms")]
    public async Task<IActionResult> GetOperatingRooms(CancellationToken cancellationToken)
    {
        return Ok(await surgeryService.GetOperatingRoomsAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await surgeryService.GetByDateAsync(targetDate, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSurgeryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await surgeryService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetByDate), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateSurgeryStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await surgeryService.UpdateStatusAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/safety-checklist")]
    public async Task<IActionResult> UpdateSafetyChecklist(
        Guid id,
        [FromBody] UpdateSurgerySafetyChecklistRequest request,
        CancellationToken cancellationToken)
    {
        var result = await surgeryService.UpdateSafetyChecklistAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
