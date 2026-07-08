using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class ConsultingRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public string? Building { get; set; }
    public ConsultingRoomStatus Status { get; set; } = ConsultingRoomStatus.Available;
    public Guid? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
}

public class ConsultingRoomSchedule : BaseEntity
{
    public Guid ConsultingRoomId { get; set; }
    public ConsultingRoom ConsultingRoom { get; set; } = null!;

    public Guid ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class HospitalityRoom : BaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public int Capacity { get; set; } = 2;
    public decimal DailyRate { get; set; }
    public HospitalityRoomStatus Status { get; set; } = HospitalityRoomStatus.Available;
}

public class HospitalityBooking : BaseEntity
{
    public Guid HospitalityRoomId { get; set; }
    public HospitalityRoom HospitalityRoom { get; set; } = null!;

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string GuestName { get; set; } = string.Empty;
    public string? GuestDocument { get; set; }
    public string? GuestPhone { get; set; }
    public HospitalityBookingStatus Status { get; set; } = HospitalityBookingStatus.Reserved;
    public DateOnly CheckInDate { get; set; }
    public DateOnly? CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public string? Notes { get; set; }
}

public class MedicalEquipment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Location { get; set; }
    public MedicalEquipmentStatus Status { get; set; } = MedicalEquipmentStatus.Operational;
    public DateOnly? LastMaintenanceDate { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
}

public class MaintenanceWorkOrder : BaseEntity
{
    public Guid MedicalEquipmentId { get; set; }
    public MedicalEquipment MedicalEquipment { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenanceWorkOrderStatus Status { get; set; } = MaintenanceWorkOrderStatus.Open;
    public DateTime? CompletedAt { get; set; }
    public string? TechnicianName { get; set; }
}

public class SecurityIncident : BaseEntity
{
    public SecurityIncidentType Type { get; set; }
    public SecurityIncidentStatus Status { get; set; } = SecurityIncidentStatus.Open;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ReportedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public ClinicalIncidentSeverity? Severity { get; set; }
}

public class VisitorLog : BaseEntity
{
    public string VisitorName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string? Destination { get; set; }
    public string? BadgeNumber { get; set; }
    public VisitorLogStatus Status { get; set; } = VisitorLogStatus.Inside;
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExitedAt { get; set; }
    public string? PhotoData { get; set; }
}
