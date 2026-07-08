using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

public static class WarehouseRules
{
    public static void ValidateLotTraceabilityForMedication(
        ProductType productType,
        string? batchNumber,
        Guid? productLotId)
    {
        if (productType != ProductType.Medication)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(batchNumber) && !productLotId.HasValue)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.LotTraceabilityRequired}] Medicamentos exigem rastreabilidade por lote na saída.");
        }
    }

    public static void ValidateDisposableNoReturn(string? category, string reason)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var isDisposable = category.Contains("descart", StringComparison.OrdinalIgnoreCase);
        var isReturn = reason.Contains("devolu", StringComparison.OrdinalIgnoreCase);

        if (isDisposable && isReturn)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.DisposableNoReturn}] Itens descartáveis não podem retornar ao estoque após consumo.");
        }
    }

    public static void ValidateExpiredLot(DateOnly? expiryDate)
    {
        HospitalBusinessRules.ValidateMedicationNotExpired(expiryDate);
    }

    public static void ValidateLotQuantity(decimal lotQuantityOnHand, decimal requested)
    {
        if (requested <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        if (lotQuantityOnHand < requested)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.StockAvailable}] Estoque insuficiente no lote. Disponível: {lotQuantityOnHand}.");
        }
    }
}
