using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class TissGuide : BaseEntity
{
    public string GuideNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public TissGuideType GuideType { get; set; }
    public TissGuideStatus Status { get; set; } = TissGuideStatus.Draft;
    public decimal TotalAmount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? AccountClosedAt { get; set; }
    public string? Notes { get; set; }
    public string? ClientRequestId { get; set; }

    public string? BeneficiaryCardNumber { get; set; }
    public string? BeneficiaryPlanName { get; set; }
    public string? BeneficiaryCns { get; set; }
    public string? BeneficiaryAccommodation { get; set; }
    public string? AuthorizationPassword { get; set; }
    public Guid? TissBatchId { get; set; }
    public TissBatch? TissBatch { get; set; }

    public string? Cid10Code { get; set; }
    public string? Cid10Secondary { get; set; }
    public string? ClinicalJustification { get; set; }
    public TissServiceCharacter? ServiceCharacter { get; set; }
    public TissAccidentIndicator? AccidentIndicator { get; set; }

    public Guid? RequestingProfessionalId { get; set; }
    public Professional? RequestingProfessional { get; set; }
    public string? RequestingProfessionalName { get; set; }
    public string? RequestingProfessionalCrm { get; set; }

    public Guid? ExecutingProfessionalId { get; set; }
    public Professional? ExecutingProfessional { get; set; }
    public string? ExecutingProfessionalName { get; set; }
    public string? ExecutingProfessionalCrm { get; set; }

    public DateTime? AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string? RequestedBedType { get; set; }

    public Guid? ParentGuideId { get; set; }
    public TissGuide? ParentGuide { get; set; }

    public TissProfessionalRole? ProfessionalRole { get; set; }
    public decimal? ParticipationPercent { get; set; }

    public Guid? SurgeryId { get; set; }
    public Surgery? Surgery { get; set; }

    public Guid? ServiceUnitId { get; set; }
    public ServiceUnit? ServiceUnit { get; set; }

    public ICollection<TissGuideItem> Items { get; set; } = [];
    public ICollection<TissGlosa> Glosas { get; set; } = [];
    public ICollection<TissGuideAnnex> Annexes { get; set; } = [];
}

public class TissGuideItem : BaseEntity
{
    public Guid TissGuideId { get; set; }
    public TissGuide TissGuide { get; set; } = null!;

    public string TussCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public TissPriceTableSource? PriceTableSource { get; set; }
    public string? Cid10Code { get; set; }
    public string? RelatedTussCode { get; set; }
    public bool IsAudited { get; set; }
}

public class TissGlosa : BaseEntity
{
    public Guid TissGuideId { get; set; }
    public TissGuide TissGuide { get; set; } = null!;

    public Guid? TissGuideItemId { get; set; }
    public TissGuideItem? TissGuideItem { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? AnsGlosaCode { get; set; }
    public decimal GlosaAmount { get; set; }
    public bool IsResolved { get; set; }
    public GlosaContestationStatus ContestationStatus { get; set; } = GlosaContestationStatus.None;
    public string? ContestationNotes { get; set; }
}
