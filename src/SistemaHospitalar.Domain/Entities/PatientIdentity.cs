using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

/// <summary>Vínculo entre identificador físico (pulseira, etiqueta) e prontuário digital.</summary>
public class PatientIdentity : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public PatientIdentityType IdentityType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? LabelContext { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public Guid? IssuedByUserId { get; set; }
}
