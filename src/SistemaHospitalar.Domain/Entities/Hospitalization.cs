using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Hospitalization : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid BedId { get; set; }
    public Bed Bed { get; set; } = null!;

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    public DateTime AdmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DischargedAt { get; set; }
    public HospitalizationStatus Status { get; set; } = HospitalizationStatus.Active;
    public string Reason { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }

    public Guid? AiTriageLogId { get; set; }
    public AiTriageLog? AiTriageLog { get; set; }

    public string? AihNumber { get; set; }
    public string? SusCompetence { get; set; }
    public string? PrimaryCid10Code { get; set; }
    public string? SecondaryCid10Code { get; set; }
    public string? PrimarySigtapProcedureCode { get; set; }
    public string? SecondarySigtapProcedureCode { get; set; }
    public SusHospitalizationCharacter? SusCharacter { get; set; }
    public SusHospitalizationModality? SusModality { get; set; }
    public string? CnesCode { get; set; }
    public string? SusAuthorizationNumber { get; set; }
    public DateTime? AihExportedAt { get; set; }
    public DateTime? BillingAccountClosedAt { get; set; }
}
