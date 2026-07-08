namespace SistemaHospitalar.Application.DTOs.Integrations;

public record IntegrationConfigVarDto(
    string EnvKey,
    string AppsettingsPath,
    string Description,
    bool IsConfigured,
    bool RequiredForProduction);

public record WhatsAppReadinessDto(
    bool Enabled,
    bool DemoMode,
    string ModeLabel,
    bool Ready,
    bool LiveMode,
    string ProviderName,
    string WebhookPath,
    string? PublicWebhookUrl,
    IReadOnlyList<IntegrationConfigVarDto> ConfigVars,
    IReadOnlyList<string> Issues);

public record PixReadinessDto(
    bool Enabled,
    bool DemoMode,
    string ModeLabel,
    bool Ready,
    bool AutoConfirmEnabled,
    string WebhookPath,
    IReadOnlyList<IntegrationConfigVarDto> ConfigVars,
    IReadOnlyList<string> Issues);

public record TissOperatorReadinessDto(
    Guid Id,
    string Name,
    bool DemoMode,
    bool WebServiceConfigured,
    string? TissVersion);

public record TissReadinessDto(
    int TotalOperators,
    int DemoOperators,
    int LiveOperators,
    int ConfiguredOperators,
    string ModeLabel,
    bool Ready,
    IReadOnlyList<TissOperatorReadinessDto> Operators,
    IReadOnlyList<IntegrationConfigVarDto> ConfigVars,
    IReadOnlyList<string> Issues);

public record IntegrationReadinessDto(
    WhatsAppReadinessDto WhatsApp,
    PixReadinessDto Pix,
    TissReadinessDto Tiss);

public record IntegrationTestResultDto(
    string Integration,
    bool Success,
    string Message,
    IReadOnlyList<string> Details,
    DateTime TestedAt);
