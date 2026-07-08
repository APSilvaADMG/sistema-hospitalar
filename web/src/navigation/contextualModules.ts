import {
  FUNI_GUIDE_BASE,
  getFuniGuideBySlug,
  type FuniGuideDefinition,
} from '../data/funiGuides/catalog';

/** Contexto operacional onde guias FUNI e relatórios aparecem inline. */
export type ModuleContextId =
  | 'reception'
  | 'emergency'
  | 'hospitalization'
  | 'medicalRecord'
  | 'nursing'
  | 'surgery'
  | 'pharmacy'
  | 'laboratory'
  | 'imaging'
  | 'oncology'
  | 'financial'
  | 'insurance'
  | 'hospitalBilling'
  | 'humanResources'
  | 'quality'
  | 'infectionControl'
  | 'supply'
  | 'businessIntelligence'
  | 'audit'
  | 'regulatory'
  | 'securityLgpd'
  | 'physicalAccess'
  | 'hemotherapy';

export type ModuleQuickLink = {
  label: string;
  to: string;
  description?: string;
};

export type ModuleContextConfig = {
  label: string;
  /** Valor do enum ReportModule no backend (ex.: Reception). */
  reportModule?: string;
  /** Aba interna em /relatorios quando existir. */
  reportsSection?: string;
  /** Slugs FUNI relevantes neste fluxo. */
  funiSlugs: string[];
  /** Atalhos fixos (ex.: AIH SUS) além das guias FUNI. */
  quickLinks?: ModuleQuickLink[];
};

