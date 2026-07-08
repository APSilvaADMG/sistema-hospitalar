using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ImagingStudy : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid RequestingProfessionalId { get; set; }
    public Professional RequestingProfessional { get; set; } = null!;

    public Guid? ReportingProfessionalId { get; set; }
    public Professional? ReportingProfessional { get; set; }

    public ImagingModality Modality { get; set; }
    public string StudyDescription { get; set; } = string.Empty;
    public ImagingStudyStatus Status { get; set; } = ImagingStudyStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ReportContent { get; set; }
    public DateTime? ReportedAt { get; set; }
    public string? AccessionNumber { get; set; }
}
