import type { ModuleTab } from './useModuleSection';

export const emergencyTabs: ModuleTab[] = [
  { slug: '', label: 'Recepção' },
  { slug: 'classificacao-risco', label: 'Classificação de Risco' },
  { slug: 'atendimento-medico', label: 'Atendimento Médico' },
  { slug: 'evolucao', label: 'Evolução' },
  { slug: 'prescricoes', label: 'Prescrições' },
  { slug: 'encaminhamentos', label: 'Encaminhamentos' },
  { slug: 'alta', label: 'Alta' },
];

export const hospitalizationTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral' },
  { slug: 'admissao', label: 'Admissão' },
  { slug: 'leitos', label: 'Leitos' },
  { slug: 'transferencias', label: 'Transferências' },
  { slug: 'altas', label: 'Altas' },
  { slug: 'obitos', label: 'Óbitos' },
];

export const surgeryTabs: ModuleTab[] = [
  { slug: '', label: 'Agenda' },
  { slug: 'pre-operatorio', label: 'Pré-operatório' },
  { slug: 'sala', label: 'Sala Cirúrgica' },
  { slug: 'rpa', label: 'RPA' },
  { slug: 'relatorios', label: 'Relatórios' },
];

export const icuTabs: ModuleTab[] = [
  { slug: '', label: 'Internações' },
  { slug: 'evolucoes', label: 'Evoluções' },
  { slug: 'indicadores', label: 'Indicadores' },
  { slug: 'escalas', label: 'Escalas' },
];

export const appointmentTabs: ModuleTab[] = [
  { slug: '', label: 'Agenda' },
  { slug: 'consultas', label: 'Consultas' },
  { slug: 'retornos', label: 'Retornos' },
  { slug: 'encaminhamentos', label: 'Encaminhamentos' },
  { slug: 'check-in', label: 'Check-in' },
];

export const patientTabs: ModuleTab[] = [
  { slug: '', label: 'Cadastro' },
  { slug: 'responsaveis', label: 'Responsáveis' },
  { slug: 'historico', label: 'Histórico' },
  { slug: 'documentos', label: 'Documentos' },
  { slug: 'consentimentos', label: 'Consentimentos' },
  { slug: 'anexos', label: 'Anexos' },
  { slug: 'carteirinha-sus', label: 'Carteirinha SUS' },
];

export const configTabs: ModuleTab[] = [
  { slug: 'parametros', label: 'Parâmetros' },
  { slug: 'aparencia', label: 'Aparência' },
  { slug: 'cadastros', label: 'Cadastros' },
  { slug: 'integracoes', label: 'APIs e Webhooks' },
  { slug: 'atualizacoes-oficiais', label: 'Atualizações Oficiais' },
  { slug: 'layout', label: 'Layout' },
  { slug: 'regras-negocio', label: 'Regras de Negócio' },
];

export const automacaoTabs: ModuleTab[] = [
  { slug: 'codigo-barras', label: 'Código de Barras' },
  { slug: 'rfid', label: 'RFID' },
  { slug: 'fluxos', label: 'Fluxos' },
];

export const pepTabs: ModuleTab[] = [
  { slug: 'anamnese', label: 'Anamnese' },
  { slug: 'evolucao-medica', label: 'Evolução Médica' },
  { slug: 'evolucao-enfermagem', label: 'Evolução Enfermagem' },
  { slug: 'evolucao-multidisciplinar', label: 'Multidisciplinar' },
  { slug: 'prescricao', label: 'Prescrição' },
  { slug: 'solicitacao-exames', label: 'Solicitação de Exames' },
  { slug: 'vias-administracao', label: 'Vias de administração' },
  { slug: 'diagnosticos', label: 'CID' },
  { slug: 'procedimentos', label: 'Procedimentos' },
  { slug: 'sinais-vitais', label: 'Sinais Vitais' },
  { slug: 'escalas', label: 'Escalas' },
  { slug: 'anexos', label: 'Anexos' },
  { slug: 'assinaturas', label: 'Assinaturas' },
];

