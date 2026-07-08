using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Guides;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/guides")]
public class GuidesController(
    IGuidesHubService guidesHubService,
    ISusGuideService susGuideService,
    IServiceUnitService serviceUnitService) : ControllerBase
{
    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
        => Ok(await guidesHubService.GetDashboardAsync(dateFrom, dateTo, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("service-units")]
    public async Task<IActionResult> GetServiceUnits(CancellationToken cancellationToken)
        => Ok(await serviceUnitService.GetAllAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("service-units")]
    public async Task<IActionResult> CreateServiceUnit(
        [FromBody] CreateServiceUnitRequest request,
        CancellationToken cancellationToken)
        => Ok(await serviceUnitService.CreateAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("service-units/{id:guid}")]
    public async Task<IActionResult> UpdateServiceUnit(
        Guid id,
        [FromBody] UpdateServiceUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await serviceUnitService.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? healthInsuranceId,
        [FromQuery] Guid? professionalId,
        [FromQuery] Guid? specialtyId,
        [FromQuery] string? procedureSearch,
        [FromQuery] string? guideNumber,
        [FromQuery] TissGuideStatus? status,
        [FromQuery] TissGuideType? guideType,
        [FromQuery] string? groupId,
        [FromQuery] string? serviceUnit,
        [FromQuery] Guid? serviceUnitId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new GuidesHubFilterDto(
            dateFrom,
            dateTo,
            patientId,
            healthInsuranceId,
            professionalId,
            specialtyId,
            procedureSearch,
            guideNumber,
            status,
            guideType,
            groupId,
            serviceUnit,
            serviceUnitId,
            skip,
            take);

        return Ok(await guidesHubService.SearchAsync(filter, cancellationToken));
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("sus")]
    public async Task<IActionResult> SearchSus(
        [FromQuery] SusGuideFilterDto filter,
        CancellationToken cancellationToken)
        => Ok(await susGuideService.SearchAsync(filter, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("sus/{id:guid}")]
    public async Task<IActionResult> GetSus(Guid id, CancellationToken cancellationToken)
    {
        var guide = await susGuideService.GetByIdAsync(id, cancellationToken);
        return guide is null ? NotFound() : Ok(guide);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("sus")]
    public async Task<IActionResult> CreateSus(
        [FromBody] CreateSusGuideRequest request,
        CancellationToken cancellationToken)
        => Ok(await susGuideService.CreateAsync(request, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPut("sus/{id:guid}")]
    public async Task<IActionResult> UpdateSus(
        Guid id,
        [FromBody] UpdateSusGuideRequest request,
        CancellationToken cancellationToken)
    {
        var result = await susGuideService.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("sus/{id:guid}/cancel")]
    public async Task<IActionResult> CancelSus(Guid id, CancellationToken cancellationToken)
    {
        var result = await susGuideService.CancelAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("sus/{id:guid}/submit")]
    public async Task<IActionResult> SubmitSus(Guid id, CancellationToken cancellationToken)
    {
        var result = await susGuideService.SubmitAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("sus/{id:guid}/authorize")]
    public async Task<IActionResult> AuthorizeSus(
        Guid id,
        [FromQuery] string? authorizationNumber,
        CancellationToken cancellationToken)
    {
        var result = await susGuideService.AuthorizeAsync(id, authorizationNumber, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("sus/{id:guid}/duplicate")]
    public async Task<IActionResult> DuplicateSus(Guid id, CancellationToken cancellationToken)
    {
        var result = await susGuideService.DuplicateAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] string? source,
        CancellationToken cancellationToken)
        => Ok(await guidesHubService.GetHistoryAsync(id, source, cancellationToken));

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        var result = await guidesHubService.DuplicateGuideAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
