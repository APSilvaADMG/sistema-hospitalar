using SistemaHospitalar.Domain.Common;

namespace SistemaHospitalar.Domain.Entities;

public class SyncMutation : BaseEntity
{
    public Guid ClientMutationId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public Guid? UserId { get; set; }
    public DateTime ClientTimestamp { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
