using System.Text.RegularExpressions;

namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-PAC-001 — Número único de paciente no formato PAC0000000001.
/// </summary>
public static partial class PatientRecordNumberRules
{
    public const string Prefix = "PAC";
    public const int SequenceDigits = 10;

    [GeneratedRegex(@"^PAC(\d{10})$", RegexOptions.CultureInvariant)]
    private static partial Regex PacFormatRegex();

    public static string Format(long sequence)
    {
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequência deve ser positiva.");
        }

        return $"{Prefix}{sequence.ToString().PadLeft(SequenceDigits, '0')}";
    }

    public static bool TryParseSequence(string? recordNumber, out long sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(recordNumber))
        {
            return false;
        }

        var match = PacFormatRegex().Match(recordNumber.Trim().ToUpperInvariant());
        if (!match.Success)
        {
            return false;
        }

        return long.TryParse(match.Groups[1].Value, out sequence);
    }

    public static void ValidateFormat(string recordNumber)
    {
        if (!TryParseSequence(recordNumber, out _))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.PatientNumberFormat}] Número de prontuário deve seguir o formato PAC0000000001.");
        }
    }
}
