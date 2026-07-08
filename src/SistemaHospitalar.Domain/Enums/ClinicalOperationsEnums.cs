namespace SistemaHospitalar.Domain.Enums;

public enum VaccineScheduleType
{
    Child = 1,
    Pregnant = 2,
    NonPregnantAdult = 3,
}

public enum WardStockMovementType
{
    TransferIn = 1,
    TransferOut = 2,
    Dispense = 3,
    Adjustment = 4,
}

public enum FinancialCashSessionStatus
{
    Open = 1,
    Closed = 2,
}

public enum EpidemicDiseaseClass
{
  /// <summary>Doença de notificação (ND).</summary>
    Notifiable = 1,
  /// <summary>Condição crônica (NC).</summary>
    Chronic = 2,
  /// <summary>Complicação materna/perinatal (MP).</summary>
    MaternalPerinatal = 3,
  /// <summary>Outras condições (OC).</summary>
    OtherCondition = 4,
  /// <summary>Outras (AO).</summary>
    Other = 5,
}
