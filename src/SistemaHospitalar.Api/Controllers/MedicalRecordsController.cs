using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.MedicalRecords;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/patients/{patientId:guid}/medical-record")]
public class MedicalRecordsController(
    IMedicalRecordService medicalRecordService,
    IAuthService authService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken cancellationToken = default)
    {
        var record = await medicalRecordService.GetByPatientIdAsync(patientId, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [RequirePermission(PermissionCodes.PepWrite)]
    [HttpPost("entries")]
    public async Task<IActionResult> AddEntry(
        Guid patientId,
        [FromBody] CreateMedicalRecordEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUser(out var userId, out var userEmail))
        {
            return Unauthorized();
        }

        var signOnCreate = !string.IsNullOrWhiteSpace(request.SignatureImage) && request.ProfessionalId.HasValue;
        if (signOnCreate)
        {
            var passwordError = await ValidateSigningPasswordAsync(userId, request.Password, cancellationToken);
            if (passwordError is not null)
            {
                return BadRequest(new { message = passwordError });
            }
        }

        var entry = await medicalRecordService.AddEntryAsync(
            patientId,
            request,
            userId,
            userEmail,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [RequirePermission(PermissionCodes.PepWrite)]
    [HttpPut("entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(
        Guid patientId,
        Guid entryId,
        [FromBody] UpdateMedicalRecordEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await medicalRecordService.UpdateEntryAsync(patientId, entryId, request, cancellationToken);
            return entry is null ? NotFound() : Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PepWrite)]
    [HttpPost("entries/{entryId:guid}/sign")]
    public async Task<IActionResult> SignEntry(
        Guid patientId,
        Guid entryId,
        [FromBody] SignMedicalRecordEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUser(out var userId, out var userEmail))
        {
            return Unauthorized();
        }

        var passwordError = await ValidateSigningPasswordAsync(userId, request.Password, cancellationToken);
        if (passwordError is not null)
        {
            return BadRequest(new { message = passwordError });
        }

        try
        {
            var entry = await medicalRecordService.SignEntryAsync(
                patientId,
                entryId,
                request,
                userId,
                userEmail,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            return entry is null ? NotFound() : Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<string?> ValidateSigningPasswordAsync(
        Guid userId, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Confirme sua senha para assinar o documento.";
        }

        if (!await authService.VerifyUserPasswordAsync(userId, password, cancellationToken))
        {
            return "Senha incorreta. Assinatura não realizada.";
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
