using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class InventoryLookupItem : BaseEntity
{
    public InventoryLookupType Type { get; set; }
    public string Name { get; set; } = string.Empty;
}
