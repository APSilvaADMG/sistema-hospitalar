using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Hemotherapy;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/hemotherapy")]
public class HemotherapyController(IHemotherapyService hemotherapyService) : ControllerBase
{
    [HttpGet("units")]
    public async Task<IActionResult> GetUnits([FromQuery] BloodUnitStatus? status, CancellationToken cancellationToken)
        => Ok(await hemotherapyService.GetBloodUnitsAsync(status, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateBloodUnitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hemotherapyService.CreateBloodUnitAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("transfusions")]
    public async Task<IActionResult> GetTransfusions(CancellationToken cancellationToken)
        => Ok(await hemotherapyService.GetTransfusionRequestsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("transfusions")]
    public async Task<IActionResult> CreateTransfusion([FromBody] CreateTransfusionRequestRequest request, CancellationToken cancellationToken)
        => Ok(await hemotherapyService.CreateTransfusionRequestAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("transfusions/{id:guid}/match")]
    public async Task<IActionResult> MatchTransfusion(Guid id, [FromBody] MatchTransfusionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await hemotherapyService.MatchTransfusionAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("transfusions/{id:guid}/complete")]
    public async Task<IActionResult> CompleteTransfusion(Guid id, CancellationToken cancellationToken)
    {
        var result = await hemotherapyService.CompleteTransfusionAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
