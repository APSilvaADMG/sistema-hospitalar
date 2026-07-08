namespace SistemaHospitalar.Domain.Enums;

public enum ConsultingRoomStatus
{
    Available = 1,
    Occupied = 2,
    Maintenance = 3
}

public enum HospitalityRoomStatus
{
    Available = 1,
    Occupied = 2,
    Cleaning = 3,
    Maintenance = 4
}

public enum HospitalityBookingStatus
{
    Reserved = 1,
    CheckedIn = 2,
    CheckedOut = 3,
    Cancelled = 4
}

public enum MedicalEquipmentStatus
{
    Operational = 1,
    Maintenance = 2,
    OutOfService = 3,
    CalibrationDue = 4
}

public enum MaintenanceWorkOrderStatus
{
    Open = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum SecurityIncidentType
{
    AccessDenied = 1,
    VisitorIssue = 2,
    AssetAlert = 3,
    Emergency = 4,
    Other = 5,
    /// <summary>Queda de paciente (HospitalRun incidents / NPSG).</summary>
    PatientFall = 6,
    /// <summary>Erro de medicação.</summary>
    MedicationError = 7,
    /// <summary>Evento adverso clínico.</summary>
    ClinicalAdverseEvent = 8,
    /// <summary>Quase-erro / near miss.</summary>
    NearMiss = 9
}

public enum ClinicalIncidentSeverity
{
    Low = 1,
    Moderate = 2,
    High = 3,
    Severe = 4
}

public enum SecurityIncidentStatus
{
    Open = 1,
    Investigating = 2,
    Resolved = 3
}

public enum VisitorLogStatus
{
    Inside = 1,
    Exited = 2
}
