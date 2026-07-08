namespace SistemaHospitalar.Domain.Enums;

public enum LabOrderStatus
{
    Requested = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum LabItemStatus
{
    Pending = 1,
    Collected = 2,
    Processing = 3,
    Completed = 4,
    Cancelled = 5
}
