using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Surgery;

public record OperatingRoomDto(
    Guid Id,
    string Name,
    OperatingRoomStatus Status,
    string? Location);

public record SurgeryDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid OperatingRoomId,
    string OperatingRoomName,
    Guid SurgeonId,
    string SurgeonName,
    string ProcedureName,
    DateTime ScheduledAt,
    int EstimatedDurationMinutes,
    SurgeryStatus Status,
    string? Notes,
    bool ConsentConfirmed,
    bool OmsSignInCompleted,
    bool OmsTimeOutCompleted,
    bool OmsSignOutCompleted);

public record UpdateSurgerySafetyChecklistRequest(
    bool? ConsentConfirmed,
    bool? OmsSignInCompleted,
    bool? OmsTimeOutCompleted,
    bool? OmsSignOutCompleted);

public record CreateSurgeryRequest(
    Guid PatientId,
    Guid OperatingRoomId,
    Guid SurgeonId,
    string ProcedureName,
    DateTime ScheduledAt,
    int EstimatedDurationMinutes,
    string? Notes);

public record UpdateSurgeryStatusRequest(SurgeryStatus Status);
