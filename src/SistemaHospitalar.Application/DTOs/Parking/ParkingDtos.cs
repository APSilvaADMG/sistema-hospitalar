using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Parking;

public record ParkingZoneDto(
    Guid Id,
    string Name,
    int TotalSpots,
    int OccupiedSpots,
    decimal HourlyRate,
    string? Description);

public record ParkingSessionDto(
    Guid Id,
    Guid ZoneId,
    string ZoneName,
    string VehiclePlate,
    string? PatientName,
    DateTime EnteredAt,
    DateTime? ExitedAt,
    ParkingSessionStatus Status,
    decimal? AmountCharged,
    bool IsPaid,
    DateTime? PaidAt,
    decimal? EstimatedAmount,
    string QrPayload);

public record CheckInParkingRequest(
    Guid ZoneId,
    string VehiclePlate,
    Guid? PatientId);

public record CheckOutParkingRequest(Guid SessionId);

public record PayParkingRequest(Guid SessionId);

public record ParkingGateExitRequest(string QrPayload);

public record ParkingGateExitResultDto(
    bool Allowed,
    string Message,
    ParkingSessionDto? Session);
