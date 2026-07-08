using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Integrations;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Infrastructure.Connect;
using SistemaHospitalar.Infrastructure.OfficialUpdates;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Pix;

namespace SistemaHospitalar.Infrastructure.Services;

public class IntegrationReadinessService(
    IOptions<ConnectSettings> connectOptions,
    IOptions<OfficialUpdatesSettings> officialUpdatesOptions,
    WhatsAppHealthService whatsAppHealth,
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory) : IIntegrationReadinessService
{
    private const string DemoModeLabel = "Modo demonstração";
    private const string ProductionModeLabel = "Produção";

    public async Task<IntegrationReadinessDto> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        var whatsApp = BuildWhatsAppReadiness();
        var pix = BuildPixReadiness();
        var tiss = await BuildTissReadinessAsync(cancellationToken);
        return new IntegrationReadinessDto(whatsApp, pix, tiss);
    }

    public async Task<IntegrationTestResultDto> TestWhatsAppAsync(CancellationToken cancellationToken = default)
    {
        var wa = connectOptions.Value.WhatsApp;
        var health = whatsAppHealth.GetReport();
        var testedAt = DateTime.UtcNow;
        var details = new List<string>();

        if (!wa.Enabled)
        {
            return new IntegrationTestResultDto("whatsapp", false, "WhatsApp desabilitado na configuração.", details, testedAt);
        }

        if (wa.UseMockProvider)
        {
            details.Add("Provedor mock ativo — mensagens são simuladas localmente.");
            details.Add("Para produção: defina Connect__WhatsApp__UseMockProvider=false e credenciais Meta.");
            return new IntegrationTestResultDto("whatsapp", true, "Modo demonstração operacional.", details, testedAt);
        }

        if (!health.MetaConfigured)
        {
            return new IntegrationTestResultDto(
                "whatsapp",
                false,
                "Credenciais Meta incompletas (PhoneNumberId / AccessToken).",
                health.Issues.ToList(),
                testedAt);
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wa.AccessToken);
        var url = $"https://graph.facebook.com/v19.0/{wa.PhoneNumberId}?fields=display_phone_number,verified_name";

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                details.Add($"HTTP {(int)response.StatusCode}");
                details.Add(body.Length > 300 ? body[..300] : body);
                return new IntegrationTestResultDto("whatsapp", false, "Falha ao validar número na Meta Cloud API.", details, testedAt);
            }

            using var doc = JsonDocument.Parse(body);
            var phone = doc.RootElement.TryGetProperty("display_phone_number", out var phoneProp)
                ? phoneProp.GetString()
                : null;
            var name = doc.RootElement.TryGetProperty("verified_name", out var nameProp)
                ? nameProp.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(phone))
            {
                details.Add($"Número: {phone}");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                details.Add($"Nome verificado: {name}");
            }

            if (!health.WebhookSecretConfigured)
            {
                details.Add("Aviso: App Secret ausente — webhook inseguro em produção.");
            }

            if (string.IsNullOrWhiteSpace(wa.PublicWebhookUrl))
            {
                details.Add("Aviso: PublicWebhookUrl não definida para o Meta Developer Console.");
            }

            return new IntegrationTestResultDto(
                "whatsapp",
                true,
                "Conexão Meta Cloud API validada com sucesso.",
                details,
                testedAt);
        }
        catch (Exception ex)
        {
            details.Add(ex.Message);
            return new IntegrationTestResultDto("whatsapp", false, "Erro de rede ao contactar Meta Cloud API.", details, testedAt);
        }
    }

    public Task<IntegrationTestResultDto> TestPixAsync(CancellationToken cancellationToken = default)
    {
        var col = connectOptions.Value.Collection;
        var testedAt = DateTime.UtcNow;
        var details = new List<string>();

        if (!col.PixEnabled)
        {
            return Task.FromResult(new IntegrationTestResultDto("pix", false, "PIX desabilitado na configuração.", details, testedAt));
        }

        if (string.IsNullOrWhiteSpace(col.PixKey))
        {
            return Task.FromResult(new IntegrationTestResultDto("pix", false, "Chave PIX não configurada.", details, testedAt));
        }

        if (string.IsNullOrWhiteSpace(col.PixBeneficiary) || string.IsNullOrWhiteSpace(col.PixCity))
        {
            return Task.FromResult(new IntegrationTestResultDto(
                "pix",
                false,
                "Beneficiário ou cidade PIX incompletos.",
                details,
                testedAt));
        }

        try
        {
            var sample = PixBrCodeHelper.BuildCopyPasteCode(
                col.PixKey,
                col.PixBeneficiary,
                col.PixCity,
                0.01m,
                PixBrCodeHelper.GenerateTxId());

            details.Add($"Chave: {col.PixKey}");
            details.Add($"Webhook: POST /api/pix/webhook (header X-Pix-Webhook-Secret)");
            details.Add($"BR Code gerado: {sample.Length} caracteres (teste R$ 0,01)");

            if (col.UseMockPixProvider)
            {
                details.Add("Modo demonstração — baixa automática via simulação ou webhook local.");
                details.Add("Para produção: integre PSP (Efi, Mercado Pago, etc.) e defina UseMockPixProvider=false.");
                return Task.FromResult(new IntegrationTestResultDto("pix", true, "Configuração PIX demonstração válida.", details, testedAt));
            }

            if (string.IsNullOrWhiteSpace(col.PixWebhookSecret))
            {
                details.Add("Aviso: PixWebhookSecret ausente — webhook aceitará qualquer requisição.");
            }

            return Task.FromResult(new IntegrationTestResultDto("pix", true, "Configuração PIX produção válida.", details, testedAt));
        }
        catch (Exception ex)
        {
            details.Add(ex.Message);
            return Task.FromResult(new IntegrationTestResultDto("pix", false, "Falha ao gerar BR Code de teste.", details, testedAt));
        }
    }

    public async Task<IntegrationTestResultDto> TestTissAsync(Guid? operatorId = null, CancellationToken cancellationToken = default)
    {
        var testedAt = DateTime.UtcNow;
        var details = new List<string>();

        if (operatorId is null)
        {
            var operators = await dbContext.HealthInsurances.AsNoTracking()
                .Where(h => h.IsActive && h.Name != "Particular")
                .ToListAsync(cancellationToken);

            if (operators.Count == 0)
            {
                return new IntegrationTestResultDto("tiss", false, "Nenhuma operadora cadastrada para TISS.", details, testedAt);
            }

            var demo = operators.Count(h => h.UseMockIntegration);
            var live = operators.Count - demo;
            var configured = operators.Count(h => !string.IsNullOrWhiteSpace(h.WebServiceUrl));

            details.Add($"Operadoras: {operators.Count} ({demo} demonstração, {live} produção)");
            details.Add($"Com WebServiceUrl: {configured}");
            details.Add("Configure cada operadora em Faturamento TISS → Integrações.");

            var ready = demo > 0 || configured > 0;
            return new IntegrationTestResultDto(
                "tiss",
                ready,
                ready ? "TISS operacional — modo demonstração ou operadoras configuradas." : "Nenhuma operadora pronta para envio.",
                details,
                testedAt);
        }

        var insurer = await dbContext.HealthInsurances.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == operatorId && h.IsActive, cancellationToken);

        if (insurer is null)
        {
            return new IntegrationTestResultDto("tiss", false, "Operadora não encontrada.", details, testedAt);
        }

        details.Add($"Operadora: {insurer.Name}");

        if (insurer.UseMockIntegration)
        {
            details.Add("Modo demonstração — elegibilidade e envio simulados localmente.");
            return new IntegrationTestResultDto("tiss", true, $"TISS demonstração OK — {insurer.Name}.", details, testedAt);
        }

        if (string.IsNullOrWhiteSpace(insurer.WebServiceUrl))
        {
            return new IntegrationTestResultDto(
                "tiss",
                false,
                $"WebServiceUrl não configurada para {insurer.Name}.",
                details,
                testedAt);
        }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, insurer.WebServiceUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            details.Add($"Endpoint: {insurer.WebServiceUrl}");
            details.Add($"HTTP {(int)response.StatusCode}");

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                return new IntegrationTestResultDto("tiss", true, $"Endpoint TISS acessível — {insurer.Name}.", details, testedAt);
            }

            return new IntegrationTestResultDto(
                "tiss",
                false,
                $"Endpoint TISS retornou {(int)response.StatusCode} — {insurer.Name}.",
                details,
                testedAt);
        }
        catch (Exception ex)
        {
            details.Add($"Endpoint: {insurer.WebServiceUrl}");
            details.Add(ex.Message);
            return new IntegrationTestResultDto("tiss", false, $"Falha ao contactar WebService — {insurer.Name}.", details, testedAt);
        }
    }

    private WhatsAppReadinessDto BuildWhatsAppReadiness()
    {
        var wa = connectOptions.Value.WhatsApp;
        var health = whatsAppHealth.GetReport();
        var configVars = new List<IntegrationConfigVarDto>
        {
            ConfigVar("Connect__WhatsApp__UseMockProvider", "Connect:WhatsApp:UseMockProvider",
                "false = Produção Meta Cloud API", !wa.UseMockProvider, true),
            ConfigVar("Connect__WhatsApp__AccessToken", "Connect:WhatsApp:AccessToken",
                "Token permanente da Meta (WABA)", !string.IsNullOrWhiteSpace(wa.AccessToken), true),
            ConfigVar("Connect__WhatsApp__PhoneNumberId", "Connect:WhatsApp:PhoneNumberId",
                "ID do número WhatsApp Business", !string.IsNullOrWhiteSpace(wa.PhoneNumberId), true),
            ConfigVar("Connect__WhatsApp__VerifyToken", "Connect:WhatsApp:VerifyToken",
                "Token de verificação do webhook Meta", !string.IsNullOrWhiteSpace(wa.VerifyToken), true),
            ConfigVar("Connect__WhatsApp__AppSecret", "Connect:WhatsApp:AppSecret",
                "App Secret para validar assinatura do webhook", !string.IsNullOrWhiteSpace(wa.AppSecret), true),
            ConfigVar("Connect__WhatsApp__PublicWebhookUrl", "Connect:WhatsApp:PublicWebhookUrl",
                "URL pública HTTPS (ex.: https://dominio/api/whatsapp/webhook)", !string.IsNullOrWhiteSpace(wa.PublicWebhookUrl), true),
        };

        return new WhatsAppReadinessDto(
            wa.Enabled,
            wa.UseMockProvider,
            wa.UseMockProvider ? DemoModeLabel : ProductionModeLabel,
            health.Ready,
            health.LiveMode,
            health.ProviderName,
            "/api/whatsapp/webhook",
            string.IsNullOrWhiteSpace(wa.PublicWebhookUrl) ? null : wa.PublicWebhookUrl,
            configVars,
            health.Issues);
    }

    private PixReadinessDto BuildPixReadiness()
    {
        var col = connectOptions.Value.Collection;
        var issues = new List<string>();

        if (!col.PixEnabled)
        {
            issues.Add("PIX desabilitado (Connect:Collection:PixEnabled=false).");
        }

        if (string.IsNullOrWhiteSpace(col.PixKey))
        {
            issues.Add("Chave PIX não configurada.");
        }

        if (!col.UseMockPixProvider && string.IsNullOrWhiteSpace(col.PixWebhookSecret))
        {
            issues.Add("PixWebhookSecret ausente — obrigatório em produção.");
        }

        var ready = col.PixEnabled
            && !string.IsNullOrWhiteSpace(col.PixKey)
            && !string.IsNullOrWhiteSpace(col.PixBeneficiary)
            && (col.UseMockPixProvider || !string.IsNullOrWhiteSpace(col.PixWebhookSecret));

        var configVars = new List<IntegrationConfigVarDto>
        {
            ConfigVar("Connect__Collection__UseMockPixProvider", "Connect:Collection:UseMockPixProvider",
                "false = PSP real (Efi, Mercado Pago, etc.)", !col.UseMockPixProvider, true),
            ConfigVar("Connect__Collection__PixKey", "Connect:Collection:PixKey",
                "Chave PIX do hospital (e-mail, CNPJ, aleatória)", !string.IsNullOrWhiteSpace(col.PixKey), true),
            ConfigVar("Connect__Collection__PixBeneficiary", "Connect:Collection:PixBeneficiary",
                "Nome do recebedor no BR Code", !string.IsNullOrWhiteSpace(col.PixBeneficiary), true),
            ConfigVar("Connect__Collection__PixCity", "Connect:Collection:PixCity",
                "Cidade do recebedor (sem acentos)", !string.IsNullOrWhiteSpace(col.PixCity), true),
            ConfigVar("Connect__Collection__PixWebhookSecret", "Connect:Collection:PixWebhookSecret",
                "Segredo do header X-Pix-Webhook-Secret", !string.IsNullOrWhiteSpace(col.PixWebhookSecret), true),
        };

        return new PixReadinessDto(
            col.PixEnabled,
            col.UseMockPixProvider,
            col.UseMockPixProvider ? DemoModeLabel : ProductionModeLabel,
            ready,
            col.PixAutoConfirmEnabled,
            "/api/pix/webhook",
            configVars,
            issues);
    }

    private async Task<TissReadinessDto> BuildTissReadinessAsync(CancellationToken cancellationToken)
    {
        var operators = await dbContext.HealthInsurances.AsNoTracking()
            .Where(h => h.IsActive && h.Name != "Particular")
            .OrderBy(h => h.Name)
            .Select(h => new TissOperatorReadinessDto(
                h.Id,
                h.Name,
                h.UseMockIntegration,
                h.WebServiceUrl != null && h.WebServiceUrl != "",
                h.TissVersion))
            .ToListAsync(cancellationToken);

        var demoCount = operators.Count(o => o.DemoMode);
        var liveCount = operators.Count - demoCount;
        var configuredCount = operators.Count(o => o.WebServiceConfigured);

        var issues = new List<string>();
        if (operators.Count == 0)
        {
            issues.Add("Cadastre operadoras em Convênios para habilitar faturamento TISS.");
        }
        else if (liveCount > 0 && configuredCount < liveCount)
        {
            issues.Add($"{liveCount - configuredCount} operadora(s) em produção sem WebServiceUrl.");
        }

        var allDemo = operators.Count > 0 && demoCount == operators.Count;
        var modeLabel = operators.Count == 0
            ? DemoModeLabel
            : allDemo
                ? DemoModeLabel
                : liveCount > 0 && demoCount > 0
                    ? "Misto (demonstração + produção)"
                    : ProductionModeLabel;

        var ready = operators.Count > 0 && (allDemo || configuredCount > 0);

        var tissSource = officialUpdatesOptions.Value.Tiss;
        var configVars = new List<IntegrationConfigVarDto>
        {
            new(
                "(banco de dados)",
                "HealthInsurance.WebServiceUrl",
                "URL do webservice TISS por operadora (Faturamento TISS → Integrações)",
                configuredCount > 0,
                true),
            new(
                "(banco de dados)",
                "HealthInsurance.UseMockIntegration",
                "false = envio real à operadora",
                liveCount > 0,
                false),
            new(
                "OfficialUpdates__Tiss__SourceUrl",
                "OfficialUpdates:Tiss:SourceUrl",
                $"Layout ANS — {tissSource.DisplayName}",
                !string.IsNullOrWhiteSpace(tissSource.SourceUrl),
                false),
        };

        return new TissReadinessDto(
            operators.Count,
            demoCount,
            liveCount,
            configuredCount,
            modeLabel,
            ready,
            operators,
            configVars,
            issues);
    }

    private static IntegrationConfigVarDto ConfigVar(
        string envKey,
        string appsettingsPath,
        string description,
        bool isConfigured,
        bool requiredForProduction)
        => new(envKey, appsettingsPath, description, isConfigured, requiredForProduction);
}
