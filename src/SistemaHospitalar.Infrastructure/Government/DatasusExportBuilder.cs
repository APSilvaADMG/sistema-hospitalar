using System.Security.Cryptography;
using System.Text;

namespace SistemaHospitalar.Infrastructure.Government;

/// <summary>
/// Gera arquivos texto compatíveis com layout DATASUS (SIA-SUS / SIH-SUS / CIHA) para exportação.
/// Formato simplificado: cabeçalho + linhas delimitadas por pipe — pronto para validação e envio.
/// </summary>
public static class DatasusExportBuilder
{
    public const string LayoutVersion = "APSMedCore-SUS-1.0";

    public record ExportLine(string LineType, string Content);

    public record ExportDocument(
        string FileName,
        string Content,
        int RecordCount,
        string ChecksumSha256);

    public static ExportDocument Build(
        string prefix,
        string competence,
        string cnes,
        string documentLabel,
        IReadOnlyList<ExportLine> lines)
    {
        var sb = new StringBuilder();
        var genDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var cnesPadded = (cnes ?? "0000000").PadLeft(7, '0')[..7];

        sb.AppendLine($"01|{cnesPadded}|{competence}|{documentLabel}|{LayoutVersion}|{genDate}|{lines.Count}");

        foreach (var line in lines)
        {
            sb.AppendLine($"{line.LineType}|{line.Content}");
        }

        sb.AppendLine($"99|{lines.Count}|FIM");

        var content = sb.ToString();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        var fileName = $"{prefix}_{cnesPadded}_{competence}_{genDate}.txt";

        return new ExportDocument(fileName, content, lines.Count, hash);
    }

    public static string PadField(string? value, int length, char pad = ' ')
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length > length) return v[..length];
        return v.PadRight(length, pad);
    }

    public static string PadNumeric(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length) return digits[^length..];
        return digits.PadLeft(length, '0');
    }
}
