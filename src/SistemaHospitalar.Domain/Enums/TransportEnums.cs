namespace SistemaHospitalar.Domain.Enums;

public enum TransportAssetType
{
    Stretcher = 1,
    Wheelchair = 2,
    ElectricVehicle = 3,
    Other = 4
}

public enum TransportAssetStatus
{
    Available = 1,
    InUse = 2,
    Cleaning = 3,
    Maintenance = 4
}

public enum TransportLocationType
{
    Emergency = 1,
    Icu = 2,
    SurgeryCenter = 3,
    Hospitalization = 4,
    ImagingTomography = 5,
    ImagingXray = 6,
    Laboratory = 7,
    Discharge = 8,
    Other = 9
}

public enum TransportRequestStatus
{
    Queued = 1,
    Accepted = 2,
    InTransit = 3,
    Completed = 4,
    Cancelled = 5
}

public enum TransportPriority
{
    Normal = 1,
    Urgent = 2
}
