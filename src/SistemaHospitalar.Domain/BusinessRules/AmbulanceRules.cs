using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

/// <summary>
/// RN-AMB-* — Regras de frota e despacho de ambulâncias.
/// </summary>
public static class AmbulanceRules
{
    public static void ValidateAvailableForDispatch(AmbulanceStatus status)
    {
        if (status == AmbulanceStatus.Maintenance)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.AmbulanceMaintenanceBlocksDispatch}] Ambulância em manutenção não pode ser despachada.");
        }

        if (status != AmbulanceStatus.Available)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.AmbulanceMaintenanceBlocksDispatch}] Ambulância indisponível para despacho.");
        }
    }

    public static void ValidateUniqueCode(bool codeExists, string code)
    {
        if (!codeExists)
        {
            return;
        }

        throw new InvalidOperationException(
            $"[{BusinessRuleCodes.AmbulanceUniqueCode}] Já existe ambulância cadastrada com o código \"{code}\".");
    }

    public static void ValidateUniquePlate(bool plateExists, string plate)
    {
        if (!plateExists)
        {
            return;
        }

        throw new InvalidOperationException(
            $"[{BusinessRuleCodes.AmbulanceUniquePlate}] Já existe ambulância cadastrada com a placa \"{plate}\".");
    }

    public static void ValidateDispatchStatusTransition(
        AmbulanceDispatchStatus current,
        AmbulanceDispatchStatus requested)
    {
        if (requested == AmbulanceDispatchStatus.Cancelled && current == AmbulanceDispatchStatus.Completed)
        {
            throw new InvalidOperationException(
                "Não é possível cancelar um despacho já concluído.");
        }
    }
}
