namespace SistemaHospitalar.Domain.Enums;

public enum IntegrationMessageType
{
    Hl7Inbound = 1,
    Hl7Outbound = 2,
    FhirExport = 3,
    FhirImport = 4,
    CnsLookup = 5,
    CnesLookup = 6,
    RndsQuery = 7,
    SihExport = 8,
    SiaExport = 9,
    HorusDispense = 10,
    EsusExport = 11,
    FhirRndsBundle = 12,
    AiInsight = 13
}

public enum IntegrationMessageStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3
}

/// <summary>Protocolo de Manchester — classificação de risco na triagem hospitalar.</summary>
public enum TriageUrgency
{
    /// <summary>Verde — pouco urgente. Atendimento em até 120 minutos.</summary>
    Low = 1,
    /// <summary>Amarelo — urgente. Atendimento em até 60 minutos.</summary>
    Medium = 2,
    /// <summary>Laranja — muito urgente. Atendimento em até 10 minutos.</summary>
    High = 3,
    /// <summary>Vermelho — emergência. Atendimento imediato.</summary>
    Emergency = 4,
    /// <summary>Azul — não urgente. Atendimento em até 240 minutos ou encaminhamento à UBS.</summary>
    NonUrgent = 5
}