export const agendaTabs: ModuleTab[] = [
  { slug: 'medica', label: 'Agenda Médica' },
  { slug: 'equipamentos', label: 'Equipamentos' },
  { slug: 'exames', label: 'Exames' },
];

export const nursingTabs: ModuleTab[] = [
  { slug: 'leito', label: 'Leito (PoC)' },
  { slug: 'sae/diagnosticos', label: 'SAE — Diagnósticos' },
  { slug: 'sae/planejamento', label: 'SAE — Planejamento' },
  { slug: 'sae/evolucao', label: 'SAE — Evolução' },
  { slug: 'medicamentos', label: 'Medicamentos' },
  { slug: 'sinais-vitais', label: 'Sinais Vitais' },
  { slug: 'curativos', label: 'Curativos' },
  { slug: 'checklists', label: 'Checklists' },
  { slug: 'escalas', label: 'Escalas' },
];

export const pharmacyTabs: ModuleTab[] = [
  { slug: '', label: 'Dispensação' },
  { slug: 'hub', label: 'Central' },
  { slug: 'faturamento', label: 'Faturamento' },
  { slug: 'solicitacoes', label: 'Solicitações' },
  { slug: 'estoque', label: 'Estoque' },
  { slug: 'lotes', label: 'Lotes' },
  { slug: 'validades', label: 'Validades' },
  { slug: 'inventario', label: 'Inventário' },
  { slug: 'transferencias', label: 'Transferências' },
  { slug: 'devolucoes', label: 'Devoluções' },
  { slug: 'relatorios', label: 'Relatórios' },
];

export const inventoryTabs: ModuleTab[] = [
  { slug: 'requisicoes', label: 'Requisições' },
  { slug: 'entradas', label: 'Entradas' },
  { slug: 'saidas', label: 'Saídas' },
  { slug: 'transferencias', label: 'Transferências' },
  { slug: '', label: 'Inventário' },
  { slug: 'relatorios', label: 'Relatórios' },
];

export const labTabs: ModuleTab[] = [
  { slug: '', label: 'Solicitações' },
  { slug: 'coleta', label: 'Coleta' },
  { slug: 'processamento', label: 'Processamento' },
  { slug: 'resultados', label: 'Resultados' },
  { slug: 'laudos', label: 'Laudos' },
  { slug: 'patologia', label: 'Patologia' },
  { slug: 'integracoes', label: 'Integrações' },
];

export const imagingTabs: ModuleTab[] = [
  { slug: 'raio-x', label: 'Raio-X' },
  { slug: 'tomografia', label: 'Tomografia' },
  { slug: 'ressonancia', label: 'RM' },
  { slug: 'ultrassom', label: 'Ultrassom' },
  { slug: 'mamografia', label: 'Mamografia' },
  { slug: '', label: 'Laudos' },
  { slug: 'pacs', label: 'PACS' },
];

export const hemotherapyTabs: ModuleTab[] = [
  { slug: 'doadores', label: 'Doadores' },
  { slug: '', label: 'Estoque' },
  { slug: 'hemocomponentes', label: 'Hemocomponentes' },
  { slug: 'transfusoes', label: 'Transfusões' },
  { slug: 'relatorios', label: 'Relatórios' },
];

export const nutritionTabs: ModuleTab[] = [
  { slug: '', label: 'Avaliação' },
  { slug: 'dietas', label: 'Dietas' },
  { slug: 'producao', label: 'Produção' },
  { slug: 'distribuicao', label: 'Distribuição' },
  { slug: 'relatorios', label: 'Relatórios' },
];

export const oncologyTabs: ModuleTab[] = [
  { slug: '', label: 'Sessões de Quimioterapia' },
];

export const susBillingTabs: ModuleTab[] = [
  { slug: '', label: 'Dashboard' },
  { slug: 'sus/aih', label: 'AIH' },
  { slug: 'sus/bpa', label: 'BPA' },
  { slug: 'sus/apac', label: 'APAC' },
  { slug: 'sus/producao-ambulatorial', label: 'Produção Amb.' },
  { slug: 'sus/exportacoes', label: 'Exportações' },
  { slug: 'auditoria/pre-faturamento', label: 'Pré-Faturamento' },
  { slug: 'auditoria/medica', label: 'Auditoria Médica' },
  { slug: 'auditoria/enfermagem', label: 'Auditoria Enfermagem' },
];

