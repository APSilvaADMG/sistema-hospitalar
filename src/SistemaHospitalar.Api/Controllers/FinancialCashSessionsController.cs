using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.ClinicalOperations;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using System.Security.Claims;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial-cash-sessions")]
public class FinancialCashSessionsController(IFinancialCashSessionService cashSessionService) : ControllerBase
{
    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("open")]
    public async Task<IActionResult> GetOpen(CancellationToken cancellationToken)
        => Ok(await cashSessionService.GetOpenSessionAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
        => Ok(await cashSessionService.ListSessionsAsync(limit, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("open")]
    public async Task<IActionResult> Open(
        [FromBody] OpenFinancialCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = TryGetUserId();
            return Ok(await cashSessionService.OpenSessionAsync(request, userId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(
        Guid id,
        [FromBody] CloseFinancialCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = TryGetUserId();
            return Ok(await cashSessionService.CloseSessionAsync(id, request, userId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
