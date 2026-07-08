using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.ClinicalOperations;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ward-pharmacy")]
public class WardPharmacyController(IWardPharmacyService wardPharmacyService) : ControllerBase
{
    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("balances")]
    public async Task<IActionResult> ListBalances(
        [FromQuery] Guid? wardId,
        [FromQuery] bool lowStockOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await wardPharmacyService.ListBalancesAsync(wardId, lowStockOnly, cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("movements")]
    public async Task<IActionResult> ListMovements(
        [FromQuery] Guid? wardId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
        => Ok(await wardPharmacyService.ListMovementsAsync(wardId, from, to, cancellationToken));

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(
        [FromBody] WardStockTransferRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await wardPharmacyService.TransferFromCentralAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.PharmacyDispense)]
    [HttpPost("dispense")]
    public async Task<IActionResult> Dispense(
        [FromBody] WardStockDispenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await wardPharmacyService.DispenseAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
