using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Telemedicine;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class TelemedicineService(AppDbContext dbContext, HospitalEventPublisher eventPublisher) : ITelemedicineService
{
    public async Task<IReadOnlyList<TelemedicineAppointmentDto>> GetAppointmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.TelemedicineAppointments
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => new TelemedicineAppointmentDto(
                a.Id,
                a.PatientId,
                a.Patient.FullName,
                a.Professional.FullName,
                a.Professional.Specialty.Name,
                a.ScheduledAt,
                a.Status,
                a.MeetingUrl,
                a.ChiefComplaint,
                a.Notes,
                a.StartedAt,
                a.CompletedAt))
            .ToListAsync(cancellationToken);

        return rows.Select(a => a with
        {
            MeetingUrl = GoogleMeetUrlBuilder.Resolve(a.Id, a.MeetingUrl)
        }).ToList();
    }

    public async Task<TelemedicineAppointmentDto> CreateAppointmentAsync(
        CreateTelemedicineAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var appointment = new TelemedicineAppointment
        {
            PatientId = request.PatientId,
            ProfessionalId = request.ProfessionalId,
            ScheduledAt = request.ScheduledAt,
            ChiefComplaint = request.ChiefComplaint.Trim(),
            Notes = request.Notes?.Trim(),
        };

        dbContext.TelemedicineAppointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        appointment.MeetingUrl = GoogleMeetUrlBuilder.FromAppointmentId(appointment.Id);
        appointment.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("telemedicine.appointment.scheduled", new
        {
            appointment.Id,
            request.PatientId,
            request.ProfessionalId,
            appointment.MeetingUrl
        }, cancellationToken);

        return (await GetAppointmentsAsync(cancellationToken)).First(a => a.Id == appointment.Id);
    }

    public async Task<TelemedicineAppointmentDto?> UpdateAppointmentStatusAsync(
        Guid id, UpdateTelemedicineStatusRequest request, CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.TelemedicineAppointments
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        appointment.Status = request.Status;
        appointment.UpdatedAt = DateTime.UtcNow;

        if (!GoogleMeetUrlBuilder.IsValidMeetUrl(appointment.MeetingUrl))
        {
            appointment.MeetingUrl = GoogleMeetUrlBuilder.FromAppointmentId(appointment.Id);
        }

        if (request.Status == TelemedicineStatus.InProgress && appointment.StartedAt is null)
        {
            appointment.StartedAt = DateTime.UtcNow;
        }

        if (request.Status == TelemedicineStatus.Completed)
        {
            appointment.CompletedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAppointmentsAsync(cancellationToken)).FirstOrDefault(a => a.Id == id);
    }
}
