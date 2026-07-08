using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize(Roles = "Patient,Admin")]
[ApiController]
[Route("api/patient-portal")]
public class PatientPortalController(
    IPatientPortalService patientPortalService,
    ISecurityComplianceService securityService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        var resolvedPatientId = ResolvePatientId(patientId);
        if (resolvedPatientId is null)
        {
            return User.IsInRole("Admin")
                ? BadRequest(new { message = "Selecione um paciente para visualizar o portal." })
                : Forbid();
        }

        var dashboard = await patientPortalService.GetDashboardAsync(resolvedPatientId.Value, cancellationToken);
        return dashboard is null ? NotFound() : Ok(dashboard);
    }

    [HttpGet("medical-record")]
    public async Task<IActionResult> GetMedicalRecord([FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        var resolvedPatientId = ResolvePatientId(patientId);
        if (resolvedPatientId is null)
        {
            return User.IsInRole("Admin")
                ? BadRequest(new { message = "Selecione um paciente para visualizar o portal." })
                : Forbid();
        }

        var record = await patientPortalService.GetMedicalRecordAsync(resolvedPatientId.Value, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpGet("consent-status")]
    public async Task<IActionResult> GetConsentStatus([FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        var resolvedPatientId = ResolvePatientId(patientId);
        if (resolvedPatientId is null)
        {
            return User.IsInRole("Admin")
                ? BadRequest(new { message = "Selecione um paciente para visualizar consentimentos." })
                : Forbid();
        }

        return Ok(await securityService.GetPatientConsentStatusAsync(resolvedPatientId.Value, cancellationToken));
    }

    [HttpPost("consents")]
    public async Task<IActionResult> SignConsent(
        [FromQuery] Guid? patientId,
        [FromBody] SignPatientConsentRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedPatientId = ResolvePatientId(patientId);
        if (resolvedPatientId is null)
        {
            return User.IsInRole("Admin")
                ? BadRequest(new { message = "Selecione um paciente para assinar consentimentos." })
                : Forbid();
        }

        Guid? userId = null;
        if (TryGetCurrentUserId(out var uid))
        {
            userId = uid;
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await securityService.SignPatientConsentAsync(resolvedPatientId.Value, request, userId, ip, cancellationToken));
    }

    [HttpGet("consents/{id:guid}")]
    public async Task<IActionResult> GetConsentDetail(
        Guid id,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        var resolvedPatientId = ResolvePatientId(patientId);
        if (resolvedPatientId is null)
        {
            return User.IsInRole("Admin")
                ? BadRequest(new { message = "Selecione um paciente." })
                : Forbid();
        }

        var detail = await securityService.GetPatientConsentDetailAsync(id, cancellationToken);
        if (detail.PatientId != resolvedPatientId.Value)
        {
            return Forbid();
        }

        return Ok(detail);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }

    private Guid? ResolvePatientId(Guid? requestedPatientId)
    {
        if (User.IsInRole("Patient"))
        {
            var claim = User.FindFirstValue("patient_id");
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        if (User.IsInRole("Admin") && requestedPatientId.HasValue)
        {
            return requestedPatientId;
        }

        return null;
    }
}
