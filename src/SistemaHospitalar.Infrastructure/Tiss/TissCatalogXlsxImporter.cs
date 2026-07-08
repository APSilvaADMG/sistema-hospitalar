using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissCatalogXlsxImporter
{
    private const uint MaxHeaderRow = 30;
    private const int DefaultBatchSize = 1500;
    private const int MaxDescriptionLength = 300;
    private static readonly Regex CodePattern = new(@"^\d{4,12}$", RegexOptions.Compiled);

    public static IReadOnlyList<TussCatalogImportItem> ParseFile(string filePath)
    {
        var items = new List<TussCatalogImportItem>();
        StreamFile(filePath, batch =>
        {
            items.AddRange(batch);
            return Task.CompletedTask;
        }).GetAwaiter().GetResult();
        return items;
    }

    public static async Task<StreamParseResult> StreamFile(
        string filePath,
        Func<IReadOnlyList<TussCatalogImportItem>, Task> onBatch,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        using var document = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException($"Arquivo XLSX inválido: {Path.GetFileName(filePath)}");

        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var tableType = ResolveTableType(filePath);
        var parsed = 0;
        var batches = 0;
        var batch = new List<TussCatalogImportItem>(batchSize);

        foreach (var sheet in workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetParsed = await StreamWorksheetAsync(
                worksheetPart,
                sharedStringTable,
                tableType,
                item =>
                {
                    batch.Add(item);
                    parsed++;
                    if (batch.Count < batchSize)
                        return Task.CompletedTask;

                    batches++;
                    var current = batch.ToList();
                    batch.Clear();
                    return onBatch(current);
                },
                cancellationToken);

            if (!sheetParsed)
                continue;
        }

        if (batch.Count > 0)
        {
            batches++;
            await onBatch(batch.ToList());
        }

        return new StreamParseResult(parsed, batches);
    }

    private static async Task<bool> StreamWorksheetAsync(
        WorksheetPart worksheetPart,
        SharedStringTable? sharedStringTable,
        TussTableType tableType,
        Func<TussCatalogImportItem, Task> onItem,
        CancellationToken cancellationToken)
    {
        using var reader = OpenXmlReader.Create(worksheetPart);
        var headerRows = new List<Row>();
        ColumnMap? columns = null;
        uint? headerExcelRow = null;
        var parsedAny = false;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                continue;

            var row = (Row)reader.LoadCurrentElement()!;
            var rowNumber = row.RowIndex?.Value ?? (uint)(headerRows.Count + 1);

            if (columns is null)
            {
                if (rowNumber > MaxHeaderRow)
                    break;

                headerRows.Add(row);
                var (_, detected) = DetectColumns(headerRows, sharedStringTable);
                if (detected.Code >= 0)
                {
                    columns = detected;
                    headerExcelRow = rowNumber;
                }

                continue;
            }

            if (headerExcelRow.HasValue && rowNumber <= headerExcelRow.Value)
                continue;

            var cells = MapRowCells(row);
            var item = ParseRow(cells, columns, sharedStringTable, tableType);
            if (item is null)
                continue;

            parsedAny = true;
            await onItem(item);
        }

        return parsedAny;
    }

    private static TussCatalogImportItem? ParseRow(
        IReadOnlyDictionary<string, Cell> cells,
        ColumnMap columns,
        SharedStringTable? sharedStringTable,
        TussTableType tableType)
    {
        var code = NormalizeCode(GetCellValue(cells, columns.Code, sharedStringTable));
        if (code is null)
            return null;

        var term = GetCellValue(cells, columns.Term, sharedStringTable)?.Trim();
        var detail = GetCellValue(cells, columns.Detail, sharedStringTable)?.Trim();
        var description = !string.IsNullOrWhiteSpace(term) ? term : detail;

        if (string.IsNullOrWhiteSpace(description) || NormalizeCode(description) == code)
        {
            description = ResolveDescriptionFallback(cells, columns, sharedStringTable, code);
            if (string.IsNullOrWhiteSpace(description))
                return null;

            term = description;
        }

        if (!string.IsNullOrWhiteSpace(detail)
            && !string.IsNullOrWhiteSpace(term)
            && !description.Contains(detail, StringComparison.OrdinalIgnoreCase)
            && detail.Length < 180)
        {
            description = $"{term} — {detail}";
        }

        var resolvedType = tableType == TussTableType.Daily && IsFeeTerm(term ?? description)
            ? TussTableType.Fee
            : tableType;

        var unit = GetCellValue(cells, columns.Unit, sharedStringTable)?.Trim();
        var referencePrice = ParseReferencePrice(GetCellValue(cells, columns.ReferencePrice, sharedStringTable));

        return new TussCatalogImportItem(
            code,
            Truncate(TissDescriptionFormatter.Format(description), MaxDescriptionLength),
            resolvedType,
            string.IsNullOrWhiteSpace(unit) ? null : unit,
            referencePrice);
    }

    private static string? ResolveDescriptionFallback(
        IReadOnlyDictionary<string, Cell> cells,
        ColumnMap columns,
        SharedStringTable? sharedStringTable,
        string code)
    {
        if (columns.Term >= 0)
        {
            var termValue = GetCellValue(cells, columns.Term, sharedStringTable)?.Trim();
            if (!string.IsNullOrWhiteSpace(termValue) && NormalizeCode(termValue) != code)
                return termValue;
        }

        if (columns.Detail >= 0)
        {
            var detailValue = GetCellValue(cells, columns.Detail, sharedStringTable)?.Trim();
            if (!string.IsNullOrWhiteSpace(detailValue) && NormalizeCode(detailValue) != code)
                return detailValue;
        }

        if (columns.Code < 0)
            return null;

        for (var offset = 1; offset <= 3; offset++)
        {
            var candidate = GetCellValue(cells, columns.Code + offset, sharedStringTable)?.Trim();
            if (string.IsNullOrWhiteSpace(candidate) || NormalizeCode(candidate) == code)
                continue;

            return candidate;
        }

        return null;
    }

    private static bool IsFeeTerm(string value)
        => value.Contains("TAXA", StringComparison.OrdinalIgnoreCase);

    private static TussTableType ResolveTableType(string filePath)
    {
        var name = (Path.GetFileName(filePath) + " " + Path.GetDirectoryName(filePath)).ToUpperInvariant();
        if (name.Contains("TUSS 22") || name.Contains("PROCEDIMENTOS"))
            return TussTableType.Procedure;
        if (name.Contains("TUSS 20") || name.Contains("MEDICAMENT"))
            return TussTableType.Medication;
        if (name.Contains("TUSS 19") || name.Contains("OPME") || name.Contains("MATERIAIS"))
            return TussTableType.Material;
        if (name.Contains("TUSS 18") || name.Contains("DIÁRIA") || name.Contains("DIARIA") || name.Contains("TAXAS"))
            return TussTableType.Daily;
        return TussTableType.Procedure;
    }

    private static (int HeaderRowIndex, ColumnMap Columns) DetectColumns(
        IReadOnlyList<Row> rows,
        SharedStringTable? sharedStringTable)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = rows[i].RowIndex?.Value ?? (uint)(i + 1);
            if (rowNumber > MaxHeaderRow)
                break;

            var cells = MapRowCells(rows[i]);
            var headers = cells.Values
                .Select(v => GetRawCellValue(v, sharedStringTable)?.Trim().ToLowerInvariant() ?? string.Empty)
                .Where(h => h.Length > 0)
                .ToList();

            if (!headers.Any(h => h.Contains("código") || h.Contains("codigo")))
                continue;

            var map = new ColumnMap
            {
                Code = FindColumnIndex(cells, sharedStringTable, -1,
                    "código do termo", "codigo do termo", "código", "codigo"),
            };

            map.Term = FindColumnIndex(cells, sharedStringTable, map.Code, "termo");
            map.Detail = FindColumnIndex(cells, sharedStringTable, map.Code,
                "descrição detalhada do termo", "descricao detalhada do termo",
                "descrição detalhada", "descricao detalhada",
                "descrição", "descricao");
            map.Unit = FindColumnIndex(cells, sharedStringTable, [map.Code, map.Term, map.Detail],
                "unidade de medida", "unidade medida", "unidade", "unid.", "unid", "und");
            map.ReferencePrice = FindColumnIndex(cells, sharedStringTable, [map.Code, map.Term, map.Detail, map.Unit],
                "valor de referência", "valor de referencia", "valor referência", "valor referencia",
                "valor ref.", "valor ref", "referência", "referencia",
                "preço", "preco", "preço referência", "preco referencia",
                "valor", "valor r$", "r$");

            // ANS TUSS sheets place the readable label in the column right after the code.
            if (map.Term < 0 && map.Code >= 0)
                map.Term = map.Code + 1;

            return (i, map);
        }

        return (-1, new ColumnMap());
    }

    private static int FindColumnIndex(
        IReadOnlyDictionary<string, Cell> cells,
        SharedStringTable? sharedStringTable,
        int excludeIndex,
        params string[] keywords)
        => FindColumnIndex(cells, sharedStringTable, [excludeIndex], keywords);

    private static int FindColumnIndex(
        IReadOnlyDictionary<string, Cell> cells,
        SharedStringTable? sharedStringTable,
        IEnumerable<int> excludedIndexes,
        params string[] keywords)
    {
        var excluded = excludedIndexes
            .Where(i => i >= 0)
            .ToHashSet();

        var ordered = cells
            .Select(kv => (Index: ColumnLetterToIndex(kv.Key), Cell: kv.Value))
            .Where(x => !excluded.Contains(x.Index))
            .OrderBy(x => x.Index)
            .ToList();

        foreach (var (index, cell) in ordered)
        {
            var text = GetRawCellValue(cell, sharedStringTable)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (keywords.Any(k => text == k))
                return index;
        }

        var matchesTerm = keywords.Any(k => k == "termo");
        var matchesDescription = keywords.Any(k => k.Contains("descri", StringComparison.Ordinal));

        foreach (var (index, cell) in ordered)
        {
            var text = GetRawCellValue(cell, sharedStringTable)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!keywords.Any(k => text.Contains(k, StringComparison.Ordinal)))
                continue;

            if (matchesTerm
                && text != "termo"
                && (text.Contains("código") || text.Contains("codigo")))
                continue;

            if (matchesDescription && (text.Contains("código") || text.Contains("codigo")))
                continue;

            return index;
        }

        return -1;
    }

    private static decimal? ParseReferencePrice(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var normalized = rawValue
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('\u00A0', ' ')
            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out var br))
            return br;

        if (normalized.Contains(','))
        {
            var brLike = normalized.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
            if (decimal.TryParse(brLike, NumberStyles.Number, CultureInfo.InvariantCulture, out var brConverted))
                return brConverted;
        }

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
            return invariant;

        return null;
    }

    private static int ColumnLetterToIndex(string columnLetters)
    {
        var sum = 0;
        foreach (var ch in columnLetters.ToUpperInvariant())
        {
            if (ch is < 'A' or > 'Z')
                continue;
            sum = sum * 26 + (ch - 'A' + 1);
        }

        return sum - 1;
    }

    private static Dictionary<string, Cell> MapRowCells(Row row)
    {
        var map = new Dictionary<string, Cell>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in row.Elements<Cell>())
        {
            var reference = cell.CellReference?.Value;
            if (string.IsNullOrWhiteSpace(reference))
                continue;
            var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
            map[letters] = cell;
        }

        return map;
    }

    private static string? GetCellValue(
        IReadOnlyDictionary<string, Cell> cells,
        int columnIndex,
        SharedStringTable? sharedStringTable)
    {
        if (columnIndex < 0)
            return null;

        var columnLetters = IndexToColumnLetters(columnIndex);
        return cells.TryGetValue(columnLetters, out var cell)
            ? GetRawCellValue(cell, sharedStringTable)
            : null;
    }

    private static string IndexToColumnLetters(int index)
    {
        var dividend = index + 1;
        var result = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            result = Convert.ToChar('A' + modulo) + result;
            dividend = (dividend - modulo) / 26;
        }

        return result;
    }

    private static string? GetRawCellValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        var value = cell.CellValue?.InnerText;
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            value = cell.InlineString?.InnerText;
            if (string.IsNullOrWhiteSpace(value))
                value = cell.InnerText;
        }

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (cell.DataType?.Value == CellValues.SharedString
            && sharedStringTable is not null
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ssIndex)
            && ssIndex >= 0
            && ssIndex < sharedStringTable.ChildElements.Count)
        {
            return sharedStringTable.ChildElements[ssIndex].InnerText;
        }

        return value;
    }

    private static string? NormalizeCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return CodePattern.IsMatch(digits) ? digits : null;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];

    private sealed class ColumnMap
    {
        public int Code { get; set; } = -1;
        public int Term { get; set; } = -1;
        public int Detail { get; set; } = -1;
        public int Unit { get; set; } = -1;
        public int ReferencePrice { get; set; } = -1;
    }

    public sealed record StreamParseResult(int ParsedRows, int Batches);
}
