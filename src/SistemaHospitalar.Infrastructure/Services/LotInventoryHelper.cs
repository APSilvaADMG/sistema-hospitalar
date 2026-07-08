using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public record LotDeductionLine(
    Guid? ProductLotId,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity);

public static class LotInventoryHelper
{
    public static async Task<IReadOnlyList<LotDeductionLine>> DeductLotsFefoAsync(
        AppDbContext dbContext,
        Product product,
        decimal quantity,
        Guid? preferredLotId,
        CancellationToken cancellationToken)
    {
        var lots = await dbContext.ProductLots
            .Where(l => l.ProductId == product.Id && l.IsActive && l.QuantityOnHand > 0)
            .OrderBy(l => l.ExpiryDate == null)
            .ThenBy(l => l.ExpiryDate)
            .ThenBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        if (lots.Count == 0)
        {
            return [new LotDeductionLine(null, null, null, quantity)];
        }

        var remaining = quantity;
        var lines = new List<LotDeductionLine>();

        if (preferredLotId.HasValue)
        {
            var preferred = lots.FirstOrDefault(l => l.Id == preferredLotId.Value)
                ?? throw new InvalidOperationException("Lote informado não encontrado ou sem saldo.");

            WarehouseRules.ValidateExpiredLot(preferred.ExpiryDate);
            var take = Math.Min(preferred.QuantityOnHand, remaining);
            WarehouseRules.ValidateLotQuantity(preferred.QuantityOnHand, take);
            preferred.QuantityOnHand -= take;
            preferred.UpdatedAt = DateTime.UtcNow;
            lines.Add(new LotDeductionLine(preferred.Id, preferred.BatchNumber, preferred.ExpiryDate, take));
            remaining -= take;
        }

        foreach (var lot in lots.Where(l => l.QuantityOnHand > 0 && l.Id != preferredLotId))
        {
            if (remaining <= 0)
            {
                break;
            }

            WarehouseRules.ValidateExpiredLot(lot.ExpiryDate);
            var take = Math.Min(lot.QuantityOnHand, remaining);
            lot.QuantityOnHand -= take;
            lot.UpdatedAt = DateTime.UtcNow;
            lines.Add(new LotDeductionLine(lot.Id, lot.BatchNumber, lot.ExpiryDate, take));
            remaining -= take;
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.StockAvailable}] Estoque insuficiente nos lotes. Faltam {remaining} {product.Unit}.");
        }

        return lines;
    }

    public static async Task<ProductLot> UpsertLotFromInboundAsync(
        AppDbContext dbContext,
        Product product,
        string batchNumber,
        DateOnly? expiryDate,
        decimal quantity,
        string? manufacturer,
        string? locationName,
        decimal? unitCost,
        CancellationToken cancellationToken)
    {
        var normalizedBatch = batchNumber.Trim();
        var lot = await dbContext.ProductLots
            .FirstOrDefaultAsync(
                l => l.ProductId == product.Id
                    && l.BatchNumber == normalizedBatch
                    && l.IsActive,
                cancellationToken);

        if (lot is null)
        {
            lot = new ProductLot
            {
                ProductId = product.Id,
                BatchNumber = normalizedBatch,
                ExpiryDate = expiryDate,
                Manufacturer = manufacturer?.Trim(),
                LocationName = locationName?.Trim(),
                UnitCost = unitCost,
                QuantityOnHand = quantity,
            };
            dbContext.ProductLots.Add(lot);
        }
        else
        {
            lot.QuantityOnHand += quantity;
            lot.ExpiryDate ??= expiryDate;
            lot.Manufacturer ??= manufacturer?.Trim();
            lot.LocationName ??= locationName?.Trim();
            lot.UnitCost ??= unitCost;
            lot.UpdatedAt = DateTime.UtcNow;
        }

        return lot;
    }
}
