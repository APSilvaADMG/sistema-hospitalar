using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.CommandCenter;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Time;

namespace SistemaHospitalar.Infrastructure.Services;

public class CommandCenterService(
    AppDbContext db,
    IHospitalEventEngine eventEngine) : ICommandCenterService
{
    public async Task<CommandCenterDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var emergency = await BuildEmergencySummaryAsync(now, cancellationToken);
        var beds = await BuildBedSummaryAsync(cancellationToken);
        var wards = await BuildWardRowsAsync(cancellationToken);
        var operations = await BuildOperationsSummaryAsync(cancellationToken);
        var emergencyQueue = await BuildEmergencyQueueAsync(now, 8, cancellationToken);
        var recentTvCalls = await BuildRecentTvCallsAsync(5, cancellationToken);
        var recentEvents = await eventEngine.GetRecentAsync(8, cancellationToken);

        var today = HospitalTime.TodayInBrazil;
        var (startOfDay, endOfDay) = HospitalTime.BrazilDayRangeUtc(today);
        var expiringBefore = today.AddDays(30);

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

        var criticalAlerts = emergency.Critical + emergency.SlaViolations;
        if (beds.OccupancyRate >= HospitalBusinessRules.CriticalBedOccupancyPercent) criticalAlerts++;
        if (lowStock > 0) criticalAlerts += Math.Min(lowStock, 3);

        return new CommandCenterDashboardDto(
            emergency,
            beds,
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
            operations,
            emergencyQueue,
            recentTvCalls,
            recentEvents,
            now);
    }

    public async Task<OperationsQueueSnapshotDto> GetQueueSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var emergency = await BuildEmergencyQueueAsync(now, 20, cancellationToken);
        var tvCalls = await BuildRecentTvCallsAsync(10, cancellationToken);
        return new OperationsQueueSnapshotDto(emergency, tvCalls, now);
    }

    private async Task<CommandCenterEmergencyDto> BuildEmergencySummaryAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
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

        return new CommandCenterEmergencyDto(
            emergencyWaiting,
            emergencyInCare,
            emergencyCritical,
            Math.Round(averageWait, 1),
            slaViolations);
    }

    private async Task<CommandCenterBedSummaryDto> BuildBedSummaryAsync(CancellationToken cancellationToken)
    {
        var totalBeds = await db.Beds.CountAsync(b => b.IsActive, cancellationToken);
        var occupiedBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);
        var availableBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Available, cancellationToken);
        var cleaningBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Cleaning, cancellationToken);
        var maintenanceBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Maintenance, cancellationToken);
        var reservedBeds = await db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Reserved, cancellationToken);
        var occupancyRate = totalBeds == 0 ? 0 : Math.Round((decimal)occupiedBeds / totalBeds * 100, 1);

        return new CommandCenterBedSummaryDto(
            totalBeds,
            occupiedBeds,
            availableBeds,
            cleaningBeds,
            maintenanceBeds,
            reservedBeds,
            occupancyRate);
    }

    private async Task<IReadOnlyList<CommandCenterWardDto>> BuildWardRowsAsync(CancellationToken cancellationToken)
    {
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
                Maintenance = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Maintenance),
                Reserved = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Reserved),
            })
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        return wardRows
            .Select(w => new CommandCenterWardDto(
                w.Id,
                w.Name,
                w.Total,
                w.Occupied,
                w.Available,
                w.Cleaning,
                w.Maintenance,
                w.Reserved))
            .ToList();
    }

    private async Task<CommandCenterOperationsDto> BuildOperationsSummaryAsync(CancellationToken cancellationToken)
    {
        var pendingCleaning = await db.CleaningRequests.CountAsync(
            r => r.IsActive
                && (r.Status == CleaningRequestStatus.Requested || r.Status == CleaningRequestStatus.InProgress),
            cancellationToken);
        var pendingTransport = await db.TransportRequests.CountAsync(
            r => r.IsActive
                && (r.Status == TransportRequestStatus.Queued
                    || r.Status == TransportRequestStatus.Accepted
                    || r.Status == TransportRequestStatus.InTransit),
            cancellationToken);
        var activeAmbulance = await db.AmbulanceDispatches.CountAsync(
            d => d.IsActive
                && d.Status != AmbulanceDispatchStatus.Completed
                && d.Status != AmbulanceDispatchStatus.Cancelled,
            cancellationToken);

        return new CommandCenterOperationsDto(pendingCleaning, pendingTransport, activeAmbulance);
    }

    private async Task<IReadOnlyList<CommandCenterEmergencyQueueItemDto>> BuildEmergencyQueueAsync(
        DateTime now,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await db.EmergencyVisits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Where(v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting)
            .OrderBy(v => v.ArrivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(v => new CommandCenterEmergencyQueueItemDto(
                v.Id,
                v.Patient?.FullName ?? "Paciente",
                v.ChiefComplaint,
                v.Urgency,
                v.ArrivedAt,
                Math.Round((now - v.ArrivedAt).TotalMinutes, 1)))
            .ToList();
    }

    private async Task<IReadOnlyList<CommandCenterTvCallDto>> BuildRecentTvCallsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        return await db.TvQueueCalls
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CalledAt)
            .Take(limit)
            .Select(c => new CommandCenterTvCallDto(
                c.TicketNumber,
                c.PatientName,
                c.Destination,
                c.CalledAt))
            .ToListAsync(cancellationToken);
    }
}
