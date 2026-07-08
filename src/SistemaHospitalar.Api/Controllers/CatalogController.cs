using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("health-insurances")]
    public async Task<IActionResult> GetHealthInsurances(CancellationToken cancellationToken = default)
    {
        var items = await catalogService.GetHealthInsurancesAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("specialties")]
    public async Task<IActionResult> GetSpecialties(CancellationToken cancellationToken = default)
    {
        var items = await catalogService.GetSpecialtiesAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("professionals")]
    public async Task<IActionResult> GetProfessionals(CancellationToken cancellationToken = default)
    {
        var items = await catalogService.GetProfessionalsAsync(cancellationToken);
        return Ok(items);
    }
}
