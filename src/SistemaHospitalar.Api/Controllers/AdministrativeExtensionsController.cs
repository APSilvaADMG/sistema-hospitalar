using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Administrative;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin-ext")]
public class AdministrativeExtensionsController(IAdministrativeExtensionsService service) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.BillingRead, PermissionCodes.ReportsRead, PermissionCodes.TpaManage)]
    [HttpGet("tpa/administrators")]
    public async Task<IActionResult> GetTpaAdministrators(CancellationToken cancellationToken)
        => Ok(await service.GetTpaAdministratorsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.BillingWrite, PermissionCodes.ReportsRead, PermissionCodes.TpaManage)]
    [HttpPost("tpa/administrators")]
    public async Task<IActionResult> CreateTpaAdministrator([FromBody] CreateTpaAdministratorRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateTpaAdministratorAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.BillingRead, PermissionCodes.ReportsRead, PermissionCodes.TpaManage)]
    [HttpGet("tpa/claims")]
    public async Task<IActionResult> GetTpaClaims([FromQuery] Guid? administratorId, [FromQuery] TpaClaimStatus? status, CancellationToken cancellationToken)
        => Ok(await service.GetTpaClaimsAsync(administratorId, status, cancellationToken));

    [RequireAnyPermission(PermissionCodes.BillingWrite, PermissionCodes.ReportsRead, PermissionCodes.TpaManage)]
    [HttpPost("tpa/claims")]
    public async Task<IActionResult> CreateTpaClaim([FromBody] CreateTpaClaimRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateTpaClaimAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.BillingWrite, PermissionCodes.ReportsRead, PermissionCodes.TpaManage)]
    [HttpPut("tpa/claims/{id:guid}/status")]
    public async Task<IActionResult> UpdateTpaClaimStatus(Guid id, [FromBody] UpdateTpaClaimStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateTpaClaimStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.BillingRead, PermissionCodes.ReportsRead, PermissionCodes.TpaManage)]
    [HttpGet("tpa/report")]
    public async Task<IActionResult> GetTpaReport(CancellationToken cancellationToken)
        => Ok(await service.GetTpaReportAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpGet("payroll/runs")]
    public async Task<IActionResult> GetPayrollRuns(CancellationToken cancellationToken)
        => Ok(await service.GetPayrollRunsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpGet("payroll/runs/{id:guid}")]
    public async Task<IActionResult> GetPayrollRun(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetPayrollRunAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpPost("payroll/runs/generate")]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRunRequest request, CancellationToken cancellationToken)
        => Ok(await service.GeneratePayrollRunAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpPut("payroll/runs/{id:guid}/status")]
    public async Task<IActionResult> UpdatePayrollStatus(Guid id, [FromBody] UpdatePayrollRunStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdatePayrollRunStatusAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpPut("payroll/runs/{runId:guid}/items/{itemId:guid}/lines")]
    public async Task<IActionResult> UpdatePayrollItemLines(
        Guid runId,
        Guid itemId,
        [FromBody] UpdatePayrollItemLinesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpdatePayrollItemLinesAsync(runId, itemId, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpGet("payroll/runs/{id:guid}/slips/{employeeId:guid}")]
    public async Task<IActionResult> GetPayrollSlip(Guid id, Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetPayrollSlipAsync(id, employeeId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate, PermissionCodes.PayrollManage)]
    [HttpGet("payroll/summary")]
    public async Task<IActionResult> GetPayrollSummary(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
        => Ok(await service.GetPayrollMonthlySummaryAsync(year, month, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PharmacyDispense, PermissionCodes.BillingRead, PermissionCodes.PharmacyBillingManage)]
    [HttpGet("pharmacy-billing")]
    public async Task<IActionResult> GetPharmacyBilling([FromQuery] bool? paid, CancellationToken cancellationToken)
        => Ok(await service.GetPharmacyBillingEntriesAsync(paid, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PharmacyDispense, PermissionCodes.BillingWrite, PermissionCodes.PharmacyBillingManage)]
    [HttpPost("pharmacy-billing")]
    public async Task<IActionResult> CreatePharmacyBilling([FromBody] CreatePharmacyBillingEntryRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreatePharmacyBillingEntryAsync(request, cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PatientsCreate)]
    [HttpGet("birth-registrations")]
    public async Task<IActionResult> GetBirthRegistrations(CancellationToken cancellationToken)
        => Ok(await service.GetBirthRegistrationsAsync(cancellationToken));

    [RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("birth-registrations")]
    public async Task<IActionResult> CreateBirthRegistration([FromBody] CreateBirthRegistrationRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateBirthRegistrationAsync(request, cancellationToken));
}

