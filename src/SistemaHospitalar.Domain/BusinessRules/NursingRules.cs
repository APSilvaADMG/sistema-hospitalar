namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-011b — Cinco certos na administração de medicamentos.
/// </summary>
public static class NursingRules
{
    public static void ValidateFiveRights(
        string medicationName,
        string dose,
        string route,
        DateTime? scheduledAt = null)
    {
        if (string.IsNullOrWhiteSpace(medicationName))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.FiveRights}] Medicamento correto: informe o nome do medicamento.");
        }

        if (string.IsNullOrWhiteSpace(dose))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.FiveRights}] Dose correta: informe a dose administrada.");
        }

        if (string.IsNullOrWhiteSpace(route))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.FiveRights}] Via correta: informe a via de administração.");
        }

        if (scheduledAt.HasValue)
        {
            var diffMinutes = Math.Abs((DateTime.UtcNow - scheduledAt.Value).TotalMinutes);
            if (diffMinutes > 120)
            {
                throw new InvalidOperationException(
                    $"[{BusinessRuleCodes.FiveRights}] Horário correto: administração fora da janela prevista (>2h).");
            }
        }
    }
}
