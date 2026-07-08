namespace SistemaHospitalar.Domain.Enums;

public enum TussTableType
{
    Procedure = 1,
    Material = 2,
    Medication = 3,
    Daily = 4,
    Fee = 5,
    Package = 6
}

public enum TissAnnexType
{
    Chemotherapy = 1,
    Radiotherapy = 2,
    Opme = 3,
    SpecialRequest = 4
}

public enum TissDemonstrativoStatus
{
    Imported = 1,
    Processed = 2,
    PartiallyProcessed = 3,
    Error = 4
}

public enum OperatorTransactionType
{
    Eligibility = 1,
    Authorization = 2,
    BatchSend = 3,
    DemonstrativoFetch = 4
}

public enum OperatorTransactionStatus
{
    Success = 1,
    Failure = 2,
    Pending = 3
}
