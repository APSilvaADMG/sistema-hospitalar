using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Cme;

public record InstrumentKitDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    InstrumentKitStatus Status,
    DateOnly? SterilityExpiration);

public record SterilizationCycleDto(
    Guid Id,
    Guid InstrumentKitId,
    string KitName,
    string KitCode,
    SterilizationMethod Method,
    SterilizationCycleStatus Status,
    string SterilizerName,
    string? OperatorName,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateOnly? ExpirationDate);

public record CreateInstrumentKitRequest(string Name, string Code, string? Description);

public record CreateSterilizationCycleRequest(
    Guid InstrumentKitId,
    SterilizationMethod Method,
    string SterilizerName,
    string? OperatorName);

public record CompleteSterilizationCycleRequest(DateOnly ExpirationDate);

public record RejectSterilizationCycleRequest(string? Reason);
