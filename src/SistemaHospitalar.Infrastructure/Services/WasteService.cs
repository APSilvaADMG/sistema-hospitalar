using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Waste;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class WasteService(AppDbContext dbContext) : IWasteService
{
    public async Task<WasteDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await WasteDemoSeed.EnsureAsync(dbContext, cancellationToken);

        var collections = await dbContext.WasteCollections
            .AsNoTracking()
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.CollectedAt)
            .ToListAsync(cancellationToken);

        var byType = collections
            .GroupBy(w => w.WasteType)
            .Select(g => new WasteKpiDto(g.Key, g.Count(), g.Sum(x => x.QuantityKg)))
            .OrderByDescending(k => k.TotalKg)
            .ToList();

        return new WasteDashboardDto(
            collections.Count,
            collections.Sum(w => w.QuantityKg),
            byType,
            collections.Take(10).Select(Map).ToList());
    }

    public async Task<IReadOnlyList<WasteCollectionDto>> ListAsync(
        WasteType? wasteType,
        WasteCollectionStatus? status,
        string? sector,
        CancellationToken cancellationToken = default)
    {
        await WasteDemoSeed.EnsureAsync(dbContext, cancellationToken);

        var query = dbContext.WasteCollections.AsNoTracking().Where(w => w.IsActive);

        if (wasteType.HasValue)
        {
            query = query.Where(w => w.WasteType == wasteType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(sector))
        {
            var term = sector.Trim();
            query = query.Where(w => EF.Functions.ILike(w.SectorName, $"%{term}%"));
        }

        var items = await query
            .OrderByDescending(w => w.CollectedAt)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<WasteCollectionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.WasteCollections
            .AsNoTracking()
            .Where(w => w.Id == id && w.IsActive)
            .FirstOrDefaultAsync(cancellationToken) is { } found
            ? Map(found)
            : null;
    }

    public async Task<WasteCollectionDto> CreateAsync(
        CreateWasteCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var collection = new WasteCollection
        {
            Code = $"RES-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            WasteType = request.WasteType,
            SectorName = request.SectorName.Trim(),
            QuantityKg = request.QuantityKg,
            ContainerCode = request.ContainerCode.Trim(),
            CollectedBy = request.CollectedBy.Trim(),
            ManifestNumber = request.ManifestNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CollectedAt = DateTime.UtcNow,
            Status = WasteCollectionStatus.Registered,
        };

        dbContext.WasteCollections.Add(collection);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(collection);
    }

    public async Task<WasteCollectionDto?> UpdateAsync(
        Guid id,
        UpdateWasteCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var collection = await dbContext.WasteCollections
            .FirstOrDefaultAsync(w => w.Id == id && w.IsActive, cancellationToken);

        if (collection is null)
        {
            return null;
        }

        if (request.Status.HasValue)
        {
            collection.Status = request.Status.Value;
        }

        if (request.ManifestNumber is not null)
        {
            collection.ManifestNumber = request.ManifestNumber.Trim();
        }

        if (request.Notes is not null)
        {
            collection.Notes = request.Notes.Trim();
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(collection);
    }

    private static WasteCollectionDto Map(WasteCollection w) => new(
        w.Id,
        w.Code,
        w.WasteType,
        w.SectorName,
        w.QuantityKg,
        w.ContainerCode,
        w.CollectedAt,
        w.CollectedBy,
        w.Status,
        w.ManifestNumber,
        w.Notes);
}
