using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Oncology;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class OncologyService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : IOncologyService
{
    public async Task<IReadOnlyList<ChemotherapySessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChemotherapySessions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ScheduledAt)
            .Select(s => new ChemotherapySessionDto(
                s.Id, s.PatientId, s.Patient.FullName,
                s.Hospitalization != null ? s.Hospitalization.Bed.Ward.Name : null,
                s.Professional.FullName, s.ProtocolName, s.DrugRegimen,
                s.CycleNumber, s.TotalCycles, s.Status, s.ScheduledAt,
                s.AdministeredAt, s.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChemotherapySessionDto> CreateSessionAsync(
        CreateChemotherapySessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new ChemotherapySession
        {
            PatientId = request.PatientId,
            ProfessionalId = request.ProfessionalId,
            HospitalizationId = request.HospitalizationId,
            ProtocolName = request.ProtocolName.Trim(),
            DrugRegimen = request.DrugRegimen.Trim(),
            CycleNumber = request.CycleNumber,
            TotalCycles = request.TotalCycles,
            ScheduledAt = request.ScheduledAt,
            Notes = request.Notes?.Trim()
        };

        dbContext.ChemotherapySessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("oncology.chemotherapy.scheduled", new
        {
            session.Id,
            request.PatientId,
            request.ProtocolName,
            request.CycleNumber
        }, cancellationToken);

        return (await GetSessionsAsync(cancellationToken)).First(s => s.Id == session.Id);
    }

    public async Task<ChemotherapySessionDto?> UpdateSessionStatusAsync(
        Guid id, UpdateChemotherapyStatusRequest request, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ChemotherapySessions
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

        if (session is null)
        {
            return null;
        }

        session.Status = request.Status;
        session.UpdatedAt = DateTime.UtcNow;

        if (request.Status == ChemotherapySessionStatus.Administered)
        {
            session.AdministeredAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSessionsAsync(cancellationToken)).FirstOrDefault(s => s.Id == id);
    }
}
