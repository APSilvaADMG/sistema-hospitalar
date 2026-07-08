namespace SistemaHospitalar.Domain.Enums;

public enum AmbulanceStatus
{
    Available = 1,
    Dispatched = 2,
    OnScene = 3,
    Transporting = 4,
    Maintenance = 5
}

public enum AmbulanceDispatchStatus
{
    Requested = 1,
    Dispatched = 2,
    OnScene = 3,
    Transporting = 4,
    Completed = 5,
    Cancelled = 6
}

public enum ParkingSessionStatus
{
    Active = 1,
    Completed = 2
}

public enum DietType
{
    Regular = 1,
    Soft = 2,
    Liquid = 3,
    Diabetic = 4,
    LowSodium = 5
}

public enum DietOrderStatus
{
    Pending = 1,
    InPreparation = 2,
    Delivered = 3,
    Cancelled = 4
}

public enum MealPeriod
{
    Breakfast = 1,
    Lunch = 2,
    Dinner = 3,
    Snack = 4
}
