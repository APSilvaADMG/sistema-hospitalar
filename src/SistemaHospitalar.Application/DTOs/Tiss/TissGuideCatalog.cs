using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Tiss;

public record TissGuideTypeCatalogDto(
    int Code,
    string Slug,
    string Name,
    string ShortName,
    string Category,
    string CategoryLabel,
    string Description,
    string WhenToUse,
    bool IsCreatable,
    bool IsImplemented,
    string? LinkedTab,
    string? AnsManualUrl);

public static class TissGuideCatalog
{
    private const string AnsPortal =
        "https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss";

    public static IReadOnlyList<TissGuideTypeCatalogDto> All { get; } =
    [
        Entry(TissGuideType.Consultation, "consulta", "Guia de Consulta", "Consulta", "billing",
            "Cobrança", "Formulário mais simples do padrão TISS.",
            "Consultas eletivas em consultório, ambulatório ou clínica — sem internação.",
            true, true, "guides"),
        Entry(TissGuideType.SpSadt, "sp-sadt", "Guia SP/SADT", "SP/SADT", "billing",
            "Cobrança", "Serviços Profissionais e Serviços Auxiliares de Diagnóstico e Terapia.",
            "Exames (lab/imagem), terapias, pequenas cirurgias, procedimentos ambulatoriais e solicitações/autorizações SP/SADT.",
            true, true, "guides"),
        Entry(TissGuideType.HospitalizationRequest, "solicitacao-internacao", "Guia de Solicitação de Internação", "Solic. internação", "authorization",
            "Autorização", "Pedido de autorização para internação hospitalar.",
            "Quando o paciente necessita internação e a operadora exige autorização prévia.",
            true, true, "authorizations"),
        Entry(TissGuideType.DischargeSummary, "resumo-internacao", "Guia de Resumo de Internação", "Resumo internação", "billing",
            "Cobrança", "Faturamento após alta hospitalar.",
            "Após a alta: consolida procedimentos, diárias, taxas, materiais e medicamentos do período de internação.",
            true, true, "guides"),
        Entry(TissGuideType.IndividualFees, "honorarios", "Guia de Honorário Individual", "Honorários", "billing",
            "Cobrança", "Honorários de profissionais específicos da equipe.",
            "Faturamento de médicos/auxiliares que atuaram em internação, centro cirúrgico ou equipe multiprofissional.",
            true, true, "guides"),
        Entry(TissGuideType.DentalTreatment, "gto", "Guia de Tratamento Odontológico (GTO)", "GTO", "dental",
            "Odontologia", "Faturamento odontológico conforme rol ANS.",
            "Consultas, procedimentos, urgências e tratamentos em consultório odontológico.",
            true, true, "guides"),
        Entry(TissGuideType.OtherExpenses, "outras-despesas", "Anexo / Guia de Outras Despesas", "Outras despesas", "annex",
            "Anexo", "Despesas vinculadas a guia principal (geralmente SP/SADT).",
            "Taxas, materiais e despesas acessórias não cobertas na guia principal — referencia a guia SP/SADT.",
            true, true, "annexes"),
        Entry(TissGuideType.OpmeAnnex, "anexo-opme", "Anexo de Solicitação de OPME", "Anexo OPME", "annex",
            "Anexo", "Órteses, próteses e materiais especiais.",
            "Solicitação/autorização de OPME vinculada à guia SP/SADT de origem.",
            false, true, "annexes"),
        Entry(TissGuideType.ChemotherapyAnnex, "anexo-quimio", "Anexo de Solicitação de Quimioterapia", "Anexo quimio", "annex",
            "Anexo", "Protocolos e medicamentos oncológicos.",
            "Quimioterapia ambulatorial ou hospitalar — vinculado à guia SP/SADT.",
            false, true, "annexes"),
        Entry(TissGuideType.RadiotherapyAnnex, "anexo-radio", "Anexo de Solicitação de Radioterapia", "Anexo radio", "annex",
            "Anexo", "Planejamento e sessões de radioterapia.",
            "Radioterapia — vinculado à guia SP/SADT.",
            false, true, "annexes"),
        Entry(TissGuideType.ExtensionRequest, "prorrogacao-internacao", "Guia de Prorrogação / Complementação de Internação", "Prorrogação", "authorization",
            "Autorização", "Extensão ou complemento de internação autorizada.",
            "Prorrogação de diárias ou complementação de procedimentos durante internação.",
            true, false, "authorizations"),
        Entry(TissGuideType.PresenceProof, "comprovante-presencial", "Guia de Comprovante Presencial", "Comprovante", "administrative",
            "Administrativo", "Registro de comparecimento do beneficiário.",
            "Telemedicina, terapias ou procedimentos que exigem comprovação de presença.",
            true, false, "guides"),
        Entry(TissGuideType.GlosaAppeal, "recurso-glosa", "Guia de Recurso de Glosa", "Recurso glosa", "administrative",
            "Administrativo", "Contestação de glosas (recusas de pagamento).",
            "Quando a operadora glosa procedimentos ou valores — recurso conforme manual ANS.",
            false, true, "guides"),
        Entry(TissGuideType.PaymentStatement, "demonstrativo-pagamento", "Demonstrativo de Pagamento", "Demonstrativo", "administrative",
            "Administrativo", "Retorno da operadora com valores pagos e glosados.",
            "Conciliação financeira — importado/processado na aba Demonstrativos.",
            false, true, "demonstrativos"),
        Entry(TissGuideType.DentalInitialAnnex, "gto-situacao-inicial", "Anexo GTO — Situação Inicial", "GTO inicial", "dental",
            "Odontologia", "Odontograma e situação bucal inicial.",
            "Primeiro atendimento odontológico ou início de tratamento conforme GTO.",
            true, false, "guides"),
        Entry(TissGuideType.DentalPaymentStatement, "demonstrativo-odonto", "Demonstrativo de Pagamento Odontológico", "Demo. odonto", "dental",
            "Odontologia", "Demonstrativo específico para planos odontológicos.",
            "Conciliação de produção odontológica com a operadora.",
            false, false, "demonstrativos"),
        Entry(TissGuideType.DentalGlosaAppeal, "recurso-glosa-odonto", "Guia de Recurso de Glosa Odontológica", "Recurso odonto", "dental",
            "Odontologia", "Recurso de glosa para tratamentos odontológicos.",
            "Contestação de glosas em produção odontológica.",
            false, false, "guides"),
        Entry(TissGuideType.Hospitalization, "internacao-legado", "Internação (conta hospitalar)", "Internação", "billing",
            "Cobrança", "Tipo legado para contas de internação em elaboração.",
            "Use preferencialmente Resumo de Internação após a alta. Mantido para guias já existentes.",
            true, true, "guides"),
        Entry(TissGuideType.MonitoringReport, "monitoramento-tiss", "Monitoramento / Envio TISS", "Monitoramento", "administrative",
            "Administrativo", "Remessa de lotes e monitoramento junto à ANS.",
            "Envio de lotes XML, protocolo e acompanhamento de produção — aba Lotes XML.",
            false, true, "batches"),
    ];

    public static TissGuideTypeCatalogDto? Find(int code) =>
        All.FirstOrDefault(g => g.Code == code);

    public static TissGuideTypeCatalogDto? Find(TissGuideType type) =>
        Find((int)type);

    private static TissGuideTypeCatalogDto Entry(
        TissGuideType type,
        string slug,
        string name,
        string shortName,
        string category,
        string categoryLabel,
        string description,
        string whenToUse,
        bool isCreatable,
        bool isImplemented,
        string? linkedTab) =>
        new(
            (int)type,
            slug,
            name,
            shortName,
            category,
            categoryLabel,
            description,
            whenToUse,
            isCreatable,
            isImplemented,
            linkedTab,
            AnsPortal);
}
