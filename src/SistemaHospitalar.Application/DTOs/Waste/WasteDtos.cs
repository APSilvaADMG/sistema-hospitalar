using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Waste;

public record WasteCollectionDto(
    Guid Id,
    string Code,
    WasteType WasteType,
    string SectorName,
    decimal QuantityKg,
    string ContainerCode,
    DateTime CollectedAt,
    string CollectedBy,
    WasteCollectionStatus Status,
    string? ManifestNumber,
    string? Notes);

public record CreateWasteCollectionRequest(
    WasteType WasteType,
    string SectorName,
    decimal QuantityKg,
    string ContainerCode,
    string CollectedBy,
    string? ManifestNumber,
    string? Notes);

public record UpdateWasteCollectionRequest(
    WasteCollectionStatus? Status,
    string? ManifestNumber,
    string? Notes);

public record WasteKpiDto(WasteType WasteType, int Count, decimal TotalKg);

public record WasteDashboardDto(
    int TotalCollections,
    decimal TotalKg,
    IReadOnlyList<WasteKpiDto> ByType,
    IReadOnlyList<WasteCollectionDto> Recent);