export const financialTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral' },
  { slug: 'hub', label: 'Central' },
  { slug: 'receber/convenios', label: 'Receber — Convênios' },
  { slug: 'receber/sus', label: 'Receber — SUS' },
  { slug: 'receber/particular', label: 'Receber — Particular' },
  { slug: 'pagar/fornecedores', label: 'Pagar — Fornecedores' },
  { slug: 'pagar/despesas', label: 'Pagar — Despesas' },
  { slug: 'pagar/impostos', label: 'Pagar — Impostos' },
  { slug: 'tesouraria/caixa', label: 'Caixa' },
  { slug: 'tesouraria/bancos', label: 'Bancos' },
  { slug: 'tesouraria/conciliacao', label: 'Conciliação' },
  { slug: 'fiscal/notas', label: 'NF' },
  { slug: 'fiscal/tributos', label: 'Tributos' },
  { slug: 'fiscal/obrigacoes', label: 'Obrigações' },
  { slug: 'cobrancas', label: 'Cobranças' },
  { slug: 'recibos-diversos', label: 'Recibos Diversos' },
  { slug: 'boletos', label: 'Boletos' },
];

export const tissTabs: ModuleTab[] = [
  { slug: '', label: 'Guias TISS' },
  { slug: 'fechamento', label: 'Fechamento' },
  { slug: 'autorizacoes', label: 'Autorizações' },
  { slug: 'lotes', label: 'Lotes' },
  { slug: 'glosas', label: 'Glosas' },
  { slug: 'recursos-glosa', label: 'Recursos' },
];

export const guidesTabs: ModuleTab[] = [
  { slug: '', label: 'Central' },
  { slug: 'consultas', label: 'Consultas' },
  { slug: 'exames', label: 'Exames' },
  { slug: 'procedimentos', label: 'Procedimentos' },
  { slug: 'internacao', label: 'Internação' },
  { slug: 'tiss', label: 'TISS' },
  { slug: 'sus', label: 'SUS' },
  { slug: 'autorizacoes', label: 'Autorizações' },
  { slug: 'faturamento', label: 'Faturamento' },
  { slug: 'auditoria', label: 'Auditoria' },
];

export const purchasingTabs: ModuleTab[] = [
  { slug: 'solicitacoes', label: 'Solicitações' },
  { slug: 'cotacoes', label: 'Cotações' },
  { slug: '', label: 'Pedidos' },
  { slug: 'recebimento', label: 'Recebimento' },
  { slug: 'contratos', label: 'Contratos' },
  { slug: 'fornecedores', label: 'Fornecedores' },
];

export const hrTabs: ModuleTab[] = [
  { slug: '', label: 'Colaboradores' },
  { slug: 'folha', label: 'Folha' },
  { slug: 'escalas', label: 'Escalas' },
  { slug: 'plantoes', label: 'Plantões' },
  { slug: 'ferias', label: 'Férias' },
  { slug: 'treinamentos', label: 'Treinamentos' },
  { slug: 'avaliacoes', label: 'Avaliações' },
];

export const ccihTabs: ModuleTab[] = [
  { slug: '', label: 'Controle' },
  { slug: 'vigilancia', label: 'Vigilância' },
  { slug: 'notificacoes', label: 'Notificações' },
  { slug: 'indicadores', label: 'Indicadores' },
];

export const securityLgpdTabs: ModuleTab[] = [
  { slug: '', label: 'Dashboard' },
  { slug: 'auditoria', label: 'Auditoria' },
  { slug: 'logins', label: 'Logins' },
  { slug: 'sessoes', label: 'Sessões' },
  { slug: 'consentimentos', label: 'Consentimentos' },
  { slug: 'titular', label: 'Direitos do Titular' },
  { slug: 'incidentes', label: 'Incidentes' },
  { slug: 'mfa', label: 'MFA' },
];