export const MODULE_CONTEXT: Record<ModuleContextId, ModuleContextConfig> = {
  reception: {
    label: 'Recepção e Ambulatório',
    reportModule: 'Reception',
    reportsSection: 'atendimento',
    funiSlugs: ['sp-sadt'],
    quickLinks: [
      {
        label: 'Guia de Consulta (FUNI 21)',
        to: '/faturamento-tiss/guias-funi/consulta',
        description: 'Formulário TISS para consultas e ambulatório',
      },
    ],
  },
  emergency: {
    label: 'Pronto Atendimento',
    reportModule: 'Emergency',
    reportsSection: 'atendimento',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'Guia de Consulta (FUNI 21)',
        to: '/faturamento-tiss/guias-funi/consulta',
        description: 'Atendimentos e retornos do pronto-socorro',
      },
      {
        label: 'SP/SADT (FUNI 30)',
        to: '/faturamento-tiss/guias-funi/sp-sadt',
        description: 'Procedimentos, exames e terapias ambulatoriais',
      },
    ],
  },
  hospitalization: {
    label: 'Internação',
    reportModule: 'Hospitalization',
    reportsSection: 'internacao',
    funiSlugs: ['solicitacao-internacao', 'resumo-internacao', 'prorrogacao', 'honorarios', 'outras-despesas'],
    quickLinks: [
      {
        label: 'AIH (SUS)',
        to: '/faturamento/sus/aih',
        description: 'Autorização e faturamento de internação hospitalar — SUS/SIH',
      },
      {
        label: 'Solicitação de Internação (FUNI 22)',
        to: '/faturamento-tiss/guias-funi/solicitacao-internacao',
        description: 'Autorização prévia TISS / convênio',
      },
      {
        label: 'Resumo de Internação (FUNI 23)',
        to: '/faturamento-tiss/guias-funi/resumo-internacao',
        description: 'Faturamento após alta hospitalar',
      },
    ],
  },
  medicalRecord: {
    label: 'Prontuário',
    reportModule: 'MedicalRecord',
    funiSlugs: ['honorarios'],
    quickLinks: [
      {
        label: 'Guia de Consulta (FUNI 21)',
        to: '/faturamento-tiss/guias-funi/consulta',
        description: 'Documentos gerados a partir do PEP',
      },
      {
        label: 'SP/SADT (FUNI 30)',
        to: '/faturamento-tiss/guias-funi/sp-sadt',
        description: 'Solicitação de procedimentos e exames',
      },
    ],
  },
  nursing: {
    label: 'Enfermagem',
    reportModule: 'Nursing',
    funiSlugs: [],
  },
  surgery: {
    label: 'Centro Cirúrgico',
    reportModule: 'Surgery',
    reportsSection: 'centro-cirurgico',
    funiSlugs: ['outras-despesas'],
    quickLinks: [
      {
        label: 'OPME (FUNI 54)',
        to: '/faturamento-tiss/guias-funi/opme',
        description: 'Órteses, próteses e materiais especiais',
      },
      {
        label: 'Honorários (FUNI 27)',
        to: '/faturamento-tiss/guias-funi/honorarios',
        description: 'Honorários médicos do ato cirúrgico',
      },
    ],
  },
  pharmacy: {
    label: 'Farmácia',
    reportModule: 'Pharmacy',
    reportsSection: 'farmacia/consumo',
    funiSlugs: [],
  },
  laboratory: {
    label: 'Laboratório',
    reportModule: 'Laboratory',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'SP/SADT (FUNI 30)',
        to: '/faturamento-tiss/guias-funi/sp-sadt',
        description: 'Solicitação de exames laboratoriais',
      },
    ],
  },
  imaging: {
    label: 'Diagnóstico por Imagem',
    reportModule: 'Imaging',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'SP/SADT (FUNI 30)',
        to: '/faturamento-tiss/guias-funi/sp-sadt',
        description: 'Solicitação de exames de imagem',
      },
    ],
  },
  hemotherapy: {
    label: 'Hemoterapia',
    reportModule: 'Laboratory',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'SP/SADT (FUNI 30)',
        to: '/faturamento-tiss/guias-funi/sp-sadt',
        description: 'Procedimentos hemoterápicos e exames',
      },
    ],
  },
  oncology: {
    label: 'Oncologia',
    funiSlugs: ['sp-sadt'],
    quickLinks: [
      {
        label: 'Quimioterapia (FUNI 55)',
        to: '/faturamento-tiss/guias-funi/quimioterapia',
        description: 'Anexo de solicitação de quimioterapia — formulário TISS',
      },
      {
        label: 'Radioterapia (FUNI 56)',
        to: '/faturamento-tiss/guias-funi/radioterapia',
        description: 'Anexo de solicitação de radioterapia — formulário TISS',
      },
    ],
  },
  financial: {
    label: 'Financeiro',
    reportModule: 'Financial',
    reportsSection: 'receitas',
    funiSlugs: ['demonstrativo-pagamento'],
  },
  insurance: {
    label: 'Faturamento TISS / Convênios',
    reportModule: 'Insurance',
    reportsSection: 'convenios/producao',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'Catálogo FUNI (formulários)',
        to: '/faturamento-tiss/guias-funi',
        description: 'Todas as guias TISS — consulta, internação, quimio, etc.',
      },
      {
        label: 'Guia de Consulta (FUNI 21)',
        to: '/faturamento-tiss/guias-funi/consulta',
        description: 'Formulário digital implementado',
      },
    ],
  },
  hospitalBilling: {
    label: 'Faturamento Hospitalar',
    reportModule: 'HospitalBilling',
    reportsSection: 'producao',
    funiSlugs: ['demonstrativo-analise', 'demonstrativo-pagamento'],
    quickLinks: [
      {
        label: 'AIH (SUS)',
        to: '/faturamento/sus/aih',
        description: 'Autorização de internação — SUS/SIH',
      },
      {
        label: 'APAC (SUS)',
        to: '/faturamento/sus/apac',
        description: 'Alta complexidade — oncologia, diálise, etc.',
      },
      {
        label: 'Resumo de Internação (FUNI 23)',
        to: '/faturamento-tiss/guias-funi/resumo-internacao',
        description: 'Faturamento TISS após alta',
      },
    ],
  },
  humanResources: {
    label: 'Recursos Humanos',
    reportModule: 'HumanResources',
    reportsSection: 'rh/escalas',
    funiSlugs: [],
  },
  quality: {
    label: 'Qualidade',
    reportModule: 'Quality',
    reportsSection: 'qualidade/indicadores',
    funiSlugs: [],
  },
  infectionControl: {
    label: 'CCIH',
    reportModule: 'InfectionControl',
    funiSlugs: [],
  },
  supply: {
    label: 'Suprimentos',
    reportModule: 'Supply',
    reportsSection: 'almoxarifado/entradas',
    funiSlugs: [],
  },
  businessIntelligence: {
    label: 'BI Executivo',
    reportModule: 'BusinessIntelligence',
    reportsSection: 'ocupacao',
    funiSlugs: [],
  },
  audit: {
    label: 'Auditoria',
    reportModule: 'Audit',
    funiSlugs: [],
  },
  regulatory: {
    label: 'Regulatório / SUS',
    reportModule: 'Regulatory',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'Integrações SUS',
        to: '/integracoes-gov',
        description: 'CNES, SIH, SIA, TISS e demais integrações',
      },
      {
        label: 'Regulação e Leitos',
        to: '/regulacao',
        description: 'SISREG, autorizações e transferências',
      },
    ],
  },
  securityLgpd: {
    label: 'Segurança e LGPD',
    reportModule: 'Audit',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'Consentimentos LGPD',
        to: '/seguranca-lgpd/consentimentos',
        description: 'Termos, registros e revogações',
      },
      {
        label: 'Coletar assinatura (Pacientes)',
        to: '/pacientes/consentimentos',
        description: 'Fluxo ler → ciência → assinar',
      },
      {
        label: 'Direitos do titular',
        to: '/seguranca-lgpd/titular',
        description: 'Exportação e solicitações LGPD',
      },
    ],
  },
  physicalAccess: {
    label: 'Acesso Físico',
    reportModule: 'Reception',
    reportsSection: 'atendimento',
    funiSlugs: [],
    quickLinks: [
      {
        label: 'Agendamentos e check-in',
        to: '/agendamentos',
        description: 'QR code e validação na catraca',
      },
      {
        label: 'Portaria e visitantes',
        to: '/seguranca',
        description: 'Controle de entrada institucional',
      },
    ],
  },
};

export function funiGuideUrl(slug: string): string {
  return `${FUNI_GUIDE_BASE}/${slug}`;
}

export function getContextFuniGuides(contextId: ModuleContextId): FuniGuideDefinition[] {
  const slugs = MODULE_CONTEXT[contextId].funiSlugs;
  return slugs
    .map((slug) => getFuniGuideBySlug(slug))
    .filter((g): g is FuniGuideDefinition => g !== undefined);
}

export function getContextQuickLinks(contextId: ModuleContextId): ModuleQuickLink[] {
  return MODULE_CONTEXT[contextId].quickLinks ?? [];
}

export function getContextReportsPath(contextId: ModuleContextId): string {
  const cfg = MODULE_CONTEXT[contextId];
  const base = cfg.reportsSection ? `/relatorios/${cfg.reportsSection}` : '/relatorios';
  if (!cfg.reportModule) return `${base}?implementedOnly=1`;
  return `${base}?module=${encodeURIComponent(cfg.reportModule)}&implementedOnly=1`;
}
