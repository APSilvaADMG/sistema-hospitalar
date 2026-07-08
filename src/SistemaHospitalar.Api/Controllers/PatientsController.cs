using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Patients;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PatientsController(
    IPatientService patientService,
    IPatientTimelineService patientTimelineService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await patientService.SearchAsync(search, page, pageSize, isActive, cancellationToken);
        return Ok(result);
    }

    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("quick-search")]
    public async Task<IActionResult> QuickSearch(
        [FromQuery] string? search,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await patientService.QuickSearchAsync(search, take, cancellationToken);
        return Ok(result);
    }

    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await patientService.GetByIdAsync(id, cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken cancellationToken = default)
    {
        var timeline = await patientTimelineService.GetTimelineAsync(id, cancellationToken);
        return timeline is null ? NotFound() : Ok(timeline);
    }

    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpGet("check-cpf")]
    public async Task<IActionResult> CheckCpf(
        [FromQuery] string cpf,
        [FromQuery] Guid? excludePatientId,
        CancellationToken cancellationToken = default)
    {
        var result = await patientService.CheckCpfAvailabilityAsync(cpf, excludePatientId, cancellationToken);
        return Ok(result);
    }

    [RequirePermission(PermissionCodes.PatientsRead)]
    [HttpPost("check-duplicates")]
    public async Task<IActionResult> CheckDuplicates(
        [FromBody] PatientDuplicateCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var duplicates = await patientService.FindPotentialDuplicatesAsync(request, cancellationToken);
        return Ok(new
        {
            hasDuplicates = duplicates.Count > 0,
            duplicates
        });
    }

    [RequirePermission(PermissionCodes.PatientsCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await patientService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Patient.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PatientsUpdate)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await patientService.UpdateAsync(id, request, cancellationToken);
            return patient is null ? NotFound() : Ok(patient);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
