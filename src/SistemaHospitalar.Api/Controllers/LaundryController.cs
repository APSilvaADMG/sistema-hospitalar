using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Laundry;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
[ApiController]
[Route("api/laundry")]
public class LaundryController(ILaundryService laundryService) : ControllerBase
{
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(CancellationToken cancellationToken)
        => Ok(await laundryService.GetBatchesAsync(cancellationToken));

    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateLaundryBatchRequest request, CancellationToken cancellationToken)
        => Ok(await laundryService.CreateBatchAsync(request, cancellationToken));

    [HttpPatch("batches/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateLaundryBatchStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await laundryService.UpdateBatchStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
