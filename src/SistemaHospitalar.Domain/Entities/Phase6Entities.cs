using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class VitalSignRecord : BaseEntity
{
    public Guid HospitalizationId { get; set; }
    public Hospitalization Hospitalization { get; set; } = null!;

    public int HeartRate { get; set; }
    public int SystolicBp { get; set; }
    public int DiastolicBp { get; set; }
    public int SpO2 { get; set; }
    public decimal Temperature { get; set; }
    public int RespiratoryRate { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Guid? RecordedByProfessionalId { get; set; }
    public Professional? RecordedByProfessional { get; set; }
    public string? Notes { get; set; }
}

public class Ambulance : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public AmbulanceStatus Status { get; set; } = AmbulanceStatus.Available;
    public string? BaseLocation { get; set; }
}

public class AmbulanceDispatch : BaseEntity
{
    public Guid? AmbulanceId { get; set; }
    public Ambulance? Ambulance { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public AmbulanceDispatchStatus Status { get; set; } = AmbulanceDispatchStatus.Requested;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class ParkingZone : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int TotalSpots { get; set; }
    public decimal HourlyRate { get; set; }
    public string? Description { get; set; }
}

public class ParkingSession : BaseEntity
{
    public Guid ParkingZoneId { get; set; }
    public ParkingZone ParkingZone { get; set; } = null!;

    public string VehiclePlate { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExitedAt { get; set; }
    public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;
    public decimal? AmountCharged { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class DietOrder : BaseEntity
{
    public Guid HospitalizationId { get; set; }
    public Hospitalization Hospitalization { get; set; } = null!;

    public DietType DietType { get; set; }
    public MealPeriod MealPeriod { get; set; }
    public DietOrderStatus Status { get; set; } = DietOrderStatus.Pending;
    public DateOnly MealDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
