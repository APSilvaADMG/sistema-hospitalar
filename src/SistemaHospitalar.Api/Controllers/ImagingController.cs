using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Application.DTOs.Imaging;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/imaging")]
public class ImagingController(IImagingService imagingService) : ControllerBase
{
    [HttpGet("studies")]
    public async Task<IActionResult> GetStudies(CancellationToken cancellationToken)
        => Ok(await imagingService.GetStudiesAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]
    [HttpPost("studies")]
    public async Task<IActionResult> CreateStudy([FromBody] CreateImagingStudyRequest request, CancellationToken cancellationToken)
        => Ok(await imagingService.CreateStudyAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]
    [HttpPatch("studies/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateImagingStudyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await imagingService.UpdateStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]
    [HttpPost("studies/{id:guid}/report")]
    public async Task<IActionResult> RegisterReport(Guid id, [FromBody] RegisterImagingReportRequest request, CancellationToken cancellationToken)
    {
        var result = await imagingService.RegisterReportAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
