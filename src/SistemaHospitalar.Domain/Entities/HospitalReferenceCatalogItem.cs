using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

/// <summary>Item de catálogo de referência ERP hospitalar (tipos de usuário, setores, alas, etc.).</summary>
public class HospitalReferenceCatalogItem : BaseEntity
{
    public HospitalReferenceCatalogType CatalogType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentGroup { get; set; }
    public int DisplayOrder { get; set; }
    public int ContentRevision { get; set; } = 1;
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}
