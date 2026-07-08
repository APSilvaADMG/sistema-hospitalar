namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-PRE-006 — Verificação de alergias antes da prescrição.
/// </summary>
public static class PrescriptionRules
{
    public static void ValidateNoAllergyConflict(string prescriptionContent, IEnumerable<string> allergyEntries)
    {
        if (string.IsNullOrWhiteSpace(prescriptionContent))
        {
            return;
        }

        var contentLower = prescriptionContent.ToLowerInvariant();
        foreach (var allergy in allergyEntries)
        {
            if (string.IsNullOrWhiteSpace(allergy))
            {
                continue;
            }

            foreach (var term in ExtractAllergyTerms(allergy))
            {
                if (term.Length >= 4 && contentLower.Contains(term, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"[{BusinessRuleCodes.PrescriptionAllergy}] Prescrição bloqueada: possível conflito com alergia registrada " +
                        $"({term}). Revise o medicamento ou registre justificativa clínica.");
                }
            }
        }
    }

    private static IEnumerable<string> ExtractAllergyTerms(string allergyText)
    {
        var normalized = allergyText.ToLowerInvariant();
        var markers = new[] { "alergia a ", "alergia:", "alérgico a ", "alergico a " };

        foreach (var marker in markers)
        {
            var index = normalized.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            var fragment = normalized[(index + marker.Length)..];
            foreach (var part in fragment.Split([',', ';', '.', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                var term = part.Trim();
                if (term.Length >= 4)
                {
                    yield return term;
                }
            }
        }

        foreach (var word in normalized.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length >= 5 && word is not ("alergia" or "alérgico" or "alergico" or "paciente"))
            {
                yield return word;
            }
        }
    }
}
