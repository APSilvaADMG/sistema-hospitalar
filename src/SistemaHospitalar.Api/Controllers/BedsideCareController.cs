using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Bedside;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bedside")]
public class BedsideCareController(
    IBedsideCareService bedsideCareService,
    IAuthService authService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PepWrite)]
    [HttpPost("patients/{patientId:guid}/vitals")]
    public async Task<IActionResult> RegisterVitals(
        Guid patientId,
        [FromBody] BedsideVitalsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var userEmail))
        {
            return Unauthorized();
        }

        var passwordError = await ValidatePasswordAsync(userId, request.Password, cancellationToken);
        if (passwordError is not null)
        {
            return BadRequest(new { message = passwordError });
        }

        try
        {
            Guid? professionalId = TryGetProfessionalId(out var profId) ? profId : null;
            var result = await bedsideCareService.RegisterVitalsAsync(
                patientId,
                request,
                userId,
                userEmail,
                professionalId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PepWrite)]
    [HttpPost("patients/{patientId:guid}/administer-medication")]
    public async Task<IActionResult> AdministerMedication(
        Guid patientId,
        [FromBody] BedsideMedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var userEmail))
        {
            return Unauthorized();
        }

        var passwordError = await ValidatePasswordAsync(userId, request.Password, cancellationToken);
        if (passwordError is not null)
        {
            return BadRequest(new { message = passwordError });
        }

        if (!TryGetProfessionalId(out var professionalId))
        {
            return BadRequest(new { message = "Usuário sem profissional vinculado para assinar administração." });
        }

        try
        {
            var result = await bedsideCareService.AdministerMedicationAsync(
                patientId,
                request,
                userId,
                userEmail,
                professionalId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetProfessionalId(out Guid professionalId)
    {
        var claim = User.FindFirstValue("professional_id");
        return Guid.TryParse(claim, out professionalId);
    }

    private async Task<string?> ValidatePasswordAsync(Guid userId, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Informe sua senha para confirmar a ação no leito.";
        }

        if (!await authService.VerifyUserPasswordAsync(userId, password, cancellationToken))
        {
            return "Senha incorreta.";
        }

        return null;
    }

    private bool TryGetCurrentUser(out Guid userId, out string userEmail)
    {
        userEmail = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? "unknown";

        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out userId);
    }
}
