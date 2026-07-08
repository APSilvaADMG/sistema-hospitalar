namespace SistemaHospitalar.Infrastructure.TvSignage;

/// <summary>Texto padronizado de chamada por voz (PT-BR) para TVs e TTS.</summary>
public static class TvCallSpeech
{
    public static string FormatPatientCall(string patientName, string? roomOrDestination)
    {
        var destination = FormatSpeechDestination(roomOrDestination);
        var lower = destination.ToLowerInvariant();
        var preposition = lower.Contains("guich") ? "ao" : "ao";
        return $"{patientName.Trim()}, dirija-se {preposition} {destination}.";
    }

    public static string FormatDisplayDestination(string? room)
    {
        var trimmed = room?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "Consultório indicado";
        }

        var lower = trimmed.ToLowerInvariant();
        if (lower.Contains("guich") || lower.Contains("consult") || lower.StartsWith("sala "))
        {
            return trimmed;
        }

        return $"Consultório {trimmed}";
    }

    private static string FormatSpeechDestination(string? roomOrDestination)
    {
        var label = FormatDisplayDestination(roomOrDestination);
        var lower = label.ToLowerInvariant();

        if (lower.Contains("guich"))
        {
            return label;
        }

        if (lower.StartsWith("sala "))
        {
            var digits = System.Text.RegularExpressions.Regex.Match(label, @"\d+");
            return digits.Success ? $"consultório {digits.Value}" : label.Replace("Sala ", "consultório ", StringComparison.OrdinalIgnoreCase);
        }

        if (lower.StartsWith("consultório") || lower.StartsWith("consultorio"))
        {
            return label;
        }

        return $"consultório {label}";
    }
}
