namespace SistemaHospitalar.Domain.Enums;

public enum GovIntegrationSystem
{
    Cns = 1,
    Cnes = 2,
    Rnds = 3,
    ConecteSus = 4,
    Horus = 5,
    EsusAps = 6,
    SihSus = 7,
    SiaSus = 8,
    Tiss = 9,
    Tuss = 10
}

public enum GovIntegrationPriority
{
    Priority1 = 1,
    Priority2 = 2,
    Priority3 = 3
}

public enum GovIntegrationCredentialStatus
{
    NotConfigured = 1,
    MockActive = 2,
    PendingCredential = 3,
    ProductionReady = 4
}

public enum SiaDocumentType
{
    Bpa = 1,
    Apac = 2
}
