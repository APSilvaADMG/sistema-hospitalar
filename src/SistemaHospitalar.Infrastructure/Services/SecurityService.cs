using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaHospitalar.Application.DTOs.Security;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

public class SecurityService(
    AppDbContext dbContext,
    HospitalEventPublisher eventPublisher,
    INotificationService notificationService,
    IOptions<SecuritySettings> securitySettings) : ISecurityService
{
    private readonly SecuritySettings _settings = securitySettings.Value;

    public SecuritySettingsDto GetSettings()
        => new(_settings.VisitorPhotoRequired);

    public async Task<SecurityDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var visitorsInside = await dbContext.VisitorLogs
            .CountAsync(v => v.Status == VisitorLogStatus.Inside && v.IsActive, cancellationToken);

        var openIncidents = await dbContext.SecurityIncidents
            .AsNoTracking()
            .Where(i => i.IsActive && i.Status != SecurityIncidentStatus.Resolved)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new SecurityIncidentDto(
                i.Id, i.Type, i.Status, i.Location, i.Description,
                i.ReportedBy, i.CreatedAt, i.ResolvedAt,
                i.PatientId,
                i.Patient != null ? i.Patient.FullName : null,
                i.Severity))
            .ToListAsync(cancellationToken);

        var recentVisitors = await dbContext.VisitorLogs
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.EnteredAt)
            .Take(10)
            .Select(v => new VisitorLogDto(
                v.Id, v.VisitorName, v.DocumentNumber,
                v.Patient != null ? v.Patient.FullName : null,
                v.Destination, v.BadgeNumber, v.Status, v.EnteredAt, v.ExitedAt,
                v.PhotoData, v.PhotoData != null))
            .ToListAsync(cancellationToken);

        return new SecurityDashboardDto(visitorsInside, openIncidents.Count, recentVisitors, openIncidents);
    }

    public async Task<IReadOnlyList<SecurityIncidentDto>> GetIncidentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SecurityIncidents
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new SecurityIncidentDto(
                i.Id, i.Type, i.Status, i.Location, i.Description,
                i.ReportedBy, i.CreatedAt, i.ResolvedAt,
                i.PatientId,
                i.Patient != null ? i.Patient.FullName : null,
                i.Severity))
            .ToListAsync(cancellationToken);
    }

    public async Task<SecurityIncidentDto> CreateIncidentAsync(
        CreateSecurityIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var incident = new SecurityIncident
        {
            Type = request.Type,
            Location = request.Location.Trim(),
            Description = request.Description.Trim(),
            ReportedBy = request.ReportedBy?.Trim(),
            PatientId = request.PatientId,
            Severity = request.Severity,
        };

        dbContext.SecurityIncidents.Add(incident);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("security.incident", new
        {
            incident.Id,
            Type = incident.Type.ToString(),
            incident.Location,
            incident.Description
        }, cancellationToken);

        if (request.Type is SecurityIncidentType.Emergency or SecurityIncidentType.AccessDenied
            or SecurityIncidentType.PatientFall or SecurityIncidentType.MedicationError
            or SecurityIncidentType.ClinicalAdverseEvent)
        {
            await notificationService.NotifyAdminsAsync(
                "Segurança — novo incidente",
                $"{request.Type}: {request.Description} ({request.Location})",
                NotificationType.Alert,
                "SecurityIncident",
                incident.Id,
                cancellationToken);
        }

        return (await GetIncidentsAsync(cancellationToken)).First(i => i.Id == incident.Id);
    }

    public async Task<SecurityIncidentDto?> ResolveIncidentAsync(
        Guid id, ResolveIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var incident = await dbContext.SecurityIncidents
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);

        if (incident is null)
        {
            return null;
        }

        incident.Status = SecurityIncidentStatus.Resolved;
        incident.ResolvedAt = DateTime.UtcNow;
        incident.ResolutionNotes = request.ResolutionNotes?.Trim();
        incident.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetIncidentsAsync(cancellationToken)).FirstOrDefault(i => i.Id == id);
    }

    public async Task<IReadOnlyList<VisitorLogDto>> GetVisitorsAsync(
        bool? insideOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.VisitorLogs.AsNoTracking().Where(v => v.IsActive);

        if (insideOnly == true)
        {
            query = query.Where(v => v.Status == VisitorLogStatus.Inside);
        }

        return await query
            .OrderByDescending(v => v.EnteredAt)
            .Take(100)
            .Select(v => new VisitorLogDto(
                v.Id, v.VisitorName, v.DocumentNumber,
                v.Patient != null ? v.Patient.FullName : null,
                v.Destination, v.BadgeNumber, v.Status, v.EnteredAt, v.ExitedAt,
                v.PhotoData, v.PhotoData != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<VisitorLogDto> RegisterVisitorAsync(
        RegisterVisitorRequest request, CancellationToken cancellationToken = default)
    {
        var photoData = NormalizePhoto(request.PhotoData);
        if (_settings.VisitorPhotoRequired && photoData is null)
        {
            throw new InvalidOperationException("A foto do visitante é obrigatória.");
        }

        var destination = request.Destination?.Trim();
        if (string.IsNullOrWhiteSpace(destination) && request.PatientId.HasValue)
        {
            destination = await ResolveVisitorDestinationAsync(request.PatientId.Value, cancellationToken);
        }

        var log = new VisitorLog
        {
            VisitorName = request.VisitorName.Trim(),
            DocumentNumber = request.DocumentNumber?.Trim(),
            PatientId = request.PatientId,
            Destination = destination,
            BadgeNumber = request.BadgeNumber?.Trim() ?? $"V-{DateTime.UtcNow:HHmmss}",
            PhotoData = photoData
        };

        dbContext.VisitorLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("security.visitor.checkin", new
        {
            log.Id,
            log.VisitorName,
            log.BadgeNumber,
            log.Destination,
            HasPhoto = log.PhotoData != null
        }, cancellationToken);

        return (await GetVisitorsAsync(null, cancellationToken)).First(v => v.Id == log.Id);
    }

    public async Task<VisitorLogDto?> RegisterExitAsync(Guid visitorLogId, CancellationToken cancellationToken = default)
    {
        var log = await dbContext.VisitorLogs
            .FirstOrDefaultAsync(v => v.Id == visitorLogId && v.IsActive, cancellationToken);

        if (log is null || log.Status != VisitorLogStatus.Inside)
        {
            return null;
        }

        log.Status = VisitorLogStatus.Exited;
        log.ExitedAt = DateTime.UtcNow;
        log.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetVisitorsAsync(null, cancellationToken)).FirstOrDefault(v => v.Id == visitorLogId);
    }

    private async Task<string?> ResolveVisitorDestinationAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var active = await dbContext.Hospitalizations
            .AsNoTracking()
            .Where(h => h.PatientId == patientId && h.Status == HospitalizationStatus.Active && h.IsActive)
            .Select(h => new
            {
                WardName = h.Bed.Ward.Name,
                WardFloor = h.Bed.Ward.Floor,
                h.Bed.BedNumber
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (active is not null)
        {
            var floor = string.IsNullOrWhiteSpace(active.WardFloor) ? string.Empty : $" — {active.WardFloor}º andar";
            return $"{active.WardName}{floor} — Leito {active.BedNumber}";
        }

        var patient = await dbContext.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.FullName, p.AddressCity })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(patient.AddressCity)
            ? $"Ambulatório — {patient.FullName}"
            : $"Ambulatório — {patient.AddressCity}";
    }

    private static string? NormalizePhoto(string? photoData)
    {
        if (string.IsNullOrWhiteSpace(photoData))
        {
            return null;
        }

        if (photoData.Length > 500_000)
        {
            throw new InvalidOperationException("A foto excede o tamanho máximo permitido.");
        }

        return photoData;
    }
}
