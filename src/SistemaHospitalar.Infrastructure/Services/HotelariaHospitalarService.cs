using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Hotelaria;
using SistemaHospitalar.Application.DTOs.Transport;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HotelariaHospitalarService(
    AppDbContext dbContext,
    IOperationsRealtimeNotifier realtime) : IHotelariaHospitalarService
{
    private static readonly TransportRequestStatus[] ActiveTransportStatuses =
    [
        TransportRequestStatus.Queued,
        TransportRequestStatus.Accepted,
        TransportRequestStatus.InTransit
    ];

    public async Task<HotelariaNocDto> GetNocDashboardAsync(CancellationToken cancellationToken = default)
    {
        await HotelariaSeed.EnsureAsync(dbContext, cancellationToken);
        await TransportSeed.EnsureAsync(dbContext, cancellationToken);

        var beds = await dbContext.Beds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .Include(b => b.Ward)
            .ToListAsync(cancellationToken);

        var activeHospitalizations = await dbContext.Hospitalizations
            .AsNoTracking()
            .Where(h => h.IsActive && h.Status == HospitalizationStatus.Active)
            .Select(h => new { h.BedId, h.Patient.FullName })
            .ToListAsync(cancellationToken);

        var occupantByBed = activeHospitalizations.ToDictionary(h => h.BedId, h => h.FullName);

        var total = beds.Count;
        var available = beds.Count(b => b.Status == BedStatus.Available);
        var occupied = beds.Count(b => b.Status == BedStatus.Occupied);
        var cleaning = beds.Count(b => b.Status == BedStatus.Cleaning);
        var maintenance = beds.Count(b => b.Status == BedStatus.Maintenance);
        var occupancyRate = total > 0 ? Math.Round(occupied * 100.0 / total, 1) : 0;

        var pendingAdmissions = await dbContext.HospitalizationRequests
            .CountAsync(r => r.IsActive && r.Status == HospitalizationRequestStatus.Approved, cancellationToken);

        var pendingCleanings = await dbContext.CleaningRequests
            .CountAsync(c => c.IsActive && c.Status != CleaningRequestStatus.Completed && c.Status != CleaningRequestStatus.Cancelled, cancellationToken);

        var activeTransports = await dbContext.TransportRequests
            .CountAsync(r => r.IsActive && ActiveTransportStatuses.Contains(r.Status), cancellationToken);

        var completedCleanings = await dbContext.CleaningRequests
            .AsNoTracking()
            .Where(c => c.IsActive && c.Status == CleaningRequestStatus.Completed && c.StartedAt != null && c.CompletedAt != null)
            .ToListAsync(cancellationToken);

        var avgCleaning = completedCleanings.Count > 0
            ? completedCleanings.Average(c => (c.CompletedAt!.Value - c.StartedAt!.Value).TotalMinutes)
            : (double?)null;

        var completedTransports = await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive && r.Status == TransportRequestStatus.Completed && r.AcceptedAt != null)
            .ToListAsync(cancellationToken);

        var avgTransportAccept = completedTransports.Count > 0
            ? completedTransports.Average(r => (r.AcceptedAt!.Value - r.RequestedAt).TotalMinutes)
            : (double?)null;

        var cleaningQueue = await GetCleaningRequestsAsync(null, cancellationToken);
        var pendingCleaningQueue = cleaningQueue
            .Where(c => c.Status is CleaningRequestStatus.Requested or CleaningRequestStatus.InProgress)
            .Take(12)
            .ToList();

        var transportQueue = await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive && ActiveTransportStatuses.Contains(r.Status))
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.RequestedAt)
            .Take(12)
            .Select(r => new TransportRequestDto(
                r.Id,
                r.PatientId,
                r.HospitalizationId,
                r.PatientName,
                r.OriginType,
                r.OriginDetail,
                r.DestinationType,
                r.DestinationDetail,
                r.Status,
                r.Priority,
                r.AssignedEmployeeId,
                r.AssignedEmployee != null ? r.AssignedEmployee.FullName : null,
                r.TransportAssetId,
                r.TransportAsset != null ? r.TransportAsset.Code : null,
                r.RequestedAt,
                r.AcceptedAt,
                r.ArrivedAtOriginAt,
                r.DepartedAt,
                r.ArrivedAtDestinationAt,
                r.CompletedAt,
                r.Notes,
                r.RequestedBy,
                r.SlaDeadlineAt,
                r.IsSlaViolated))
            .ToListAsync(cancellationToken);

        var bedMap = beds
            .OrderBy(b => b.Ward.Name)
            .ThenBy(b => b.BedNumber)
            .Select(b => new HotelariaBedSnapshotDto(
                b.Id,
                b.Ward.Name,
                b.BedNumber,
                b.Status,
                occupantByBed.GetValueOrDefault(b.Id)))
            .Take(60)
            .ToList();

        return new HotelariaNocDto(
            total,
            available,
            occupied,
            cleaning,
            maintenance,
            occupancyRate,
            pendingAdmissions,
            pendingCleanings,
            activeTransports,
            avgCleaning,
            avgTransportAccept,
            pendingCleaningQueue,
            transportQueue,
            bedMap);
    }

    public async Task<IReadOnlyList<CleaningRequestDto>> GetCleaningRequestsAsync(
        CleaningRequestStatus? status, CancellationToken cancellationToken = default)
    {
        await HotelariaSeed.EnsureAsync(dbContext, cancellationToken);

        var query = dbContext.CleaningRequests.AsNoTracking().Where(c => c.IsActive);
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var items = await query
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Include(c => c.AssignedEmployee)
            .OrderByDescending(c => c.RequestedAt)
            .ToListAsync(cancellationToken);

        return items.Select(MapCleaning).ToList();
    }

    public async Task<CleaningRequestDto> CreateCleaningRequestAsync(
        CreateCleaningRequestRequest request, CancellationToken cancellationToken = default)
    {
        await RequestBedCleaningAsync(
            request.BedId,
            null,
            request.CleaningType,
            CleaningTriggerReason.Manual,
            cancellationToken);

        var created = await dbContext.CleaningRequests
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Include(c => c.AssignedEmployee)
            .Where(c => c.BedId == request.BedId && c.IsActive && c.Status != CleaningRequestStatus.Completed)
            .OrderByDescending(c => c.RequestedAt)
            .FirstAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            created.Notes = request.Notes.Trim();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapCleaning(created);
    }

    public async Task RequestBedCleaningAsync(
        Guid bedId,
        Guid? hospitalizationId,
        CleaningType cleaningType,
        CleaningTriggerReason triggerReason,
        CancellationToken cancellationToken = default)
    {
        var bed = await dbContext.Beds.FirstOrDefaultAsync(b => b.Id == bedId && b.IsActive, cancellationToken);
        if (bed is null)
        {
            return;
        }

        if (triggerReason == CleaningTriggerReason.Manual)
        {
            var isOccupied = await dbContext.Hospitalizations.AnyAsync(
                h => h.BedId == bedId && h.Status == HospitalizationStatus.Active && h.IsActive,
                cancellationToken);

            if (isOccupied)
            {
                throw new InvalidOperationException("Não é possível solicitar higienização em leito ocupado.");
            }
        }

        var hasPending = await dbContext.CleaningRequests.AnyAsync(
            c => c.BedId == bedId && c.IsActive
                && c.Status != CleaningRequestStatus.Completed
                && c.Status != CleaningRequestStatus.Cancelled,
            cancellationToken);

        if (hasPending)
        {
            bed.Status = BedStatus.Cleaning;
            bed.StatusReason = "Aguardando higienização";
            bed.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var cleaning = new CleaningRequest
        {
            BedId = bedId,
            HospitalizationId = hospitalizationId,
            CleaningType = cleaningType,
            TriggerReason = triggerReason,
            ChecklistJson = BuildDefaultChecklistJson(cleaningType),
            Status = CleaningRequestStatus.Requested,
        };

        bed.Status = BedStatus.Cleaning;
        bed.StatusReason = cleaningType switch
        {
            CleaningType.Terminal => "Higienização terminal",
            CleaningType.Concurrent => "Higienização concorrente",
            _ => "Higienização de rotina",
        };
        bed.BlockedUntil = null;
        bed.UpdatedAt = DateTime.UtcNow;

        dbContext.CleaningRequests.Add(cleaning);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyCleaningChangedAsync(cleaning.Id, cancellationToken);
        await realtime.NotifyBedsChangedAsync(cancellationToken);
    }

    public async Task<CleaningRequestDto?> StartCleaningAsync(
        Guid id, StartCleaningRequestRequest request, CancellationToken cancellationToken = default)
    {
        var cleaning = await dbContext.CleaningRequests
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Include(c => c.AssignedEmployee)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cleaning is null || cleaning.Status != CleaningRequestStatus.Requested)
        {
            return null;
        }

        cleaning.Status = CleaningRequestStatus.InProgress;
        cleaning.StartedAt = DateTime.UtcNow;
        cleaning.AssignedTeam = request.AssignedTeam?.Trim();
        cleaning.AssignedEmployeeId = request.AssignedEmployeeId;
        cleaning.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyCleaningChangedAsync(cleaning.Id, cancellationToken);
        return MapCleaning(cleaning);
    }

    public async Task<CleaningRequestDto?> UpdateChecklistAsync(
        Guid id, UpdateCleaningChecklistRequest request, CancellationToken cancellationToken = default)
    {
        var cleaning = await dbContext.CleaningRequests
            .Include(c => c.Bed).ThenInclude(b => b.Ward)
            .Include(c => c.AssignedEmployee)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cleaning is null)
        {
            return null;
        }

        cleaning.ChecklistJson = JsonSerializer.Serialize(request.Checklist);
        cleaning.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyCleaningChangedAsync(cleaning.Id, cancellationToken);
        return MapCleaning(cleaning);
    }

    public async Task<CleaningRequestDto?> CompleteCleaningAsync(
        Guid id, CompleteCleaningRequestRequest request, CancellationToken cancellationToken = default)
    {
        var cleaning = await dbContext.CleaningRequests
            .Include(c => c.Bed)
            .Include(c => c.AssignedEmployee)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cleaning is null || cleaning.Status == CleaningRequestStatus.Completed)
        {
            return null;
        }

        cleaning.Status = CleaningRequestStatus.Completed;
        cleaning.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            cleaning.Notes = request.Notes.Trim();
        }
        cleaning.UpdatedAt = DateTime.UtcNow;

        cleaning.Bed.Status = BedStatus.Available;
        cleaning.Bed.StatusReason = null;
        cleaning.Bed.BlockedUntil = null;
        cleaning.Bed.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyCleaningChangedAsync(cleaning.Id, cancellationToken);
        await realtime.NotifyBedsChangedAsync(cancellationToken);
        return MapCleaning(cleaning);
    }

    public async Task<CleaningRequestDto?> CancelCleaningAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cleaning = await dbContext.CleaningRequests
            .Include(c => c.Bed)
            .Include(c => c.AssignedEmployee)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cleaning is null || cleaning.Status is CleaningRequestStatus.Completed or CleaningRequestStatus.Cancelled)
        {
            return null;
        }

        cleaning.Status = CleaningRequestStatus.Cancelled;
        cleaning.UpdatedAt = DateTime.UtcNow;
        cleaning.Bed.Status = BedStatus.Available;
        cleaning.Bed.StatusReason = null;
        cleaning.Bed.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyCleaningChangedAsync(cleaning.Id, cancellationToken);
        await realtime.NotifyBedsChangedAsync(cancellationToken);
        return MapCleaning(cleaning);
    }

    private static CleaningRequestDto MapCleaning(CleaningRequest c) => new(
        c.Id,
        c.BedId,
        c.Bed?.Ward?.Name ?? "—",
        c.Bed?.BedNumber ?? "—",
        c.HospitalizationId,
        c.CleaningType,
        c.Status,
        c.TriggerReason,
        c.AssignedTeam,
        c.AssignedEmployeeId,
        c.AssignedEmployee?.FullName,
        c.RequestedAt,
        c.StartedAt,
        c.CompletedAt,
        ParseChecklist(c.ChecklistJson),
        c.Notes);

    private static IReadOnlyList<CleaningChecklistItemDto> ParseChecklist(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CleaningChecklistItemDto>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string BuildDefaultChecklistJson(CleaningType type)
    {
        CleaningChecklistItemDto[] items = type switch
        {
            CleaningType.Concurrent =>
            [
                new CleaningChecklistItemDto("1", "Higienização de superfícies de alto toque", false),
                new CleaningChecklistItemDto("2", "Descarte de resíduos", false),
                new CleaningChecklistItemDto("3", "Reposição de materiais", false),
            ],
            CleaningType.Routine =>
            [
                new CleaningChecklistItemDto("1", "Limpeza de piso e mobiliário", false),
                new CleaningChecklistItemDto("2", "Banheiro do quarto", false),
                new CleaningChecklistItemDto("3", "Verificação de enxoval", false),
            ],
            _ =>
            [
                new CleaningChecklistItemDto("1", "Remoção de enxoval", false),
                new CleaningChecklistItemDto("2", "Desinfecção de superfícies", false),
                new CleaningChecklistItemDto("3", "Troca de lençóis", false),
                new CleaningChecklistItemDto("4", "Verificação de equipamentos", false),
                new CleaningChecklistItemDto("5", "Liberação do leito", false),
            ],
        };

        return JsonSerializer.Serialize(items);
    }
}
