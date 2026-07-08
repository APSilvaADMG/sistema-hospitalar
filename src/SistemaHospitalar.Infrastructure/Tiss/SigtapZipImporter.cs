using System.Globalization;

using System.IO.Compression;

using System.Text;

using System.Text.RegularExpressions;



namespace SistemaHospitalar.Infrastructure.Tiss;



public static class SigtapZipImporter

{

    private static readonly Regex CompetenceRegex = new(@"(?<!\d)(20\d{2})(0[1-9]|1[0-2])(?!\d)", RegexOptions.Compiled);



    private static readonly HashSet<string> IgnoredEntryNames = new(StringComparer.OrdinalIgnoreCase)

    {

        "layout.txt",

        "leia_me.txt",

        "config.inf",

        "versao",

    };



    public static async Task<SigtapParseResult> ParseAsync(

        Stream fileStream,

        string fileName,

        CancellationToken cancellationToken = default)

    {

        var lowerName = fileName.ToLowerInvariant();

        return lowerName.EndsWith(".zip", StringComparison.Ordinal)

            ? await ParseZipAsync(fileStream, fileName, cancellationToken)

            : await ParseSingleTextAsync(fileStream, fileName, cancellationToken);

    }



    private static async Task<SigtapParseResult> ParseZipAsync(

        Stream fileStream,

        string fileName,

        CancellationToken cancellationToken)

    {

        var competence = TryParseCompetence(fileName);

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);



        var candidates = GetCandidateEntries(archive);

        if (candidates.Count == 0)

        {

            var entryNames = archive.Entries

                .Where(e => !string.IsNullOrEmpty(e.Name))

                .Select(e => e.FullName)

                .Take(12)

                .ToList();

            var preview = entryNames.Count > 0

                ? $" Arquivos encontrados: {string.Join(", ", entryNames)}."

                : " O arquivo ZIP está vazio.";

            return new SigtapParseResult(

                [],

                competence ?? string.Empty,

                "Nenhum arquivo SIGTAP reconhecível no .zip (esperado tb_procedimento.txt ou equivalente)." + preview);

        }



        var procedureEntries = candidates

            .Where(e => IsProcedureTableEntry(e))

            .ToList();

        var entriesToParse = procedureEntries.Count > 0 ? procedureEntries : candidates;



        var items = new List<SigtapParsedItem>();

        foreach (var entry in entriesToParse)

        {

            cancellationToken.ThrowIfCancellationRequested();

            var entryCompetence = TryParseCompetence(entry.Name) ?? competence;

            var parsed = await ParseEntryAsync(entry, entryCompetence, cancellationToken);

            if (parsed.Count == 0)

                continue;



            competence ??= entryCompetence ?? parsed.Select(i => i.Competence).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

            items.AddRange(parsed);



            if (procedureEntries.Count > 0 && IsProcedureTableEntry(entry))

                break;

        }



        if (items.Count == 0)

        {

            var parsedNames = string.Join(", ", entriesToParse.Select(e => e.FullName).Take(8));

            return new SigtapParseResult(

                [],

                competence ?? string.Empty,

                $"Nenhum procedimento SIGTAP válido encontrado nos arquivos do .zip (analisados: {parsedNames}). " +

                "Verifique se o pacote é a Tabela Unificada oficial do DATASUS.");

        }



        var finalCompetence = competence

            ?? items.Select(i => i.Competence).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

        if (string.IsNullOrWhiteSpace(finalCompetence))

            throw new InvalidOperationException("Não foi possível identificar a competência SIGTAP (YYYY-MM) no nome do arquivo.");



        var normalized = NormalizeItems(items, finalCompetence);

