using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.HospitalCatalog;

public record HospitalReferenceCatalogItemDto(
    string Code,
    string Name,
    HospitalReferenceCatalogType CatalogType,
    string? ParentGroup,
    int DisplayOrder,
    string? Description,
    string? MetadataJson);

public record HospitalReferenceCatalogGroupDto(
    string? ParentGroup,
    int ItemCount);

public record HospitalReferenceCatalogSummaryDto(
    HospitalReferenceCatalogType CatalogType,
    string Label,
    int ItemCount,
    int GroupCount);

public record HospitalReferenceCatalogTypeInfoDto(
    HospitalReferenceCatalogType CatalogType,
    string Label,
    string Description);
