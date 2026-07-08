using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class BedTransfer : BaseEntity
{
    public Guid HospitalizationId { get; set; }
    public Hospitalization Hospitalization { get; set; } = null!;

    public Guid FromBedId { get; set; }
    public Bed FromBed { get; set; } = null!;

    public Guid ToBedId { get; set; }
    public Bed ToBed { get; set; } = null!;

    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
