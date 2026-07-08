using System.Globalization;
using System.Text;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissCatalogCsvImporter
{
    public static IReadOnlyList<TussCatalogImportItem> Parse(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            return [];

        var lines = csvContent
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
            return [];

        var delimiter = DetectDelimiter(lines[0]);
        var startIndex = LooksLikeHeader(lines[0]) ? 1 : 0;
        var items = new List<TussCatalogImportItem>();

        for (var i = startIndex; i < lines.Length; i++)
        {
            var cols = SplitLine(lines[i], delimiter);
            if (cols.Count < 2)
                continue;

            var mapped = MapColumns(cols);
            if (mapped is null)
                continue;

            items.Add(mapped);
        }

        return items;
    }

    private static char DetectDelimiter(string line)
    {
        var counts = new Dictionary<char, int>
        {
            [';'] = line.Count(c => c == ';'),
            [','] = line.Count(c => c == ','),
            ['\t'] = line.Count(c => c == '\t'),
        };
        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static bool LooksLikeHeader(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("codigo") || lower.Contains("código")
            || lower.Contains("descricao") || lower.Contains("descrição")
            || lower.Contains("tuss");
    }

    private static List<string> SplitLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == delimiter && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    private static TussCatalogImportItem? MapColumns(IReadOnlyList<string> cols)
    {
        if (cols.Count >= 5 && TryParseRow(cols[0], cols[1], cols[2], cols[3], cols[4], out var item))
            return item;

        if (cols.Count >= 3 && TryParseRow(cols[0], cols[1], cols.Count > 2 ? cols[2] : "procedimento", null, cols.Count > 3 ? cols[3] : null, out item))
            return item;

        if (cols.Count == 2 && TryParseRow(cols[0], cols[1], "procedimento", null, null, out item))
            return item;

        return null;
    }

    private static bool TryParseRow(
        string codeRaw,
        string descriptionRaw,
        string? typeRaw,
        string? unitRaw,
        string? priceRaw,
        out TussCatalogImportItem item)
    {
        item = null!;
        var code = new string(codeRaw.Where(char.IsDigit).ToArray());
        if (code.Length < 6)
            return false;

        var description = TissDescriptionFormatter.Format(descriptionRaw.Trim());
        if (string.IsNullOrWhiteSpace(description))
            return false;

        decimal? price = null;
        if (!string.IsNullOrWhiteSpace(priceRaw))
        {
            var normalized = priceRaw.Trim().Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out var br))
                price = br;
            else if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv))
                price = inv;
        }

        item = new TussCatalogImportItem(
            code,
            description,
            MapTableType(typeRaw),
            string.IsNullOrWhiteSpace(unitRaw) ? null : unitRaw.Trim(),
            price);

        return true;
    }

    private static TussTableType MapTableType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return TussTableType.Procedure;

        var key = raw.Trim().ToLowerInvariant();
        return key switch
        {
            "1" or "procedimento" or "procedure" or "proc" => TussTableType.Procedure,
            "2" or "material" or "mat" => TussTableType.Material,
            "3" or "medicamento" or "med" or "medicamento" => TussTableType.Medication,
            "4" or "diaria" or "diária" or "daily" => TussTableType.Daily,
            "5" or "taxa" or "fee" => TussTableType.Fee,
            "6" or "pacote" or "package" => TussTableType.Package,
            _ => TussTableType.Procedure,
        };
    }
}
