using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.PatientPortal;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PatientPortalService(AppDbContext dbContext) : IPatientPortalService
{
    public async Task<PatientPortalDashboardDto?> GetDashboardAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId && a.ScheduledAt >= now && a.IsActive
                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Completed)
            .OrderBy(a => a.ScheduledAt)
            .Take(5)
            .Select(a => new PatientAppointmentDto(
                a.Id, a.ScheduledAt, a.Professional.FullName, a.Professional.Specialty.Name, (int)a.Status))
            .ToListAsync(cancellationToken);

        var labResults = await dbContext.LabOrderItems
            .AsNoTracking()
            .Where(i => i.LabOrder.PatientId == patientId && i.Result != null)
            .OrderByDescending(i => i.Result!.ReleasedAt)
            .Take(5)
            .Select(i => new PatientLabResultDto(
                i.LabExamCatalog.Name,
                i.Result!.Value,
                i.Result.ReferenceRange,
                i.Result.IsAbnormal,
                i.Result.ReleasedAt))
            .ToListAsync(cancellationToken);

        return new PatientPortalDashboardDto(
            patient.FullName,
            patient.MedicalRecord?.RecordNumber,
            appointments,
            labResults);
    }

    public async Task<PatientMedicalRecordDto?> GetMedicalRecordAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.MedicalRecords
            .AsNoTracking()
            .Where(m => m.PatientId == patientId)
            .Select(m => new PatientMedicalRecordDto(
                m.RecordNumber,
                m.Entries
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(20)
                    .Select(e => new PatientRecordEntryDto(
                        e.EntryType.ToString(),
                        e.Content,
                        e.Professional != null ? e.Professional.FullName : null,
                        e.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
