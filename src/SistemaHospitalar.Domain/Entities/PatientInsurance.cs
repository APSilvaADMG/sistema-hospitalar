using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class PatientInsurance : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public string CardNumber { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public string? CardHolderName { get; set; }
    public string? ProductCode { get; set; }
    public string? CnsNumber { get; set; }
    public string? AccommodationType { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsPrimary { get; set; }
}
