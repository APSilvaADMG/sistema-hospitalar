namespace SistemaHospitalar.Domain.Enums;

public enum AccessPersonType
{
    Patient = 1,
    Companion = 2,
    Employee = 3,
    Visitor = 4,
    Contractor = 5,
    Doctor = 6,
    Nurse = 7
}

public enum AccessMethod
{
    Facial = 1,
    QrCode = 2,
    Rfid = 3,
    Password = 4,
    Biometric = 5,
    PlateLpr = 6
}

public enum AccessDirection
{
    Entry = 1,
    Exit = 2
}

public enum AccessValidationResult
{
    Granted = 1,
    Denied = 2,
    Expired = 3,
    WrongZone = 4,
    MaxCompanions = 5,
    NoAppointment = 6,
    OutsideHours = 7
}

public enum AccessCredentialType
{
    QrCode = 1,
    Rfid = 2,
    FacialLinked = 3
}

public enum AccessCredentialStatus
{
    Active = 1,
    Revoked = 2,
    Expired = 3
}

public enum FacialBiometricStatus
{
    Active = 1,
    PendingReview = 2,
    Revoked = 3
}

public enum VehicleOwnerCategory
{
    Patient = 1,
    Doctor = 2,
    Employee = 3,
    Visitor = 4,
    Contractor = 5
}

public enum KioskTicketType
{
    Consultation = 1,
    Exam = 2,
    Hospitalization = 3,
    Emergency = 4,
    Laboratory = 5
}

public enum AccessIntegrationCategory
{
    Turnstile = 1,
    FacialRecognition = 2,
    Parking = 3
}
