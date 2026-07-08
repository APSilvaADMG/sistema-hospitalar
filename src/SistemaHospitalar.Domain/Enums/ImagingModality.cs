namespace SistemaHospitalar.Domain.Enums;

public enum ImagingModality
{
    XRay = 1,
    CT = 2,
    MRI = 3,
    Ultrasound = 4,
    Mammography = 5
}

public enum ImagingStudyStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
