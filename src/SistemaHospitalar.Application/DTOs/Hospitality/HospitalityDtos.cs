using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Hospitality;

public record HospitalityRoomDto(
    Guid Id,
    string RoomNumber,
    string? Floor,
    int Capacity,
    decimal DailyRate,
    HospitalityRoomStatus Status);

public record HospitalityBookingDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    string GuestName,
    string? PatientName,
    HospitalityBookingStatus Status,
    DateOnly CheckInDate,
    DateOnly? CheckOutDate,
    decimal DailyRate,
    string? Notes);

public record CreateHospitalityBookingRequest(
    Guid RoomId,
    Guid? PatientId,
    string GuestName,
    string? GuestDocument,
    string? GuestPhone,
    DateOnly CheckInDate,
    DateOnly? CheckOutDate,
    string? Notes);
