using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Purchasing;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/purchasing")]
public class PurchasingController(IPurchasingService purchasingService) : ControllerBase
{
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
        => Ok(await purchasingService.GetSuppliersAsync(cancellationToken));

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
        => Ok(await purchasingService.CreateSupplierAsync(request, cancellationToken));

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] PurchaseOrderStatus? status, CancellationToken cancellationToken)
        => Ok(await purchasingService.GetOrdersAsync(status, cancellationToken));

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetCreateSuggestions(
        [FromQuery] PurchaseSector? sector,
        [FromQuery] PurchasePriority? priority,
        CancellationToken cancellationToken)
        => Ok(await purchasingService.GetCreateSuggestionsAsync(sector, priority, cancellationToken));

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await purchasingService.CreateOrderAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("orders/{id:guid}/send")]
    public async Task<IActionResult> SendOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await purchasingService.SendOrderAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("orders/{id:guid}/receive")]
    public async Task<IActionResult> ReceiveOrder(
        Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await purchasingService.ReceiveOrderAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
