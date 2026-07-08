using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Laboratory;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class LabService(AppDbContext dbContext) : ILabService
{
    public async Task<IReadOnlyList<LabExamCatalogDto>> GetExamCatalogAsync(
        Guid? specialtyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LabExamCatalogs.AsNoTracking().Where(e => e.IsActive);

        if (specialtyId.HasValue)
        {
            var sid = specialtyId.Value;
            query = query.Where(e => e.IsGeneral || e.SpecialtyLinks.Any(l => l.SpecialtyId == sid));
        }

        return await query
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .Select(e => new LabExamCatalogDto(
                e.Id, e.Name, e.TussCode, e.SampleType, e.ReferenceRange, e.Unit, e.Category, e.IsGeneral))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabOrderDto>> GetOrdersAsync(
        LabOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.LabOrders.AsNoTracking().Where(o => o.IsActive);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapOrder())
            .ToListAsync(cancellationToken);
    }

    public async Task<LabOrderDto> CreateOrderAsync(
        CreateLabOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ExamCatalogIds.Count == 0)
        {
            throw new InvalidOperationException("Selecione ao menos um exame.");
        }

        var order = new LabOrder
        {
            PatientId = request.PatientId,
            RequestingProfessionalId = request.RequestingProfessionalId,
            Notes = request.Notes?.Trim()
        };

        foreach (var examId in request.ExamCatalogIds.Distinct())
        {
            order.Items.Add(new LabOrderItem { LabExamCatalogId = examId });
        }

        dbContext.LabOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<LabResultDto?> RegisterResultAsync(
        RegisterLabResultRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.LabOrderItems
            .Include(i => i.LabOrder)
            .Include(i => i.Result)
            .FirstOrDefaultAsync(i => i.Id == request.OrderItemId, cancellationToken);

        if (item is null)
        {
            return null;
        }

        if (item.Result is not null)
        {
            item.Result.Value = request.Value.Trim();
            item.Result.Unit = request.Unit?.Trim();
            item.Result.ReferenceRange = request.ReferenceRange?.Trim();
            item.Result.IsAbnormal = request.IsAbnormal;
            item.Result.Notes = request.Notes?.Trim();
            item.Result.ReleasedAt = DateTime.UtcNow;
            item.Result.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            item.Result = new LabResult
            {
                LabOrderItemId = item.Id,
                Value = request.Value.Trim(),
                Unit = request.Unit?.Trim(),
                ReferenceRange = request.ReferenceRange?.Trim(),
                IsAbnormal = request.IsAbnormal,
                Notes = request.Notes?.Trim(),
                ReleasedAt = DateTime.UtcNow
            };
            dbContext.LabResults.Add(item.Result);
        }

        item.Status = LabItemStatus.Completed;
        item.UpdatedAt = DateTime.UtcNow;

        if (item.LabOrder.Status == LabOrderStatus.Requested)
        {
            item.LabOrder.Status = LabOrderStatus.InProgress;
            item.LabOrder.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var orderId = item.LabOrderId;
        var incompleteCount = await dbContext.LabOrderItems
            .CountAsync(i => i.LabOrderId == orderId && i.Status != LabItemStatus.Completed, cancellationToken);

        if (incompleteCount == 0)
        {
            var order = await dbContext.LabOrders.FirstAsync(o => o.Id == orderId, cancellationToken);
            order.Status = LabOrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new LabResultDto(
            item.Result.Id,
            item.Result.Value,
            item.Result.Unit,
            item.Result.ReferenceRange,
            item.Result.IsAbnormal,
            item.Result.ReleasedAt);
    }

    private async Task<LabOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.LabOrders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(MapOrder())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<LabOrder, LabOrderDto>> MapOrder() =>
        o => new LabOrderDto(
            o.Id,
            o.PatientId,
            o.Patient.FullName,
            o.RequestingProfessional.FullName,
            o.Status,
            o.CreatedAt,
            o.Items.Select(i => new LabOrderItemDto(
                i.Id,
                i.LabExamCatalogId,
                i.LabExamCatalog.Name,
                i.Status,
                i.Result != null
                    ? new LabResultDto(i.Result.Id, i.Result.Value, i.Result.Unit,
                        i.Result.ReferenceRange, i.Result.IsAbnormal, i.Result.ReleasedAt)
                    : null)).ToList());
}
