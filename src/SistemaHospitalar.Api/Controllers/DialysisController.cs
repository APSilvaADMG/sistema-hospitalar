using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Dialysis;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dialysis")]
public class DialysisController(IDialysisService dialysisService) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        => Ok(await dialysisService.GetSessionsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateDialysisSessionRequest request, CancellationToken cancellationToken)
        => Ok(await dialysisService.CreateSessionAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPatch("sessions/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDialysisSessionStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await dialysisService.UpdateSessionStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
