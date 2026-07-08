using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Ward : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Floor { get; set; }
    public string? Description { get; set; }
    public WardCoverageModality CoverageModality { get; set; } = WardCoverageModality.Mixed;
    public WardCategory Category { get; set; } = WardCategory.Enfermaria;

    public ICollection<Bed> Beds { get; set; } = [];
}
