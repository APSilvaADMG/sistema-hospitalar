/**
 * Layouts de impressão A4 adaptados dos repositórios open source integrados ao APSMedCore.
 * Cada relatório herda cabeçalho, rodapé e estilo visual do template de origem.
 */

export type ReportPrintLayoutKind =
  | 'sitrep-oms'
  | 'hospitalrun'
  | 'bayanno-sghc'
  | 'database-hospital'
  | 'regulatory-sus'
  | 'bi-managerial'
  | 'dev-queiroz'
  | 'default';

export type ReportPrintLayout = {
  kind: ReportPrintLayoutKind;
  /** Repositório Git de referência */
  sourceRepo: string;
  sourceUrl: string;
  sourceLabel: string;
  /** Título do tipo de documento no cabeçalho */
  documentType: string;
  showSignatureBlock: boolean;
  showChart: boolean;
};

const SITREP_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'R4EPI/sitrep',
  sourceUrl: 'https://github.com/R4EPI/sitrep',
  sourceLabel: 'R4EPI sitrep (OMS/MSF)',
  documentType: 'Relatório de Situação Epidemiológica',
  showSignatureBlock: true,
  showChart: true,
};

const HOSPITALRUN_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'HospitalRun/hospitalrun-frontend',
  sourceUrl: 'https://github.com/HospitalRun/hospitalrun-frontend',
  sourceLabel: 'HospitalRun HMS',
  documentType: 'Relatório Clínico-Operacional',
  showSignatureBlock: false,
  showChart: false,
};

const BAYANNO_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'Bayanno SGHC (PHP)',
  sourceUrl: 'https://github.com/bayanno',
  sourceLabel: 'Bayanno SGHC',
  documentType: 'Relatório Administrativo Hospitalar',
  showSignatureBlock: true,
  showChart: false,
};

const DATABASE_HOSPITAL_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'FabiolaCosta/DataBase-Hospital',
  sourceUrl: 'https://github.com/FabiolaCosta/DataBase-Hospital',
  sourceLabel: 'DataBase-Hospital',
  documentType: 'Relatório de Internação e Leitos',
  showSignatureBlock: true,
  showChart: false,
};

const REGULATORY_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'DATASUS / e-SUS',
  sourceUrl: 'https://datasus.saude.gov.br',
  sourceLabel: 'Regulatório SUS',
  documentType: 'Relatório Regulatório / Produção SUS',
  showSignatureBlock: true,
  showChart: false,
};

const BI_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'APSMedCore BI',
  sourceUrl: '',
  sourceLabel: 'Inteligência gerencial',
  documentType: 'Relatório Gerencial',
  showSignatureBlock: true,
  showChart: false,
};

const DEV_QUEIROZ_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'APSilvaADMG/sistema-hospitalar',
  sourceUrl: 'https://github.com/APSilvaADMG/sistema-hospitalar',
  sourceLabel: 'sistema-hospitalar (dev-queiroz)',
  documentType: 'Relatório de Pronto Atendimento / Triagem',
  showSignatureBlock: false,
  showChart: true,
};

const DEFAULT_LAYOUT: Omit<ReportPrintLayout, 'kind'> = {
  sourceRepo: 'APSMedCore',
  sourceUrl: '',
  sourceLabel: 'APSMedCore',
  documentType: 'Relatório Analítico',
  showSignatureBlock: false,
  showChart: false,
};

/** Códigos com gráfico de barras (curva epidemiológica / sitrep / ABC). */
export const REPORT_CHART_CODES = new Set([
  'ccih.epidemic.curve',
  'ccih.outbreak.indicators',
  'er.visits.by-triage',
  'er.wait.by-triage',
  'bi.appointments.monthly',
  'pharmacy.abc-curve',
  'supply.abc-curve',
]);

export function resolveReportPrintLayout(code: string): ReportPrintLayout {
  const prefix = code.split('.')[0] ?? code;

  if (code.startsWith('ccih.')) {
    return { kind: 'sitrep-oms', ...SITREP_LAYOUT, showChart: REPORT_CHART_CODES.has(code) };
  }
  if (prefix === 'lab' || prefix === 'pharmacy' || prefix === 'img' || prefix === 'supply') {
    return {
      kind: 'hospitalrun',
      ...HOSPITALRUN_LAYOUT,
      showChart: REPORT_CHART_CODES.has(code),
    };
  }
  if (code.startsWith('hosp.beds') || code.startsWith('admin.beds')) {
    return { kind: 'database-hospital', ...DATABASE_HOSPITAL_LAYOUT };
  }
  if (prefix === 'er') {
    return { kind: 'dev-queiroz', ...DEV_QUEIROZ_LAYOUT, showChart: REPORT_CHART_CODES.has(code) };
  }
  if (prefix === 'reg') {
    return { kind: 'regulatory-sus', ...REGULATORY_LAYOUT };
  }
  if (prefix === 'bi' || prefix === 'fin' || prefix === 'bill' || prefix === 'ins') {
    return { kind: 'bi-managerial', ...BI_LAYOUT };
  }
  if (
    prefix === 'admin'
    || prefix === 'reception'
    || prefix === 'nursing'
    || prefix === 'surgery'
    || prefix === 'hosp'
    || prefix === 'pep'
    || prefix === 'hr'
    || prefix === 'quality'
    || prefix === 'audit'
  ) {
    return { kind: 'bayanno-sghc', ...BAYANNO_LAYOUT };
  }

  return { kind: 'default', ...DEFAULT_LAYOUT };
}
