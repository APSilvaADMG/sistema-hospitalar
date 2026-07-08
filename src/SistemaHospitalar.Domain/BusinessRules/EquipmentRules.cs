using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-EQP-016/017 — Equipamentos clínicos indisponíveis para reserva/uso.
/// </summary>
public static class EquipmentRules
{
    public static void ValidateAvailableForReservation(MedicalEquipmentStatus status)
    {
        if (status is MedicalEquipmentStatus.Maintenance or MedicalEquipmentStatus.OutOfService)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.EquipmentMaintenanceBlocksReservation}] Equipamento em manutenção ou interditado não pode ser reservado.");
        }
    }

    public static void ValidateOperationalForClinicalUse(MedicalEquipmentStatus status)
    {
        ValidateAvailableForReservation(status);

        if (status == MedicalEquipmentStatus.CalibrationDue)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.EquipmentCalibrationDue}] Equipamento com calibração vencida — uso clínico bloqueado até manutenção.");
        }
    }
}
