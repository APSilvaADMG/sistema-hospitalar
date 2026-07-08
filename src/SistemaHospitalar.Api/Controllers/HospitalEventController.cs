using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/events")]
public class HospitalEventController(IHospitalEventEngine eventEngine) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage, PermissionCodes.PatientsRead)]
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        => Ok(await eventEngine.GetRecentAsync(limit, cancellationToken));
}
