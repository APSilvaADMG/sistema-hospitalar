namespace SistemaHospitalar.Infrastructure.Tiss;

/// <summary>
/// Perfis de integração TISS para as principais operadoras do mercado brasileiro.
/// URLs são templates — cada prestador configura o endpoint real no portal da operadora.
/// Com UseMockIntegration=true (padrão), o mock simula respostas realistas por operadora.
/// </summary>
public static class OperatorIntegrationProfiles
{
    public static IReadOnlyList<OperatorProfile> All { get; } =
    [
        Profile("BRADESCO", ["Bradesco Saúde"], 10, true,
            "Autorização online obrigatória para internação, OPME e SP/SADT de alto custo. Prazo padrão: 10 dias úteis.",
            "https://www.bradescoseguros.com.br/prestadores"),
        Profile("AMIL", ["Amil", "One Health"], 7, true,
            "Elegibilidade e autorização via portal Amil / One Health. Guias TISS 4.03.",
            "https://www.amil.com.br/prestadores"),
        Profile("SULAMERICA", ["Sul América Saúde", "SulAmérica Saúde"], 10, true,
            "Autorização prévia para internações e procedimentos de alta complexidade.",
            "https://www.sulamerica.com.br/saude/prestadores"),
        Profile("HAPVIDA", ["Hapvida"], 5, true,
            "Operadora com alto volume no Norte/Nordeste. Autorização ágil para urgência.",
            "https://www.hapvida.com.br/prestador"),
        Profile("UNIMED", ["Seguros Unimed", "Unimed Nacional", "Unimed Guarulhos", "Unimed Jundiaí", "Unimed Santos"], 7, true,
            "Rede cooperativa — cada singular pode ter endpoint próprio. Usar código da singular no cadastro.",
            "https://www.unimed.coop.br/prestador"),
        Profile("PORTO", ["Porto Seguro"], 10, true,
            "Integração TISS via portal Porto Seguro Saúde.",
            "https://www.portoseguro.com.br/saude/prestadores"),
        Profile("GNDI", ["Notre Dame"], 10, true,
            "GNDI / Notre Dame Intermédica — autorização para internação e OPME.",
            "https://www.gndi.com.br/prestadores"),
        Profile("GOLDENCROSS", ["Golden Cross"], 10, true,
            "Autorização via portal Golden Cross / Qualicorp.",
            "https://www.goldencross.com.br"),
        Profile("CAREPLUS", ["Care Plus"], 5, true,
            "Planos premium — autorização rápida para eletivos.",
            "https://www.careplus.com.br"),
        Profile("PREVENT", ["Prevent Senior"], 3, true,
            "Modelo de medicina preventiva — autorização simplificada para ambulatorial.",
            "https://www.preventsenior.com.br"),
    ];

    public static OperatorProfile? FindByOperatorCode(string? operatorCode)
    {
        if (string.IsNullOrWhiteSpace(operatorCode))
            return null;
        return All.FirstOrDefault(p =>
            p.OperatorCode.Equals(operatorCode, StringComparison.OrdinalIgnoreCase));
    }

    public static OperatorProfile? FindByName(string name)
    {
        var normalized = Normalize(name);
        foreach (var profile in All)
        {
            if (profile.Names.Any(n => Normalize(n) == normalized || normalized.Contains(Normalize(n))))
                return profile;
        }
        return null;
    }

    public static void ApplyTo(OperatorProfile profile, Domain.Entities.HealthInsurance entity)
    {
        entity.OperatorCode ??= profile.OperatorCode;
        entity.TissVersion ??= "4.03.00";
        entity.AuthorizationDeadlineDays ??= profile.AuthorizationDeadlineDays;
        entity.RequiresOnlineAuthorization = profile.RequiresOnlineAuthorization;
        entity.BusinessRules ??= profile.BusinessRules;
        entity.PortalUrl ??= profile.PortalUrl;
        entity.WebServiceUrl ??= profile.WebServiceUrlTemplate;
    }

    private static OperatorProfile Profile(
        string code,
        string[] names,
        int deadlineDays,
        bool requiresOnline,
        string rules,
        string portal) => new(
        code,
        names,
        deadlineDays,
        requiresOnline,
        rules,
        portal,
        $"https://tiss-gateway.local/{code.ToLowerInvariant()}/v4");

    private static string Normalize(string value) =>
        new string(value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray())
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace("-", string.Empty);
}

public sealed record OperatorProfile(
    string OperatorCode,
    string[] Names,
    int AuthorizationDeadlineDays,
    bool RequiresOnlineAuthorization,
    string BusinessRules,
    string PortalUrl,
    string WebServiceUrlTemplate);
