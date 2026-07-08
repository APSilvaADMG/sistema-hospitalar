using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.ConsultingRooms;

public record ConsultingRoomDto(
    Guid Id,
    string Name,
    string? Floor,
    string? Building,
    ConsultingRoomStatus Status,
    string? SpecialtyName);

public record ConsultingRoomScheduleDto(
    Guid Id,
    Guid ConsultingRoomId,
    string RoomName,
    Guid ProfessionalId,
    string ProfessionalName,
    string SpecialtyName,
    DayOfWeek DayOfWeek,
    string StartTime,
    string EndTime);

public record CreateConsultingRoomRequest(string Name, string? Floor, string? Building, Guid? SpecialtyId);

public record CreateRoomScheduleRequest(
    Guid ConsultingRoomId,
    Guid ProfessionalId,
    DayOfWeek DayOfWeek,
    string StartTime,
    string EndTime);
