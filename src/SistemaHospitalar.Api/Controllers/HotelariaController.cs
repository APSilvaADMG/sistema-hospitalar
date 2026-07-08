using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Hotelaria;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/hotelaria")]
public class HotelariaController(IHotelariaHospitalarService hotelariaService) : ControllerBase
{
    [RequirePermission(PermissionCodes.CleaningOperate)]
    [HttpGet("noc")]
    public async Task<IActionResult> GetNoc(CancellationToken cancellationToken)
        => Ok(await hotelariaService.GetNocDashboardAsync(cancellationToken));

    [RequirePermission(PermissionCodes.CleaningOperate)]
    [HttpGet("cleaning")]
    public async Task<IActionResult> GetCleaningRequests(
        [FromQuery] CleaningRequestStatus? status, CancellationToken cancellationToken)
        => Ok(await hotelariaService.GetCleaningRequestsAsync(status, cancellationToken));

    [RequirePermission(PermissionCodes.CleaningManage)]
    [HttpPost("cleaning")]
    public async Task<IActionResult> CreateCleaningRequest(
        [FromBody] CreateCleaningRequestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hotelariaService.CreateCleaningRequestAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.CleaningOperate)]
    [HttpPost("cleaning/{id:guid}/start")]
    public async Task<IActionResult> StartCleaning(
        Guid id, [FromBody] StartCleaningRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await hotelariaService.StartCleaningAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.CleaningOperate)]
    [HttpPatch("cleaning/{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(
        Guid id, [FromBody] UpdateCleaningChecklistRequest request, CancellationToken cancellationToken)
    {
        var result = await hotelariaService.UpdateChecklistAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.CleaningOperate)]
    [HttpPost("cleaning/{id:guid}/complete")]
    public async Task<IActionResult> CompleteCleaning(
        Guid id, [FromBody] CompleteCleaningRequestRequest? request, CancellationToken cancellationToken)
    {
        var result = await hotelariaService.CompleteCleaningAsync(
            id, request ?? new CompleteCleaningRequestRequest(null), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.CleaningManage)]
    [HttpPost("cleaning/{id:guid}/cancel")]
    public async Task<IActionResult> CancelCleaning(Guid id, CancellationToken cancellationToken)
    {
        var result = await hotelariaService.CancelCleaningAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
