using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tasks;

public record UserMissionDto(
    Guid Id,
    string Title,
    string Description,
    PendingItemType Type,
    PendingItemPriority Priority,
    string? LinkDestino,
    DateTime DataAbertura,
    DateTime? DataLimite,
    string? Setor,
    bool IsPendingItem);

public record UserMissionsDto(
    int Total,
    int HighPriority,
    IReadOnlyList<UserMissionDto> Missions);
