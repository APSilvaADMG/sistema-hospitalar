using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class LabExamCatalogSpecialty
{
    public Guid LabExamCatalogId { get; set; }
    public LabExamCatalog LabExamCatalog { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
}

public class ImagingProcedureCatalog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TussCode { get; set; }
    public ImagingModality Modality { get; set; }
    public string? BodyPart { get; set; }
    public string? Description { get; set; }
    public bool IsGeneral { get; set; }

    public ICollection<ImagingProcedureSpecialty> SpecialtyLinks { get; set; } = [];
}

public class ImagingProcedureSpecialty
{
    public Guid ImagingProcedureCatalogId { get; set; }
    public ImagingProcedureCatalog ImagingProcedureCatalog { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
}

public class MedicationCatalog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ActiveIngredient { get; set; }
    public string? PharmaceuticalForm { get; set; }
    public string? Strength { get; set; }
    public string? DefaultDosage { get; set; }
    public string? Route { get; set; }
    public string? Notes { get; set; }
    public string? PackageInsert { get; set; }
    /// <summary>Slug do Consulta Remédios (ex.: amaryl) para bulas importadas.</summary>
    public string? ExternalBulaSlug { get; set; }
    public bool IsGeneral { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public ICollection<MedicationCatalogSpecialty> SpecialtyLinks { get; set; } = [];
}

public class MedicationCatalogSpecialty
{
    public Guid MedicationCatalogId { get; set; }
    public MedicationCatalog MedicationCatalog { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
}
