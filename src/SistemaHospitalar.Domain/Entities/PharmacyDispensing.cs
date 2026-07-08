using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class PharmacyDispensing : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    public decimal ReversedQuantity { get; set; }

    public ICollection<PharmacyDispensingReversal> Reversals { get; set; } = [];
}
