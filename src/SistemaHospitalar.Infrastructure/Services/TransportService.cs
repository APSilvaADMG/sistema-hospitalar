using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Transport;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class TransportService(
    AppDbContext dbContext,
    IOperationsRealtimeNotifier realtime) : ITransportService
{
    public async Task<TransportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await TransportSeed.EnsureAsync(dbContext, cancellationToken);
        await RefreshSlaViolationsAsync(cancellationToken);

        var assets = await dbContext.TransportAssets.AsNoTracking().Where(a => a.IsActive).ToListAsync(cancellationToken);
        var activeStatuses = new[] { TransportRequestStatus.Queued, TransportRequestStatus.Accepted, TransportRequestStatus.InTransit };

        var active = await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive && activeStatuses.Contains(r.Status))
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.RequestedAt)
            .Select(MapRequest)
            .ToListAsync(cancellationToken);

        var recentCompleted = await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive && r.Status == TransportRequestStatus.Completed)
            .OrderByDescending(r => r.CompletedAt)
            .Take(10)
            .Select(MapRequest)
            .ToListAsync(cancellationToken);

        var completed = await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive && r.Status == TransportRequestStatus.Completed && r.AcceptedAt != null && r.CompletedAt != null)
            .ToListAsync(cancellationToken);

        var metrics = BuildMetrics(completed);

        return new TransportDashboardDto(
            assets.Count,
            assets.Count(a => a.Status == TransportAssetStatus.Available),
            active.Count,
            active.Count(r => r.Status == TransportRequestStatus.Queued),
            active.Count(r => r.Status == TransportRequestStatus.InTransit),
            metrics.AvgAcceptMinutes,
            metrics.AvgCompleteMinutes,
            metrics.SectorDemand.FirstOrDefault()?.OriginType.ToString(),
            metrics.PorterProductivity.FirstOrDefault()?.EmployeeName,
            active,
            recentCompleted);
    }

    public async Task<TransportMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var completed = await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.IsActive && r.Status == TransportRequestStatus.Completed)
            .Include(r => r.AssignedEmployee)
            .ToListAsync(cancellationToken);

        var metrics = BuildMetrics(completed);
        var transitAvg = completed
            .Where(r => r.DepartedAt != null && r.CompletedAt != null)
            .Select(r => (r.CompletedAt!.Value - r.DepartedAt!.Value).TotalMinutes)
            .DefaultIfEmpty()
            .Average();

        return new TransportMetricsDto(
            metrics.AvgAcceptMinutes,
            metrics.AvgCompleteMinutes,
            completed.Count > 0 ? transitAvg : null,
            metrics.SectorDemand,
            metrics.PorterProductivity);
    }

    public async Task<IReadOnlyList<TransportAssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        await TransportSeed.EnsureAsync(dbContext, cancellationToken);
        return await dbContext.TransportAssets
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .Select(a => new TransportAssetDto(
                a.Id, a.Code, a.AssetTag, a.AssetType, a.Sector, a.Status, a.TrackingCode, a.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<TransportAssetDto> CreateAssetAsync(CreateTransportAssetRequest request, CancellationToken cancellationToken = default)
    {
        var asset = new TransportAsset
        {
            Code = request.Code.Trim(),
            AssetTag = request.AssetTag.Trim(),
            AssetType = request.AssetType,
            Sector = request.Sector.Trim(),
            TrackingCode = request.TrackingCode?.Trim(),
            Notes = request.Notes?.Trim(),
        };

        dbContext.TransportAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TransportAssetDto(
            asset.Id, asset.Code, asset.AssetTag, asset.AssetType, asset.Sector,
            asset.Status, asset.TrackingCode, asset.Notes);
    }

    public async Task<TransportAssetDto?> UpdateAssetStatusAsync(
        Guid id, UpdateTransportAssetStatusRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.TransportAssets.FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
        if (asset is null) return null;

        asset.Status = request.Status;
        asset.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TransportAssetDto(
            asset.Id, asset.Code, asset.AssetTag, asset.AssetType, asset.Sector,
            asset.Status, asset.TrackingCode, asset.Notes);
    }

    public async Task<IReadOnlyList<TransportRequestDto>> GetRequestsAsync(
        TransportRequestStatus? status, CancellationToken cancellationToken = default)
    {
        await RefreshSlaViolationsAsync(cancellationToken);

        var query = dbContext.TransportRequests.AsNoTracking().Where(r => r.IsActive);
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.RequestedAt)
            .Select(MapRequest)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransportPorterDto>> GetPortersAsync(CancellationToken cancellationToken = default)
    {
        await TransportSeed.EnsureAsync(dbContext, cancellationToken);

        return await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.JobTitle != null && e.JobTitle.Contains("Maqueiro"))
            .OrderBy(e => e.FullName)
            .Select(e => new TransportPorterDto(e.Id, e.FullName, e.JobTitle))
            .ToListAsync(cancellationToken);
    }

    public async Task<TransportRequestDto> CreateRequestAsync(
        CreateTransportRequestRequest request, string? requestedBy, CancellationToken cancellationToken = default)
    {
        var transport = new TransportRequest
        {
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            PatientName = request.PatientName.Trim(),
            OriginType = request.OriginType,
            OriginDetail = request.OriginDetail?.Trim(),
            DestinationType = request.DestinationType,
            DestinationDetail = request.DestinationDetail?.Trim(),
            Priority = request.Priority,
            Notes = request.Notes?.Trim(),
            RequestedBy = requestedBy,
            Status = TransportRequestStatus.Queued,
            SlaDeadlineAt = DateTime.UtcNow.AddMinutes(request.Priority == TransportPriority.Urgent ? 10 : 30),
        };

        dbContext.TransportRequests.Add(transport);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyTransportChangedAsync(transport.Id, cancellationToken);

        return (await GetRequestByIdAsync(transport.Id, cancellationToken))!;
    }

    public async Task<TransportRequestDto?> AcceptRequestAsync(
        Guid id, AcceptTransportRequestRequest request, CancellationToken cancellationToken = default)
    {
        var transport = await dbContext.TransportRequests
            .Include(r => r.TransportAsset)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (transport is null || transport.Status != TransportRequestStatus.Queued)
        {
            return null;
        }

        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.IsActive, cancellationToken);
        if (employee is null)
        {
            throw new InvalidOperationException("Maqueiro não encontrado.");
        }

        TransportAsset? asset = null;
        if (request.TransportAssetId.HasValue)
        {
            asset = await dbContext.TransportAssets
                .FirstOrDefaultAsync(a => a.Id == request.TransportAssetId && a.IsActive, cancellationToken);

            if (asset is null)
            {
                throw new InvalidOperationException("Equipamento não encontrado.");
            }

            if (asset.Status != TransportAssetStatus.Available)
            {
                throw new InvalidOperationException("Equipamento indisponível.");
            }

            asset.Status = TransportAssetStatus.InUse;
            asset.UpdatedAt = DateTime.UtcNow;
        }

        transport.AssignedEmployeeId = employee.Id;
        transport.TransportAssetId = asset?.Id;
        transport.Status = TransportRequestStatus.Accepted;
        transport.AcceptedAt = DateTime.UtcNow;
        transport.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyTransportChangedAsync(id, cancellationToken);
        return await GetRequestByIdAsync(id, cancellationToken);
    }

    public async Task<TransportRequestDto?> AdvanceRequestAsync(
        Guid id, AdvanceTransportRequestRequest request, CancellationToken cancellationToken = default)
    {
        var transport = await dbContext.TransportRequests
            .Include(r => r.TransportAsset)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (transport is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        switch (request.Status)
        {
            case TransportRequestStatus.InTransit when transport.Status == TransportRequestStatus.Accepted:
                transport.ArrivedAtOriginAt ??= now;
                transport.DepartedAt = now;
                transport.Status = TransportRequestStatus.InTransit;
                break;
            case TransportRequestStatus.Completed when transport.Status == TransportRequestStatus.InTransit:
                transport.ArrivedAtDestinationAt = now;
                transport.CompletedAt = now;
                transport.Status = TransportRequestStatus.Completed;
                if (transport.TransportAsset is not null)
                {
                    transport.TransportAsset.Status = TransportAssetStatus.Available;
                    transport.TransportAsset.UpdatedAt = now;
                }
                break;
            default:
                throw new InvalidOperationException("Transição de status inválida.");
        }

        transport.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyTransportChangedAsync(id, cancellationToken);
        return await GetRequestByIdAsync(id, cancellationToken);
    }

    public async Task<TransportRequestDto?> CancelRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transport = await dbContext.TransportRequests
            .Include(r => r.TransportAsset)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);

        if (transport is null || transport.Status is TransportRequestStatus.Completed or TransportRequestStatus.Cancelled)
        {
            return null;
        }

        transport.Status = TransportRequestStatus.Cancelled;
        transport.UpdatedAt = DateTime.UtcNow;

        if (transport.TransportAsset is not null)
        {
            transport.TransportAsset.Status = TransportAssetStatus.Available;
            transport.TransportAsset.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtime.NotifyTransportChangedAsync(id, cancellationToken);
        return await GetRequestByIdAsync(id, cancellationToken);
    }

    private async Task<TransportRequestDto?> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.TransportRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(MapRequest)
            .FirstOrDefaultAsync(cancellationToken);

    private static readonly System.Linq.Expressions.Expression<Func<TransportRequest, TransportRequestDto>> MapRequest = r =>
        new TransportRequestDto(
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
            r.IsSlaViolated);

    private async Task RefreshSlaViolationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeStatuses = new[] { TransportRequestStatus.Queued, TransportRequestStatus.Accepted, TransportRequestStatus.InTransit };
        var overdue = await dbContext.TransportRequests
            .Where(r => r.IsActive
                && activeStatuses.Contains(r.Status)
                && r.SlaDeadlineAt != null
                && r.SlaDeadlineAt < now
                && !r.IsSlaViolated)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var request in overdue)
        {
            request.IsSlaViolated = true;
            request.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (double? AvgAcceptMinutes, double? AvgCompleteMinutes,
        IReadOnlyList<TransportSectorDemandDto> SectorDemand,
        IReadOnlyList<TransportPorterProductivityDto> PorterProductivity) BuildMetrics(List<TransportRequest> completed)
    {
        var acceptTimes = completed
            .Where(r => r.AcceptedAt != null)
            .Select(r => (r.AcceptedAt!.Value - r.RequestedAt).TotalMinutes)
            .ToList();

        var completeTimes = completed
            .Where(r => r.CompletedAt != null)
            .Select(r => (r.CompletedAt!.Value - r.RequestedAt).TotalMinutes)
            .ToList();

        var sectorDemand = completed
            .GroupBy(r => r.OriginType)
            .Select(g => new TransportSectorDemandDto(g.Key, g.Count()))
            .OrderByDescending(s => s.RequestCount)
            .Take(5)
            .ToList();

        var porterProductivity = completed
            .Where(r => r.AssignedEmployeeId != null)
            .GroupBy(r => new { r.AssignedEmployeeId, Name = r.AssignedEmployee?.FullName ?? "—" })
            .Select(g => new TransportPorterProductivityDto(
                g.Key.AssignedEmployeeId!.Value,
                g.Key.Name,
                g.Count(),
                g.Where(r => r.CompletedAt != null)
                    .Select(r => (r.CompletedAt!.Value - r.RequestedAt).TotalMinutes)
                    .DefaultIfEmpty()
                    .Average()))
            .OrderByDescending(p => p.CompletedCount)
            .Take(5)
            .ToList();

        return (
            acceptTimes.Count > 0 ? acceptTimes.Average() : null,
            completeTimes.Count > 0 ? completeTimes.Average() : null,
            sectorDemand,
            porterProductivity);
    }
}
