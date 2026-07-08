using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class MedicationInsuranceMapping : BaseEntity
{
    public Guid PrescribedProductId { get; set; }
    public Product PrescribedProduct { get; set; } = null!;

    public Guid ReferenceProductId { get; set; }
    public Product ReferenceProduct { get; set; } = null!;

    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;
}
