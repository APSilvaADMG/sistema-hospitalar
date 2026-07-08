using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Bed : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    public string BedNumber { get; set; } = string.Empty;
    public BedStatus Status { get; set; } = BedStatus.Available;
    public string? StatusReason { get; set; }
    public DateTime? BlockedUntil { get; set; }

    public ICollection<Hospitalization> Hospitalizations { get; set; } = [];
}
