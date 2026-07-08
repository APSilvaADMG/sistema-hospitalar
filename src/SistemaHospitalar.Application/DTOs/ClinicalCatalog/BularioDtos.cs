namespace SistemaHospitalar.Application.DTOs.ClinicalCatalog;

public record BularioMedicationListItemDto(
    Guid Id,
    string Name,
    string? ActiveIngredient,
    string? Source,
    bool HasPackageInsert);

public record BularioSearchResultDto(
    IReadOnlyList<BularioMedicationListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int CatalogTotal,
    bool AnvisaAvailable,
    object? Anvisa = null);

public record BularioStatsDto(
    int CatalogTotal,
    int WithPackageInsert,
    bool AnvisaAvailable);
