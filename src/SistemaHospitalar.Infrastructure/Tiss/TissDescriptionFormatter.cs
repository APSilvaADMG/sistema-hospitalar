using System.Text.RegularExpressions;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissDescriptionFormatter
{
    private static readonly HashSet<string> PtSmallWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "da", "do", "das", "dos", "e", "em", "na", "no", "nas", "nos", "a", "o", "as", "os",
    };

    private static readonly Regex WordPattern = new(
        @"^([^A-Za-zÀ-ÿ]*)([\p{L}0-9]+)(.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Format(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = Regex.Replace(text.Trim(), @"\s+", " ");
        if (!IsMostlyUppercase(normalized))
            return normalized;

        var words = normalized.Split(' ');
        for (var i = 0; i < words.Length; i++)
            words[i] = FormatWord(words[i], i == 0);

        return string.Join(' ', words);
    }

    private static bool IsMostlyUppercase(string text)
    {
        var upper = 0;
        var total = 0;
        foreach (var ch in text)
        {
            if (!char.IsLetter(ch))
                continue;

            total++;
            if (char.IsUpper(ch))
                upper++;
        }

        return total > 0 && upper / (double)total > 0.7;
    }

    private static bool IsPreservedAcronym(string core)
    {
        if (core.Length is < 2 or > 4)
            return false;

        foreach (var ch in core)
        {
            if (!char.IsUpper(ch) && !char.IsDigit(ch))
                return false;
        }

        return true;
    }

    private static string FormatWord(string word, bool isFirstWord)
    {
        var match = WordPattern.Match(word);
        if (!match.Success)
            return word;

        var prefix = match.Groups[1].Value;
        var core = match.Groups[2].Value;
        var suffix = match.Groups[3].Value;

        var lower = core.ToLowerInvariant();
        if (!isFirstWord && PtSmallWords.Contains(lower))
            return $"{prefix}{lower}{suffix}";

        if (IsPreservedAcronym(core))
            return $"{prefix}{core}{suffix}";

        return $"{prefix}{char.ToUpper(lower[0])}{lower[1..]}{suffix}";
    }
}
