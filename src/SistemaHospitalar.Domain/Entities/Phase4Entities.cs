using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class IntegrationMessage : BaseEntity
{
    public IntegrationMessageType Type { get; set; }
    public IntegrationMessageStatus Status { get; set; } = IntegrationMessageStatus.Pending;
    public string Source { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
}

public class Cid10Catalog : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public string? Category { get; set; }
    public string? Keywords { get; set; }
}

public class AiTriageLog : BaseEntity
{
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? UserId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public TriageUrgency Urgency { get; set; }
    public string RecommendedSpecialty { get; set; } = string.Empty;
    public string? SuggestedCid10 { get; set; }
    public string? SuggestedCid10Description { get; set; }
    public string? Notes { get; set; }
}
