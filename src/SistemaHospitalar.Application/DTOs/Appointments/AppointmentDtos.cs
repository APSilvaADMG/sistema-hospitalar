using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Appointments;

public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid ProfessionalId,
    string ProfessionalName,
    string SpecialtyName,
    DateTime ScheduledAt,
    int DurationMinutes,
    AppointmentStatus Status,
    string? Reason,
    string? Room);

public record CreateAppointmentRequest(
    Guid PatientId,
    Guid ProfessionalId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Reason,
    string? Notes,
    string? Room,
    bool IgnoreEligibilityWarning = false);

public record CreateAppointmentResultDto(
    AppointmentDto Appointment,
    IReadOnlyList<string> Warnings);

public record UpdateAppointmentStatusRequest(AppointmentStatus Status, string? CancellationReason = null);

public record UpdateAppointmentRequest(
    Guid? ProfessionalId,
    DateTime? ScheduledAt,
    int? DurationMinutes,
    string? Reason,
    string? Room);
