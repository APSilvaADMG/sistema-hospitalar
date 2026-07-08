namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-ALT-004 — Pendências antes da alta hospitalar.
/// </summary>
public static class DischargeRules
{
    public static void ValidateDischargePendingChecks(bool hasOpenPrescriptions, bool hasCriticalLabs)
    {
        if (hasOpenPrescriptions)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.DischargePendingChecks}] Alta bloqueada: prescrições ativas pendentes de revisão.");
        }

        if (hasCriticalLabs)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.DischargePendingChecks}] Alta bloqueada: exames laboratoriais críticos pendentes.");
        }
    }
}
