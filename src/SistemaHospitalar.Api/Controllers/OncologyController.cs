using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Oncology;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/oncology")]
public class OncologyController(IOncologyService oncologyService) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        => Ok(await oncologyService.GetSessionsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChemotherapySessionRequest request, CancellationToken cancellationToken)
        => Ok(await oncologyService.CreateSessionAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPatch("sessions/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateChemotherapyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await oncologyService.UpdateSessionStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
