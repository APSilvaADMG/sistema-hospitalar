using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class InstrumentKit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InstrumentKitStatus Status { get; set; } = InstrumentKitStatus.Available;
    public DateOnly? SterilityExpiration { get; set; }
}

public class SterilizationCycle : BaseEntity
{
    public Guid InstrumentKitId { get; set; }
    public InstrumentKit InstrumentKit { get; set; } = null!;

    public SterilizationMethod Method { get; set; }
    public SterilizationCycleStatus Status { get; set; } = SterilizationCycleStatus.Pending;
    public string SterilizerName { get; set; } = string.Empty;
    public string? OperatorName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateOnly? ExpirationDate { get; set; }
}

public class BloodUnit : BaseEntity
{
    public string UnitCode { get; set; } = string.Empty;
    public BloodType BloodType { get; set; }
    public BloodComponent Component { get; set; }
    public int VolumeMl { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public BloodUnitStatus Status { get; set; } = BloodUnitStatus.Available;
}

public class TransfusionRequest : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public Guid RequestingProfessionalId { get; set; }
    public Professional RequestingProfessional { get; set; } = null!;

    public Guid? BloodUnitId { get; set; }
    public BloodUnit? BloodUnit { get; set; }

    public BloodType BloodTypeRequired { get; set; }
    public BloodComponent Component { get; set; }
    public int UnitsRequested { get; set; } = 1;
    public TransfusionRequestStatus Status { get; set; } = TransfusionRequestStatus.Requested;
    public string? Notes { get; set; }
    public DateTime? TransfusedAt { get; set; }
}

public class DialysisSession : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }

    public string MachineNumber { get; set; } = string.Empty;
    public DialysisSessionStatus Status { get; set; } = DialysisSessionStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? DryWeightKg { get; set; }
    public string? NurseName { get; set; }
    public string? Notes { get; set; }
}

public class LaundryBatch : BaseEntity
{
    public string BatchNumber { get; set; } = string.Empty;
    public LaundryOrigin Origin { get; set; }
    public string? OriginDetail { get; set; }
    public int ItemCount { get; set; }
    public decimal WeightKg { get; set; }
    public LaundryBatchStatus Status { get; set; } = LaundryBatchStatus.Collected;
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }
}
