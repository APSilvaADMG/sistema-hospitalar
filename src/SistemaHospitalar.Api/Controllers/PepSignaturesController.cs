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
[Route("api/pep")]
public class PepSignaturesController(
    IMedicalRecordService medicalRecordService,
    IAuditService auditService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet("pending-signatures")]
    public async Task<IActionResult> GetPendingSignatures(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await medicalRecordService.GetPendingSignaturesAsync(limit, cancellationToken);
        return Ok(items);
    }

    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet("signature-audit")]
    public async Task<IActionResult> GetSignatureAudit(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var logs = await auditService.GetLogsAsync(limit, "MedicalRecordEntry", cancellationToken);
        return Ok(logs.Where(l => l.ActionCategory == "ClinicalSignature" || l.Action.Contains("AssinarRegistro")).ToList());
    }
}
