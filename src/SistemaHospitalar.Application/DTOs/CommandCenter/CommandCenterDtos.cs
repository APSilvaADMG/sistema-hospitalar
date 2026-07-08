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
    int Cleaning);
