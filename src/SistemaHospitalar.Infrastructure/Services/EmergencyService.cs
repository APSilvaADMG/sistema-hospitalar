using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Emergency;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class EmergencyService(
    AppDbContext dbContext,
    INotificationService notificationService,
    ClinicalStatusAuditLogger clinicalStatusAuditLogger) : IEmergencyService
{
    public async Task<IReadOnlyList<EmergencyVisitDto>> GetVisitsAsync(
        EmergencyVisitStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmergencyVisits.AsNoTracking().Where(v => v.IsActive);

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        var visits = await query
            .OrderBy(v => v.ArrivedAt)
            .Select(v => new EmergencyVisitDto(
                v.Id,
                v.PatientId,
                v.Patient.FullName,
                v.ChiefComplaint,
                v.Urgency,
                v.Status,
                v.Professional != null ? v.Professional.FullName : null,
                v.ArrivedAt,
                v.StartedAt,
                v.DischargedAt,
                v.Notes))
            .ToListAsync(cancellationToken);

        return visits
            .OrderBy(v => v.Urgency switch
            {
                TriageUrgency.Emergency => 0,
                TriageUrgency.High => 1,
                TriageUrgency.Medium => 2,
                TriageUrgency.Low => 3,
                TriageUrgency.NonUrgent => 4,
                _ => 5
            })
            .ThenBy(v => v.ArrivedAt)
            .ToList();
    }

    public async Task<EmergencyVisitDto> CreateVisitAsync(
        CreateEmergencyVisitRequest request, CancellationToken cancellationToken = default)
    {
        await PatientCareValidation.RequireEligibleForCareAsync(
            dbContext, request.PatientId, encryption: null, validateSusCns: true, cancellationToken);

        var visit = new EmergencyVisit
        {
            PatientId = request.PatientId,
            ChiefComplaint = request.ChiefComplaint.Trim(),
            Urgency = request.Urgency,
            AiTriageLogId = request.AiTriageLogId,
            Notes = request.Notes?.Trim()
        };

        dbContext.EmergencyVisits.Add(visit);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Urgency is TriageUrgency.Emergency or TriageUrgency.High)
        {
            await notificationService.NotifyAdminsAsync(
                "Emergência — novo atendimento",
                $"Paciente na urgência {request.Urgency}: {request.ChiefComplaint}",
                NotificationType.Alert,
                "EmergencyVisit",
                visit.Id,
                cancellationToken);
        }

        return (await GetVisitByIdAsync(visit.Id, cancellationToken))!;
    }

    public async Task<EmergencyVisitDto?> UpdateStatusAsync(
        Guid id, UpdateEmergencyVisitStatusRequest request, CancellationToken cancellationToken = default)
    {
        var visit = await dbContext.EmergencyVisits
            .FirstOrDefaultAsync(v => v.Id == id && v.IsActive, cancellationToken);

        if (visit is null)
        {
            return null;
        }

        var previousStatus = visit.Status;

        HospitalBusinessRules.ValidateCannotChangePatientAfterCareStarted(
            visit.StartedAt.HasValue || visit.Status == EmergencyVisitStatus.InCare,
            visit.PatientId,
            request.PatientId);

        if (request.PatientId.HasValue && request.PatientId.Value != visit.PatientId)
        {
            await PatientCareValidation.RequireEligibleForCareAsync(
                dbContext, request.PatientId.Value, encryption: null, validateSusCns: true, cancellationToken);
            visit.PatientId = request.PatientId.Value;
        }

        visit.Status = request.Status;
        visit.Notes = request.Notes?.Trim() ?? visit.Notes;
        visit.UpdatedAt = DateTime.UtcNow;

        if (request.ProfessionalId.HasValue)
        {
            visit.ProfessionalId = request.ProfessionalId;
        }

        if (request.Status == EmergencyVisitStatus.InCare)
        {
            HospitalBusinessRules.ValidateTriageBeforeMedicalCare(visit.Urgency, visit.AiTriageLogId);
        }

        if (request.Status == EmergencyVisitStatus.InCare && visit.StartedAt is null)
        {
            visit.StartedAt = DateTime.UtcNow;
        }

        if (request.Status is EmergencyVisitStatus.Discharged or EmergencyVisitStatus.Referred)
        {
            visit.DischargedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Status != previousStatus)
        {
            await clinicalStatusAuditLogger.LogStatusChangeAsync(
                "EmergencyVisit",
                visit.Id,
                "AlterarStatusAtendimentoPS",
                previousStatus.ToString(),
                request.Status.ToString(),
                cancellationToken: cancellationToken);
        }

        return await GetVisitByIdAsync(id, cancellationToken);
    }

    private async Task<EmergencyVisitDto?> GetVisitByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.EmergencyVisits
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new EmergencyVisitDto(
                v.Id,
                v.PatientId,
                v.Patient.FullName,
                v.ChiefComplaint,
                v.Urgency,
                v.Status,
                v.Professional != null ? v.Professional.FullName : null,
                v.ArrivedAt,
                v.StartedAt,
                v.DischargedAt,
                v.Notes))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
