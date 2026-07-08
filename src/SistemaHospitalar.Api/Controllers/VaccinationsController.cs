using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.ClinicalOperations;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/vaccinations")]
public class VaccinationsController(IVaccinationService vaccinationService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet("catalog")]
    public async Task<IActionResult> ListCatalog(
        [FromQuery] VaccineScheduleType? scheduleType,
        CancellationToken cancellationToken)
        => Ok(await vaccinationService.ListCatalogAsync(scheduleType, cancellationToken));

    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> ListByPatient(Guid patientId, CancellationToken cancellationToken)
        => Ok(await vaccinationService.ListByPatientAsync(patientId, cancellationToken));

    [RequirePermission(PermissionCodes.PepWrite)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePatientVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await vaccinationService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListByPatient), new { patientId = request.PatientId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet("epidemic-diseases")]
    public async Task<IActionResult> ListEpidemicDiseases(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
        => Ok(await vaccinationService.ListEpidemicDiseasesAsync(search, cancellationToken));
}
