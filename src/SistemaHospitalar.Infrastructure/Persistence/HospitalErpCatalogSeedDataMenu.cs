namespace SistemaHospitalar.Infrastructure.Persistence;

internal static partial class HospitalErpCatalogSeedData
{
    internal static readonly (string Module, string[] Submenus)[] SystemMenus =
    [
        ("DASHBOARD", ["Visão Geral", "Painel Assistencial", "Indicadores BI", "Alertas", "Tarefas Pendentes", "Agenda do Dia"]),
        ("RECEPCAO", ["Central de Recepção", "Cadastro de Pacientes", "Responsáveis", "Check-in", "Carteirinha SUS", "Consentimentos", "Registro de Nascimento"]),
        ("AMBULATORIO", ["Central Ambulatorial", "Consultórios", "Escalas Médicas", "Retornos", "Encaminhamentos"]),
        ("AGENDAMENTOS", ["Agenda Médica", "Agenda de Equipamentos", "Agenda de Exames", "Confirmação", "Lista de Espera"]),
        ("EMERGENCIA", ["Recepção PS", "Classificação de Risco", "Atendimento Médico", "Evolução", "Prescrições", "Encaminhamentos", "Alta"]),
        ("INTERNACAO", ["Visão Geral", "Admissão", "Mapa de Leitos", "Transferências", "Altas", "Óbitos"]),
        ("UTI", ["Internações UTI", "Evoluções", "Indicadores", "Escalas Clínicas"]),
        ("CENTRO_CIRURGICO", ["Agenda Cirúrgica", "Pré-operatório", "Sala Cirúrgica", "RPA", "Relatórios"]),
        ("PEP", ["Anamnese", "Evolução Médica", "Evolução Enfermagem", "Prescrição", "Solicitação de Exames", "CID", "Procedimentos", "Sinais Vitais", "Assinaturas"]),
        ("ENFERMAGEM", ["SAE — Diagnósticos", "SAE — Planejamento", "SAE — Evolução", "Medicamentos", "Sinais Vitais", "Curativos", "Checklists", "Escalas"]),
        ("CCIH", ["Controle de Infecção", "Vigilância Epidemiológica", "Notificações", "Indicadores"]),
        ("LABORATORIO", ["Solicitações", "Coleta", "Processamento", "Resultados", "Laudos", "Patologia", "Integrações"]),
        ("IMAGEM", ["Raio-X", "Tomografia", "Ressonância", "Ultrassom", "Mamografia", "Laudos", "PACS"]),
        ("FARMACIA", ["Dispensação", "Central Farmácia", "Solicitações", "Estoque", "Lotes", "Validades", "Inventário", "Transferências", "Devoluções"]),
        ("HEMOTERAPIA", ["Doadores", "Estoque", "Hemocomponentes", "Transfusões", "Relatórios"]),
        ("NUTRICAO", ["Avaliação Nutricional", "Dietas", "Produção", "Distribuição", "Relatórios"]),
        ("ONCOLOGIA", ["Sessões de Quimioterapia", "Protocolos", "Agenda Oncológica"]),
        ("FATURAMENTO", ["Painel Faturamento", "AIH SUS", "BPA", "APAC", "Produção Ambulatorial", "Exportações SUS"]),
        ("TISS", ["Guias TISS", "Fechamento", "Autorizações", "Lotes", "Glosas", "Recursos de Glosa"]),
        ("FINANCEIRO", ["Visão Geral", "Receber Convênios", "Receber SUS", "Receber Particular", "Pagar Fornecedores", "Caixa", "Conciliação", "Cobranças"]),
        ("ESTOQUE", ["Requisições", "Entradas", "Saídas", "Transferências", "Inventário", "Relatórios"]),
        ("COMPRAS", ["Solicitações", "Cotações", "Pedidos", "Recebimento", "Contratos", "Fornecedores"]),
        ("RH", ["Colaboradores", "Folha de Pagamento", "Escalas", "Plantões", "Férias", "Treinamentos", "Avaliações"]),
        ("ENGENHARIA", ["Equipamentos", "Manutenções", "Calibrações", "Contratos", "Indicadores"]),
        ("SEGURANCA", ["Visitantes", "Incidentes", "Credenciais", "Monitoramento", "Auditoria de Acesso"]),
        ("LGPD", ["Dashboard", "Auditoria", "Consentimentos", "Direitos do Titular", "Incidentes", "MFA"]),
        ("INTEGRACOES", ["Painel Integrações", "HL7", "FHIR", "TISS", "ANS", "CNES", "PACS", "Laboratório"]),
        ("GOVERNO", ["Painel Governamental", "CNS", "CNES", "SIH-SUS", "SIA-SUS", "Hórus", "RNDS", "e-SUS APS"]),
        ("CONFIGURACOES", ["Parâmetros", "Aparência", "Cadastros Auxiliares", "Catálogo Hospitalar", "APIs e Webhooks", "Atualizações Oficiais", "Layout"]),
        ("CONNECT", ["Caixa de Entrada", "Chat", "WhatsApp", "Chamados", "Tarefas", "Aprovações", "Mural", "Assistente"]),
        ("RELATORIOS", ["Central de Relatórios", "Pacientes", "Agenda", "Estoque", "Financeiro", "Internação", "RH", "Downloads"]),
    ];
}
