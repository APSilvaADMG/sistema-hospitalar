using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsRead, PermissionCodes.PepRead)]
    [HttpGet("operational")]
    public async Task<IActionResult> GetOperational(
        [FromQuery] string? date,
        [FromQuery] Guid? professionalId,
        CancellationToken cancellationToken)
    {
        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
        {
            parsedDate = d;
        }

        return Ok(await dashboardService.GetOperationalDashboardAsync(
            GetUserId(),
            parsedDate,
            professionalId,
            cancellationToken));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
