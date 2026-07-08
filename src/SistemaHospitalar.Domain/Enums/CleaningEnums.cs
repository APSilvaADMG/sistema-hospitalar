namespace SistemaHospitalar.Domain.Enums;

public enum CleaningType
{
    Terminal = 1,
    Concurrent = 2,
    Routine = 3
}

public enum CleaningRequestStatus
{
    Requested = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum CleaningTriggerReason
{
    Manual = 1,
    Discharge = 2,
    Transfer = 3,
    Routine = 4
}
