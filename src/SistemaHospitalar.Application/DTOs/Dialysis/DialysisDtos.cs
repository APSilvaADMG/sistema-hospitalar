using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Dialysis;

public record DialysisSessionDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string? WardName,
    string MachineNumber,
    DialysisSessionStatus Status,
    DateTime ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    decimal? DryWeightKg,
    string? NurseName,
    string? Notes);

public record CreateDialysisSessionRequest(
    Guid PatientId,
    Guid? HospitalizationId,
    string MachineNumber,
    DateTime ScheduledAt,
    decimal? DryWeightKg,
    string? NurseName,
    string? Notes);

public record UpdateDialysisSessionStatusRequest(DialysisSessionStatus Status);
