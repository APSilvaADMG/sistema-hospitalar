using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Interfaces;

namespace SistemaHospitalar.Infrastructure.Connect;

public class MockWhatsAppProvider(ILogger<MockWhatsAppProvider> logger) : IWhatsAppProvider
{
    public string ProviderName => "mock";
    public bool IsMock => true;

    public Task<WhatsAppSendResult> SendTextAsync(string phone, string body, CancellationToken cancellationToken = default)
    {
        var id = $"mock-{Guid.NewGuid():N}"[..16];
        logger.LogInformation("[WhatsApp Mock] Para {Phone}: {Body}", MaskPhone(phone), body.Length > 120 ? body[..120] + "…" : body);
        return Task.FromResult(new WhatsAppSendResult(true, ExternalId: id));
    }

    public Task<WhatsAppSendResult> SendTemplateAsync(
        string phone,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default)
    {
        var id = $"mock-tpl-{Guid.NewGuid():N}"[..20];
        logger.LogInformation(
            "[WhatsApp Mock Template] Para {Phone}: {Template} ({Lang}) params={Params}",
            MaskPhone(phone),
            templateName,
            languageCode,
            string.Join(", ", bodyParameters));
        return Task.FromResult(new WhatsAppSendResult(true, ExternalId: id));
    }

    private static string MaskPhone(string phone)
        => phone.Length <= 4 ? "****" : new string('*', phone.Length - 4) + phone[^4..];
}
