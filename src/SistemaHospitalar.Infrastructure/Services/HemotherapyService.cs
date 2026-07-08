using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Hemotherapy;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HemotherapyService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : IHemotherapyService
{
    public async Task<IReadOnlyList<BloodUnitDto>> GetBloodUnitsAsync(
        BloodUnitStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.BloodUnits.AsNoTracking().Where(b => b.IsActive);

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        return await query
            .OrderBy(b => b.ExpiresAt)
            .Select(b => new BloodUnitDto(
                b.Id, b.UnitCode, b.BloodType, b.Component, b.VolumeMl,
                b.CollectedAt, b.ExpiresAt, b.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<BloodUnitDto> CreateBloodUnitAsync(
        CreateBloodUnitRequest request, CancellationToken cancellationToken = default)
    {
        if (await dbContext.BloodUnits.AnyAsync(b => b.UnitCode == request.UnitCode.Trim() && b.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Código de bolsa já cadastrado.");
        }

        var unit = new BloodUnit
        {
            UnitCode = request.UnitCode.Trim(),
            BloodType = request.BloodType,
            Component = request.Component,
            VolumeMl = request.VolumeMl,
            CollectedAt = request.CollectedAt,
            ExpiresAt = request.ExpiresAt
        };

        dbContext.BloodUnits.Add(unit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetBloodUnitsAsync(null, cancellationToken)).First(b => b.Id == unit.Id);
    }

    public async Task<IReadOnlyList<TransfusionRequestDto>> GetTransfusionRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TransfusionRequests
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new TransfusionRequestDto(
                r.Id, r.PatientId, r.Patient.FullName,
                r.Hospitalization != null ? r.Hospitalization.Bed.Ward.Name : null,
                r.Hospitalization != null ? r.Hospitalization.Bed.BedNumber : null,
                r.RequestingProfessional.FullName,
                r.BloodTypeRequired, r.Component, r.UnitsRequested, r.Status,
                r.BloodUnit != null ? r.BloodUnit.UnitCode : null,
                r.Notes, r.CreatedAt, r.TransfusedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TransfusionRequestDto> CreateTransfusionRequestAsync(
        CreateTransfusionRequestRequest request, CancellationToken cancellationToken = default)
    {
        var transfusion = new TransfusionRequest
        {
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            RequestingProfessionalId = request.RequestingProfessionalId,
            BloodTypeRequired = request.BloodTypeRequired,
            Component = request.Component,
            UnitsRequested = request.UnitsRequested,
            Notes = request.Notes?.Trim()
        };

        dbContext.TransfusionRequests.Add(transfusion);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("hemotherapy.transfusion.requested", new
        {
            transfusion.Id,
            PatientId = request.PatientId,
            BloodType = request.BloodTypeRequired.ToString(),
            Component = request.Component.ToString()
        }, cancellationToken);

        return (await GetTransfusionRequestsAsync(cancellationToken)).First(r => r.Id == transfusion.Id);
    }

    public async Task<TransfusionRequestDto?> MatchTransfusionAsync(
        Guid requestId, MatchTransfusionRequest request, CancellationToken cancellationToken = default)
    {
        var transfusion = await dbContext.TransfusionRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive, cancellationToken);

        if (transfusion is null || transfusion.Status != TransfusionRequestStatus.Requested)
        {
            return null;
        }

        var unit = await dbContext.BloodUnits
            .FirstOrDefaultAsync(b => b.Id == request.BloodUnitId && b.IsActive && b.Status == BloodUnitStatus.Available, cancellationToken);

        if (unit is null)
        {
            throw new InvalidOperationException("Bolsa indisponível.");
        }

        if (unit.BloodType != transfusion.BloodTypeRequired || unit.Component != transfusion.Component)
        {
            throw new InvalidOperationException("Bolsa incompatível com a solicitação.");
        }

        unit.Status = BloodUnitStatus.Reserved;
        transfusion.BloodUnitId = unit.Id;
        transfusion.Status = TransfusionRequestStatus.Matched;
        transfusion.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetTransfusionRequestsAsync(cancellationToken)).FirstOrDefault(r => r.Id == requestId);
    }

    public async Task<TransfusionRequestDto?> CompleteTransfusionAsync(
        Guid requestId, CancellationToken cancellationToken = default)
    {
        var transfusion = await dbContext.TransfusionRequests
            .Include(r => r.BloodUnit)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive, cancellationToken);

        if (transfusion is null || transfusion.Status != TransfusionRequestStatus.Matched)
        {
            return null;
        }

        transfusion.Status = TransfusionRequestStatus.Transfused;
        transfusion.TransfusedAt = DateTime.UtcNow;
        transfusion.UpdatedAt = DateTime.UtcNow;

        if (transfusion.BloodUnit is not null)
        {
            transfusion.BloodUnit.Status = BloodUnitStatus.Transfused;
            transfusion.BloodUnit.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("hemotherapy.transfusion.completed", new
        {
            transfusion.Id,
            PatientId = transfusion.PatientId,
            UnitCode = transfusion.BloodUnit?.UnitCode
        }, cancellationToken);

        return (await GetTransfusionRequestsAsync(cancellationToken)).FirstOrDefault(r => r.Id == requestId);
    }
}
