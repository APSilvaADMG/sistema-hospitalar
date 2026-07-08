using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string RecordNumber { get; set; } = string.Empty;

    public ICollection<MedicalRecordEntry> Entries { get; set; } = [];
}
