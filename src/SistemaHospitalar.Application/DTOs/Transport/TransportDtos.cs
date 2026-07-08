using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Transport;

public record TransportAssetDto(
    Guid Id,
    string Code,
    string AssetTag,
    TransportAssetType AssetType,
    string Sector,
    TransportAssetStatus Status,
    string? TrackingCode,
    string? Notes);

public record TransportRequestDto(
    Guid Id,
    Guid? PatientId,
    Guid? HospitalizationId,
    string PatientName,
    TransportLocationType OriginType,
    string? OriginDetail,
    TransportLocationType DestinationType,
    string? DestinationDetail,
    TransportRequestStatus Status,
    TransportPriority Priority,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    Guid? TransportAssetId,
    string? TransportAssetCode,
    DateTime RequestedAt,
    DateTime? AcceptedAt,
    DateTime? ArrivedAtOriginAt,
    DateTime? DepartedAt,
    DateTime? ArrivedAtDestinationAt,
    DateTime? CompletedAt,
    string? Notes,
    string? RequestedBy,
    DateTime? SlaDeadlineAt,
    bool IsSlaViolated);

public record TransportDashboardDto(
    int TotalAssets,
    int AvailableAssets,
    int ActiveRequests,
    int QueuedRequests,
    int InTransitRequests,
    double? AvgAcceptMinutes,
    double? AvgCompleteMinutes,
    string? TopOriginSector,
    string? MostProductivePorter,
    IReadOnlyList<TransportRequestDto> LiveQueue,
    IReadOnlyList<TransportRequestDto> RecentCompleted);

public record TransportMetricsDto(
    double? AvgAcceptMinutes,
    double? AvgCompleteMinutes,
    double? AvgTransitMinutes,
    IReadOnlyList<TransportSectorDemandDto> SectorDemand,
    IReadOnlyList<TransportPorterProductivityDto> PorterProductivity);

public record TransportSectorDemandDto(
    TransportLocationType OriginType,
    int RequestCount);

public record TransportPorterProductivityDto(
    Guid EmployeeId,
    string EmployeeName,
    int CompletedCount,
    double? AvgCompleteMinutes);

public record CreateTransportAssetRequest(
    string Code,
    string AssetTag,
    TransportAssetType AssetType,
    string Sector,
    string? TrackingCode,
    string? Notes);

public record UpdateTransportAssetStatusRequest(TransportAssetStatus Status);

public record CreateTransportRequestRequest(
    Guid? PatientId,
    Guid? HospitalizationId,
    string PatientName,
    TransportLocationType OriginType,
    string? OriginDetail,
    TransportLocationType DestinationType,
    string? DestinationDetail,
    TransportPriority Priority,
    string? Notes);

public record AcceptTransportRequestRequest(
    Guid EmployeeId,
    Guid? TransportAssetId);

public record AdvanceTransportRequestRequest(TransportRequestStatus Status);

public record TransportPorterDto(
    Guid Id,
    string FullName,
    string? JobTitle);
