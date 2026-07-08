using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class HospitalizationRequest : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid RequestingProfessionalId { get; set; }
    public Professional RequestingProfessional { get; set; } = null!;

    public Guid? PreferredWardId { get; set; }
    public Ward? PreferredWard { get; set; }

    public WardCategory? PreferredWardCategory { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? Cid10Code { get; set; }
    public string? Notes { get; set; }

    public HospitalizationRequestPriority Priority { get; set; } = HospitalizationRequestPriority.Elective;
    public HospitalizationRequestStatus Status { get; set; } = HospitalizationRequestStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedByProfessionalId { get; set; }
    public Professional? ReviewedByProfessional { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public Guid? AiTriageLogId { get; set; }
    public AiTriageLog? AiTriageLog { get; set; }
}
