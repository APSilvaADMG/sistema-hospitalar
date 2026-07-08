namespace SistemaHospitalar.Domain.Enums;

public enum HospitalizationRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Admitted = 4,
    Cancelled = 5
}

public enum HospitalizationRequestPriority
{
    Elective = 1,
    Urgent = 2,
    Emergency = 3
}
