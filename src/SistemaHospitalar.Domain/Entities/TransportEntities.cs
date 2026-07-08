using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class TransportAsset : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public TransportAssetType AssetType { get; set; } = TransportAssetType.Stretcher;
    public string Sector { get; set; } = string.Empty;
    public TransportAssetStatus Status { get; set; } = TransportAssetStatus.Available;
    public string? TrackingCode { get; set; }
    public string? Notes { get; set; }
}

public class TransportRequest : BaseEntity
{
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public TransportLocationType OriginType { get; set; }
    public string? OriginDetail { get; set; }

    public TransportLocationType DestinationType { get; set; }
    public string? DestinationDetail { get; set; }

    public TransportRequestStatus Status { get; set; } = TransportRequestStatus.Queued;
    public TransportPriority Priority { get; set; } = TransportPriority.Normal;

    public Guid? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public Guid? TransportAssetId { get; set; }
    public TransportAsset? TransportAsset { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ArrivedAtOriginAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public DateTime? ArrivedAtDestinationAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }
    public string? RequestedBy { get; set; }

    public DateTime? SlaDeadlineAt { get; set; }
    public bool IsSlaViolated { get; set; }
}
