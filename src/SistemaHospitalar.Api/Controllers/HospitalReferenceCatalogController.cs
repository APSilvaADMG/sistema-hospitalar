using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/hospital-catalog")]
public class HospitalReferenceCatalogController(IHospitalReferenceCatalogService catalogService) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.UsersManage, PermissionCodes.SecurityManage, PermissionCodes.IntegrationsManage)]
    [HttpGet("types")]
    public IActionResult GetTypes()
        => Ok(catalogService.GetCatalogTypes());

    [RequireAnyPermission(PermissionCodes.UsersManage, PermissionCodes.SecurityManage, PermissionCodes.IntegrationsManage)]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => Ok(await catalogService.GetSummaryAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.UsersManage, PermissionCodes.SecurityManage, PermissionCodes.IntegrationsManage)]
    [HttpGet]
    public async Task<IActionResult> GetByType(
        [FromQuery] HospitalReferenceCatalogType type,
        [FromQuery] string? group,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
        => Ok(await catalogService.GetByTypeAsync(type, group, search, cancellationToken));

    [RequireAnyPermission(PermissionCodes.UsersManage, PermissionCodes.SecurityManage, PermissionCodes.IntegrationsManage)]
    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups(
        [FromQuery] HospitalReferenceCatalogType type,
        CancellationToken cancellationToken)
        => Ok(await catalogService.GetGroupsAsync(type, cancellationToken));
}
