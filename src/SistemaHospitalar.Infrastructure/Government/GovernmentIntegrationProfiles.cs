using SistemaHospitalar.Application.DTOs.Government;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Government;

public static class GovernmentIntegrationProfiles
{
    public static IReadOnlyList<GovIntegrationProfileDto> All { get; } =
    [
        new(GovIntegrationSystem.Cns, "CNS — Cartão Nacional de Saúde", "Cadastro e validação de pacientes via CADSUS/CNS", GovIntegrationPriority.Priority1, GovIntegrationCredentialStatus.MockActive, true, "https://servicos.saude.gov.br/cadsus", "Requer credenciamento DATASUS"),
        new(GovIntegrationSystem.Cnes, "CNES", "Estabelecimentos, profissionais, especialidades e equipamentos", GovIntegrationPriority.Priority1, GovIntegrationCredentialStatus.MockActive, true, "http://cnes.datasus.gov.br", "Consulta pública; API oficial requer credenciamento"),
        new(GovIntegrationSystem.Tiss, "TISS", "Troca de informações com operadoras de saúde", GovIntegrationPriority.Priority1, GovIntegrationCredentialStatus.MockActive, true, null, "Módulo já implementado com perfis por operadora"),
        new(GovIntegrationSystem.Tuss, "TUSS", "Tabela Unificada de Procedimentos e materiais", GovIntegrationPriority.Priority1, GovIntegrationCredentialStatus.ProductionReady, false, null, "Catálogo local importável"),
        new(GovIntegrationSystem.SihSus, "SIH-SUS", "AIH e faturamento de internação hospitalar", GovIntegrationPriority.Priority1, GovIntegrationCredentialStatus.MockActive, true, "https://sihd.datasus.gov.br", "Exportação AIH — credenciamento estadual"),
        new(GovIntegrationSystem.SiaSus, "SIA-SUS", "BPA e APAC — produção ambulatorial", GovIntegrationPriority.Priority1, GovIntegrationCredentialStatus.MockActive, true, "https://sia.datasus.gov.br", "Competência mensal — credenciamento municipal/estadual"),
        new(GovIntegrationSystem.Horus, "Hórus", "Assistência farmacêutica e dispensação SUS", GovIntegrationPriority.Priority2, GovIntegrationCredentialStatus.PendingCredential, true, "https://horus.saude.gov.br", "Credenciamento Ministério da Saúde"),
        new(GovIntegrationSystem.Rnds, "RNDS", "Rede Nacional de Dados em Saúde — FHIR", GovIntegrationPriority.Priority2, GovIntegrationCredentialStatus.PendingCredential, true, "https://rnds.saude.gov.br", "Certificado ICP-Brasil + credenciamento RNDS"),
        new(GovIntegrationSystem.EsusAps, "e-SUS APS", "Atenção primária — cadastros e produção ambulatorial", GovIntegrationPriority.Priority2, GovIntegrationCredentialStatus.PendingCredential, true, "https://esusaps.bridge.ufsc.br", "Para unidades com APS vinculada"),
        new(GovIntegrationSystem.ConecteSus, "Conecte SUS", "Carteira de vacinação e dados do cidadão", GovIntegrationPriority.Priority3, GovIntegrationCredentialStatus.NotConfigured, true, "https://conectesus.saude.gov.br", "Integração via gov.br / RNDS"),
    ];
}
