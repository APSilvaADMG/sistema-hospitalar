using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-EST-* — Ciclos de esterilização CME.
/// </summary>
public static class SterilizationRules
{
    public static void ValidateNoConcurrentCycle(bool kitHasCycleInProgress)
    {
        if (kitHasCycleInProgress)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SterilizationUniqueCycle}] Kit já possui ciclo de esterilização em andamento.");
        }
    }

    public static void ValidateKitNotAlreadyInSterilization(InstrumentKitStatus kitStatus)
    {
        if (kitStatus == InstrumentKitStatus.InSterilization)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SterilizationKitInProgress}] Kit já está em processo de esterilização.");
        }
    }

    public static void ValidateCanCompleteCycle(SterilizationCycleStatus cycleStatus)
    {
        if (cycleStatus == SterilizationCycleStatus.Failed)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SterilizationFailedNoSterile}] Ciclo reprovado não pode liberar kit como estéril.");
        }

        if (cycleStatus != SterilizationCycleStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SterilizationFailedNoSterile}] Somente ciclos em andamento podem ser concluídos.");
        }
    }

    public static void ValidateCanMarkKitSterile(
        SterilizationCycleStatus cycleStatus,
        DateOnly? expirationDate)
    {
        ValidateCanCompleteCycle(cycleStatus);

        if (!expirationDate.HasValue
            || expirationDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.SterilizationFailedNoSterile}] Validade estéril obrigatória e futura para liberar o kit.");
        }
    }
}
