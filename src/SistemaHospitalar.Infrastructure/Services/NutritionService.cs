using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Nutrition;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class NutritionService(AppDbContext dbContext) : INutritionService
{
    public async Task<IReadOnlyList<DietOrderDto>> GetOrdersAsync(
        DietOrderStatus? status, DateOnly? mealDate, CancellationToken cancellationToken = default)
    {
        var query = dbContext.DietOrders.AsNoTracking().Where(d => d.IsActive);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (mealDate.HasValue)
        {
            query = query.Where(d => d.MealDate == mealDate.Value);
        }

        return await query
            .OrderBy(d => d.MealDate)
            .ThenBy(d => d.MealPeriod)
            .Select(d => new DietOrderDto(
                d.Id,
                d.HospitalizationId,
                d.Hospitalization.Patient.FullName,
                d.Hospitalization.Bed.Ward.Name,
                d.Hospitalization.Bed.BedNumber,
                d.DietType,
                d.MealPeriod,
                d.Status,
                d.MealDate,
                d.Notes,
                d.DeliveredAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DietOrderDto> CreateOrderAsync(
        CreateDietOrderRequest request, CancellationToken cancellationToken = default)
    {
        var hospitalization = await dbContext.Hospitalizations
            .FirstOrDefaultAsync(h => h.Id == request.HospitalizationId && h.IsActive
                && h.Status == HospitalizationStatus.Active, cancellationToken);

        if (hospitalization is null)
        {
            throw new InvalidOperationException("Internação ativa não encontrada.");
        }

        var order = new DietOrder
        {
            HospitalizationId = request.HospitalizationId,
            DietType = request.DietType,
            MealPeriod = request.MealPeriod,
            MealDate = request.MealDate,
            Notes = request.Notes?.Trim()
        };

        dbContext.DietOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<DietOrderDto?> UpdateStatusAsync(
        Guid id, UpdateDietOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DietOrders
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        if (request.Status == DietOrderStatus.Delivered)
        {
            order.DeliveredAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(id, cancellationToken);
    }

    private async Task<DietOrderDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.DietOrders
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DietOrderDto(
                d.Id,
                d.HospitalizationId,
                d.Hospitalization.Patient.FullName,
                d.Hospitalization.Bed.Ward.Name,
                d.Hospitalization.Bed.BedNumber,
                d.DietType,
                d.MealPeriod,
                d.Status,
                d.MealDate,
                d.Notes,
                d.DeliveredAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
