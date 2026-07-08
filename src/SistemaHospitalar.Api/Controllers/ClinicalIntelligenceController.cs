using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/clinical-intelligence")]
public class ClinicalIntelligenceController(IClinicalIntelligenceService clinicalIntelligence) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PatientsRead, PermissionCodes.HospitalizationManage)]
    [HttpGet("patients/{patientId:guid}/alerts")]
    public async Task<IActionResult> GetPatientAlerts(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await clinicalIntelligence.GetPatientClinicalAlertsAsync(patientId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequirePermission(PermissionCodes.WarehouseManage)]
    [HttpGet("stock/replenishment")]
    public async Task<IActionResult> GetStockReplenishment(CancellationToken cancellationToken)
        => Ok(await clinicalIntelligence.GetStockReplenishmentSuggestionsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.WarehouseManage, PermissionCodes.HospitalizationManage)]
    [HttpGet("operational")]
    public async Task<IActionResult> GetOperationalInsights(CancellationToken cancellationToken)
        => Ok(await clinicalIntelligence.GetOperationalInsightsAsync(cancellationToken));
}
