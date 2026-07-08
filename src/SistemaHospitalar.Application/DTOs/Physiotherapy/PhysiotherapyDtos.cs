using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Physiotherapy;

public record PhysiotherapySessionDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? WardName,
    string TherapistName,
    PhysiotherapySessionType SessionType,
    PhysiotherapySessionStatus Status,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Goals,
    string? Notes);

public record CreatePhysiotherapySessionRequest(
    Guid PatientId,
    Guid? HospitalizationId,
    string TherapistName,
    PhysiotherapySessionType SessionType,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Goals,
    string? Notes);

public record UpdatePhysiotherapyStatusRequest(PhysiotherapySessionStatus Status);
