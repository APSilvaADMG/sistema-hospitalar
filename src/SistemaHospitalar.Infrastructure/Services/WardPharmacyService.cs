using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.ClinicalOperations;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class WardPharmacyService(AppDbContext dbContext) : IWardPharmacyService
{
    public async Task<IReadOnlyList<WardStockBalanceDto>> ListBalancesAsync(
        Guid? wardId = null,
        bool lowStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WardStockBalances
            .AsNoTracking()
            .Where(b => b.IsActive);

        if (wardId.HasValue)
        {
            query = query.Where(b => b.WardId == wardId.Value);
        }

        if (lowStockOnly)
        {
            query = query.Where(b => b.QuantityOnHand <= b.MinimumStock);
        }

        return await query
            .OrderBy(b => b.Ward.Name)
            .ThenBy(b => b.Product.Name)
            .Select(b => new WardStockBalanceDto(
                b.Id,
                b.WardId,
                b.Ward.Name,
                b.ProductId,
                b.Product.Name,
                b.Product.Sku,
                b.QuantityOnHand,
                b.MinimumStock,
                b.Unit,
                b.QuantityOnHand <= b.MinimumStock))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WardStockMovementDto>> ListMovementsAsync(
        Guid? wardId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WardStockMovements.AsNoTracking().Where(m => m.IsActive);

        if (wardId.HasValue)
        {
            query = query.Where(m => m.WardId == wardId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(m => m.MovementDate >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.MovementDate <= to.Value.ToUniversalTime());
        }

        return await query
            .OrderByDescending(m => m.MovementDate)
            .Take(500)
            .Select(MapMovement())
            .ToListAsync(cancellationToken);
    }

    public async Task<WardStockBalanceDto> TransferFromCentralAsync(
        WardStockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        var ward = await dbContext.Wards.FirstOrDefaultAsync(
            w => w.Id == request.WardId && w.IsActive,
            cancellationToken) ?? throw new InvalidOperationException("Ala não encontrada.");

        var product = await dbContext.Products.FirstOrDefaultAsync(
            p => p.Id == request.ProductId && p.IsActive,
            cancellationToken) ?? throw new InvalidOperationException("Produto não encontrado.");

        if (product.QuantityOnHand < request.Quantity)
        {
            throw new InvalidOperationException(
                $"Estoque central insuficiente. Disponível: {product.QuantityOnHand:F3} {product.Unit}.");
        }

        product.QuantityOnHand -= request.Quantity;

        var balance = await dbContext.WardStockBalances.FirstOrDefaultAsync(
            b => b.WardId == request.WardId && b.ProductId == request.ProductId && b.IsActive,
            cancellationToken);

        if (balance is null)
        {
            balance = new WardStockBalance
            {
                WardId = request.WardId,
                ProductId = request.ProductId,
                QuantityOnHand = request.Quantity,
                MinimumStock = product.MinimumStock,
                Unit = product.Unit,
            };
            dbContext.WardStockBalances.Add(balance);
        }
        else
        {
            balance.QuantityOnHand += request.Quantity;
        }

        dbContext.WardStockMovements.Add(new WardStockMovement
        {
            WardId = request.WardId,
            ProductId = request.ProductId,
            MovementType = WardStockMovementType.TransferIn,
            Quantity = request.Quantity,
            Unit = product.Unit,
            Reference = request.Reference,
            Notes = request.Notes,
            MovementDate = DateTime.UtcNow,
        });

        dbContext.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = StockMovementType.Outbound,
            Quantity = request.Quantity,
            Reason = $"Transferência para ala {ward.Name}",
            Reference = request.Reference ?? $"ward:{ward.Id}",
            Location = ward.Name,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await ListBalancesAsync(request.WardId, cancellationToken: cancellationToken))
            .First(b => b.ProductId == request.ProductId);
    }

    public async Task<WardStockMovementDto> DispenseAsync(
        WardStockDispenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        var balance = await dbContext.WardStockBalances.FirstOrDefaultAsync(
            b => b.WardId == request.WardId && b.ProductId == request.ProductId && b.IsActive,
            cancellationToken) ?? throw new InvalidOperationException("Produto não disponível no estoque da ala.");

        if (balance.QuantityOnHand < request.Quantity)
        {
            throw new InvalidOperationException("Quantidade insuficiente no estoque da ala.");
        }

        var patientExists = await dbContext.Patients.AnyAsync(
            p => p.Id == request.PatientId && p.IsActive,
            cancellationToken);
        if (!patientExists)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        balance.QuantityOnHand -= request.Quantity;

        var movement = new WardStockMovement
        {
            WardId = request.WardId,
            ProductId = request.ProductId,
            PatientId = request.PatientId,
            MovementType = WardStockMovementType.Dispense,
            Quantity = request.Quantity,
            Unit = balance.Unit,
            Notes = request.Notes,
            MovementDate = DateTime.UtcNow,
        };

        dbContext.WardStockMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.WardStockMovements
            .AsNoTracking()
            .Where(m => m.Id == movement.Id)
            .Select(MapMovement())
            .FirstAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<WardStockMovement, WardStockMovementDto>> MapMovement() =>
        m => new WardStockMovementDto(
            m.Id,
            m.WardId,
            m.Ward.Name,
            m.ProductId,
            m.Product.Name,
            m.MovementType,
            m.Quantity,
            m.Unit,
            m.PatientId,
            m.Patient != null ? m.Patient.FullName : null,
            m.Reference,
            m.Notes,
            m.MovementDate);
}
