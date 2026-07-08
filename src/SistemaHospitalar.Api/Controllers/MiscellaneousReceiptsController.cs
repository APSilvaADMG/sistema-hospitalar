using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using System.Security.Claims;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/miscellaneous-receipts")]
public class MiscellaneousReceiptsController(IMiscellaneousReceiptService receiptService) : ControllerBase
{
    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await receiptService.SearchAsync(search, page, pageSize, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await receiptService.GetByIdAsync(id, cancellationToken);
        return receipt is null ? NotFound() : Ok(receipt);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMiscellaneousReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = TryGetUserId();
            var created = await receiptService.CreateAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMiscellaneousReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await receiptService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await receiptService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
