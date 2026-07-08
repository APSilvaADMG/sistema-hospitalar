using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.InfectionControl;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class InfectionControlService(
    AppDbContext dbContext,
    HospitalEventPublisher eventPublisher,
    INotificationService notificationService) : IInfectionControlService
{
    public async Task<InfectionControlDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var activeIsolations = await dbContext.IsolationPrecautions
            .CountAsync(p => p.IsActive && p.Status == IsolationPrecautionStatus.Active, cancellationToken);

        var openCases = await GetSurveillanceCasesAsync(cancellationToken);
        var activeOpen = openCases.Where(c => c.Status != InfectionSurveillanceStatus.Resolved).ToList();

        var precautions = await GetIsolationPrecautionsAsync(true, cancellationToken);

        return new InfectionControlDashboardDto(
            activeIsolations,
            activeOpen.Count,
            activeOpen.Take(10).ToList(),
            precautions);
    }

    public async Task<IReadOnlyList<InfectionSurveillanceDto>> GetSurveillanceCasesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.InfectionSurveillances
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.DetectedAt)
            .Select(i => new InfectionSurveillanceDto(
                i.Id, i.PatientId,
                i.Patient != null ? i.Patient.FullName : null,
                i.Hospitalization != null ? i.Hospitalization.Bed.Ward.Name : null,
                i.Location, i.InfectionType, i.Organism, i.Site, i.Status,
                i.DetectedAt, i.ReportedBy, i.Notes, i.ResolvedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<InfectionSurveillanceDto> CreateSurveillanceCaseAsync(
        CreateInfectionSurveillanceRequest request, CancellationToken cancellationToken = default)
    {
        var surveillance = new InfectionSurveillance
        {
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            Location = request.Location.Trim(),
            InfectionType = request.InfectionType,
            Organism = request.Organism.Trim(),
            Site = request.Site?.Trim(),
            ReportedBy = request.ReportedBy?.Trim(),
            Notes = request.Notes?.Trim()
        };

        dbContext.InfectionSurveillances.Add(surveillance);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("ccih.surveillance.created", new
        {
            surveillance.Id,
            InfectionType = request.InfectionType.ToString(),
            request.Organism,
            request.Location
        }, cancellationToken);

        await notificationService.NotifyAdminsAsync(
            "CCIH — caso de vigilância",
            $"{request.InfectionType}: {request.Organism} em {request.Location}",
            NotificationType.Alert,
            "InfectionSurveillance",
            surveillance.Id,
            cancellationToken);

        return (await GetSurveillanceCasesAsync(cancellationToken)).First(i => i.Id == surveillance.Id);
    }

    public async Task<InfectionSurveillanceDto?> ResolveSurveillanceCaseAsync(
        Guid id, ResolveInfectionRequest request, CancellationToken cancellationToken = default)
    {
        var surveillance = await dbContext.InfectionSurveillances
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);

        if (surveillance is null)
        {
            return null;
        }

        surveillance.Status = InfectionSurveillanceStatus.Resolved;
        surveillance.ResolvedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            surveillance.Notes = request.Notes.Trim();
        }

        surveillance.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSurveillanceCasesAsync(cancellationToken)).FirstOrDefault(i => i.Id == id);
    }

    public async Task<IReadOnlyList<IsolationPrecautionDto>> GetIsolationPrecautionsAsync(
        bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.IsolationPrecautions.AsNoTracking().Where(p => p.IsActive);

        if (activeOnly == true)
        {
            query = query.Where(p => p.Status == IsolationPrecautionStatus.Active);
        }

        return await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new IsolationPrecautionDto(
                p.Id, p.PatientId, p.Patient.FullName,
                p.Hospitalization != null ? p.Hospitalization.Bed.Ward.Name : null,
                p.PrecautionType, p.Status, p.StartDate, p.EndDate, p.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task<IsolationPrecautionDto> CreateIsolationPrecautionAsync(
        CreateIsolationPrecautionRequest request, CancellationToken cancellationToken = default)
    {
        var precaution = new IsolationPrecaution
        {
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            PrecautionType = request.PrecautionType,
            StartDate = request.StartDate,
            Reason = request.Reason.Trim()
        };

        dbContext.IsolationPrecautions.Add(precaution);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("ccih.isolation.created", new
        {
            precaution.Id,
            request.PatientId,
            PrecautionType = request.PrecautionType.ToString()
        }, cancellationToken);

        return (await GetIsolationPrecautionsAsync(null, cancellationToken)).First(p => p.Id == precaution.Id);
    }

    public async Task<IsolationPrecautionDto?> LiftIsolationPrecautionAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var precaution = await dbContext.IsolationPrecautions
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);

        if (precaution is null || precaution.Status != IsolationPrecautionStatus.Active)
        {
            return null;
        }

        precaution.Status = IsolationPrecautionStatus.Lifted;
        precaution.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        precaution.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetIsolationPrecautionsAsync(null, cancellationToken)).FirstOrDefault(p => p.Id == id);
    }
}
