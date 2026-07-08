namespace SistemaHospitalar.Domain.Enums;

public enum EligibilityStatus
{
    Eligible = 1,
    Ineligible = 2,
    Pending = 3,
    Error = 4
}

public enum InsuranceAuthorizationType
{
    Consultation = 1,
    SpSadt = 2,
    Hospitalization = 3,
    Opme = 4,
    Extension = 5
}

public enum InsuranceAuthorizationStatus
{
    Requested = 1,
    Approved = 2,
    Denied = 3,
    Partial = 4,
    Expired = 5,
    Cancelled = 6
}

public enum TissBatchStatus
{
    Draft = 1,
    Generated = 2,
    Sent = 3,
    Processed = 4,
    Rejected = 5
}

public enum GlosaContestationStatus
{
    None = 0,
    Submitted = 1,
    Accepted = 2,
    Rejected = 3
}
