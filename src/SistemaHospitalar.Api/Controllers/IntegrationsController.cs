using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Integrations;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
[ApiController]
[Route("api/integrations")]
public class IntegrationsController(IIntegrationService integrationService) : ControllerBase
{
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] int limit = 30, CancellationToken cancellationToken = default)
        => Ok(await integrationService.GetMessagesAsync(limit, cancellationToken));

    [HttpPost("hl7/inbound")]
    public async Task<IActionResult> ProcessHl7Inbound(
        [FromBody] Hl7InboundRequest request, CancellationToken cancellationToken)
        => Ok(await integrationService.ProcessHl7InboundAsync(request, cancellationToken));

    [HttpGet("fhir/Patient/{patientId:guid}")]
    public async Task<IActionResult> ExportFhirPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await integrationService.ExportFhirPatientAsync(patientId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("fhir/import")]
    public async Task<IActionResult> ImportFhirPatient([FromBody] FhirImportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await integrationService.ImportFhirPatientAsync(request.Json, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
