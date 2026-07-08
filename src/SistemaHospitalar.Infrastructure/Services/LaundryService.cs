using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Laundry;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class LaundryService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : ILaundryService
{
    public async Task<IReadOnlyList<LaundryBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LaundryBatches
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CollectedAt)
            .Select(b => new LaundryBatchDto(
                b.Id, b.BatchNumber, b.Origin, b.OriginDetail,
                b.ItemCount, b.WeightKg, b.Status, b.CollectedAt, b.DeliveredAt, b.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<LaundryBatchDto> CreateBatchAsync(
        CreateLaundryBatchRequest request, CancellationToken cancellationToken = default)
    {
        var batch = new LaundryBatch
        {
            BatchNumber = $"LAV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Origin = request.Origin,
            OriginDetail = request.OriginDetail?.Trim(),
            ItemCount = request.ItemCount,
            WeightKg = request.WeightKg,
            Notes = request.Notes?.Trim()
        };

        dbContext.LaundryBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("laundry.batch.collected", new
        {
            batch.Id,
            batch.BatchNumber,
            Origin = batch.Origin.ToString(),
            batch.ItemCount
        }, cancellationToken);

        return (await GetBatchesAsync(cancellationToken)).First(b => b.Id == batch.Id);
    }

    public async Task<LaundryBatchDto?> UpdateBatchStatusAsync(
        Guid id, UpdateLaundryBatchStatusRequest request, CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.LaundryBatches
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);

        if (batch is null)
        {
            return null;
        }

        batch.Status = request.Status;
        batch.UpdatedAt = DateTime.UtcNow;

        if (request.Status == LaundryBatchStatus.Delivered)
        {
            batch.DeliveredAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBatchesAsync(cancellationToken)).FirstOrDefault(b => b.Id == id);
    }
}
