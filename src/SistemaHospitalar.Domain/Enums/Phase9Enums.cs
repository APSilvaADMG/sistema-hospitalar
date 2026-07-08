namespace SistemaHospitalar.Domain.Enums;

public enum ChemotherapySessionStatus
{
    Scheduled = 1,
    InPreparation = 2,
    Administered = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PhysiotherapySessionType
{
    Mobility = 1,
    Respiratory = 2,
    Neurological = 3,
    PostOperative = 4,
    Other = 5
}

public enum PhysiotherapySessionStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TelemedicineStatus
{
    Scheduled = 1,
    Waiting = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum InfectionType
{
    Urinary = 1,
    Respiratory = 2,
    SurgicalSite = 3,
    Bloodstream = 4,
    Other = 5
}

public enum InfectionSurveillanceStatus
{
    Suspected = 1,
    Confirmed = 2,
    Resolved = 3
}

public enum IsolationPrecautionType
{
    Contact = 1,
    Droplet = 2,
    Airborne = 3,
    Protective = 4
}

public enum IsolationPrecautionStatus
{
    Active = 1,
    Lifted = 2
}
