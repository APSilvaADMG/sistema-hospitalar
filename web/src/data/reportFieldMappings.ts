/**
 * Rótulos em português brasileiro para relatórios (sincronizado com ReportFieldMappings.cs).
 */

export type ReportFieldMapping = {
  sourceRepo: string;
  sourceTemplate: string;
  printSubtitle?: string;
  documentType?: string;
  sectionTitles?: Partial<Record<'summary' | 'chart' | 'data', string>>;
  columnLabels: Record<string, string>;
  kpiLabels?: Record<string, string>;
  metaLabels?: Record<string, string>;
};

const COMMON_COLUMN_LABELS: Record<string, string> = {
  patient: 'Paciente',
  name: 'Nome',
  cpf: 'CPF',
  cns: 'CNS',
  date: 'Data',
  count: 'Quantidade (n)',
  doctor: 'Profissional',
  professional: 'Profissional',
  specialty: 'Especialidade',
  urgency: 'Nível de gravidade',
  complaint: 'Queixa principal',
  arrivedAt: 'Chegada',
  admittedAt: 'Internação',
  dischargedAt: 'Alta',
  diagnosis: 'Diagnóstico',
  cid: 'CID-10',
  procedure: 'Procedimento',
  product: 'Produto',
  sku: 'SKU',
  quantity: 'Quantidade (n)',
  ward: 'Ala',
  bed: 'Leito',
  status: 'Situação',
  indicator: 'Indicador',
  value: 'Valor',
  week: 'Semana epidemiológica',
  cases: 'Casos (n)',
  deaths: 'Óbitos (n)',
  population: 'População em risco',
  arPer10000: 'TA (por 10.000)',
  insurance: 'Convênio',
  amount: 'Valor (R$)',
  label: 'Descrição',
};

type PrefixMapping = Pick<ReportFieldMapping, 'sourceRepo' | 'sourceTemplate' | 'documentType'> & {
  columnOverrides?: Record<string, string>;
};

const PREFIX_MAP: Record<string, PrefixMapping> = {
  'ccih.': {
    sourceRepo: 'R4EPI/sitrep',
    sourceTemplate: 'inst/rmarkdown/templates/',
    documentType: 'Relatório de Situação — CCIH',
  },
  'er.': {
    sourceRepo: 'APSilvaADMG/sistema-hospitalar',
    sourceTemplate: 'src/utils/pdfCompiler.ts',
    documentType: 'Relatório operacional — Pronto Socorro',
    columnOverrides: {
      urgency: 'Nível de gravidade (Manchester)',
      complaint: 'Queixa principal',
      arrivedAt: 'Data/hora de chegada',
    },
  },
  'lab.': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'src/labs/',
    documentType: 'Relatório Laboratorial',
  },
  'pharmacy.': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'src/medications/',
    documentType: 'Relatório de Farmácia',
  },
  'img.': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'src/imagings/',
    documentType: 'Relatório de Diagnóstico por Imagem',
  },
  'supply.': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'src/inventory/',
    documentType: 'Relatório de Almoxarifado',
  },
  'hosp.': {
    sourceRepo: 'FabiolaCosta/DataBase-Hospital',
    sourceTemplate: 'SQL schema — internacao/leito',
    documentType: 'Relatório de Internação',
  },
  'admin.': {
    sourceRepo: 'Bayanno SGHC',
    sourceTemplate: 'reports/administrative',
    documentType: 'Relatório Administrativo Hospitalar',
  },
  'reception.': {
    sourceRepo: 'Bayanno SGHC',
    sourceTemplate: 'reports/reception',
    documentType: 'Relatório de Recepção',
  },
  'pep.': {
    sourceRepo: 'APSilvaADMG/sistema-hospitalar',
    sourceTemplate: 'src/utils/pdfCompiler.ts',
    documentType: 'Prontuário Médico — Informações Gerais',
  },
  'nursing.': {
    sourceRepo: 'Bayanno SGHC',
    sourceTemplate: 'reports/nursing',
    documentType: 'Relatório de Enfermagem',
  },
  'surgery.': {
    sourceRepo: 'Bayanno SGHC',
    sourceTemplate: 'reports/surgery',
    documentType: 'Relatório Cirúrgico',
  },
  'fin.': {
    sourceRepo: 'APSMedCore BI',
    sourceTemplate: 'reports/financial',
    documentType: 'Relatório Gerencial — Financeiro',
  },
  'bi.': {
    sourceRepo: 'APSMedCore BI',
    sourceTemplate: 'reports/bi',
    documentType: 'Relatório Gerencial — BI',
  },
  'ins.': {
    sourceRepo: 'APSMedCore',
    sourceTemplate: 'reports/tiss',
    documentType: 'Relatório TISS / Convênios',
  },
  'bill.': {
    sourceRepo: 'APSMedCore',
    sourceTemplate: 'reports/billing',
    documentType: 'Faturamento Hospitalar',
  },
  'reg.': {
    sourceRepo: 'DATASUS/SIA-SUS',
    sourceTemplate: 'Exportação regulatória',
    documentType: 'Relatório Regulatório SUS',
  },
  'quality.': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'src/incidents/',
    documentType: 'Relatório de Qualidade e Segurança',
  },
  'audit.': {
    sourceRepo: 'APSMedCore',
    sourceTemplate: 'reports/audit',
    documentType: 'Relatório de Auditoria',
  },
  'hr.': {
    sourceRepo: 'Bayanno SGHC',
    sourceTemplate: 'reports/hr',
    documentType: 'Recursos Humanos',
  },
};