export const qualityTabs: ModuleTab[] = [
  { slug: 'nao-conformidades', label: 'Não Conformidades' },
  { slug: 'protocolos', label: 'Protocolos' },
  { slug: 'indicadores', label: 'Indicadores' },
  { slug: 'ona', label: 'ONA' },
  { slug: 'jci', label: 'JCI' },
];

export const regulationTabs: ModuleTab[] = [
  { slug: 'sisreg', label: 'SISREG' },
  { slug: 'leitos', label: 'Central de Leitos' },
  { slug: 'autorizacoes', label: 'Autorizações' },
  { slug: 'transferencias', label: 'Transferências' },
];

export const clinicalEngTabs: ModuleTab[] = [
  { slug: '', label: 'Equipamentos' },
  { slug: 'manutencoes', label: 'Manutenções' },
  { slug: 'calibracoes', label: 'Calibrações' },
  { slug: 'contratos', label: 'Contratos' },
  { slug: 'indicadores', label: 'Indicadores' },
];

export const biTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral' },
  { slug: 'ocupacao', label: 'Ocupação' },
  { slug: 'permanencia', label: 'Permanência' },
  { slug: 'giro-leitos', label: 'Giro de Leitos' },
  { slug: 'custos', label: 'Custos' },
  { slug: 'inadimplencia', label: 'Inadimplência' },
  { slug: 'producao-medica', label: 'Produção Médica' },
  { slug: 'producao-hospitalar', label: 'Produção Hospitalar' },
  { slug: 'faturamento', label: 'Faturamento' },
];

export const hotelariaTabs: ModuleTab[] = [
  { slug: '', label: 'NOC' },
  { slug: 'higienizacao', label: 'Higienização' },
];

export const transportTabs: ModuleTab[] = [
  { slug: '', label: 'Painel' },
  { slug: 'fila', label: 'Fila' },
  { slug: 'equipamentos', label: 'Macas e equipamentos' },
  { slug: 'indicadores', label: 'Indicadores' },
];

export const dialysisTabs: ModuleTab[] = [
  { slug: '', label: 'Sessões' },
];

export const physiotherapyTabs: ModuleTab[] = [
  { slug: '', label: 'Sessões' },
];

export const laundryTabs: ModuleTab[] = [
  { slug: '', label: 'Lotes' },
];

export const ambulanceTabs: ModuleTab[] = [
  { slug: '', label: 'Operações' },
];

export const physicalAccessTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral' },
  { slug: 'visitantes', label: 'Visitantes' },
  { slug: 'catracas', label: 'Catracas' },
  { slug: 'facial', label: 'Reconhecimento Facial' },
  { slug: 'totens', label: 'Totens' },
  { slug: 'estacionamento', label: 'Estacionamento' },
  { slug: 'lpr', label: 'LPR' },
  { slug: 'credenciais', label: 'Credenciais' },
  { slug: 'setores', label: 'Acesso por Setor' },
  { slug: 'monitoramento', label: 'Monitoramento' },
  { slug: 'auditoria', label: 'Auditoria' },
  { slug: 'integracoes', label: 'Integrações' },
  { slug: 'chaves', label: 'Chaves' },
  { slug: 'armarios', label: 'Armários' },
  { slug: 'elevadores', label: 'Elevadores' },
  { slug: 'terceiros', label: 'Terceiros' },
  { slug: 'central', label: 'Central Patrimonial' },
];

export const govIntegrationTabs: ModuleTab[] = [
  { slug: '', label: 'Painel' },
  { slug: 'cns', label: 'CNS' },
  { slug: 'cnes', label: 'CNES' },
  { slug: 'sih', label: 'SIH-SUS' },
  { slug: 'sia', label: 'SIA-SUS' },
  { slug: 'tiss', label: 'TISS' },
  { slug: 'tuss', label: 'TUSS' },
  { slug: 'horus', label: 'Hórus' },
  { slug: 'rnds', label: 'RNDS' },
  { slug: 'esus', label: 'e-SUS APS' },
  { slug: 'conecte', label: 'Conecte SUS' },
  { slug: 'fhir', label: 'FHIR' },
];

