using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/security")]
public class SecurityComplianceController(
    ISecurityComplianceService securityService,
    IPermissionService permissionService) : ControllerBase
{
    [RequirePermission(PermissionCodes.SecurityManage)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        => Ok(await securityService.GetDashboardAsync(cancellationToken));

    [RequirePermission(PermissionCodes.AuditRead)]
    [HttpGet("login-attempts")]
    public async Task<IActionResult> LoginAttempts([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
        => Ok(await securityService.GetLoginAttemptsAsync(limit, cancellationToken));

    [RequirePermission(PermissionCodes.SecurityManage)]
    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
        => Ok(await securityService.GetSessionsAsync(activeOnly, cancellationToken));

    [RequirePermission(PermissionCodes.SecurityManage)]
    [HttpPost("sessions/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        await securityService.RevokeSessionAsync(id, cancellationToken);
        return NoContent();
    }

    [RequirePermission(PermissionCodes.SecurityManage)]
    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken cancellationToken)
        => Ok(await permissionService.GetAllDefinitionsAsync(cancellationToken));

    [RequirePermission(PermissionCodes.SecurityManage)]
    [HttpGet("role-matrix")]
    public async Task<IActionResult> RoleMatrix(CancellationToken cancellationToken)
        => Ok(await permissionService.GetRoleMatrixAsync(cancellationToken));

    [RequirePermission(PermissionCodes.LgpdConsentManage)]
    [HttpGet("consent-terms")]
    public async Task<IActionResult> ConsentTerms(CancellationToken cancellationToken)
        => Ok(await securityService.GetConsentTermsAsync(cancellationToken));

    [Authorize]
    [HttpGet("consent-terms/current")]
    public async Task<IActionResult> CurrentConsentTerms(CancellationToken cancellationToken)
        => Ok(await securityService.GetCurrentConsentTermsAsync(cancellationToken));

    [Authorize]
    [HttpGet("consent-status")]
    public async Task<IActionResult> ConsentStatus([FromQuery] Guid patientId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Reception") && !HasAnyPermission(PermissionCodes.LgpdConsentManage, PermissionCodes.PatientsRead, PermissionCodes.PatientsCreate))
        {
            return Forbid();
        }

        return Ok(await securityService.GetPatientConsentStatusAsync(patientId, cancellationToken));
    }

    [RequirePermission(PermissionCodes.LgpdConsentManage)]
    [HttpPost("consent-terms")]
    public async Task<IActionResult> CreateConsentTerm(
        [FromBody] CreateConsentTermRequest request,
        CancellationToken cancellationToken)
        => Ok(await securityService.CreateConsentTermAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.LgpdConsentManage)]
    [HttpGet("consents")]
    public async Task<IActionResult> PatientConsents(
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
        => Ok(await securityService.GetPatientConsentsAsync(patientId, cancellationToken));

    [Authorize]
    [HttpGet("consents/{id:guid}")]
    public async Task<IActionResult> PatientConsentDetail(Guid id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !HasAnyPermission(
            PermissionCodes.LgpdConsentManage, PermissionCodes.PatientsRead, PermissionCodes.PatientsCreate))
        {
            return Forbid();
        }

        return Ok(await securityService.GetPatientConsentDetailAsync(id, cancellationToken));
    }

    [RequirePermission(PermissionCodes.LgpdConsentManage)]
    [HttpPost("consents")]
    public async Task<IActionResult> RecordConsent(
        [FromBody] RecordPatientConsentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await securityService.RecordConsentAsync(request, userId, ip, cancellationToken));
    }

    [RequirePermission(PermissionCodes.LgpdConsentManage)]
    [HttpPost("consents/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeConsent(Guid id, CancellationToken cancellationToken)
    {
        await securityService.RevokeConsentAsync(id, cancellationToken);
        return NoContent();
    }

    [RequirePermission(PermissionCodes.LgpdSubjectRequests)]
    [HttpGet("subject-requests")]
    public async Task<IActionResult> SubjectRequests(
        [FromQuery] DataSubjectRequestStatus? status,
        CancellationToken cancellationToken)
        => Ok(await securityService.GetSubjectRequestsAsync(status, cancellationToken));

    [RequirePermission(PermissionCodes.LgpdSubjectRequests)]
    [HttpPost("subject-requests")]
    public async Task<IActionResult> CreateSubjectRequest(
        [FromBody] CreateDataSubjectRequest request,
        CancellationToken cancellationToken)
        => Ok(await securityService.CreateSubjectRequestAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.LgpdSubjectRequests)]
    [HttpPatch("subject-requests/{id:guid}")]
    public async Task<IActionResult> UpdateSubjectRequest(
        Guid id,
        [FromBody] UpdateDataSubjectRequestStatus request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await securityService.UpdateSubjectRequestAsync(id, request, userId, cancellationToken));
    }

    [RequirePermission(PermissionCodes.LgpdSubjectRequests)]
    [HttpPost("subject-requests/{id:guid}/export")]
    public async Task<IActionResult> ExportSubjectRequest(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var exportedBy = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? "Usuário";

        var export = await securityService.ExportSubjectRequestAsync(id, userId, exportedBy, cancellationToken);
        return File(
            JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions { WriteIndented = true }),
            "application/json",
            $"lgpd-export-{export.PatientId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
    }

    [RequirePermission(PermissionCodes.IncidentsManage)]
    [HttpGet("incidents")]
    public async Task<IActionResult> Incidents(CancellationToken cancellationToken)
        => Ok(await securityService.GetPrivacyIncidentsAsync(cancellationToken));

    [RequirePermission(PermissionCodes.IncidentsManage)]
    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident(
        [FromBody] CreatePrivacyIncidentRequest request,
        CancellationToken cancellationToken)
    {
        TryGetCurrentUserId(out var userId);
        return Ok(await securityService.CreatePrivacyIncidentAsync(request, userId == Guid.Empty ? null : userId, cancellationToken));
    }

    [RequirePermission(PermissionCodes.IncidentsManage)]
    [HttpPatch("incidents/{id:guid}")]
    public async Task<IActionResult> UpdateIncident(
        Guid id,
        [FromBody] UpdatePrivacyIncidentRequest request,
        CancellationToken cancellationToken)
        => Ok(await securityService.UpdatePrivacyIncidentAsync(id, request, cancellationToken));

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }

    private bool HasAnyPermission(params string[] codes)
    {
        foreach (var code in codes)
        {
            if (User.HasClaim("permission", code))
            {
                return true;
            }
        }

        return false;
    }
}
