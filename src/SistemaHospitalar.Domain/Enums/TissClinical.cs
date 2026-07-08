namespace SistemaHospitalar.Domain.Enums;

public enum TissServiceCharacter
{
    Elective = 1,
    Urgent = 2,
    Emergency = 3
}

public enum TissAccidentIndicator
{
    NotApplicable = 0,
    WorkAccident = 1,
    TrafficAccident = 2,
    OtherAccidents = 3
}

public enum TissProfessionalRole
{
    Surgeon = 1,
    FirstAssistant = 2,
    SecondAssistant = 3,
    Anesthesiologist = 4,
    Instrumentator = 5
}

public enum TissPriceTableSource
{
    Tuss = 1,
    Cbhpm = 2,
    Brasindice = 3,
    Simpro = 4,
    Manual = 5
}
