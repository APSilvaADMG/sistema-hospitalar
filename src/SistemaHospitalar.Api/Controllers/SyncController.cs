using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Sync;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sync")]
public class SyncController(ISyncService syncService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.TransportOperate, PermissionCodes.CleaningOperate)]
    [HttpPost("push")]
    public async Task<IActionResult> Push([FromBody] SyncPushRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest(new { message = "deviceId é obrigatório." });
        }

        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;

        return Ok(await syncService.PushAsync(request, userId, cancellationToken));
    }

    [RequireAnyPermission(PermissionCodes.TransportOperate, PermissionCodes.CleaningOperate)]
    [HttpPost("pull")]
    public async Task<IActionResult> Pull([FromBody] SyncPullRequest request, CancellationToken cancellationToken)
        => Ok(await syncService.PullAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.TransportOperate, PermissionCodes.CleaningOperate)]
    [HttpGet("pull")]
    public async Task<IActionResult> PullGet(
        [FromQuery] DateTime? since,
        [FromQuery] string? sector,
        [FromQuery] Guid? wardId,
        CancellationToken cancellationToken)
        => Ok(await syncService.PullAsync(new SyncPullRequest(since, sector, wardId), cancellationToken));
}
