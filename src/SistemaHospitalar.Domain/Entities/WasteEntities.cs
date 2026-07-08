using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class WasteCollection : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public WasteType WasteType { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public decimal QuantityKg { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public string CollectedBy { get; set; } = string.Empty;
    public WasteCollectionStatus Status { get; set; } = WasteCollectionStatus.Registered;
    public string? ManifestNumber { get; set; }
    public string? Notes { get; set; }
}
