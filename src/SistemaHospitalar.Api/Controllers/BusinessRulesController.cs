using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/business-rules")]
public class BusinessRulesController : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.AuditRead, PermissionCodes.SecurityManage)]
    [HttpGet]
    public IActionResult List([FromQuery] bool implementedOnly = false)
    {
        var rules = implementedOnly
            ? BusinessRuleCatalog.All.Where(r => r.Implemented)
            : BusinessRuleCatalog.All;

        return Ok(rules.Select(r => new
        {
            r.Code,
            r.Module,
            r.Title,
            r.Description,
            r.Implemented,
            r.BrReference,
            r.Layer
        }));
    }
}
