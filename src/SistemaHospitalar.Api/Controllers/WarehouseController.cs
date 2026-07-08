using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/warehouse")]
public class WarehouseController(IWarehouseService warehouseService) : ControllerBase
{
    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await warehouseService.GetDashboardAsync(cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("lots")]
    public async Task<IActionResult> GetLots(
        [FromQuery] Guid? productId,
        [FromQuery] int? expiringWithinDays,
        CancellationToken cancellationToken)
        => Ok(await warehouseService.GetLotsAsync(productId, expiringWithinDays, cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring(
        CancellationToken cancellationToken,
        [FromQuery] int days = 30)
        => Ok(await warehouseService.GetExpiringLotsAsync(days, cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock(CancellationToken cancellationToken)
        => Ok(await warehouseService.GetLowStockProductsAsync(cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("consumption-by-sector")]
    public async Task<IActionResult> GetConsumptionBySector(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
        => Ok(await warehouseService.GetConsumptionBySectorAsync(from, to, cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("receipts")]
    public async Task<IActionResult> CreateReceipt(
        [FromBody] CreateStockReceiptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await warehouseService.CreateReceiptAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("issues")]
    public async Task<IActionResult> CreateIssue(
        [FromBody] CreateStockIssueRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await warehouseService.CreateIssueAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
