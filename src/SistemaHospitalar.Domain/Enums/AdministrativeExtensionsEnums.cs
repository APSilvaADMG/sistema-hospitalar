namespace SistemaHospitalar.Domain.Enums;

public enum TpaClaimStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Denied = 4,
    Paid = 5
}

public enum PayrollRunStatus
{
    Draft = 1,
    Generated = 2,
    Approved = 3,
    Paid = 4
}

public enum PayrollLineType
{
    Earning = 1,
    Discount = 2
}

public enum PharmacyBillingPayerType
{
    Private = 1,
    Insurance = 2
}