        return new SigtapParseResult(

            normalized,

            finalCompetence,

            $"{normalized.Count} procedimento(s) SIGTAP lido(s) do arquivo {fileName}.");

    }



    private static async Task<SigtapParseResult> ParseSingleTextAsync(

        Stream fileStream,

        string fileName,

        CancellationToken cancellationToken)

    {

        var competence = TryParseCompetence(fileName)

            ?? throw new InvalidOperationException("Não foi possível identificar a competência no nome do arquivo .txt (use YYYYMM).");

        var items = await ParseTextInternalAsync(fileStream, fileName, competence, cancellationToken);

        var normalized = NormalizeItems(items, competence);

        if (normalized.Count == 0)

            return new SigtapParseResult([], competence, "Nenhum procedimento válido encontrado no .txt informado.");



        return new SigtapParseResult(

            normalized,

            competence,

            $"{normalized.Count} procedimento(s) SIGTAP lido(s) do arquivo {fileName}.");

    }



    private static List<ZipArchiveEntry> GetCandidateEntries(ZipArchive archive)

    {

        return archive.Entries

            .Where(IsSigtapCandidateEntry)

            .OrderBy(GetEntryPriority)

            .ThenBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)

            .ToList();

    }



    private static bool IsSigtapCandidateEntry(ZipArchiveEntry entry)

    {

        if (string.IsNullOrEmpty(entry.Name))

            return false;



        var baseName = Path.GetFileName(entry.Name);

        if (IgnoredEntryNames.Contains(baseName))

            return false;



        if (baseName.EndsWith("_layout.txt", StringComparison.OrdinalIgnoreCase))

            return false;



        if (baseName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))

            return false;



        if (baseName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))

            return true;



        var stem = Path.GetFileNameWithoutExtension(baseName);

        return stem.StartsWith("tb_", StringComparison.OrdinalIgnoreCase)

            || stem.StartsWith("rl_procedimento", StringComparison.OrdinalIgnoreCase);

    }



    private static bool IsProcedureTableEntry(ZipArchiveEntry entry)

    {

        var stem = Path.GetFileNameWithoutExtension(entry.Name);

        return stem.Equals("tb_procedimento", StringComparison.OrdinalIgnoreCase);

    }



    private static int GetEntryPriority(ZipArchiveEntry entry)

    {

        var stem = Path.GetFileNameWithoutExtension(entry.Name);

        if (stem.Equals("tb_procedimento", StringComparison.OrdinalIgnoreCase))

            return 0;

        if (stem.Contains("procedimento", StringComparison.OrdinalIgnoreCase))

            return 1;

        return 2;

    }



    private static async Task<IReadOnlyList<SigtapParsedItem>> ParseEntryAsync(

        ZipArchiveEntry entry,

        string? fallbackCompetence,

        CancellationToken cancellationToken)

    {

        await using var entryStream = entry.Open();

        using var memory = new MemoryStream();

        await entryStream.CopyToAsync(memory, cancellationToken);

        memory.Position = 0;

        return await ParseTextInternalAsync(memory, entry.Name, fallbackCompetence, cancellationToken);

    }



    private static async Task<IReadOnlyList<SigtapParsedItem>> ParseTextInternalAsync(

        Stream fileStream,

        string sourceName,

        string? fallbackCompetence,

        CancellationToken cancellationToken)

    {

        fileStream.Position = 0;

        var text = await ReadAllTextAsync(fileStream, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))

            return [];



        var lines = text

            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Where(l => l.Length > 2)

            .ToList();

        if (lines.Count == 0)

            return [];



        foreach (var delimiter in new[] { ';', '|', '\t' })

        {

            var parsed = ParseDelimited(lines, delimiter, sourceName, fallbackCompetence);

            if (parsed.Count > 0)

                return parsed;

        }



        if (IsDatasusFixedWidthFormat(lines))

            return ParseDatasusFixedWidth(lines, sourceName, fallbackCompetence);



        return ParseLegacyFixedWidth(lines, sourceName, fallbackCompetence);

    }



    private static bool IsDatasusFixedWidthFormat(IReadOnlyList<string> lines)

    {

        var sample = lines

            .Where(l => !l.Contains("CO_PROCEDIMENTO", StringComparison.OrdinalIgnoreCase))

            .Take(25)

            .ToList();

        if (sample.Count == 0)

            return false;



        return sample.Count(l => l.Length >= 330) >= Math.Max(1, sample.Count / 2);

    }



    private static List<SigtapParsedItem> ParseDatasusFixedWidth(

        IReadOnlyList<string> lines,

        string sourceName,

        string? fallbackCompetence)

    {

        var items = new List<SigtapParsedItem>();

        foreach (var rawLine in lines)

        {

            if (rawLine.Length < 330 || rawLine.Contains("CO_PROCEDIMENTO", StringComparison.OrdinalIgnoreCase))

                continue;



            var line = rawLine.PadRight(336);

            var code = NormalizeCode(line[..10]);

            if (code is null)

                continue;



            var description = line[10..260].Trim();

            if (string.IsNullOrWhiteSpace(description))

                continue;



            var competence = ResolveCompetence(line[330..336], fallbackCompetence, sourceName);

            if (string.IsNullOrWhiteSpace(competence))

                continue;



            var complexity = NullIfEmpty(line[260..261]);

            var hospitalAmount = ParseDecimal(line[282..294]);

            var professionalAmount = ParseDecimal(line[294..306]);

            var groupName = ResolveGroupNameFromCode(code);



            items.Add(new SigtapParsedItem(

                code,

                competence,

                description,

                groupName,

                complexity,

                hospitalAmount,

                professionalAmount));

        }



        return items;

    }



    private static List<SigtapParsedItem> ParseDelimited(

        IReadOnlyList<string> lines,

        char delimiter,

        string sourceName,

        string? fallbackCompetence)

    {

        var headerIndex = -1;

        for (var i = 0; i < lines.Count; i++)

        {

            if (lines[i].Contains("CO_PROCEDIMENTO", StringComparison.OrdinalIgnoreCase))

            {

                headerIndex = i;

                break;

            }

        }

        if (headerIndex < 0)

            return [];



        var headers = lines[headerIndex].Split(delimiter).Select(NormalizeHeader).ToList();

        var codeIndex = FindHeaderIndex(headers, "CO_PROCEDIMENTO", "CODIGO", "PROCEDIMENTO");

        var nameIndex = FindHeaderIndex(headers, "NO_PROCEDIMENTO", "DS_PROCEDIMENTO", "NOME_PROCEDIMENTO");

        if (codeIndex < 0 || nameIndex < 0)

            return [];



        var complexityIndex = FindHeaderIndex(headers, "TP_COMPLEXIDADE", "CO_COMPLEXIDADE");

        var groupIndex = FindHeaderIndex(headers, "NO_GRUPO", "CO_GRUPO", "NO_GRUPO_PROCEDIMENTO");

        var vlShIndex = FindHeaderIndex(headers, "VL_SH", "VALOR_SH");

        var vlSaIndex = FindHeaderIndex(headers, "VL_SA", "VALOR_SA");

        var competenceIndex = FindHeaderIndex(headers, "NU_COMPETENCIA", "COMPETENCIA", "CO_COMPETENCIA", "DT_COMPETENCIA");



        var items = new List<SigtapParsedItem>();

        for (var i = headerIndex + 1; i < lines.Count; i++)

        {

            var columns = lines[i].Split(delimiter);

            if (columns.Length <= Math.Max(codeIndex, nameIndex))

                continue;



            var code = NormalizeCode(columns[codeIndex]);

            if (code is null)

                continue;



            var description = columns[nameIndex].Trim();

            if (string.IsNullOrWhiteSpace(description))

                continue;



            var competence = ResolveCompetence(

                competenceIndex >= 0 && competenceIndex < columns.Length ? columns[competenceIndex] : null,

                fallbackCompetence,

                sourceName);

            if (string.IsNullOrWhiteSpace(competence))

                continue;



            var groupName = groupIndex >= 0 && groupIndex < columns.Length

                ? NullIfEmpty(columns[groupIndex])

                : ResolveGroupNameFromCode(code);



            var complexity = complexityIndex >= 0 && complexityIndex < columns.Length

                ? NullIfEmpty(columns[complexityIndex])

                : null;



            var hospitalAmount = vlShIndex >= 0 && vlShIndex < columns.Length ? ParseDecimal(columns[vlShIndex]) : null;

            var professionalAmount = vlSaIndex >= 0 && vlSaIndex < columns.Length ? ParseDecimal(columns[vlSaIndex]) : null;



            items.Add(new SigtapParsedItem(

                code,

                competence,

                description,

                groupName,

                complexity,

                hospitalAmount,

                professionalAmount));

        }



        return items;

    }



    private static List<SigtapParsedItem> ParseLegacyFixedWidth(

        IReadOnlyList<string> lines,

        string sourceName,

        string? fallbackCompetence)

    {

        var competence = ResolveCompetence(null, fallbackCompetence, sourceName);

        if (string.IsNullOrWhiteSpace(competence))

            return [];



        var items = new List<SigtapParsedItem>();

        foreach (var rawLine in lines)

        {

            if (rawLine.Length < 50 || rawLine.Contains("CO_PROCEDIMENTO", StringComparison.OrdinalIgnoreCase))

                continue;



            var line = rawLine.PadRight(220);

            var code = NormalizeCode(line[..10]);

            if (code is null)

                continue;



            var description = line[10..170].Trim();

            if (string.IsNullOrWhiteSpace(description))

                continue;



            var complexity = NullIfEmpty(line[170..172]);

            var hospitalAmount = ParseDecimal(line[172..186]);

            var professionalAmount = ParseDecimal(line[186..200]);

            var groupName = ResolveGroupNameFromCode(code);



            items.Add(new SigtapParsedItem(

                code,

                competence,

                description,

                groupName,

                complexity,

                hospitalAmount,

                professionalAmount));

        }



        return items;

    }



    private static List<SigtapParsedItem> NormalizeItems(IEnumerable<SigtapParsedItem> parsed, string fallbackCompetence)

    {

        return parsed

            .Where(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Description))

            .Select(x => x with

            {

                Competence = string.IsNullOrWhiteSpace(x.Competence) ? fallbackCompetence : x.Competence,

                Description = x.Description.Trim(),

                GroupName = NullIfEmpty(x.GroupName),

                Complexity = NullIfEmpty(x.Complexity),

            })

            .GroupBy(x => new { x.Code, x.Competence })

            .Select(g => g.First())

            .ToList();

    }



    private static string NormalizeHeader(string value)

        => value.Trim().ToUpperInvariant();



    private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] candidates)

    {

        for (var i = 0; i < headers.Count; i++)

        {

            var header = headers[i];

            if (candidates.Any(c => header.Equals(c, StringComparison.OrdinalIgnoreCase)))

                return i;

        }



        for (var i = 0; i < headers.Count; i++)

        {

            var header = headers[i];

            if (candidates.Any(c => header.Contains(c, StringComparison.OrdinalIgnoreCase)))

                return i;

        }



        return -1;

    }



    private static string? ResolveCompetence(string? fromField, string? fallbackCompetence, string sourceName)

    {

        var normalizedField = TryParseCompetence(fromField);

        if (!string.IsNullOrWhiteSpace(normalizedField))

            return normalizedField;

        if (!string.IsNullOrWhiteSpace(fallbackCompetence))

            return fallbackCompetence;

        return TryParseCompetence(sourceName);

    }



    private static string? TryParseCompetence(string? value)

    {

        if (string.IsNullOrWhiteSpace(value))

            return null;



        var match = CompetenceRegex.Match(value);

        if (!match.Success)

            return null;



        var year = match.Groups[1].Value;

        var month = match.Groups[2].Value;

        return $"{year}-{month}";

    }



    private static string? NormalizeCode(string? raw)

    {

        if (string.IsNullOrWhiteSpace(raw))

            return null;



        var digits = new string(raw.Where(char.IsDigit).ToArray());

        return digits.Length >= 6 ? digits : null;

    }



    private static string? NullIfEmpty(string? value)

        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();



    private static decimal? ParseDecimal(string? raw)

    {

        if (string.IsNullOrWhiteSpace(raw))

            return null;



        var normalized = raw

            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)

            .Replace('\u00A0', ' ')

            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))

            return null;



        if (decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out var ptBr))

            return ptBr;

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv))

            return inv;



        if (normalized.All(char.IsDigit) && normalized.Length >= 3

            && decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var cents))

            return cents / 100m;



        return null;

    }



    private static async Task<string> ReadAllTextAsync(Stream stream, CancellationToken cancellationToken)

    {

        cancellationToken.ThrowIfCancellationRequested();

        stream.Position = 0;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var text = await reader.ReadToEndAsync();

        if (!string.IsNullOrWhiteSpace(text))

            return text;



        cancellationToken.ThrowIfCancellationRequested();

        stream.Position = 0;

        using var latin = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1"), detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        return await latin.ReadToEndAsync();

    }



    private static string ResolveGroupNameFromCode(string code)

        => code.Length < 2

            ? "Outros"

            : code[..2] switch

            {

                "01" => "Ações de Promoção e Prevenção",

                "02" => "Procedimentos com Finalidade Diagnóstica",

                "03" => "Procedimentos Clínicos",

                "04" => "Procedimentos Cirúrgicos",

                "05" => "Transplantes",

                "06" => "Medicamentos",

                "07" => "Órteses, Próteses e Materiais Especiais",

                "08" => "Ações Complementares da Atenção à Saúde",

                _ => "Outros",

            };

}



public sealed record SigtapParsedItem(

    string Code,

    string Competence,

    string Description,

    string? GroupName,

    string? Complexity,

    decimal? HospitalAmount,

    decimal? ProfessionalAmount);



public sealed record SigtapParseResult(

    IReadOnlyList<SigtapParsedItem> Items,

    string Competence,

    string Message);


