using SistemaHospitalar.Application.DTOs.Transport;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Hotelaria;

public record CleaningChecklistItemDto(string Id, string Label, bool Done);

public record CleaningRequestDto(
    Guid Id,
    Guid BedId,
    string WardName,
    string BedNumber,
    Guid? HospitalizationId,
    CleaningType CleaningType,
    CleaningRequestStatus Status,
    CleaningTriggerReason TriggerReason,
    string? AssignedTeam,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    DateTime RequestedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<CleaningChecklistItemDto> Checklist,
    string? Notes);

public record CreateCleaningRequestRequest(
    Guid BedId,
    CleaningType CleaningType,
    string? Notes);

public record StartCleaningRequestRequest(
    string? AssignedTeam,
    Guid? AssignedEmployeeId);

public record UpdateCleaningChecklistRequest(
    IReadOnlyList<CleaningChecklistItemDto> Checklist);

public record CompleteCleaningRequestRequest(string? Notes);

public record HotelariaNocDto(
    int TotalBeds,
    int AvailableBeds,
    int OccupiedBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    double OccupancyRate,
    int PendingAdmissions,
    int PendingCleanings,
    int ActiveTransports,
    double? AvgCleaningMinutes,
    double? AvgTransportAcceptMinutes,
    IReadOnlyList<CleaningRequestDto> PendingCleaningQueue,
    IReadOnlyList<TransportRequestDto> ActiveTransportQueue,
    IReadOnlyList<HotelariaBedSnapshotDto> BedMap);

public record HotelariaBedSnapshotDto(
    Guid BedId,
    string WardName,
    string BedNumber,
    BedStatus Status,
    string? OccupantName);
