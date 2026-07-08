using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Dialysis;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class DialysisService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : IDialysisService
{
    public async Task<IReadOnlyList<DialysisSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.DialysisSessions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ScheduledAt)
            .Select(s => new DialysisSessionDto(
                s.Id, s.PatientId, s.Patient.FullName,
                s.Hospitalization != null ? s.Hospitalization.Bed.Ward.Name : null,
                s.MachineNumber, s.Status, s.ScheduledAt, s.StartedAt, s.CompletedAt,
                s.DryWeightKg, s.NurseName, s.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<DialysisSessionDto> CreateSessionAsync(
        CreateDialysisSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new DialysisSession
        {
            PatientId = request.PatientId,
            HospitalizationId = request.HospitalizationId,
            MachineNumber = request.MachineNumber.Trim(),
            ScheduledAt = request.ScheduledAt,
            DryWeightKg = request.DryWeightKg,
            NurseName = request.NurseName?.Trim(),
            Notes = request.Notes?.Trim()
        };

        dbContext.DialysisSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("dialysis.session.scheduled", new
        {
            session.Id,
            PatientId = request.PatientId,
            session.MachineNumber,
            session.ScheduledAt
        }, cancellationToken);

        return (await GetSessionsAsync(cancellationToken)).First(s => s.Id == session.Id);
    }

    public async Task<DialysisSessionDto?> UpdateSessionStatusAsync(
        Guid id, UpdateDialysisSessionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.DialysisSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

        if (session is null)
        {
            return null;
        }

        session.Status = request.Status;
        session.UpdatedAt = DateTime.UtcNow;

        if (request.Status == DialysisSessionStatus.InProgress && session.StartedAt is null)
        {
            session.StartedAt = DateTime.UtcNow;
        }

        if (request.Status == DialysisSessionStatus.Completed)
        {
            session.CompletedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSessionsAsync(cancellationToken)).FirstOrDefault(s => s.Id == id);
    }
}
