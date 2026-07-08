using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Inventory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ProductKitService(AppDbContext dbContext) : IProductKitService
{
    public async Task<IReadOnlyList<ProductKitDto>> GetKitsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductKits
            .AsNoTracking()
            .Where(k => k.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(k => k.Name.Contains(term));
        }

        return await query
            .OrderBy(k => k.Name)
            .Select(k => new ProductKitDto(
                k.Id,
                k.Name,
                k.PriceTable,
                k.Items.Count(i => i.IsActive),
                k.Items.Where(i => i.IsActive).Sum(i => i.UnitPrice * i.Quantity)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductKitDetailDto?> GetKitByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductKits
            .AsNoTracking()
            .Where(k => k.Id == id && k.IsActive)
            .Select(k => new ProductKitDetailDto(
                k.Id,
                k.Name,
                k.PriceTable,
                k.Items
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => new ProductKitItemDto(
                        i.Id,
                        i.ProductId,
                        i.Product.Name,
                        i.Product.Sku,
                        i.Quantity,
                        i.InsuranceCode,
                        i.UnitPrice,
                        i.VariablePrice))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductKitDetailDto> CreateKitAsync(
        CreateProductKitRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateKitRequest(request.Name, request.Items);

        var kit = new ProductKit
        {
            Name = request.Name.Trim(),
            PriceTable = request.PriceTable?.Trim(),
        };

        await AddItemsAsync(kit, request.Items, cancellationToken);

        dbContext.ProductKits.Add(kit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetKitByIdAsync(kit.Id, cancellationToken))!;
    }

    public async Task<ProductKitDetailDto> UpdateKitAsync(
        Guid id,
        UpdateProductKitRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateKitRequest(request.Name, request.Items);

        var kit = await dbContext.ProductKits
            .Include(k => k.Items)
            .FirstOrDefaultAsync(k => k.Id == id && k.IsActive, cancellationToken);

        if (kit is null)
        {
            throw new InvalidOperationException("Kit não encontrado.");
        }

        kit.Name = request.Name.Trim();
        kit.PriceTable = request.PriceTable?.Trim();
        kit.UpdatedAt = DateTime.UtcNow;

        foreach (var item in kit.Items)
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await AddItemsAsync(kit, request.Items, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetKitByIdAsync(kit.Id, cancellationToken))!;
    }

    public async Task DeleteKitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var kit = await dbContext.ProductKits
            .FirstOrDefaultAsync(k => k.Id == id && k.IsActive, cancellationToken);

        if (kit is null)
        {
            throw new InvalidOperationException("Kit não encontrado.");
        }

        kit.IsActive = false;
        kit.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddItemsAsync(
        ProductKit kit,
        IReadOnlyList<ProductKitItemRequest> items,
        CancellationToken cancellationToken)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var existingIds = await dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count != productIds.Count)
        {
            throw new InvalidOperationException("Um ou mais produtos do kit não foram encontrados.");
        }

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Quantidade do produto deve ser maior que zero.");
            }

            kit.Items.Add(new ProductKitItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                InsuranceCode = item.InsuranceCode?.Trim(),
                UnitPrice = item.UnitPrice,
                VariablePrice = item.VariablePrice,
            });
        }
    }

    private static void ValidateKitRequest(string name, IReadOnlyList<ProductKitItemRequest> items)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Informe o nome do kit.");
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um produto ao kit.");
        }
    }
}
