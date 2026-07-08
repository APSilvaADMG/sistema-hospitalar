using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class AccessZone : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public bool RequiresAuthorization { get; set; }
    public string? Description { get; set; }
}

public class AccessTurnstile : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? AccessZoneId { get; set; }
    public AccessZone? AccessZone { get; set; }
    public string? IntegrationVendor { get; set; }
    public bool IsEntry { get; set; } = true;
}

public class AccessCredential : BaseEntity
{
    public AccessPersonType PersonType { get; set; }
    public string HolderName { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? VisitorLogId { get; set; }
    public VisitorLog? VisitorLog { get; set; }
    public AccessCredentialType CredentialType { get; set; }
    public AccessCredentialStatus Status { get; set; } = AccessCredentialStatus.Active;
    public string Token { get; set; } = string.Empty;
    public Guid? AllowedZoneId { get; set; }
    public AccessZone? AllowedZone { get; set; }
    public TimeOnly? VisitStartTime { get; set; }
    public TimeOnly? VisitEndTime { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? MaxDailyUses { get; set; }
}

public class AccessControlRecord : BaseEntity
{
    public AccessPersonType PersonType { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? VisitorLogId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? AccessZoneId { get; set; }
    public AccessZone? AccessZone { get; set; }
    public Guid? TurnstileId { get; set; }
    public AccessTurnstile? Turnstile { get; set; }
    public AccessMethod Method { get; set; }
    public AccessDirection Direction { get; set; } = AccessDirection.Entry;
    public AccessValidationResult Result { get; set; }
    public string? Location { get; set; }
    public string? Details { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class FacialBiometricTemplate : BaseEntity
{
    public AccessPersonType PersonType { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }
    public string TemplateHash { get; set; } = string.Empty;
    public string? PhotoData { get; set; }
    public FacialBiometricStatus Status { get; set; } = FacialBiometricStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}

public class RegisteredVehicle : BaseEntity
{
    public string Plate { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Color { get; set; }
    public VehicleOwnerCategory OwnerCategory { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool ParkingExempt { get; set; }
}

public class LprReadEvent : BaseEntity
{
    public string Plate { get; set; } = string.Empty;
    public string CameraLocation { get; set; } = string.Empty;
    public AccessDirection Direction { get; set; }
    public bool GateOpened { get; set; }
    public Guid? RegisteredVehicleId { get; set; }
    public RegisteredVehicle? RegisteredVehicle { get; set; }
    public Guid? ParkingSessionId { get; set; }
    public ParkingSession? ParkingSession { get; set; }
    public string? OwnerName { get; set; }
    public VehicleOwnerCategory? OwnerCategory { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}

public class KioskTicket : BaseEntity
{
    public KioskTicketType TicketType { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? Sector { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public bool Called { get; set; }
    public DateTime? CalledAt { get; set; }
}
