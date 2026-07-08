using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.ClinicalEngineering;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/clinical-engineering")]
public class ClinicalEngineeringController(IClinicalEngineeringService clinicalEngineeringService) : ControllerBase
{
    [HttpGet("equipment")]
    public async Task<IActionResult> GetEquipment(CancellationToken cancellationToken)
        => Ok(await clinicalEngineeringService.GetEquipmentAsync(cancellationToken));

    [HttpPost("equipment")]
    public async Task<IActionResult> CreateEquipment([FromBody] CreateMedicalEquipmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await clinicalEngineeringService.CreateEquipmentAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("work-orders")]
    public async Task<IActionResult> GetWorkOrders(CancellationToken cancellationToken)
        => Ok(await clinicalEngineeringService.GetWorkOrdersAsync(cancellationToken));

    [HttpPost("work-orders")]
    public async Task<IActionResult> CreateWorkOrder([FromBody] CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await clinicalEngineeringService.CreateWorkOrderAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("work-orders/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWorkOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await clinicalEngineeringService.UpdateWorkOrderStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
