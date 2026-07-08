using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Connect;

public class MetaWhatsAppProvider(
    HttpClient httpClient,
    IOptions<ConnectSettings> settings,
    ILogger<MetaWhatsAppProvider> logger) : IWhatsAppProvider
{
    private readonly WhatsAppSettings _wa = settings.Value.WhatsApp;

    public string ProviderName => "meta";
    public bool IsMock => false;

    public Task<WhatsAppSendResult> SendTextAsync(string phone, string body, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "text",
            text = new { preview_url = false, body }
        };

        return SendAsync(payload, cancellationToken);
    }

    public Task<WhatsAppSendResult> SendTemplateAsync(
        string phone,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default)
    {
        var components = bodyParameters.Count > 0
            ? new[]
            {
                new
                {
                    type = "body",
                    parameters = bodyParameters.Select(p => new { type = "text", text = p }).ToArray()
                }
            }
            : Array.Empty<object>();

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components
            }
        };

        return SendAsync(payload, cancellationToken);
    }

    private async Task<WhatsAppSendResult> SendAsync(object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_wa.PhoneNumberId) || string.IsNullOrWhiteSpace(_wa.AccessToken))
        {
            logger.LogWarning("WhatsApp Meta API não configurada (PhoneNumberId/AccessToken).");
            return new WhatsAppSendResult(false, ErrorCode: "NOT_CONFIGURED", ErrorMessage: "Credenciais Meta ausentes.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://graph.facebook.com/v19.0/{_wa.PhoneNumberId}/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _wa.AccessToken);
        request.Content = JsonContent.Create(payload);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de rede ao enviar mensagem WhatsApp Meta.");
            return new WhatsAppSendResult(false, ErrorCode: "NETWORK", ErrorMessage: ex.Message);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var (code, message) = ParseMetaError(responseBody);
            logger.LogError("Falha WhatsApp API ({Status}): {Error}", (int)response.StatusCode, responseBody);
            return new WhatsAppSendResult(false, ErrorCode: code, ErrorMessage: message);
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var externalId = doc.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
            return new WhatsAppSendResult(true, ExternalId: externalId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resposta Meta sem message id: {Body}", responseBody);
            return new WhatsAppSendResult(false, ErrorCode: "INVALID_RESPONSE", ErrorMessage: "Resposta Meta inválida.");
        }
    }

    private static (string Code, string Message) ParseMetaError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.TryGetProperty("code", out var codeProp)
                    ? codeProp.GetRawText()
                    : "META_ERROR";
                var message = error.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString() ?? body
                    : body;
                return (code, message);
            }
        }
        catch
        {
            // fall through
        }

        return ("META_ERROR", body.Length > 500 ? body[..500] : body);
    }
}
