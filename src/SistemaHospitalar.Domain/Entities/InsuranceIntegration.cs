using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class InsuranceEligibilityCheck : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public string CardNumber { get; set; } = string.Empty;
    public EligibilityStatus Status { get; set; }
    public string? PlanName { get; set; }
    public string? CoverageSummary { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? ResponseMessage { get; set; }
    public string? RawResponseJson { get; set; }
}

public class InsuranceAuthorization : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public InsuranceAuthorizationType AuthorizationType { get; set; }
    public InsuranceAuthorizationStatus Status { get; set; } = InsuranceAuthorizationStatus.Requested;
    public string AuthorizationNumber { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? ProcedureSummary { get; set; }
    public Guid? TissGuideId { get; set; }
    public TissGuide? TissGuide { get; set; }
    public string? Notes { get; set; }
}

public class TissBatch : BaseEntity
{
    public string BatchNumber { get; set; } = string.Empty;
    public Guid HealthInsuranceId { get; set; }
    public HealthInsurance HealthInsurance { get; set; } = null!;

    public string Competence { get; set; } = string.Empty;
    public TissBatchStatus Status { get; set; } = TissBatchStatus.Draft;
    public string? ProtocolNumber { get; set; }
    public DateTime? SentAt { get; set; }
    public string? XmlContent { get; set; }
    public decimal TotalAmount { get; set; }
    public int GuideCount { get; set; }

    public ICollection<TissGuide> Guides { get; set; } = [];
}
