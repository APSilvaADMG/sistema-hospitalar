using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Cme;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class CmeService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : ICmeService
{
    public async Task<IReadOnlyList<InstrumentKitDto>> GetKitsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InstrumentKits
            .AsNoTracking()
            .Where(k => k.IsActive)
            .OrderBy(k => k.Code)
            .Select(k => new InstrumentKitDto(
                k.Id, k.Name, k.Code, k.Description, k.Status, k.SterilityExpiration))
            .ToListAsync(cancellationToken);
    }

    public async Task<InstrumentKitDto> CreateKitAsync(
        CreateInstrumentKitRequest request, CancellationToken cancellationToken = default)
    {
        if (await dbContext.InstrumentKits.AnyAsync(k => k.Code == request.Code.Trim() && k.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Código de kit já cadastrado.");
        }

        var kit = new InstrumentKit
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Description = request.Description?.Trim()
        };

        dbContext.InstrumentKits.Add(kit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetKitsAsync(cancellationToken)).First(k => k.Id == kit.Id);
    }

    public async Task<IReadOnlyList<SterilizationCycleDto>> GetCyclesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SterilizationCycles
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new SterilizationCycleDto(
                c.Id, c.InstrumentKitId, c.InstrumentKit.Name, c.InstrumentKit.Code,
                c.Method, c.Status, c.SterilizerName, c.OperatorName,
                c.StartedAt, c.CompletedAt, c.ExpirationDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<SterilizationCycleDto> CreateCycleAsync(
        CreateSterilizationCycleRequest request, CancellationToken cancellationToken = default)
    {
        var kit = await dbContext.InstrumentKits
            .FirstOrDefaultAsync(k => k.Id == request.InstrumentKitId && k.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Kit não encontrado.");

        kit.Status = InstrumentKitStatus.InSterilization;

        var cycle = new SterilizationCycle
        {
            InstrumentKitId = request.InstrumentKitId,
            Method = request.Method,
            SterilizerName = request.SterilizerName.Trim(),
            OperatorName = request.OperatorName?.Trim()
        };

        dbContext.SterilizationCycles.Add(cycle);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("cme.cycle.created", new
        {
            cycle.Id,
            KitCode = kit.Code,
            Method = cycle.Method.ToString()
        }, cancellationToken);

        return (await GetCyclesAsync(cancellationToken)).First(c => c.Id == cycle.Id);
    }

    public async Task<SterilizationCycleDto?> StartCycleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cycle = await dbContext.SterilizationCycles
            .Include(c => c.InstrumentKit)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cycle is null || cycle.Status != SterilizationCycleStatus.Pending)
        {
            return null;
        }

        cycle.Status = SterilizationCycleStatus.InProgress;
        cycle.StartedAt = DateTime.UtcNow;
        cycle.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetCyclesAsync(cancellationToken)).FirstOrDefault(c => c.Id == id);
    }

    public async Task<SterilizationCycleDto?> CompleteCycleAsync(
        Guid id, CompleteSterilizationCycleRequest request, CancellationToken cancellationToken = default)
    {
        var cycle = await dbContext.SterilizationCycles
            .Include(c => c.InstrumentKit)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cycle is null || cycle.Status != SterilizationCycleStatus.InProgress)
        {
            return null;
        }

        cycle.Status = SterilizationCycleStatus.Completed;
        cycle.CompletedAt = DateTime.UtcNow;
        cycle.ExpirationDate = request.ExpirationDate;
        cycle.UpdatedAt = DateTime.UtcNow;

        if (request.ExpirationDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.ExpiredKit}] Data de validade não pode ser anterior à data atual (RN-022).");
        }

        cycle.InstrumentKit.Status = InstrumentKitStatus.Sterile;
        cycle.InstrumentKit.SterilityExpiration = request.ExpirationDate;
        cycle.InstrumentKit.UpdatedAt = DateTime.UtcNow;

        HospitalBusinessRules.ValidateSterileKit(
            cycle.InstrumentKit.Status,
            cycle.InstrumentKit.SterilityExpiration);

        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("cme.cycle.completed", new
        {
            cycle.Id,
            KitCode = cycle.InstrumentKit.Code,
            ExpirationDate = request.ExpirationDate
        }, cancellationToken);

        return (await GetCyclesAsync(cancellationToken)).FirstOrDefault(c => c.Id == id);
    }

    public async Task<SterilizationCycleDto?> RejectSterilizationCycleAsync(
        Guid id, string? reason, CancellationToken cancellationToken = default)
    {
        var cycle = await dbContext.SterilizationCycles
            .Include(c => c.InstrumentKit)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

        if (cycle is null || cycle.Status is SterilizationCycleStatus.Completed or SterilizationCycleStatus.Failed)
        {
            return null;
        }

        cycle.Status = SterilizationCycleStatus.Failed;
        cycle.CompletedAt = DateTime.UtcNow;
        cycle.UpdatedAt = DateTime.UtcNow;
        cycle.InstrumentKit.Status = InstrumentKitStatus.Available;
        cycle.InstrumentKit.SterilityExpiration = null;
        cycle.InstrumentKit.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            cycle.OperatorName = $"{cycle.OperatorName} | Rejeição: {reason.Trim()}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("cme.cycle.rejected", new
        {
            cycle.Id,
            KitCode = cycle.InstrumentKit.Code,
            Reason = reason
        }, cancellationToken);

        return (await GetCyclesAsync(cancellationToken)).FirstOrDefault(c => c.Id == id);
    }
}
