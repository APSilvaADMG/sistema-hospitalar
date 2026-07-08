namespace SistemaHospitalar.Domain.Enums;

public enum WasteType
{
    Infectious = 1,
    Sharps = 2,
    Common = 3,
    Chemical = 4,
    Pharmaceutical = 5
}

public enum WasteCollectionStatus
{
    Registered = 1,
    Stored = 2,
    PickedUp = 3,
    Disposed = 4
}
