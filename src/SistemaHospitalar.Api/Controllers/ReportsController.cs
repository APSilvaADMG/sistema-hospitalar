using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Reports;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController(IReportsService reportsService) : ControllerBase
{
    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpGet("catalog/summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => Ok(await reportsService.GetCatalogSummaryAsync(cancellationToken));

    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] string? module,
        [FromQuery] bool? essentialOnly,
        [FromQuery] bool? implementedOnly,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
        => Ok(await reportsService.GetCatalogAsync(module, essentialOnly, implementedOnly, search, cancellationToken));

    [RequirePermission(PermissionCodes.ReportsRead)]
    [HttpGet("{code}")]
    public async Task<IActionResult> Execute(
        string code,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? professionalId,
        [FromQuery] Guid? specialtyId,
        [FromQuery] Guid? healthInsuranceId,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? tpaAdministratorId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? department,
        CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilterDto(dateFrom, dateTo, professionalId, specialtyId, healthInsuranceId, patientId, tpaAdministratorId, year, month, department);
        return Ok(await reportsService.ExecuteAsync(code, filter, cancellationToken));
    }
}
