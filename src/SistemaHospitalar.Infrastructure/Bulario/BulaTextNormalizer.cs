using System.Text.RegularExpressions;

namespace SistemaHospitalar.Infrastructure.Bulario;

public static partial class BulaTextNormalizer
{
    private static readonly string[] CanonicalOrder =
    [
        "INDICAÇÕES",
        "POSOLOGIA",
        "CONTRAINDICAÇÕES",
        "EFEITOS ADVERSOS",
        "INTERAÇÕES",
        "CUIDADOS NA ADMINISTRAÇÃO",
    ];

    private static readonly (Regex Pattern, string Title)[] SectionMarkers =
    [
        (Indicacoes(), "INDICAÇÕES"),
        (Posologia(), "POSOLOGIA"),
        (Contraindicacoes(), "CONTRAINDICAÇÕES"),
        (Efeitos(), "EFEITOS ADVERSOS"),
        (Interacoes(), "INTERAÇÕES"),
        (Gravidez(), "GRAVIDEZ E LACTAÇÃO"),
        (Conservacao(), "CUIDADOS NA ADMINISTRAÇÃO"),
        (Superdosagem(), "SUPERDOSAGEM"),
    ];

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = LeadingMarker().Replace(raw.Trim(), string.Empty).Trim();
        Dictionary<string, string> blocks;

        if (StructuredHeader().IsMatch(text))
            blocks = ParseStructuredBlocks(text);
        else
            blocks = ParseQuestionBlocks(text);

        if (blocks.Count == 0)
            return text;

        if (blocks.TryGetValue("CONSERVAÇÃO", out var conservacao))
        {
            blocks.Remove("CONSERVAÇÃO");
            blocks["CUIDADOS NA ADMINISTRAÇÃO"] = JoinBlocks(
                blocks.GetValueOrDefault("CUIDADOS NA ADMINISTRAÇÃO"),
                conservacao);
        }

        var sections = new List<string>();
        foreach (var title in CanonicalOrder)
        {
            if (!blocks.TryGetValue(title, out var body) || string.IsNullOrWhiteSpace(body))
                continue;

            var maxChars = title == "POSOLOGIA" ? 220 : 320;
            sections.Add($"{title}\n{SummarizeBody(body, maxChars)}");
        }

        return sections.Count == 0 ? text : string.Join("\n\n", sections);
    }

    public static string? ExtractPosologia(string? raw)
    {
        var normalized = Normalize(raw);
        foreach (var block in normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var nl = block.IndexOf('\n');
            if (nl <= 0)
                continue;

            if (!string.Equals(block[..nl].Trim(), "POSOLOGIA", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = block[(nl + 1)..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static Dictionary<string, string> ParseStructuredBlocks(string text)
    {
        var blocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentTitle = null;
        var currentLines = new List<string>();

        foreach (var line in text.Split('\n'))
        {
            var stripped = line.Trim();
            var upper = stripped.ToUpperInvariant();
            if (IsSectionTitle(upper))
            {
                if (currentTitle is not null)
                    blocks[currentTitle] = string.Join('\n', currentLines).Trim();

                currentTitle = upper == "CONSERVAÇÃO" ? "CUIDADOS NA ADMINISTRAÇÃO" : upper;
                currentLines.Clear();
                continue;
            }

            if (currentTitle is not null)
                currentLines.Add(line);
        }

        if (currentTitle is not null)
            blocks[currentTitle] = string.Join('\n', currentLines).Trim();

        return blocks;
    }

    private static Dictionary<string, string> ParseQuestionBlocks(string text)
    {
        var blocks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = SectionMarkers
            .SelectMany(m => m.Pattern.Matches(text).Select(match => (match.Index, m.Title, m.Pattern)))
            .OrderBy(x => x.Index)
            .ToList();

        if (matches.Count == 0)
            return blocks;

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var chunk = text[start..end].Trim();
            var title = matches[i].Title == "CONSERVAÇÃO" ? "CUIDADOS NA ADMINISTRAÇÃO" : matches[i].Title;
            var body = matches[i].Pattern.Replace(chunk, string.Empty, 1).Trim();
            if (string.IsNullOrWhiteSpace(body))
                body = chunk;

            blocks[title] = blocks.TryGetValue(title, out var existing)
                ? JoinBlocks(existing, body)
                : body;
        }

        return blocks;
    }

    private static string SummarizeBody(string body, int maxChars)
    {
        var text = HowItWorks().Replace(body.Trim(), string.Empty).Trim();
        text = Whitespace().Replace(text, " ");
        if (text.Length == 0)
            return body.Trim()[..Math.Min(body.Trim().Length, maxChars)];

        var sentences = SentenceSplit().Split(text)
            .Select(s => s.Trim())
            .Where(s => s.Length > 15)
            .ToList();

        if (sentences.Count == 0)
            return text[..Math.Min(text.Length, maxChars)];

        var parts = new List<string>();
        var total = 0;
        foreach (var sentence in sentences)
        {
            if (total + sentence.Length > maxChars && parts.Count > 0)
                break;

            parts.Add(sentence);
            total += sentence.Length + 1;
        }

        return string.Join(' ', parts);
    }

    private static bool IsSectionTitle(string upper) =>
        Array.Exists(CanonicalOrder, title => title.Equals(upper, StringComparison.OrdinalIgnoreCase))
        || upper is "GRAVIDEZ E LACTAÇÃO" or "CONSERVAÇÃO" or "SUPERDOSAGEM";

    private static string JoinBlocks(string? first, string? second)
    {
        var parts = new[] { first, second }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());
        return string.Join("\n", parts);
    }

    [GeneratedRegex(@"^\s*>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingMarker();

    [GeneratedRegex(
        @"^INDICAÇÕES$|^POSOLOGIA$|^CONTRAINDICAÇÕES$|^EFEITOS ADVERSOS$|^INTERAÇÕES$|^CUIDADOS NA ADMINISTRAÇÃO$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex StructuredHeader();

    [GeneratedRegex(@"(?:,\s*)?para o que [eé] indicado e para o que serve\?", RegexOptions.IgnoreCase)]
    private static partial Regex Indicacoes();

    [GeneratedRegex(@"Como (?:devo usar|usar|tomar)[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Posologia();

    [GeneratedRegex(@"Quais as contraindica(?:ç|c)[oõ]es[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Contraindicacoes();

    [GeneratedRegex(@"Quais (?:os )?efeitos colaterais[^?]*\?|Quais as rea(?:ç|c)[oõ]es adversas[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Efeitos();

    [GeneratedRegex(@"Intera(?:ç|c)[oõ]es medicamentosas[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Interacoes();

    [GeneratedRegex(@"(?:Este medicamento )?pode ser utilizado durante a gravidez[^?]*\?|Gravidez e lacta(?:ç|c)[aã]o[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Gravidez();

    [GeneratedRegex(@"Como devo armazenar[^?]*\?|Conserva(?:ç|c)[aã]o[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Conservacao();

    [GeneratedRegex(@"O que fazer se (?:eu )?tomar[^?]*\?|Superdosagem[^?]*\?", RegexOptions.IgnoreCase)]
    private static partial Regex Superdosagem();

    [GeneratedRegex(@"Como o .+? funciona\?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex HowItWorks();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplit();
}
