using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class CleaningRequest : BaseEntity
{
    public Guid BedId { get; set; }
    public Bed Bed { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public CleaningType CleaningType { get; set; } = CleaningType.Terminal;
    public CleaningRequestStatus Status { get; set; } = CleaningRequestStatus.Requested;
    public CleaningTriggerReason TriggerReason { get; set; } = CleaningTriggerReason.Manual;

    public string? AssignedTeam { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ChecklistJson { get; set; }
    public string? Notes { get; set; }
}
