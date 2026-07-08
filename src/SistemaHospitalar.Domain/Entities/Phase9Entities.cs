using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ChemotherapySession : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string ProtocolName { get; set; } = string.Empty;
    public string DrugRegimen { get; set; } = string.Empty;
    public int CycleNumber { get; set; }
    public int TotalCycles { get; set; }
    public ChemotherapySessionStatus Status { get; set; } = ChemotherapySessionStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public DateTime? AdministeredAt { get; set; }
    public string? Notes { get; set; }
}

public class PhysiotherapySession : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string TherapistName { get; set; } = string.Empty;
    public PhysiotherapySessionType SessionType { get; set; }
    public PhysiotherapySessionStatus Status { get; set; } = PhysiotherapySessionStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public string? Goals { get; set; }
    public string? Notes { get; set; }
}

public class TelemedicineAppointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }
    public TelemedicineStatus Status { get; set; } = TelemedicineStatus.Scheduled;
    public string? MeetingUrl { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class InfectionSurveillance : BaseEntity
{
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string Location { get; set; } = string.Empty;
    public InfectionType InfectionType { get; set; }
    public string Organism { get; set; } = string.Empty;
    public string? Site { get; set; }
    public InfectionSurveillanceStatus Status { get; set; } = InfectionSurveillanceStatus.Suspected;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? ReportedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class IsolationPrecaution : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public IsolationPrecautionType PrecautionType { get; set; }
    public IsolationPrecautionStatus Status { get; set; } = IsolationPrecautionStatus.Active;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