const EXACT_MAP: Record<string, ReportFieldMapping> = {
  'ccih.epidemic.curve': {
    sourceRepo: 'R4EPI/sitrep',
    sourceTemplate: 'inst/rmarkdown/templates/measles_outbreak/skeleton/skeleton.Rmd',
    printSubtitle: 'Casos por semana de início',
    documentType: 'Relatório de Situação — Curva epidêmica',
    sectionTitles: { chart: 'Casos por semana de início', data: 'Taxa de ataque por semana epidemiológica' },
    columnLabels: {
      week: 'Semana epidemiológica',
      cases: 'Casos (n)',
      population: 'População em risco',
      arPer10000: 'TA (por 10.000)',
    },
  },
  'ccih.mortality.surveillance': {
    sourceRepo: 'R4EPI/sitrep',
    sourceTemplate: 'inst/rmarkdown/templates/mortality/skeleton/skeleton.Rmd',
    printSubtitle: 'Vigilância de mortalidade',
    documentType: 'Relatório de Situação — Mortalidade',
    columnLabels: { week: 'Semana epidemiológica', deaths: 'Óbitos (n)' },
  },
  'ccih.vaccination.coverage': {
    sourceRepo: 'R4EPI/sitrep',
    sourceTemplate: 'inst/rmarkdown/templates/vaccination_long/skeleton/skeleton.Rmd',
    printSubtitle: 'Cobertura vacinal',
    documentType: 'Relatório de Situação — Vacinação',
    columnLabels: { vaccine: 'Imunobiológico', doses: 'Doses aplicadas (n)' },
  },
  'ccih.outbreak.indicators': {
    sourceRepo: 'APSilvaADMG/sistema-hospitalar',
    sourceTemplate: 'src/modulo/ia/service/GroqService.ts',
    printSubtitle: 'Indicadores de surto',
    documentType: 'Relatório IA — Surto respiratório',
    sectionTitles: { summary: 'Resumo executivo', data: 'Indicadores agregados' },
    columnLabels: { indicator: 'Indicador', value: 'Valor' },
  },
  'er.visits.by-triage': {
    sourceRepo: 'APSilvaADMG/sistema-hospitalar',
    sourceTemplate: 'src/modulo/triagem/model/Triagem.ts',
    printSubtitle: 'Distribuição por nível de gravidade',
    documentType: 'Relatório operacional — Triagens PS',
    columnLabels: { urgency: 'Nível de gravidade', count: 'Triagens (n)' },
  },
  'er.wait.by-triage': {
    sourceRepo: 'APSilvaADMG/sistema-hospitalar',
    sourceTemplate: 'src/modulo/triagem/service/TriagemService.ts',
    documentType: 'Relatório operacional — Fila PS',
    columnLabels: { urgency: 'Nível de gravidade', avgMinutes: 'Tempo médio de espera (min)' },
  },
  'er.patients.served': {
    sourceRepo: 'APSilvaADMG/sistema-hospitalar',
    sourceTemplate: 'src/utils/pdfCompiler.ts — Triagens Associadas',
    printSubtitle: 'Pacientes atendidos no PS',
    documentType: 'Relatório clínico — PS',
    columnLabels: {
      arrivedAt: 'Data de chegada',
      patient: 'Paciente',
      urgency: 'Gravidade',
      status: 'Situação do atendimento',
    },
  },
  'reg.ciha': {
    sourceRepo: 'DATASUS/SIA-SUS',
    sourceTemplate: 'Produção CIHA',
    documentType: 'Relatório Regulatório — CIHA',
    columnLabels: {
      date: 'Data do atendimento',
      patient: 'Paciente',
      cns: 'CNS',
      procedure: 'Procedimento',
      protocol: 'Protocolo / Máquina',
      cycle: 'Ciclo',
      status: 'Situação',
      sigtap: 'Código SIGTAP',
    },
  },
  'reg.apac': {
    sourceRepo: 'DATASUS/SIA-SUS',
    sourceTemplate: 'Autorização APAC',
    documentType: 'Relatório Regulatório — APAC',
    columnLabels: {
      apac: 'Nº APAC',
      date: 'Data',
      patient: 'Paciente',
      cns: 'CNS',
      procedure: 'Procedimento SIGTAP',
      label: 'Descrição do procedimento',
      cid: 'CID-10',
      professional: 'Profissional responsável',
      validity: 'Validade',
      status: 'Situação',
    },
  },
  'reg.bpa': {
    sourceRepo: 'DATASUS/SIA-SUS',
    sourceTemplate: 'Boletim de Produção Ambulatorial',
    documentType: 'Relatório Regulatório — BPA',
    columnLabels: {
      date: 'Data',
      patient: 'Paciente',
      cns: 'CNS',
      professional: 'Profissional',
      specialty: 'Especialidade (CBO)',
      procedure: 'Procedimento SIGTAP',
    },
  },
  'reg.aih': {
    sourceRepo: 'DATASUS/SIH-SUS',
    sourceTemplate: 'Autorização de Internação Hospitalar',
    documentType: 'Relatório Regulatório — AIH',
    columnLabels: {
      admitted: 'Data internação',
      patient: 'Paciente',
      cns: 'CNS',
      aih: 'Nº AIH',
      cid: 'CID-10',
      procedure: 'SIGTAP',
      competence: 'Competência',
      ward: 'Ala / Setor',
    },
  },
  'reg.compulsory-notifications': {
    sourceRepo: 'R4EPI/sitrep',
    sourceTemplate: 'OpenHospital notification diseases',
    documentType: 'Relatório Regulatório — NOTIFICA',
    columnLabels: {
      code: 'Código',
      disease: 'Doença / Agravos',
      cases: 'Suspeitas/registros (n)',
      lastCase: 'Último registro',
    },
  },
  'pharmacy.abc-curve': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'inventory ABC',
    documentType: 'Relatório gerencial — Curva ABC',
    sectionTitles: { chart: 'Distribuição por valor (curva ABC)', data: 'Classificação detalhada' },
    columnLabels: {
      product: 'Produto',
      sku: 'SKU',
      qty: 'Quantidade consumida',
      value: 'Valor (R$)',
      pct: '% individual',
      cumulative: '% acumulado',
      class: 'Classe ABC',
    },
  },
  'supply.abc-curve': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'inventory ABC',
    documentType: 'Relatório gerencial — Curva ABC',
    columnLabels: {
      product: 'Produto',
      sku: 'SKU',
      qty: 'Quantidade consumida',
      value: 'Valor (R$)',
      pct: '% individual',
      cumulative: '% acumulado',
      class: 'Classe ABC',
    },
  },
  'quality.adverse-events': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'src/incidents/',
    documentType: 'Relatório de Segurança — Eventos adversos',
    columnLabels: {
      date: 'Data do evento',
      type: 'Tipo de incidente',
      location: 'Local',
      severity: 'Gravidade',
      description: 'Descrição',
    },
  },
  'quality.patient-falls': {
    sourceRepo: 'HospitalRun/hospitalrun-frontend',
    sourceTemplate: 'Patient fall incidents',
    documentType: 'Relatório de Segurança — Quedas',
    columnLabels: {
      date: 'Data',
      patient: 'Paciente',
      location: 'Local',
      severity: 'Gravidade',
      description: 'Descrição',
    },
  },
};

