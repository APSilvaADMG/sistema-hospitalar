using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-ATD-007 / RN-CAN-001 — Cancelamento e histórico de status de atendimento.
/// </summary>
public static class AttendanceRules
{
    public static void ValidateCancellationJustification(AppointmentStatus requestedStatus, string? justification)
    {
        if (requestedStatus != AppointmentStatus.Cancelled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.CancellationJustification}] Cancelamento exige justificativa.");
        }
    }

    public static void ValidateCancellationJustification(TelemedicineStatus requestedStatus, string? justification)
    {
        if (requestedStatus != TelemedicineStatus.Cancelled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.CancellationJustification}] Cancelamento exige justificativa.");
        }
    }
}
