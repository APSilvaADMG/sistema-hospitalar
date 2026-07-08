using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Telemedicine;

public record TelemedicineAppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string ProfessionalName,
    string SpecialtyName,
    DateTime ScheduledAt,
    TelemedicineStatus Status,
    string? MeetingUrl,
    string ChiefComplaint,
    string? Notes,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public record CreateTelemedicineAppointmentRequest(
    Guid PatientId,
    Guid ProfessionalId,
    DateTime ScheduledAt,
    string ChiefComplaint,
    string? Notes);

public record UpdateTelemedicineStatusRequest(TelemedicineStatus Status);
