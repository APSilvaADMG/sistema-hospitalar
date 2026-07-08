using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Physiotherapy;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/physiotherapy")]
public class PhysiotherapyController(IPhysiotherapyService physiotherapyService) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        => Ok(await physiotherapyService.GetSessionsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreatePhysiotherapySessionRequest request, CancellationToken cancellationToken)
        => Ok(await physiotherapyService.CreateSessionAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPatch("sessions/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePhysiotherapyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await physiotherapyService.UpdateSessionStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
