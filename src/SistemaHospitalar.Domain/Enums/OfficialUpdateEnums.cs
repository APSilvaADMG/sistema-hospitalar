namespace SistemaHospitalar.Domain.Enums;

public enum OfficialSourceType
{
    Tuss = 1,
    Tiss = 2,
    Sigtap = 3,
    Ans = 4,
    SusTables = 5,
    Anvisa = 6,
    Brasindice = 7,
    Simpro = 8,
}

public enum OfficialVersionStatus
{
    NeverChecked = 0,
    UpToDate = 1,
    UpdateAvailable = 2,
    ManualDownloadRequired = 3,
    CheckFailed = 4,
    Importing = 5,
}

public enum IntegrationLogStatus
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Failed = 3,
}
