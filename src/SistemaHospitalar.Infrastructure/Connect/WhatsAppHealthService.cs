using Microsoft.Extensions.Options;

namespace SistemaHospitalar.Infrastructure.Connect;

public sealed class WhatsAppHealthService(IOptions<ConnectSettings> settings)
{
    public WhatsAppHealthReport GetReport()
    {
        var wa = settings.Value.WhatsApp;
        var metaConfigured = !string.IsNullOrWhiteSpace(wa.PhoneNumberId)
            && !string.IsNullOrWhiteSpace(wa.AccessToken);
        var verifyTokenConfigured = !string.IsNullOrWhiteSpace(wa.VerifyToken);
        var webhookSecretConfigured = !string.IsNullOrWhiteSpace(wa.AppSecret);
        var providerName = wa.UseMockProvider ? "mock" : "meta";
        var liveMode = wa.Enabled && !wa.UseMockProvider && metaConfigured;

        var issues = new List<string>();
        if (!wa.Enabled)
        {
            issues.Add("WhatsApp desabilitado na configuração.");
        }

        if (!wa.UseMockProvider && !metaConfigured)
        {
            issues.Add("Modo Meta ativo sem PhoneNumberId/AccessToken.");
        }

        if (!wa.UseMockProvider && !webhookSecretConfigured)
        {
            issues.Add("App Secret não configurado — webhook aceita qualquer POST (inseguro em produção).");
        }

        if (!verifyTokenConfigured)
        {
            issues.Add("Verify Token não configurado — verificação Meta falhará.");
        }

        if (liveMode && string.IsNullOrWhiteSpace(wa.PublicWebhookUrl))
        {
            issues.Add("PublicWebhookUrl não definida — configure no Meta Developer Console.");
        }

        var ready = wa.Enabled
            && (wa.UseMockProvider || (metaConfigured && webhookSecretConfigured && verifyTokenConfigured));

        return new WhatsAppHealthReport(
            wa.Enabled,
            wa.UseMockProvider,
            providerName,
            metaConfigured,
            verifyTokenConfigured,
            webhookSecretConfigured,
            liveMode,
            ready,
            issues);
    }
}

public sealed record WhatsAppHealthReport(
    bool Enabled,
    bool UseMockProvider,
    string ProviderName,
    bool MetaConfigured,
    bool VerifyTokenConfigured,
    bool WebhookSecretConfigured,
    bool LiveMode,
    bool Ready,
    IReadOnlyList<string> Issues);
