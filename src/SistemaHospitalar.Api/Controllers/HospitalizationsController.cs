using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/hospitalizations")]
public class HospitalizationsController(
    IHospitalizationService hospitalizationService,
    IHospitalizationHubService hospitalizationHubService) : ControllerBase
{
    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpGet("hub/dashboard")]
    public async Task<IActionResult> GetHubDashboard(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
        => Ok(await hospitalizationHubService.GetDashboardAsync(dateFrom, dateTo, cancellationToken));

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpGet("hub")]
    public async Task<IActionResult> SearchHub(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? wardId,
        [FromQuery] Guid? professionalId,
        [FromQuery] WardCoverageModality? modality,
        [FromQuery] WardCategory? category,
        [FromQuery] HospitalizationStatus? status,
        [FromQuery] string? search,
        [FromQuery] string? groupId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new HospitalizationHubFilterDto(
            dateFrom,
            dateTo,
            patientId,
            wardId,
            professionalId,
            modality,
            category,
            status,
            search,
            groupId,
            skip,
            take);

        return Ok(await hospitalizationHubService.SearchAsync(filter, cancellationToken));
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpGet("wards")]
    public async Task<IActionResult> GetWards(
        [FromQuery] WardCoverageModality? modality,
        [FromQuery] WardCategory? category,
        CancellationToken cancellationToken)
    {
        return Ok(await hospitalizationService.GetWardsAsync(modality, category, cancellationToken));
    }

    [RequireAnyPermission(PermissionCodes.HospitalizationManage, PermissionCodes.PatientsCreate)]
    [HttpGet("beds")]
    public async Task<IActionResult> GetBeds(
        [FromQuery] Guid? wardId,
        [FromQuery] WardCoverageModality? modality,
        [FromQuery] WardCategory? category,
        [FromQuery] BedStatus? status,
        CancellationToken cancellationToken)
    {
        return Ok(await hospitalizationService.GetBedsAsync(wardId, modality, category, status, cancellationToken));
    }

    [RequireAnyPermission(PermissionCodes.HospitalizationManage, PermissionCodes.PatientsCreate)]
    [HttpGet("beds/available-for-patient/{patientId:guid}")]
    public async Task<IActionResult> GetAvailableBedsForPatient(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.GetAvailableBedsForPatientAsync(patientId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? patientId,
        [FromQuery] HospitalizationListScope? scope,
        CancellationToken cancellationToken)
    {
        if (patientId.HasValue)
        {
            return Ok(await hospitalizationService.GetByPatientAsync(patientId.Value, cancellationToken));
        }

        return Ok(await hospitalizationService.GetListAsync(scope ?? HospitalizationListScope.Active, cancellationToken));
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("admit")]
    public async Task<IActionResult> Admit([FromBody] AdmitPatientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await hospitalizationService.AdmitAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.HospitalizationManage, PermissionCodes.BillingWrite)]
    [HttpPost("{id:guid}/close-billing-account")]
    public async Task<IActionResult> CloseBillingAccount(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await hospitalizationService.CloseBillingAccountAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("{id:guid}/register-death")]
    public async Task<IActionResult> RegisterDeath(
        Guid id,
        [FromBody] RegisterPatientDeathRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await hospitalizationService.RegisterPatientDeathAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/discharge")]
    public async Task<IActionResult> Discharge(
        Guid id,
        [FromBody] DischargePatientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await hospitalizationService.DischargeAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> TransferBed(
        Guid id,
        [FromBody] TransferBedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.TransferBedAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers([FromQuery] int? limit, CancellationToken cancellationToken)
        => Ok(await hospitalizationService.GetBedTransfersAsync(limit, cancellationToken));

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("wards")]
    public async Task<IActionResult> CreateWard(
        [FromBody] CreateWardRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ward = await hospitalizationService.CreateWardAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetWards), ward);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPut("wards/{id:guid}")]
    public async Task<IActionResult> UpdateWard(
        Guid id,
        [FromBody] UpdateWardRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ward = await hospitalizationService.UpdateWardAsync(id, request, cancellationToken);
            return ward is null ? NotFound() : Ok(ward);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpDelete("wards/{id:guid}")]
    public async Task<IActionResult> DeactivateWard(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await hospitalizationService.DeactivateWardAsync(id, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("beds")]
    public async Task<IActionResult> CreateBed(
        [FromBody] CreateBedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var bed = await hospitalizationService.CreateBedAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetBeds), bed);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPut("beds/{id:guid}")]
    public async Task<IActionResult> UpdateBed(
        Guid id,
        [FromBody] UpdateBedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var bed = await hospitalizationService.UpdateBedAsync(id, request, cancellationToken);
            return bed is null ? NotFound() : Ok(bed);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPatch("beds/{id:guid}/status")]
    public async Task<IActionResult> UpdateBedStatus(
        Guid id,
        [FromBody] UpdateBedStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.UpdateBedStatusAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpDelete("beds/{id:guid}")]
    public async Task<IActionResult> DeactivateBed(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await hospitalizationService.DeactivateBedAsync(id, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("beds/{id:guid}/reserve")]
    public async Task<IActionResult> ReserveBed(
        Guid id,
        [FromBody] ReserveBedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.ReserveBedAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("beds/{id:guid}/block")]
    public async Task<IActionResult> BlockBed(
        Guid id,
        [FromBody] BlockBedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.BlockBedAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("beds/{id:guid}/release")]
    public async Task<IActionResult> ReleaseBed(
        Guid id,
        [FromBody] ReleaseBedRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.ReleaseBedAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpGet("bed-events")]
    public async Task<IActionResult> GetBedEvents(
        [FromQuery] Guid? bedId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
        => Ok(await hospitalizationService.GetBedEventsAsync(bedId, activeOnly, cancellationToken));

    [HttpGet("snippets")]
    public async Task<IActionResult> GetSnippets(
        [FromQuery] HospitalizationSnippetType type,
        CancellationToken cancellationToken)
        => Ok(await hospitalizationService.GetSnippetsAsync(type, cancellationToken));

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("snippets")]
    public async Task<IActionResult> RegisterSnippet(
        [FromBody] RegisterHospitalizationSnippetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;
            return Ok(await hospitalizationService.RegisterSnippetAsync(request, userId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] HospitalizationRequestStatus? status,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
        => Ok(await hospitalizationService.GetHospitalizationRequestsAsync(status, patientId, cancellationToken));

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateHospitalizationRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await hospitalizationService.CreateHospitalizationRequestAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetRequests), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPatch("requests/{id:guid}/review")]
    public async Task<IActionResult> ReviewRequest(
        Guid id,
        [FromBody] ReviewHospitalizationRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.ReviewHospitalizationRequestAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.CancelHospitalizationRequestAsync(id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPatch("{id:guid}/sus-data")]
    public async Task<IActionResult> UpdateSusData(
        Guid id,
        [FromBody] UpdateHospitalizationSusDataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.UpdateSusDataAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.HospitalizationManage)]
    [HttpPost("requests/{id:guid}/admit")]
    public async Task<IActionResult> AdmitFromRequest(
        Guid id,
        [FromBody] AdmitFromHospitalizationRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hospitalizationService.AdmitFromHospitalizationRequestAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
