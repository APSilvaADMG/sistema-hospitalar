using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class HospitalEventLog : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime? ProcessedAt { get; set; }
    public HospitalEventLogStatus Status { get; set; } = HospitalEventLogStatus.Pending;
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? ErrorMessage { get; set; }
}
