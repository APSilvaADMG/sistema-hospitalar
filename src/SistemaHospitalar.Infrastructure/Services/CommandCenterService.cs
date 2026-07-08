using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.CommandCenter;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Services;

public class CommandCenterService(AppDbContext db) : ICommandCenterService
{
    public async Task<CommandCenterDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = HospitalTime.TodayInBrazil;
        var (startOfDay, endOfDay) = HospitalTime.BrazilDayRangeUtc(today);
        var expiringBefore = today.AddDays(30);
        var now = DateTime.UtcNow;

        var emergencyWaiting = await db.EmergencyVisits.CountAsync(
            v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting, cancellationToken);
        var emergencyInCare = await db.EmergencyVisits.CountAsync(
            v => v.IsActive && v.Status == EmergencyVisitStatus.InCare, cancellationToken);
        var emergencyCritical = await db.EmergencyVisits.CountAsync(
            v => v.IsActive
                && v.Status == EmergencyVisitStatus.Waiting
                && (v.Urgency == TriageUrgency.Emergency || v.Urgency == TriageUrgency.High),
            cancellationToken);

        var emergencyWaitingList = await db.EmergencyVisits
            .AsNoTracking()
            .Where(v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting)
            .Select(v => new { v.ArrivedAt, v.Urgency })
            .ToListAsync(cancellationToken);

        var averageWait = emergencyWaitingList.Count == 0
            ? 0d
            : emergencyWaitingList.Average(v => (now - v.ArrivedAt).TotalMinutes);
        var slaViolations = emergencyWaitingList.Count(v =>
            HospitalBusinessRules.IsEmergencyWaitExceeded(v.ArrivedAt, v.Urgency, now));

        var totalBeds = await db.Beds.CountAsync(b => b.IsActive, cancellationToken);
        var occupiedBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);
        var availableBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Available, cancellationToken);
        var cleaningBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Cleaning, cancellationToken);
        var maintenanceBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Maintenance, cancellationToken);
        var occupancyRate = totalBeds == 0 ? 0 : Math.Round((decimal)occupiedBeds / totalBeds * 100, 1);

        var lowStock = await db.Products.CountAsync(
            p => p.IsActive && p.QuantityOnHand <= p.MinimumStock, cancellationToken);
        var expiringLots = await db.ProductLots.CountAsync(
            l => l.IsActive
                && l.QuantityOnHand > 0
                && l.ExpiryDate != null
                && l.ExpiryDate <= expiringBefore,
            cancellationToken);
        var pendingRequisitions = await db.StockRequisitions.CountAsync(
            r => r.IsActive && r.Status == StockRequisitionStatus.Pending, cancellationToken);

        var surgeriesToday = await db.Surgeries
            .AsNoTracking()
            .Where(s => s.IsActive && s.ScheduledAt >= startOfDay && s.ScheduledAt < endOfDay)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Scheduled = g.Count(s => s.Status == SurgeryStatus.Scheduled),
                InProgress = g.Count(s => s.Status == SurgeryStatus.InProgress),
                Completed = g.Count(s => s.Status == SurgeryStatus.Completed),
                Cancelled = g.Count(s => s.Status == SurgeryStatus.Cancelled),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var openPendencies = await db.PendingItems.CountAsync(
            p => p.IsActive
                && (p.Status == PendingItemStatus.Aberta || p.Status == PendingItemStatus.EmAndamento),
            cancellationToken);

        var criticalAlerts = emergencyCritical + slaViolations;
        if (occupancyRate >= HospitalBusinessRules.CriticalBedOccupancyPercent) criticalAlerts++;
        if (lowStock > 0) criticalAlerts += Math.Min(lowStock, 3);

        var wardRows = await db.Wards
            .AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new
            {
                w.Id,
                w.Name,
                Total = w.Beds.Count(b => b.IsActive),
                Occupied = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied),
                Available = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
                Cleaning = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Cleaning),
            })
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        var wards = wardRows
            .Select(w => new CommandCenterWardDto(w.Id, w.Name, w.Total, w.Occupied, w.Available, w.Cleaning))
            .ToList();

        return new CommandCenterDashboardDto(
            new CommandCenterEmergencyDto(
                emergencyWaiting,
                emergencyInCare,
                emergencyCritical,
                Math.Round(averageWait, 1),
                slaViolations),
            new CommandCenterBedSummaryDto(
                totalBeds,
                occupiedBeds,
                availableBeds,
                cleaningBeds,
                maintenanceBeds,
                occupancyRate),
            new CommandCenterWarehouseDto(lowStock, expiringLots),
            pendingRequisitions,
            new CommandCenterSurgeryDto(
                surgeriesToday?.Total ?? 0,
                surgeriesToday?.Scheduled ?? 0,
                surgeriesToday?.InProgress ?? 0,
                surgeriesToday?.Completed ?? 0,
                surgeriesToday?.Cancelled ?? 0),
            openPendencies,
            criticalAlerts,
            wards,
            now);
    }
}
