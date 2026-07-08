using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Ambulance;

public record AmbulanceDto(
    Guid Id,
    string Code,
    string Plate,
    AmbulanceStatus Status,
    string? BaseLocation);

public record AmbulanceDispatchDto(
    Guid Id,
    Guid? AmbulanceId,
    string? AmbulanceCode,
    string PatientName,
    string PickupAddress,
    string Destination,
    AmbulanceDispatchStatus Status,
    DateTime RequestedAt,
    DateTime? DispatchedAt,
    DateTime? CompletedAt,
    string? Notes);

public record CreateAmbulanceDispatchRequest(
    string PatientName,
    string PickupAddress,
    string Destination,
    string? Notes);

public record AssignAmbulanceRequest(Guid AmbulanceId);

public record UpdateDispatchStatusRequest(AmbulanceDispatchStatus Status);
