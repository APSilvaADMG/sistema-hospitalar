using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/financial-accounts")]
public class FinancialAccountsController(IFinancialAccountService financialAccountService) : ControllerBase
{
    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] FinancialAccountStatus? status,
        [FromQuery] FinancialAccountDirection? direction,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await financialAccountService.SearchAsync(status, direction, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => Ok(await financialAccountService.GetSummaryAsync(cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("payable-presets")]
    public IActionResult GetPayablePresets()
        => Ok(financialAccountService.GetPayableCategoryPresets());

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayments(Guid id, CancellationToken cancellationToken)
        => Ok(await financialAccountService.GetPaymentsAsync(id, cancellationToken));

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken cancellationToken = default)
    {
        var accounts = await financialAccountService.GetByPatientAsync(patientId, cancellationToken);
        return Ok(accounts);
    }

    [RequirePermission(PermissionCodes.BillingRead)]
    [HttpGet("suggestions/{patientId:guid}")]
    public async Task<IActionResult> GetCreateSuggestions(Guid patientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = await financialAccountService.GetCreateSuggestionsAsync(patientId, cancellationToken);
            return Ok(suggestions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateFinancialAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await financialAccountService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetByPatient), new { patientId = account.PatientId }, account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cancelled = await financialAccountService.CancelAsync(id, cancellationToken);
            return cancelled ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("{id:guid}/convert-to-billing")]
    public async Task<IActionResult> ConvertProposalToBilling(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await financialAccountService.ConvertProposalToBillingAsync(id, cancellationToken);
            return account is null ? NotFound() : Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [RequirePermission(PermissionCodes.BillingWrite)]
    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RegisterPayment(
        Guid id,
        [FromBody] RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await financialAccountService.RegisterPaymentAsync(id, request, cancellationToken);
            return account is null ? NotFound() : Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
