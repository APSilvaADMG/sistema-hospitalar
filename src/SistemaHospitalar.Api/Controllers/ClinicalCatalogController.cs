using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/clinical-catalog")]
public class ClinicalCatalogController(IClinicalCatalogService clinicalCatalogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBySpecialty([FromQuery] Guid? specialtyId, CancellationToken cancellationToken)
        => Ok(await clinicalCatalogService.GetBySpecialtyAsync(specialtyId, cancellationToken));

    [HttpGet("by-professional/{professionalId:guid}")]
    public async Task<IActionResult> GetByProfessional(Guid professionalId, CancellationToken cancellationToken)
        => Ok(await clinicalCatalogService.GetByProfessionalAsync(professionalId, cancellationToken));

    [HttpGet("medications")]
    public async Task<IActionResult> GetMedications(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool referenceOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await clinicalCatalogService.SearchMedicationsAsync(search, page, pageSize, referenceOnly, cancellationToken));

    [HttpGet("medications/{id:guid}")]
    public async Task<IActionResult> GetMedication(Guid id, CancellationToken cancellationToken)
    {
        var med = await clinicalCatalogService.GetMedicationByIdAsync(id, cancellationToken);
        return med is null ? NotFound() : Ok(med);
    }

    [HttpGet("cid10")]
    public async Task<IActionResult> GetCid10([FromQuery] string? search, CancellationToken cancellationToken)
        => Ok(await clinicalCatalogService.GetCid10CatalogAsync(search, cancellationToken));

    [HttpGet("cid10/children")]
    public async Task<IActionResult> GetCid10Children([FromQuery] string? parentCode, CancellationToken cancellationToken)
        => Ok(await clinicalCatalogService.GetCid10ChildrenAsync(parentCode, cancellationToken));

    [HttpGet("administration-routes")]
    public async Task<IActionResult> GetAdministrationRoutes(CancellationToken cancellationToken)
        => Ok(await clinicalCatalogService.GetAdministrationRoutesAsync(cancellationToken));

    [HttpGet("patient-reference")]
    public async Task<IActionResult> GetPatientReferenceCatalog(
        [FromQuery] PatientReferenceCatalogType type,
        CancellationToken cancellationToken)
        => Ok(await clinicalCatalogService.GetPatientReferenceCatalogAsync(type, cancellationToken));
}
