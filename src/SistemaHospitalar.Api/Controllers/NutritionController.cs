using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Nutrition;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/nutrition")]
public class NutritionController(INutritionService nutritionService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] DietOrderStatus? status, [FromQuery] DateOnly? mealDate, CancellationToken cancellationToken)
        => Ok(await nutritionService.GetOrdersAsync(status, mealDate, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateDietOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await nutritionService.CreateOrderAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPatch("orders/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDietOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await nutritionService.UpdateStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
