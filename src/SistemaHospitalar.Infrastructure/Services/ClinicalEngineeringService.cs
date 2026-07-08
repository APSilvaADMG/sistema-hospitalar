using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.ClinicalEngineering;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ClinicalEngineeringService(AppDbContext dbContext) : IClinicalEngineeringService
{
    public async Task<IReadOnlyList<MedicalEquipmentDto>> GetEquipmentAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MedicalEquipments
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new MedicalEquipmentDto(
                e.Id, e.Name, e.AssetTag, e.Manufacturer, e.Model, e.Location,
                e.Status, e.LastMaintenanceDate, e.NextMaintenanceDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalEquipmentDto> CreateEquipmentAsync(
        CreateMedicalEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var tag = request.AssetTag.Trim().ToUpperInvariant();

        if (await dbContext.MedicalEquipments.AnyAsync(e => e.AssetTag == tag, cancellationToken))
        {
            throw new InvalidOperationException("Patrimônio já cadastrado.");
        }

        var equipment = new MedicalEquipment
        {
            Name = request.Name.Trim(),
            AssetTag = tag,
            Manufacturer = request.Manufacturer?.Trim(),
            Model = request.Model?.Trim(),
            Location = request.Location?.Trim(),
            NextMaintenanceDate = request.NextMaintenanceDate,
            Status = request.NextMaintenanceDate.HasValue
                && request.NextMaintenanceDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)
                ? MedicalEquipmentStatus.CalibrationDue
                : MedicalEquipmentStatus.Operational
        };

        dbContext.MedicalEquipments.Add(equipment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetEquipmentAsync(cancellationToken)).First(e => e.Id == equipment.Id);
    }

    public async Task<IReadOnlyList<MaintenanceWorkOrderDto>> GetWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MaintenanceWorkOrders
            .AsNoTracking()
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new MaintenanceWorkOrderDto(
                w.Id, w.MedicalEquipmentId, w.MedicalEquipment.Name,
                w.Title, w.Description, w.Status, w.TechnicianName,
                w.CreatedAt, w.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MaintenanceWorkOrderDto> CreateWorkOrderAsync(
        CreateWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        var equipment = await dbContext.MedicalEquipments
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId && e.IsActive, cancellationToken);

        if (equipment is null)
        {
            throw new InvalidOperationException("Equipamento não encontrado.");
        }

        equipment.Status = MedicalEquipmentStatus.Maintenance;

        var order = new MaintenanceWorkOrder
        {
            MedicalEquipmentId = request.EquipmentId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            TechnicianName = request.TechnicianName?.Trim()
        };

        dbContext.MaintenanceWorkOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetWorkOrdersAsync(cancellationToken)).First(w => w.Id == order.Id);
    }

    public async Task<MaintenanceWorkOrderDto?> UpdateWorkOrderStatusAsync(
        Guid id, UpdateWorkOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.MaintenanceWorkOrders
            .Include(w => w.MedicalEquipment)
            .FirstOrDefaultAsync(w => w.Id == id && w.IsActive, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        if (request.Status == MaintenanceWorkOrderStatus.Completed)
        {
            order.CompletedAt = DateTime.UtcNow;
            order.MedicalEquipment.Status = MedicalEquipmentStatus.Operational;
            order.MedicalEquipment.LastMaintenanceDate = DateOnly.FromDateTime(DateTime.UtcNow);
            order.MedicalEquipment.NextMaintenanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetWorkOrdersAsync(cancellationToken)).FirstOrDefault(w => w.Id == id);
    }
}
