namespace SistemaHospitalar.Domain.Enums;

public enum InstrumentKitStatus
{
    Available = 1,
    InSterilization = 2,
    Sterile = 3,
    Expired = 4,
    InUse = 5
}

public enum SterilizationMethod
{
    Steam = 1,
    Eto = 2,
    Plasma = 3
}

public enum SterilizationCycleStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

public enum BloodType
{
    APositive = 1,
    ANegative = 2,
    BPositive = 3,
    BNegative = 4,
    ABPositive = 5,
    ABNegative = 6,
    OPositive = 7,
    ONegative = 8
}

public enum BloodComponent
{
    WholeBlood = 1,
    PackedRedCells = 2,
    Platelets = 3,
    Plasma = 4
}

public enum BloodUnitStatus
{
    Available = 1,
    Reserved = 2,
    Transfused = 3,
    Discarded = 4,
    Expired = 5
}

public enum TransfusionRequestStatus
{
    Requested = 1,
    Matched = 2,
    Transfused = 3,
    Cancelled = 4
}

public enum DialysisSessionStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum LaundryBatchStatus
{
    Collected = 1,
    Washing = 2,
    Drying = 3,
    Delivered = 4
}

public enum LaundryOrigin
{
    Ward = 1,
    Icu = 2,
    Surgery = 3,
    Emergency = 4,
    Other = 5
}
