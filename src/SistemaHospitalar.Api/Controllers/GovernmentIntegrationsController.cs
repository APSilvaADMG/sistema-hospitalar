using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Government;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
[ApiController]
[Route("api/gov-integrations")]
public class GovernmentIntegrationsController(IGovernmentIntegrationService govService) : ControllerBase
{
    [HttpGet("profiles")]
    public IActionResult GetProfiles() => Ok(govService.GetProfiles());

    [HttpGet("cns/{cns}")]
    public async Task<IActionResult> LookupCns(string cns, CancellationToken cancellationToken)
        => Ok(await govService.LookupCnsAsync(cns, cancellationToken));

    [HttpGet("cnes/{code}")]
    public async Task<IActionResult> LookupCnes(string code, CancellationToken cancellationToken)
        => Ok(await govService.LookupCnesEstablishmentAsync(code, cancellationToken));

    [HttpPost("patients/{patientId:guid}/cns")]
    public async Task<IActionResult> ApplyCns(
        Guid patientId, [FromBody] ApplyCnsToPatientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await govService.ApplyCnsToPatientAsync(patientId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sih/aih/{hospitalizationId:guid}")]
    public async Task<IActionResult> PreviewAih(Guid hospitalizationId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await govService.GenerateSihAihPreviewAsync(hospitalizationId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sia/preview")]
    public async Task<IActionResult> PreviewSia(
        [FromQuery] SiaDocumentType documentType,
        [FromQuery] string? competence,
        CancellationToken cancellationToken)
        => Ok(await govService.GenerateSiaPreviewAsync(documentType, competence ?? string.Empty, cancellationToken));

    [HttpGet("sia/export")]
    public async Task<IActionResult> ExportSia(
        [FromQuery] SiaDocumentType documentType,
        [FromQuery] string? competence,
        CancellationToken cancellationToken)
    {
        var file = await govService.ExportSiaDocumentAsync(documentType, competence ?? string.Empty, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(file.Content), file.ContentType, file.FileName);
    }

    [HttpGet("sih/export")]
    public async Task<IActionResult> ExportSihBatch(
        [FromQuery] string? competence,
        CancellationToken cancellationToken)
    {
        var file = await govService.ExportSihAihBatchAsync(competence ?? string.Empty, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(file.Content), file.ContentType, file.FileName);
    }

    [HttpGet("ciha/export")]
    public async Task<IActionResult> ExportCiha(
        [FromQuery] string? competence,
        CancellationToken cancellationToken)
    {
        var file = await govService.ExportCihaDocumentAsync(competence ?? string.Empty, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(file.Content), file.ContentType, file.FileName);
    }

    [HttpGet("rnds/patients/{patientId:guid}")]
    public async Task<IActionResult> QueryRnds(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await govService.QueryRndsPatientAsync(patientId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
