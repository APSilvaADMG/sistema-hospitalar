using Microsoft.Extensions.Configuration;

namespace SistemaHospitalar.Infrastructure;

/// <summary>
/// Opções gerais do hospital. <see cref="EnableDemoSeeds"/> controla seeds idempotentes
/// que criam pacientes/lançamentos fictícios (homologação vs. demo local).
/// </summary>
public class HospitalOptions
{
    public const string SectionName = "Hospital";

    /// <summary>
    /// Quando false, pula seeds de demonstração (WaitingRoom, BI, financeiro fictício, etc.).
    /// Migrações EF e catálogos essenciais continuam.
    /// </summary>
    public bool EnableDemoSeeds { get; set; } = true;

    /// <summary>
    /// Bootstrap mínimo (migração + usuário admin) para pipelines CI/E2E.
    /// </summary>
    public bool CiMinimalBootstrap { get; set; }

    /// <summary>
    /// Resolve o flag: valor explícito em config/env tem prioridade;
    /// caso contrário, true fora de Production e false em Production.
    /// </summary>
    public static bool ResolveEnableDemoSeeds(IConfiguration configuration)
    {
        var configured = configuration.GetValue<bool?>($"{SectionName}:EnableDemoSeeds");
        if (configured.HasValue)
        {
            return configured.Value;
        }

        var environment = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        return !string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ResolveCiMinimalBootstrap(IConfiguration configuration)
        => configuration.GetValue<bool>($"{SectionName}:CiMinimalBootstrap");
}
