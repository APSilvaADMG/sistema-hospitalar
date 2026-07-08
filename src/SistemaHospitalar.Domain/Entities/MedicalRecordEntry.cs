using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class MedicalRecordEntry : BaseEntity
{
    public Guid MedicalRecordId { get; set; }
    public MedicalRecord MedicalRecord { get; set; } = null!;

    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public MedicalRecordEntryType EntryType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Cid10Code { get; set; }

    /// <summary>Idempotency key for offline sync / mobile replay.</summary>
    public string? ClientRequestId { get; set; }

    public bool IsSigned { get; set; }
    public DateTime? SignedAt { get; set; }
    public Guid? SignedByProfessionalId { get; set; }
    public Professional? SignedByProfessional { get; set; }
    public string? SignatureImage { get; set; }
    public string? SignatureHash { get; set; }
}
