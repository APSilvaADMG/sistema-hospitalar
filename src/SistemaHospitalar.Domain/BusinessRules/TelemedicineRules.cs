using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-TEL-* — Agendamento e sessão de telemedicina.
/// </summary>
public static class TelemedicineRules
{
    public static void ValidateFutureSchedule(DateTime scheduledAt, DateTime? referenceUtc = null)
    {
        var reference = referenceUtc ?? DateTime.UtcNow;
        var scheduledUtc = scheduledAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc)
            : scheduledAt.ToUniversalTime();

        if (scheduledUtc <= reference)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.TelemedicineFutureSchedule}] Teleconsulta deve ser agendada para data/hora futura.");
        }
    }

    public static void ValidateCompletedRequiresStart(TelemedicineStatus requestedStatus, DateTime? startedAt)
    {
        if (requestedStatus == TelemedicineStatus.Completed && startedAt is null)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.TelemedicineSessionStart}] Conclusão exige sessão iniciada (StartedAt).");
        }
    }
}
