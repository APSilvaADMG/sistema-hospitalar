using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class Specialty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CboCode { get; set; }

    public ICollection<Professional> Professionals { get; set; } = [];
}
