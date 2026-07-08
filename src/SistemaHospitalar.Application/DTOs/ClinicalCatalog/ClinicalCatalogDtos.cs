using SistemaHospitalar.Application.DTOs.Laboratory;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.ClinicalCatalog;



public record ImagingProcedureDto(

    Guid Id,

    string Name,

    string? TussCode,

    ImagingModality Modality,

    string? BodyPart,

    string? Description,

    bool IsGeneral);



public record MedicationCatalogDto(

    Guid Id,

    string Name,

    string? ActiveIngredient,

    string? PharmaceuticalForm,

    string? Strength,

    string? DefaultDosage,

    string? Route,

    string? Notes,

    string? PackageInsert,

    bool IsGeneral,

    Guid? ProductId,

    decimal? StockAvailable);



public record SpecialtyClinicalCatalogDto(

    Guid? SpecialtyId,

    string? SpecialtyName,

    IReadOnlyList<LabExamCatalogDto> LabExams,

    IReadOnlyList<ImagingProcedureDto> ImagingProcedures,

    IReadOnlyList<MedicationCatalogDto> Medications);

public record Cid10CatalogItemDto(
    string Code,
    string Description,
    string? Category,
    string? ParentCode = null);

public record AdministrationRouteDto(
    string Code,
    string Name,
    string? Abbreviation);

public record PatientReferenceCatalogItemDto(
    string Code,
    string Name,
    PatientReferenceCatalogType CatalogType);


