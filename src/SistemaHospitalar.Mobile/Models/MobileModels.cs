using SQLite;

namespace SistemaHospitalar.Mobile.Models;

public class SyncMetadata
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class OutboxMutation
{
    [PrimaryKey]
    public Guid ClientMutationId { get; set; } = Guid.NewGuid();
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime ClientTimestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
}

public class CachedTransportRequest
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string OriginType { get; set; } = string.Empty;
    public string? OriginDetail { get; set; }
    public string DestinationType { get; set; } = string.Empty;
    public string? DestinationDetail { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedEmployeeName { get; set; }
    public string? TransportAssetCode { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Json { get; set; } = string.Empty;
}
