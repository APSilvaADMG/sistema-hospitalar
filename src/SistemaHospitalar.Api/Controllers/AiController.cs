using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Ai;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Ai;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiController(IAiService aiService, IGroqLlmService groqLlm, IOptions<GroqOptions> groqOptions) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PepRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("groq/status")]
    public IActionResult GetGroqStatus()
        => Ok(new
        {
            configured = groqLlm.IsConfigured,
            enabled = groqOptions.Value.Enabled,
            model = groqOptions.Value.Model,
        });    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("triage")]
    public async Task<IActionResult> AnalyzeTriage([FromBody] TriageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await aiService.AnalyzeTriageAsync(request, userId, cancellationToken));
    }

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("cid10/suggest")]
    public async Task<IActionResult> SuggestCid10([FromBody] Cid10SuggestionRequest request, CancellationToken cancellationToken)
        => Ok(await aiService.SuggestCid10Async(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("triage/logs")]
    public async Task<IActionResult> GetTriageLogs([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        => Ok(await aiService.GetRecentTriageLogsAsync(limit, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("triage/patient/{patientId:guid}/admission-suggestion")]
    public async Task<IActionResult> GetAdmissionSuggestion(Guid patientId, CancellationToken cancellationToken)
    {
        var suggestion = await aiService.GetAdmissionSuggestionForPatientAsync(patientId, cancellationToken);
        return suggestion is null ? NoContent() : Ok(suggestion);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PepRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("insights/outbreak")]
    public async Task<IActionResult> AnalyzeOutbreak([FromQuery] int days = 30, CancellationToken cancellationToken = default)
        => Ok(await aiService.AnalyzeOutbreakAsync(days, GetUserId(), cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead)]
    [HttpGet("insights/patient/{patientId:guid}/recurrent")]
    public async Task<IActionResult> AnalyzeRecurrentPatient(Guid patientId, CancellationToken cancellationToken = default)
        => Ok(await aiService.AnalyzeRecurrentPatientAsync(patientId, GetUserId(), cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("insights/triage-operational")]
    public async Task<IActionResult> AnalyzeTriageOperational([FromQuery] int days = 7, CancellationToken cancellationToken = default)
        => Ok(await aiService.AnalyzeTriageOperationalAsync(days, GetUserId(), cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PepRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("insights/reports")]
    public async Task<IActionResult> GetInsightReports(
        [FromQuery] int limit = 20,
        [FromQuery] AiInsightType? type = null,
        CancellationToken cancellationToken = default)
        => Ok(await aiService.GetInsightReportsAsync(limit, type, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PepRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("insights/reports/{id:guid}")]
    public async Task<IActionResult> GetInsightReport(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await aiService.GetInsightReportAsync(id, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PatientsRead)]
    [HttpPost("prescription/safety")]
    public async Task<IActionResult> AnalyzePrescriptionSafety(
        [FromBody] PrescriptionSafetyRequest request,
        CancellationToken cancellationToken)
        => Ok(await aiService.AnalyzePrescriptionSafetyAsync(
            request.PatientId, request.PrescriptionContent, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("insights/hospital-dashboard")]
    public async Task<IActionResult> AnalyzeHospitalDashboard(CancellationToken cancellationToken = default)
        => Ok(await aiService.AnalyzeHospitalDashboardAsync(GetUserId(), cancellationToken));

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
