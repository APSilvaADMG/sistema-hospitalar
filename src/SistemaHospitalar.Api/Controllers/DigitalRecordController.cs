using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/patients/{patientId:guid}/digital-record")]
public class DigitalRecordController(IDigitalRecordService digitalRecordService) : ControllerBase
{
    [RequirePermission(PermissionCodes.PepRead)]
    [HttpGet]
    public async Task<IActionResult> Get(Guid patientId, CancellationToken cancellationToken)
    {
        var record = await digitalRecordService.GetByPatientIdAsync(patientId, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }
}
