using SistemaHospitalar.Application.DTOs.Events;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.CommandCenter;

public record CommandCenterDashboardDto(
    CommandCenterEmergencyDto Emergency,
    CommandCenterBedSummaryDto Beds,
    CommandCenterWarehouseDto Warehouse,
    int PendingRequisitions,
    CommandCenterSurgeryDto Surgeries,
    int OpenPendencies,
    int CriticalClinicalAlerts,
    IReadOnlyList<CommandCenterWardDto> Wards,
    CommandCenterOperationsDto Operations,
    IReadOnlyList<CommandCenterEmergencyQueueItemDto> EmergencyQueue,
    IReadOnlyList<CommandCenterTvCallDto> RecentTvCalls,
    IReadOnlyList<HospitalEventLogDto> RecentEvents,
    DateTime GeneratedAt);

public record CommandCenterEmergencyDto(
    int Waiting,
    int InCare,
    int Critical,
    double AverageWaitMinutes,
    int SlaViolations);

public record CommandCenterBedSummaryDto(
    int Total,
    int Occupied,
    int Available,
    int Cleaning,
    int Maintenance,
    int Reserved,
    decimal OccupancyRate);

public record CommandCenterWarehouseDto(
    int LowStockProducts,
    int ExpiringLots);

public record CommandCenterSurgeryDto(
    int Total,
    int Scheduled,
    int InProgress,
    int Completed,
    int Cancelled);

public record CommandCenterWardDto(
    Guid WardId,
    string WardName,
    int Total,
    int Occupied,
    int Available,
    int Cleaning,
    int Maintenance,
    int Reserved);

public record CommandCenterOperationsDto(
    int PendingCleaning,
    int PendingTransport,
    int ActiveAmbulanceDispatches);

public record CommandCenterEmergencyQueueItemDto(
    Guid Id,
    string PatientName,
    string ChiefComplaint,
    TriageUrgency Urgency,
    DateTime ArrivedAt,
    double WaitMinutes);

public record CommandCenterTvCallDto(
    string TicketNumber,
    string? PatientName,
    string Destination,
    DateTime CalledAt);

public record OperationsQueueSnapshotDto(
    IReadOnlyList<CommandCenterEmergencyQueueItemDto> Emergency,
    IReadOnlyList<CommandCenterTvCallDto> RecentTvCalls,
    DateTime GeneratedAt);
