using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ServiceUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Cnes { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

public class SusGuide : BaseEntity
{
    public string GuideNumber { get; set; } = string.Empty;
    public SusGuideType GuideType { get; set; }
    public SusGuideStatus Status { get; set; } = SusGuideStatus.Draft;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    public Guid? ServiceUnitId { get; set; }
    public ServiceUnit? ServiceUnit { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string? Cid10Code { get; set; }
    public string? SigtapProcedureCode { get; set; }
    public string? ProcedureDescription { get; set; }
    public string? Competence { get; set; }
    public string? AuthorizationNumber { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Notes { get; set; }
}
