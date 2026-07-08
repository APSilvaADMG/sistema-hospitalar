using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Security;

using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Financial;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure.Connect;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/pix")]
public class PixController(
    IPixPaymentService pixPaymentService,
    IOptions<ConnectSettings> settings) : ControllerBase
{
    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("charges/account/{accountId:guid}")]
    public async Task<IActionResult> CreateCharge(Guid accountId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pixPaymentService.CreateChargeForAccountAsync(accountId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Reception,Doctor,Patient")]
    [HttpGet("charges/{id:guid}")]
    public async Task<IActionResult> GetCharge(Guid id, CancellationToken cancellationToken)
    {
        var charge = await pixPaymentService.GetChargeAsync(id, cancellationToken);
        return charge is null ? NotFound() : Ok(charge);
    }

    [Authorize(Roles = "Admin,Reception,Doctor,Patient")]
    [HttpGet("charges/account/{accountId:guid}/active")]
    public async Task<IActionResult> GetActiveCharge(Guid accountId, CancellationToken cancellationToken)
    {
        var charge = await pixPaymentService.GetActiveChargeForAccountAsync(accountId, cancellationToken);
        return charge is null ? NotFound() : Ok(charge);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromBody] PixWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var secret = settings.Value.Collection.PixWebhookSecret;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var header = Request.Headers["X-Pix-Webhook-Secret"].ToString();
            if (!string.Equals(header, secret, StringComparison.Ordinal))
            {
                return Unauthorized();
            }
        }

        var result = await pixPaymentService.ProcessWebhookAsync(request, cancellationToken);
        return Ok(result);
    }

    [RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]
    [HttpPost("charges/{id:guid}/simulate-payment")]
    public async Task<IActionResult> SimulatePayment(Guid id, CancellationToken cancellationToken)
    {
        if (!settings.Value.Collection.UseMockPixProvider)
        {
            return BadRequest(new { message = "Simulação disponível apenas com provedor PIX mock." });
        }

        try
        {
            var charge = await pixPaymentService.SimulatePaymentAsync(id, cancellationToken);
            return charge is null ? NotFound() : Ok(charge);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
