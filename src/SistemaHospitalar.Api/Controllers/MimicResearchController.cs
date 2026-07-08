using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

/// <summary>
/// Sandbox MIMIC-III — status, ETL subset e consultas de sinais vitais.
/// Não expõe nem grava dados de produção (sistema_hospitalar).
/// </summary>
[Authorize]
[ApiController]
[Route("api/research/mimic")]
public class MimicResearchController(IMimicResearchService mimicResearchService) : ControllerBase
{
    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        => Ok(await mimicResearchService.GetStatusAsync(cancellationToken));

    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpGet("etl/status")]
    public async Task<IActionResult> GetEtlStatus(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await mimicResearchService.GetEtlStatusAsync(cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpGet("vitals")]
    public async Task<IActionResult> GetVitals(
        [FromQuery] int subjectId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (subjectId <= 0)
        {
            return BadRequest(new { message = "subjectId deve ser um inteiro MIMIC positivo." });
        }

        try
        {
            return Ok(await mimicResearchService.GetVitalsAsync(subjectId, limit, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Dispara ETL subset CHARTEVENTS → mimic_staging (somente Development).</summary>
    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpPost("etl/import")]
    public async Task<IActionResult> TriggerSubsetImport(
        [FromQuery] int? maxSubjects,
        CancellationToken cancellationToken)
    {
        var result = await mimicResearchService.TriggerSubsetImportAsync(maxSubjects, cancellationToken);
        if (!result.Accepted)
        {
            return BadRequest(result);
        }

        return Accepted(result);
    }
}
