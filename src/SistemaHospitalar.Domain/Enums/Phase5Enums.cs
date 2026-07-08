namespace SistemaHospitalar.Domain.Enums;

public enum EmergencyVisitStatus
{
    Waiting = 1,
    InCare = 2,
    Discharged = 3,
    Referred = 4
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}

public enum PurchaseSector
{
    Pharmacy = 1,
    Laboratory = 2,
    Imaging = 3,
    SurgeryCenter = 4,
    Icu = 5,
    Emergency = 6,
    Nutrition = 7,
    Laundry = 8,
    ClinicalEngineering = 9,
    InfectionControl = 10,
    Hospitality = 11,
    Nursing = 12,
    Administration = 13
}

public enum PurchasePriority
{
    Normal = 1,
    Urgent = 2,
    Critical = 3
}

public enum NotificationType
{
    Info = 1,
    Warning = 2,
    Alert = 3
}
