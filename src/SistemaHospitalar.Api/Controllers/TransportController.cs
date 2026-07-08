using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Transport;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/transport")]
public class TransportController(ITransportService transportService) : ControllerBase
{
    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await transportService.GetDashboardAsync(cancellationToken));

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
        => Ok(await transportService.GetMetricsAsync(cancellationToken));

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpGet("assets")]
    public async Task<IActionResult> GetAssets(CancellationToken cancellationToken)
        => Ok(await transportService.GetAssetsAsync(cancellationToken));

    [RequirePermission(PermissionCodes.TransportManage)]
    [HttpPost("assets")]
    public async Task<IActionResult> CreateAsset([FromBody] CreateTransportAssetRequest request, CancellationToken cancellationToken)
        => Ok(await transportService.CreateAssetAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.TransportManage)]
    [HttpPatch("assets/{id:guid}/status")]
    public async Task<IActionResult> UpdateAssetStatus(
        Guid id, [FromBody] UpdateTransportAssetStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await transportService.UpdateAssetStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] TransportRequestStatus? status, CancellationToken cancellationToken)
        => Ok(await transportService.GetRequestsAsync(status, cancellationToken));

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpGet("porters")]
    public async Task<IActionResult> GetPorters(CancellationToken cancellationToken)
        => Ok(await transportService.GetPortersAsync(cancellationToken));

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateTransportRequestRequest request, CancellationToken cancellationToken)
    {
        var requestedBy = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        return Ok(await transportService.CreateRequestAsync(request, requestedBy, cancellationToken));
    }

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpPost("requests/{id:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(
        Guid id, [FromBody] AcceptTransportRequestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await transportService.AcceptRequestAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpPatch("requests/{id:guid}/advance")]
    public async Task<IActionResult> AdvanceRequest(
        Guid id, [FromBody] AdvanceTransportRequestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await transportService.AdvanceRequestAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.TransportOperate)]
    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await transportService.CancelRequestAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
