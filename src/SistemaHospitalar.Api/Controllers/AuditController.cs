using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/audit")]
public class AuditController(IAuditService auditService) : ControllerBase
{
    [RequirePermission(PermissionCodes.AuditRead)]
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int limit = 50, [FromQuery] string? entityType = null, CancellationToken cancellationToken = default)
        => Ok(await auditService.GetLogsAsync(limit, entityType, cancellationToken));
}
