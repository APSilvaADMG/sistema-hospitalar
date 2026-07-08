using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Cme;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cme")]
public class CmeController(ICmeService cmeService) : ControllerBase
{
    [HttpGet("kits")]
    public async Task<IActionResult> GetKits(CancellationToken cancellationToken)
        => Ok(await cmeService.GetKitsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("kits")]
    public async Task<IActionResult> CreateKit([FromBody] CreateInstrumentKitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await cmeService.CreateKitAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("cycles")]
    public async Task<IActionResult> GetCycles(CancellationToken cancellationToken)
        => Ok(await cmeService.GetCyclesAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("cycles")]
    public async Task<IActionResult> CreateCycle([FromBody] CreateSterilizationCycleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await cmeService.CreateCycleAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("cycles/{id:guid}/start")]
    public async Task<IActionResult> StartCycle(Guid id, CancellationToken cancellationToken)
    {
        var result = await cmeService.StartCycleAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("cycles/{id:guid}/complete")]
    public async Task<IActionResult> CompleteCycle(Guid id, [FromBody] CompleteSterilizationCycleRequest request, CancellationToken cancellationToken)
    {
        var result = await cmeService.CompleteCycleAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("cycles/{id:guid}/reject")]
    public async Task<IActionResult> RejectCycle(Guid id, [FromBody] RejectSterilizationCycleRequest? request, CancellationToken cancellationToken)
    {
        var result = await cmeService.RejectSterilizationCycleAsync(id, request?.Reason, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
