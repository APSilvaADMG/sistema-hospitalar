using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.PatientIdentity;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/patient-identity")]
public class PatientIdentityController(IPatientIdentityService patientIdentityService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("resolve/{code}")]
    public async Task<IActionResult> Resolve(string code, CancellationToken cancellationToken)
    {
        var result = await patientIdentityService.ResolveAsync(code, cancellationToken);
        return result is null ? NotFound(new { message = "Identificador não encontrado ou inativo." }) : Ok(result);
    }
}

[Authorize]
[ApiController]
[Route("api/patients/{patientId:guid}/identity")]
public class PatientIdentityPatientController(IPatientIdentityService patientIdentityService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet]
    public async Task<IActionResult> List(Guid patientId, CancellationToken cancellationToken)
    {
        var items = await patientIdentityService.ListActiveAsync(patientId, cancellationToken);
        return Ok(items);
    }

    [RequireAnyPermission(PermissionCodes.PatientsCreate, PermissionCodes.PatientsUpdate, PermissionCodes.HospitalizationManage)]
    [HttpPost("bracelet")]
    public async Task<IActionResult> GenerateBracelet(
        Guid patientId,
        [FromBody] GenerateBraceletRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var identity = await patientIdentityService.GenerateBraceletAsync(
                patientId, request, TryGetCurrentUserId(), cancellationToken);
            return Ok(identity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.PatientsCreate, PermissionCodes.PatientsUpdate, PermissionCodes.PepWrite)]
    [HttpPost("labels")]
    public async Task<IActionResult> GenerateLabel(
        Guid patientId,
        [FromBody] GenerateLabelRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var identity = await patientIdentityService.GenerateLabelAsync(
                patientId, request, TryGetCurrentUserId(), cancellationToken);
            return Ok(identity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
