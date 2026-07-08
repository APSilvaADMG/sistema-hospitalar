using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.InfectionControl;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
[ApiController]
[Route("api/infection-control")]
public class InfectionControlController(IInfectionControlService infectionControlService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await infectionControlService.GetDashboardAsync(cancellationToken));

    [HttpGet("surveillance")]
    public async Task<IActionResult> GetSurveillance(CancellationToken cancellationToken)
        => Ok(await infectionControlService.GetSurveillanceCasesAsync(cancellationToken));

    [HttpPost("surveillance")]
    public async Task<IActionResult> CreateSurveillance([FromBody] CreateInfectionSurveillanceRequest request, CancellationToken cancellationToken)
        => Ok(await infectionControlService.CreateSurveillanceCaseAsync(request, cancellationToken));

    [HttpPost("surveillance/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveSurveillance(Guid id, [FromBody] ResolveInfectionRequest request, CancellationToken cancellationToken)
    {
        var result = await infectionControlService.ResolveSurveillanceCaseAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("isolations")]
    public async Task<IActionResult> GetIsolations([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
        => Ok(await infectionControlService.GetIsolationPrecautionsAsync(activeOnly, cancellationToken));

    [HttpPost("isolations")]
    public async Task<IActionResult> CreateIsolation([FromBody] CreateIsolationPrecautionRequest request, CancellationToken cancellationToken)
        => Ok(await infectionControlService.CreateIsolationPrecautionAsync(request, cancellationToken));

    [HttpPost("isolations/{id:guid}/lift")]
    public async Task<IActionResult> LiftIsolation(Guid id, CancellationToken cancellationToken)
    {
        var result = await infectionControlService.LiftIsolationPrecautionAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
