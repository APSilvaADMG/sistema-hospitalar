using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/physical-security")]
public class SecurityController(ISecurityService securityService) : ControllerBase
{
    [HttpGet("settings")]
    public IActionResult GetSettings()
        => Ok(securityService.GetSettings());

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await securityService.GetDashboardAsync(cancellationToken));

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents(CancellationToken cancellationToken)
        => Ok(await securityService.GetIncidentsAsync(cancellationToken));

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateSecurityIncidentRequest request, CancellationToken cancellationToken)
        => Ok(await securityService.CreateIncidentAsync(request, cancellationToken));

    [HttpPost("incidents/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveIncident(Guid id, [FromBody] ResolveIncidentRequest request, CancellationToken cancellationToken)
    {
        var result = await securityService.ResolveIncidentAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("visitors")]
    public async Task<IActionResult> GetVisitors([FromQuery] bool? insideOnly, CancellationToken cancellationToken)
        => Ok(await securityService.GetVisitorsAsync(insideOnly, cancellationToken));

    [HttpPost("visitors")]
    public async Task<IActionResult> RegisterVisitor([FromBody] RegisterVisitorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await securityService.RegisterVisitorAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("visitors/{id:guid}/exit")]
    public async Task<IActionResult> RegisterExit(Guid id, CancellationToken cancellationToken)
    {
        var result = await securityService.RegisterExitAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