export const dashboardTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral', to: '/' },
  { slug: 'command-center', label: 'Centro de Comando', to: '/dashboard/command-center' },
  { slug: 'assistencial', label: 'Painel Assistencial', to: '/dashboard/assistencial' },
  { slug: 'indicadores', label: 'Indicadores', to: '/bi' },
  { slug: 'alertas', label: 'Alertas', to: '/notificacoes' },
  { slug: 'tarefas', label: 'Tarefas Pendentes', to: '/dashboard/tarefas' },
  { slug: 'agenda', label: 'Agenda do Dia', to: '/agendamentos' },
];

export const reportsTabs: ModuleTab[] = [
  { slug: '', label: 'Central' },
  { slug: 'pacientes', label: 'Pacientes' },
  { slug: 'agenda', label: 'Agenda' },
  { slug: 'estoque-farmacia', label: 'Estoque e Farmácia' },
  { slug: 'financeiro', label: 'Financeiro' },
  { slug: 'faturamento', label: 'Faturamento' },
  { slug: 'internacao', label: 'Internação' },
  { slug: 'rh-gestao', label: 'RH e Gestão' },
  { slug: 'downloads', label: 'Downloads' },
  { slug: 'indicadores', label: 'Indicadores' },
];

export const connectTabs: ModuleTab[] = [
  { slug: '', label: 'Caixa de Entrada' },
  { slug: 'enviadas', label: 'Enviadas' },
  { slug: 'rascunhos', label: 'Rascunhos' },
  { slug: 'chat', label: 'Chat' },
  { slug: 'notificacoes', label: 'Notificações' },
  { slug: 'mural', label: 'Mural' },
  { slug: 'chamados', label: 'Chamados' },
  { slug: 'tarefas', label: 'Tarefas' },
  { slug: 'aprovacoes', label: 'Aprovações' },
  { slug: 'agenda', label: 'Agenda' },
  { slug: 'assistente', label: 'Assistente' },
];

export const connectWhatsAppTabs: ModuleTab[] = [
  { slug: '', label: 'Painel' },
  { slug: 'inbox', label: 'Inbox' },
  { slug: 'simulador', label: 'Simulador WhatsApp' },
  { slug: 'conversas', label: 'Conversas' },
  { slug: 'lista-espera', label: 'Lista de espera' },
  { slug: 'faq', label: 'Base FAQ' },
  { slug: 'nps', label: 'Satisfação' },
];

export const consultingRoomsTabs: ModuleTab[] = [
  { slug: '', label: 'Salas' },
  { slug: 'escalas', label: 'Escalas' },
];

export const cmeTabs: ModuleTab[] = [
  { slug: '', label: 'Kits' },
  { slug: 'ciclos', label: 'Ciclos' },
];

export const securityPortariaTabs: ModuleTab[] = [
  { slug: '', label: 'Visitantes' },
  { slug: 'incidentes', label: 'Incidentes' },
];

export const aiTabs: ModuleTab[] = [
  { slug: '', label: 'Triagem' },
  { slug: 'epidemiologia', label: 'Epidemiologia' },
  { slug: 'historico', label: 'Histórico' },
];

export const integrationTabs: ModuleTab[] = [
  { slug: '', label: 'Painel' },
  { slug: 'status', label: 'Status (mock/prod)' },
  { slug: 'hl7', label: 'HL7' },
  { slug: 'fhir', label: 'FHIR' },
  { slug: 'tiss', label: 'TISS' },
  { slug: 'ans', label: 'ANS' },
  { slug: 'cnes', label: 'CNES' },
  { slug: 'cadsus', label: 'CADSUS' },
  { slug: 'sisreg', label: 'SISREG' },
  { slug: 'esus', label: 'e-SUS' },
  { slug: 'pacs', label: 'PACS' },
  { slug: 'laboratorio', label: 'Laboratório' },
];

export const PEP_ENTRY_TYPE: Record<string, number> = {
  anamnese: 1,
  'evolucao-medica': 2,
  'evolucao-enfermagem': 2,
  'evolucao-multidisciplinar': 2,
  prescricao: 3,
  'solicitacao-exames': 4,
  procedimentos: 5,
};
