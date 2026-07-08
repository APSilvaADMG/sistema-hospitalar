namespace SistemaHospitalar.Domain.Enums;

public enum HospitalEventLogStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2,
    Partial = 3
}

public static class HospitalEventTypes
{
    public const string PatientDischarged = "patient.discharged";
    public const string PrescriptionSigned = "prescription.signed";
    public const string StockLow = "stock.low";
}
