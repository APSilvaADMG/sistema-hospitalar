using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Waste;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/waste")]
public class WasteController(IWasteService wasteService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await wasteService.GetDashboardAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] WasteType? wasteType,
        [FromQuery] WasteCollectionStatus? status,
        [FromQuery] string? sector,
        CancellationToken cancellationToken)
        => Ok(await wasteService.ListAsync(wasteType, status, sector, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await wasteService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWasteCollectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await wasteService.CreateAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWasteCollectionRequest request, CancellationToken cancellationToken)
    {
        var result = await wasteService.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
