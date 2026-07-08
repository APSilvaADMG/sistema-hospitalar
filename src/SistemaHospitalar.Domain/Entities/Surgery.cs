using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Surgery : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid OperatingRoomId { get; set; }
    public OperatingRoom OperatingRoom { get; set; } = null!;

    public Guid SurgeonId { get; set; }
    public Professional Surgeon { get; set; } = null!;

    public string ProcedureName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;
    public SurgeryStatus Status { get; set; } = SurgeryStatus.Scheduled;
    public string? Notes { get; set; }
    public bool ConsentConfirmed { get; set; }
    public bool OmsSignInCompleted { get; set; }
    public bool OmsTimeOutCompleted { get; set; }
    public bool OmsSignOutCompleted { get; set; }
}
