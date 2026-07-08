namespace SistemaHospitalar.Domain.Enums;

/// <summary>
/// Tipos de guia e documentos do Padrão TISS (ANS). Valores 1–7 mantidos por compatibilidade.
/// </summary>
public enum TissGuideType
{
    Consultation = 1,
    SpSadt = 2,
    /// <summary>Faturamento de internação (legado). Preferir DischargeSummary para alta.</summary>
    Hospitalization = 3,
    DischargeSummary = 4,
    IndividualFees = 5,
    HospitalizationRequest = 6,
    OtherExpenses = 7,
    PresenceProof = 8,
    ExtensionRequest = 9,
    GlosaAppeal = 10,
    PaymentStatement = 11,
    DentalTreatment = 12,
    DentalInitialAnnex = 13,
    DentalPaymentStatement = 14,
    DentalGlosaAppeal = 15,
    OpmeAnnex = 16,
    ChemotherapyAnnex = 17,
    RadiotherapyAnnex = 18,
    MonitoringReport = 19,
}

public enum TissGuideStatus
{
    Draft = 1,
    Sent = 2,
    Paid = 3,
    Glosa = 4,
    Cancelled = 5
}
