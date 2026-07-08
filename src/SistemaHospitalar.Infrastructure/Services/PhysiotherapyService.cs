using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Physiotherapy;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PhysiotherapyService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : IPhysiotherapyService
{
    public async Task<IReadOnlyList<PhysiotherapySessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PhysiotherapySessions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ScheduledAt)
            .Select(s => new PhysiotherapySessionDto(
                s.Id, s.PatientId, s.Patient.FullName,
                s.Hospitalization != null ? s.Hospitalization.Bed.Ward.Name : null,
                s.TherapistName, s.SessionType, s.Status, s.ScheduledAt,
                s.DurationMinutes, s.Goals, s.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<PhysiotherapySessionDto> CreateSessionAsync(
        CreatePhysiotherapySessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new PhysiotherapySession
        {
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            TherapistName = request.TherapistName.Trim(),
            SessionType = request.SessionType,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Goals = request.Goals?.Trim(),
            Notes = request.Notes?.Trim()
        };

        dbContext.PhysiotherapySessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("physiotherapy.session.scheduled", new
        {
            session.Id,
            request.PatientId,
            SessionType = request.SessionType.ToString()
        }, cancellationToken);

        return (await GetSessionsAsync(cancellationToken)).First(s => s.Id == session.Id);
    }

    public async Task<PhysiotherapySessionDto?> UpdateSessionStatusAsync(
        Guid id, UpdatePhysiotherapyStatusRequest request, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.PhysiotherapySessions
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

        if (session is null)
        {
            return null;
        }

        session.Status = request.Status;
        session.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSessionsAsync(cancellationToken)).FirstOrDefault(s => s.Id == id);
    }
}
