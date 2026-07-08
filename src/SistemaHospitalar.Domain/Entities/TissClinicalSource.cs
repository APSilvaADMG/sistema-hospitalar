using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

/// <summary>
/// Dados clínicos/faturamento capturados no fluxo assistencial, usados para gerar guias FUNI/TISS depois.
/// </summary>
public class TissClinicalSource : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HealthInsuranceId { get; set; }
    public HealthInsurance? HealthInsurance { get; set; }

    public ClinicalDocumentKind DocumentKind { get; set; } = ClinicalDocumentKind.TissGuide;

    public TissGuideType GuideType { get; set; }

    public string? ReportCode { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public Guid? ChemotherapySessionId { get; set; }
    public ChemotherapySession? ChemotherapySession { get; set; }

    public Guid? SurgeryId { get; set; }
    public Surgery? Surgery { get; set; }

    public Guid? LabOrderId { get; set; }
    public LabOrder? LabOrder { get; set; }

    public Guid? ImagingStudyId { get; set; }
    public ImagingStudy? ImagingStudy { get; set; }

    public string Label { get; set; } = string.Empty;
    public string FormDataJson { get; set; } = "{}";

    public Guid? GeneratedTissGuideId { get; set; }
    public TissGuide? GeneratedTissGuide { get; set; }
    public string? GeneratedArtifactJson { get; set; }
    public DateTime? GeneratedAt { get; set; }
}
