using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Laundry;

public record LaundryBatchDto(
    Guid Id,
    string BatchNumber,
    LaundryOrigin Origin,
    string? OriginDetail,
    int ItemCount,
    decimal WeightKg,
    LaundryBatchStatus Status,
    DateTime CollectedAt,
    DateTime? DeliveredAt,
    string? Notes);

public record CreateLaundryBatchRequest(
    LaundryOrigin Origin,
    string? OriginDetail,
    int ItemCount,
    decimal WeightKg,
    string? Notes);

public record UpdateLaundryBatchStatusRequest(LaundryBatchStatus Status);
