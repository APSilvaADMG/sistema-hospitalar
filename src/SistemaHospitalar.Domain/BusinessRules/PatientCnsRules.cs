namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// Validação do Cartão Nacional de Saúde (CNS) — algoritmo de soma ponderada mod 11
/// (referência: sistema Madre/Basis TI).
/// </summary>
public static class PatientCnsRules
{
    public const int RequiredLength = 15;

    public static string Normalize(string? cns) =>
        new string((cns ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsMissing(string? cns)
    {
        var normalized = Normalize(cns);
        return normalized.Length == 0;
    }

    public static bool IsValidChecksum(string normalizedCns)
    {
        if (normalizedCns.Length != RequiredLength)
        {
            return false;
        }

        if (!normalizedCns.All(char.IsDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < normalizedCns.Length; i++)
        {
            sum += (normalizedCns[i] - '0') * (RequiredLength - i);
        }

        return sum % 11 == 0;
    }

    public static void ValidateFormat(string normalizedCns)
    {
        if (normalizedCns.Length != RequiredLength)
        {
            throw new InvalidOperationException("CNS deve conter 15 dígitos.");
        }

        if (!IsValidChecksum(normalizedCns))
        {
            throw new InvalidOperationException("CNS inválido — dígito verificador incorreto.");
        }
    }

    public static void ValidateForRegistration(string? cns)
    {
        if (IsMissing(cns))
        {
            return;
        }

        ValidateFormat(Normalize(cns));
    }
}
