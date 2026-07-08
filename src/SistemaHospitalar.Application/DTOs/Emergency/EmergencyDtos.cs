using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Emergency;

public record EmergencyVisitDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string ChiefComplaint,
    TriageUrgency Urgency,
    EmergencyVisitStatus Status,
    string? ProfessionalName,
    DateTime ArrivedAt,
    DateTime? StartedAt,
    DateTime? DischargedAt,
    string? Notes);

public record CreateEmergencyVisitRequest(
    Guid PatientId,
    string ChiefComplaint,
    TriageUrgency Urgency,
    Guid? AiTriageLogId,
    string? Notes);

public record UpdateEmergencyVisitStatusRequest(
    EmergencyVisitStatus Status,
    Guid? ProfessionalId,
    string? Notes,
    Guid? PatientId = null);
