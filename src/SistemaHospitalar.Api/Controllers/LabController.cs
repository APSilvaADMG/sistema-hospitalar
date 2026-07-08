using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Laboratory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/lab")]
public class LabController(ILabService labService) : ControllerBase
{
    [HttpGet("exams")]
    public async Task<IActionResult> GetExamCatalog([FromQuery] Guid? specialtyId, CancellationToken cancellationToken)
        => Ok(await labService.GetExamCatalogAsync(specialtyId, cancellationToken));

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] LabOrderStatus? status, CancellationToken cancellationToken)
        => Ok(await labService.GetOrdersAsync(status, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateLabOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await labService.CreateOrderAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]
    [HttpPost("results")]
    public async Task<IActionResult> RegisterResult([FromBody] RegisterLabResultRequest request, CancellationToken cancellationToken)
    {
        var result = await labService.RegisterResultAsync(request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
