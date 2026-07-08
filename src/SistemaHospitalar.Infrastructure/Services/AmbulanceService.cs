using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Ambulance;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class AmbulanceService(AppDbContext dbContext) : IAmbulanceService
{
    public async Task<IReadOnlyList<AmbulanceDto>> GetAmbulancesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Ambulances
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Code)
            .Select(a => new AmbulanceDto(a.Id, a.Code, a.Plate, a.Status, a.BaseLocation))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AmbulanceDispatchDto>> GetDispatchesAsync(
        AmbulanceDispatchStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AmbulanceDispatches.AsNoTracking().Where(d => d.IsActive);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return await query
            .OrderByDescending(d => d.RequestedAt)
            .Select(d => new AmbulanceDispatchDto(
                d.Id,
                d.AmbulanceId,
                d.Ambulance != null ? d.Ambulance.Code : null,
                d.PatientName,
                d.PickupAddress,
                d.Destination,
                d.Status,
                d.RequestedAt,
                d.DispatchedAt,
                d.CompletedAt,
                d.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<AmbulanceDispatchDto> CreateDispatchAsync(
        CreateAmbulanceDispatchRequest request, CancellationToken cancellationToken = default)
    {
        var dispatch = new AmbulanceDispatch
        {
            PatientName = request.PatientName.Trim(),
            PickupAddress = request.PickupAddress.Trim(),
            Destination = request.Destination.Trim(),
            Notes = request.Notes?.Trim()
        };

        dbContext.AmbulanceDispatches.Add(dispatch);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetDispatchByIdAsync(dispatch.Id, cancellationToken))!;
    }

    public async Task<AmbulanceDispatchDto?> AssignAmbulanceAsync(
        Guid dispatchId, AssignAmbulanceRequest request, CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.AmbulanceDispatches
            .FirstOrDefaultAsync(d => d.Id == dispatchId && d.IsActive, cancellationToken);

        var ambulance = await dbContext.Ambulances
            .FirstOrDefaultAsync(a => a.Id == request.AmbulanceId && a.IsActive, cancellationToken);

        if (dispatch is null || ambulance is null)
        {
            return null;
        }

        if (ambulance.Status != AmbulanceStatus.Available)
        {
            throw new InvalidOperationException("Ambulância indisponível.");
        }

        dispatch.AmbulanceId = ambulance.Id;
        dispatch.Status = AmbulanceDispatchStatus.Dispatched;
        dispatch.DispatchedAt = DateTime.UtcNow;
        ambulance.Status = AmbulanceStatus.Dispatched;
        dispatch.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDispatchByIdAsync(dispatchId, cancellationToken);
    }

    public async Task<AmbulanceDispatchDto?> UpdateDispatchStatusAsync(
        Guid dispatchId, UpdateDispatchStatusRequest request, CancellationToken cancellationToken = default)
    {
        var dispatch = await dbContext.AmbulanceDispatches
            .Include(d => d.Ambulance)
            .FirstOrDefaultAsync(d => d.Id == dispatchId && d.IsActive, cancellationToken);

        if (dispatch is null)
        {
            return null;
        }

        dispatch.Status = request.Status;
        dispatch.UpdatedAt = DateTime.UtcNow;

        if (request.Status == AmbulanceDispatchStatus.Completed)
        {
            dispatch.CompletedAt = DateTime.UtcNow;
            if (dispatch.Ambulance is not null)
            {
                dispatch.Ambulance.Status = AmbulanceStatus.Available;
            }
        }
        else if (dispatch.Ambulance is not null)
        {
            dispatch.Ambulance.Status = request.Status switch
            {
                AmbulanceDispatchStatus.OnScene => AmbulanceStatus.OnScene,
                AmbulanceDispatchStatus.Transporting => AmbulanceStatus.Transporting,
                AmbulanceDispatchStatus.Cancelled => AmbulanceStatus.Available,
                _ => dispatch.Ambulance.Status
            };
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDispatchByIdAsync(dispatchId, cancellationToken);
    }

    private async Task<AmbulanceDispatchDto?> GetDispatchByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.AmbulanceDispatches
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new AmbulanceDispatchDto(
                d.Id,
                d.AmbulanceId,
                d.Ambulance != null ? d.Ambulance.Code : null,
                d.PatientName,
                d.PickupAddress,
                d.Destination,
                d.Status,
                d.RequestedAt,
                d.DispatchedAt,
                d.CompletedAt,
                d.Notes))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
