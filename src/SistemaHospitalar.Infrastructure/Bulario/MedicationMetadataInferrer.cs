using System.Text.RegularExpressions;

namespace SistemaHospitalar.Infrastructure.Bulario;

public static partial class MedicationMetadataInferrer
{
    private static readonly (Regex Pattern, string Route)[] RouteHints =
    [
        (ViaOral(), "VO"),
        (Intravenosa(), "IV"),
        (Intramuscular(), "IM"),
        (Subcutanea(), "SC"),
        (Topica(), "Tópica"),
        (Oftalmica(), "Oftálmica"),
        (Nasal(), "Nasal"),
        (Retal(), "Retal"),
        (Inalatoria(), "Inalatória"),
    ];

    public static string FormatDisplayName(string name, string? strength)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(strength))
            return trimmedName;

        var normalizedStrength = strength.Trim();
        if (trimmedName.Contains(normalizedStrength, StringComparison.OrdinalIgnoreCase))
            return trimmedName;

        var compactName = Compact(trimmedName);
        var compactStrength = Compact(normalizedStrength);
        if (compactName.Contains(compactStrength, StringComparison.OrdinalIgnoreCase))
            return trimmedName;

        return $"{trimmedName} {normalizedStrength}";
    }

    public static string? InferStrength(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var match = StrengthPattern().Match(name);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public static string? InferRoute(string? pharmaceuticalForm, string? packageInsert)
    {
        var corpus = $"{pharmaceuticalForm}\n{packageInsert}".ToLowerInvariant();
        foreach (var (pattern, route) in RouteHints)
        {
            if (pattern.IsMatch(corpus))
                return route;
        }

        if (string.IsNullOrWhiteSpace(pharmaceuticalForm))
            return null;

        var form = pharmaceuticalForm.ToLowerInvariant();
        if (form.Contains("comprim") || form.Contains("caps") || form.Contains("cáps")
            || form.Contains("gotas") || form.Contains("xarope") || form.Contains("suspens")
            || form.Contains("solução oral") || form.Contains("solucao oral"))
            return "VO";

        if (form.Contains("injet") || form.Contains("infus"))
            return "IV";

        if (form.Contains("creme") || form.Contains("pomada") || form.Contains("gel"))
            return "Tópica";

        if (form.Contains("oftalm") || form.Contains("colírio") || form.Contains("colirio"))
            return "Oftálmica";

        if (form.Contains("spray") && form.Contains("nasal"))
            return "Nasal";

        if (form.Contains("suposit"))
            return "Retal";

        return null;
    }

    public static string? NormalizeForm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var form = value.Trim();
        var lower = form.ToLowerInvariant();
        if (lower.Contains("comprim"))
            return "Comprimido";
        if (lower.Contains("caps") || lower.Contains("cáps"))
            return "Cápsula";
        if (lower.Contains("gotas") && !lower.Contains("oft"))
            return "Gotas";
        if (lower.Contains("xarope") || lower.Contains("suspens"))
            return "Suspensão oral";
        if (lower.Contains("solu") && lower.Contains("oral"))
            return "Solução oral";
        if (lower.Contains("injet") || lower.Contains("infus"))
            return "Solução injetável";
        if (lower.Contains("creme"))
            return "Creme";
        if (lower.Contains("pomada"))
            return "Pomada";
        if (lower.Contains("gel"))
            return "Gel";
        if (lower.Contains("spray") && lower.Contains("nasal"))
            return "Spray nasal";
        if (lower.Contains("oftalm") || lower.Contains("colírio") || lower.Contains("colirio"))
            return "Solução oftálmica";
        if (lower.Contains("suposit"))
            return "Supositório";

        return form;
    }

    private static string Compact(string value) =>
        value.Replace(" ", "", StringComparison.Ordinal)
            .Replace("/", "", StringComparison.Ordinal);

    [GeneratedRegex(
        @"(\d+(?:[.,]\d+)?\s*(?:mg|mcg|µg|g|UI|U|%|mL|mg/mL|mcg/mL|UI/mL)(?:\s*/\s*\d+(?:[.,]\d+)?\s*(?:mg|mcg|g|UI|mL|mg/mL|mcg/mL|UI/mL))*)",
        RegexOptions.IgnoreCase)]
    private static partial Regex StrengthPattern();

    [GeneratedRegex(@"\bvia oral\b|\bpor via oral\b", RegexOptions.IgnoreCase)]
    private static partial Regex ViaOral();

    [GeneratedRegex(@"\bintravenos[a]?\b|\bvia intravenos[a]?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Intravenosa();

    [GeneratedRegex(@"\bintramuscular\b|\bvia intramuscular\b", RegexOptions.IgnoreCase)]
    private static partial Regex Intramuscular();

    [GeneratedRegex(@"\bsubcut[aâ]ne[a]?\b|\bvia subcut[aâ]ne[a]?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Subcutanea();

    [GeneratedRegex(@"\bt[oó]pic[oa]?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Topica();

    [GeneratedRegex(@"\boft[aá]lmic[oa]?\b|\bcol[ií]rio\b", RegexOptions.IgnoreCase)]
    private static partial Regex Oftalmica();

    [GeneratedRegex(@"\bnasal\b|\bspray nasal\b", RegexOptions.IgnoreCase)]
    private static partial Regex Nasal();

    [GeneratedRegex(@"\bretal\b|\bsuposit[oó]rio\b", RegexOptions.IgnoreCase)]
    private static partial Regex Retal();

    [GeneratedRegex(@"\binhalat[oó]ri[oa]?\b|\binala[cç][aã]o\b", RegexOptions.IgnoreCase)]
    private static partial Regex Inalatoria();
}
