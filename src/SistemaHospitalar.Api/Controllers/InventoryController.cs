using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public class InventoryController(
    IInventoryService inventoryService,
    IInventoryConfigService inventoryConfigService,
    IProductKitService productKitService,
    IStockRequisitionService stockRequisitionService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] ProductType? type,
        CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.GetProductsAsync(search, lowStockOnly, type, cancellationToken));
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("products/{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await inventoryService.GetProductByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("inbound")]
    public async Task<IActionResult> RegisterInbound(
        [FromBody] StockInboundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryService.RegisterInboundAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? productId,
        [FromQuery] string? search,
        [FromQuery] StockMovementType? type,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int limit = 300,
        CancellationToken cancellationToken = default)
        => Ok(await inventoryService.GetMovementsAsync(productId, search, type, from, to, limit, cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("outbound")]
    public async Task<IActionResult> RegisterOutbound(
        [FromBody] StockOutboundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryService.RegisterOutboundAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("products/{productId:guid}/billing-rules")]
    public async Task<IActionResult> GetBillingRules(
        Guid productId,
        [FromQuery] string? priceTable,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.GetBillingRulesAsync(productId, priceTable, isActive, cancellationToken));
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("products/{productId:guid}/billing-rules")]
    public async Task<IActionResult> CreateBillingRule(
        Guid productId,
        [FromBody] CreateProductBillingRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryService.CreateBillingRuleAsync(productId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPut("billing-rules/{ruleId:guid}")]
    public async Task<IActionResult> UpdateBillingRule(
        Guid ruleId,
        [FromBody] UpdateProductBillingRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryService.UpdateBillingRuleAsync(ruleId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpDelete("billing-rules/{ruleId:guid}")]
    public async Task<IActionResult> DeleteBillingRule(Guid ruleId, CancellationToken cancellationToken)
    {
        try
        {
            await inventoryService.DeleteBillingRuleAsync(ruleId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("config/lookup-items")]
    public async Task<IActionResult> GetLookupItems(
        [FromQuery] InventoryLookupType type,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await inventoryConfigService.GetLookupItemsAsync(type, search, cancellationToken));
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("config/lookup-items")]
    public async Task<IActionResult> CreateLookupItem(
        [FromQuery] InventoryLookupType type,
        [FromBody] CreateInventoryLookupItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryConfigService.CreateLookupItemAsync(type, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPut("config/lookup-items/{id:guid}")]
    public async Task<IActionResult> UpdateLookupItem(
        Guid id,
        [FromBody] UpdateInventoryLookupItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryConfigService.UpdateLookupItemAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpDelete("config/lookup-items/{id:guid}")]
    public async Task<IActionResult> DeleteLookupItem(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await inventoryConfigService.DeleteLookupItemAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("config/medication-mappings")]
    public async Task<IActionResult> GetMedicationMappings(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await inventoryConfigService.GetMedicationMappingsAsync(search, cancellationToken));
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("config/medication-mappings")]
    public async Task<IActionResult> CreateMedicationMapping(
        [FromBody] CreateMedicationInsuranceMappingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryConfigService.CreateMedicationMappingAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPut("config/medication-mappings/{id:guid}")]
    public async Task<IActionResult> UpdateMedicationMapping(
        Guid id,
        [FromBody] UpdateMedicationInsuranceMappingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await inventoryConfigService.UpdateMedicationMappingAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpDelete("config/medication-mappings/{id:guid}")]
    public async Task<IActionResult> DeleteMedicationMapping(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await inventoryConfigService.DeleteMedicationMappingAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("kits")]
    public async Task<IActionResult> GetKits([FromQuery] string? search, CancellationToken cancellationToken)
    {
        return Ok(await productKitService.GetKitsAsync(search, cancellationToken));
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("kits/{id:guid}")]
    public async Task<IActionResult> GetKit(Guid id, CancellationToken cancellationToken)
    {
        var result = await productKitService.GetKitByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("kits")]
    public async Task<IActionResult> CreateKit(
        [FromBody] CreateProductKitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await productKitService.CreateKitAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetKit), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPut("kits/{id:guid}")]
    public async Task<IActionResult> UpdateKit(
        Guid id,
        [FromBody] UpdateProductKitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await productKitService.UpdateKitAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpDelete("kits/{id:guid}")]
    public async Task<IActionResult> DeleteKit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await productKitService.DeleteKitAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("requisitions")]
    public async Task<IActionResult> GetRequisitions(
        [FromQuery] StockRequisitionStatus? status,
        [FromQuery] StockRequisitionPriority? priority,
        [FromQuery] DateOnly? dueDateBefore,
        CancellationToken cancellationToken)
    {
        return Ok(await stockRequisitionService.GetRequisitionsAsync(status, priority, dueDateBefore, cancellationToken));
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpGet("requisitions/{id:guid}")]
    public async Task<IActionResult> GetRequisition(Guid id, CancellationToken cancellationToken)
    {
        var result = await stockRequisitionService.GetRequisitionByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpPost("requisitions")]
    public async Task<IActionResult> CreateRequisition(
        [FromBody] CreateStockRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockRequisitionService.CreateRequisitionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetRequisition), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpPut("requisitions/{id:guid}")]
    public async Task<IActionResult> UpdateRequisition(
        Guid id,
        [FromBody] UpdateStockRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockRequisitionService.UpdateRequisitionAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("requisitions/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequisition(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockRequisitionService.ApproveRequisitionAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("requisitions/{id:guid}/fulfill")]
    public async Task<IActionResult> FulfillRequisition(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockRequisitionService.FulfillRequisitionAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.WarehouseManage, PermissionCodes.PharmacyDispense)]
    [HttpPost("requisitions/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequisition(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await stockRequisitionService.CancelRequisitionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPatch("requisitions/{id:guid}/deny")]
    public async Task<IActionResult> DenyRequisition(
        Guid id,
        [FromBody] DenyStockRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockRequisitionService.DenyRequisitionAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

[Authorize]
[ApiController]
[Route("api/pharmacy")]
public class PharmacyController(IPharmacyService pharmacyService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.PharmacyDispense, PermissionCodes.PepRead)]
    [HttpGet("dispensings")]
    public async Task<IActionResult> GetDispensings([FromQuery] Guid? patientId, CancellationToken cancellationToken)
    {
        return Ok(await pharmacyService.GetDispensingsAsync(patientId, cancellationToken));
    }

    [RequirePermission(PermissionCodes.PharmacyDispense)]
    [HttpPost("dispense")]
    public async Task<IActionResult> Dispense(
        [FromBody] DispenseMedicationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await pharmacyService.DispenseAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PharmacyDispense)]
    [HttpPost("dispensings/{id:guid}/reverse")]
    public async Task<IActionResult> ReverseDispensing(
        Guid id,
        [FromBody] ReversePharmacyDispensingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;
            return Ok(await pharmacyService.ReverseDispensingAsync(id, request, userId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