function findPrefix(code: string): PrefixMapping | undefined {
  const entry = Object.entries(PREFIX_MAP).find(([prefix]) => code.startsWith(prefix));
  return entry?.[1];
}

export function getReportFieldMapping(code: string): ReportFieldMapping | undefined {
  const exact = EXACT_MAP[code];
  if (exact) return exact;

  const prefix = findPrefix(code);
  if (!prefix) return undefined;

  return {
    sourceRepo: prefix.sourceRepo,
    sourceTemplate: prefix.sourceTemplate,
    documentType: prefix.documentType,
    columnLabels: prefix.columnOverrides ?? {},
  };
}

export function resolveColumnLabel(code: string, key: string, fallback: string): string {
  const exact = EXACT_MAP[code]?.columnLabels[key];
  if (exact) return exact;

  const prefix = findPrefix(code)?.columnOverrides?.[key];
  if (prefix) return prefix;

  return COMMON_COLUMN_LABELS[key] ?? fallback;
}

export function resolveSectionTitle(
  code: string,
  section: 'summary' | 'chart' | 'data',
  fallback: string,
): string {
  return EXACT_MAP[code]?.sectionTitles?.[section] ?? fallback;
}

export function resolveKpiLabel(code: string, label: string): string {
  return EXACT_MAP[code]?.kpiLabels?.[label] ?? label;
}
