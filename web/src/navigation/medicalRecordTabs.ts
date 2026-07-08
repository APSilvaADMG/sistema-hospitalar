export type MedicalRecordTab =
  | 'summary'
  | 'clinical'
  | 'care'
  | 'hospitalization'
  | 'tiss'
  | 'comunicacao';

const SLUG_TO_TAB: Record<string, MedicalRecordTab> = {
  resumo: 'summary',
  clinico: 'clinical',
  cuidados: 'care',
  internacao: 'hospitalization',
  tiss: 'tiss',
  comunicacao: 'comunicacao',
};

const TAB_TO_SLUG: Record<MedicalRecordTab, string> = {
  summary: 'resumo',
  clinical: 'clinico',
  care: 'cuidados',
  hospitalization: 'internacao',
  tiss: 'tiss',
  comunicacao: 'comunicacao',
};

export function medicalRecordTabFromSlug(slug?: string): MedicalRecordTab {
  if (!slug) return 'summary';
  return SLUG_TO_TAB[slug] ?? 'summary';
}

export function medicalRecordSlugFromTab(tab: MedicalRecordTab): string {
  return TAB_TO_SLUG[tab];
}

export const medicalRecordTabLabels: { tab: MedicalRecordTab; label: string }[] = [
  { tab: 'summary', label: 'Resumo' },
  { tab: 'clinical', label: 'Clínico' },
  { tab: 'care', label: 'Cuidados' },
  { tab: 'hospitalization', label: 'Intern.' },
  { tab: 'tiss', label: 'TISS' },
  { tab: 'comunicacao', label: 'Comunicação' },
];
