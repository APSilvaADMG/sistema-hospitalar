using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Serialization;

public static partial class PortugueseEnumRegistry
{
    private static void RegisterIntegrationMaps(Dictionary<Type, EnumMap> maps)
    {
        Register(maps,
            (IntegrationMessageType.Hl7Inbound, "HL7 entrada"),
            (IntegrationMessageType.Hl7Outbound, "HL7 saída"),
            (IntegrationMessageType.FhirExport, "Exportação FHIR"),
            (IntegrationMessageType.FhirImport, "Importação FHIR"),
            (IntegrationMessageType.CnsLookup, "Consulta CNS"),
            (IntegrationMessageType.CnesLookup, "Consulta CNES"),
            (IntegrationMessageType.RndsQuery, "Consulta RNDS"),
            (IntegrationMessageType.SihExport, "Exportação SIH"),
            (IntegrationMessageType.SiaExport, "Exportação SIA"),
            (IntegrationMessageType.HorusDispense, "Dispensação Hórus"),
            (IntegrationMessageType.EsusExport, "Exportação e-SUS"),
            (IntegrationMessageType.FhirRndsBundle, "Bundle FHIR RNDS"),
            (IntegrationMessageType.AiInsight, "Insight IA"));

        Register(maps,
            (IntegrationMessageStatus.Pending, "Pendente"),
            (IntegrationMessageStatus.Processed, "Processado"),
            (IntegrationMessageStatus.Failed, "Falhou"));

        Register(maps,
            (OfficialSourceType.Tuss, "TUSS"),
            (OfficialSourceType.Tiss, "TISS"),
            (OfficialSourceType.Sigtap, "SIGTAP"),
            (OfficialSourceType.Ans, "ANS"),
            (OfficialSourceType.SusTables, "Tabelas SUS"),
            (OfficialSourceType.Anvisa, "ANVISA"),
            (OfficialSourceType.Brasindice, "Brasíndice"),
            (OfficialSourceType.Simpro, "Simpro"));

        Register(maps,
            (OfficialVersionStatus.NeverChecked, "Nunca verificado"),
            (OfficialVersionStatus.UpToDate, "Atualizado"),
            (OfficialVersionStatus.UpdateAvailable, "Atualização disponível"),
            (OfficialVersionStatus.ManualDownloadRequired, "Download manual necessário"),
            (OfficialVersionStatus.CheckFailed, "Verificação falhou"),
            (OfficialVersionStatus.Importing, "Importando"));

        Register(maps,
            (IntegrationLogStatus.Info, "Informação"),
            (IntegrationLogStatus.Success, "Sucesso"),
            (IntegrationLogStatus.Warning, "Aviso"),
            (IntegrationLogStatus.Failed, "Falhou"));

        Register(maps,
            (GovIntegrationSystem.Cns, "CNS"),
            (GovIntegrationSystem.Cnes, "CNES"),
            (GovIntegrationSystem.Rnds, "RNDS"),
            (GovIntegrationSystem.ConecteSus, "Conecte SUS"),
            (GovIntegrationSystem.Horus, "Hórus"),
            (GovIntegrationSystem.EsusAps, "e-SUS APS"),
            (GovIntegrationSystem.SihSus, "SIH-SUS"),
            (GovIntegrationSystem.SiaSus, "SIA-SUS"),
            (GovIntegrationSystem.Tiss, "TISS"),
            (GovIntegrationSystem.Tuss, "TUSS"));

        Register(maps,
            (GovIntegrationPriority.Priority1, "Prioridade 1"),
            (GovIntegrationPriority.Priority2, "Prioridade 2"),
            (GovIntegrationPriority.Priority3, "Prioridade 3"));

        Register(maps,
            (GovIntegrationCredentialStatus.NotConfigured, "Não configurado"),
            (GovIntegrationCredentialStatus.MockActive, "Mock ativo"),
            (GovIntegrationCredentialStatus.PendingCredential, "Credencial pendente"),
            (GovIntegrationCredentialStatus.ProductionReady, "Pronto para produção"));

        Register(maps,
            (SiaDocumentType.Bpa, "BPA"),
            (SiaDocumentType.Apac, "APAC"));
    }
}
