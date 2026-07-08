using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Connect;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppWebhookController(
    IConnectBotService botService,
    AppDbContext dbContext,
    IOptions<ConnectSettings> settings,
    ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    [HttpGet("webhook")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" && token == settings.Value.WhatsApp.VerifyToken)
        {
            return Content(challenge ?? string.Empty);
        }

        logger.LogWarning("Tentativa de verificação WhatsApp com token inválido.");
        return Unauthorized();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
        {
            return Ok();
        }

        var wa = settings.Value.WhatsApp;
        var signature = Request.Headers["X-Hub-Signature-256"].ToString();
        if (!WhatsAppSignatureValidator.IsValid(body, signature, wa.AppSecret, wa.UseMockProvider))
        {
            logger.LogWarning("Webhook WhatsApp rejeitado — assinatura inválida ou App Secret ausente em modo Meta.");
            return Unauthorized();
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
            {
                return Ok();
            }

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes))
                {
                    continue;
                }

                foreach (var changeWrapper in changes.EnumerateArray())
                {
                    if (!changeWrapper.TryGetProperty("value", out var change))
                    {
                        continue;
                    }

                    if (change.TryGetProperty("statuses", out var statuses))
                    {
                        await ProcessStatusesAsync(statuses, cancellationToken);
                    }

                    if (!change.TryGetProperty("messages", out var messages))
                    {
                        continue;
                    }

                    foreach (var message in messages.EnumerateArray())
                    {
                        await ProcessInboundMessageAsync(change, message, cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar webhook WhatsApp.");
        }

        return Ok();
    }

    private async Task ProcessInboundMessageAsync(JsonElement change, JsonElement message, CancellationToken cancellationToken)
    {
        if (message.GetProperty("type").GetString() != "text")
        {
            return;
        }

        var externalId = message.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            var alreadyProcessed = await dbContext.ConnectMessages.AsNoTracking()
                .AnyAsync(m => m.ExternalId == externalId, cancellationToken);
            if (alreadyProcessed)
            {
                logger.LogDebug("Webhook idempotente — ignorando mensagem Meta {ExternalId}.", externalId);
                return;
            }
        }

        var from = message.GetProperty("from").GetString() ?? string.Empty;
        var text = message.GetProperty("text").GetProperty("body").GetString() ?? string.Empty;
        var name = change.TryGetProperty("contacts", out var contacts)
            ? contacts[0].GetProperty("profile").GetProperty("name").GetString()
            : null;

        await botService.ProcessInboundAsync(from, text, name, externalId, cancellationToken);
    }

    private async Task ProcessStatusesAsync(JsonElement statuses, CancellationToken cancellationToken)
    {
        foreach (var status in statuses.EnumerateArray())
        {
            if (!status.TryGetProperty("id", out var idProp))
            {
                continue;
            }

            var externalId = idProp.GetString();
            if (string.IsNullOrWhiteSpace(externalId))
            {
                continue;
            }

            var statusValue = status.GetProperty("status").GetString();
            var mapped = statusValue switch
            {
                "sent" => ConnectMessageStatus.Sent,
                "delivered" => ConnectMessageStatus.Delivered,
                "read" => ConnectMessageStatus.Read,
                "failed" => ConnectMessageStatus.Failed,
                _ => (ConnectMessageStatus?)null
            };

            if (mapped is null)
            {
                continue;
            }

            var message = await dbContext.ConnectMessages
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, cancellationToken);

            if (message is null)
            {
                continue;
            }

            message.Status = mapped.Value;
            message.UpdatedAt = DateTime.UtcNow;

            if (mapped == ConnectMessageStatus.Failed && status.TryGetProperty("errors", out var errors)
                && errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                var code = first.TryGetProperty("code", out var codeProp) ? codeProp.GetRawText() : null;
                var title = first.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
                message.FailureReason = string.IsNullOrWhiteSpace(title)
                    ? code
                    : $"{code}: {title}";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
