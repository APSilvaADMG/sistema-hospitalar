export type FeegowPatientNavItem = {
  id: string;
  label: string;
  badge?: string;
};

export const FEEGOW_PATIENT_NAV: FeegowPatientNavItem[] = [
  { id: 'resumos-ia', label: 'Resumos de IA', badge: 'Novo' },
  { id: 'dados-principais', label: 'Dados Principais' },
  { id: 'anamnese', label: 'Anamnese e Evoluções' },
  { id: 'laudos', label: 'Laudos e Formulários' },
  { id: 'diagnosticos', label: 'Diagnósticos' },
  { id: 'encaminhamentos', label: 'Encaminhamentos' },
  { id: 'prescricoes', label: 'Prescrições' },
  { id: 'textos', label: 'Textos e Atestados' },
  { id: 'tarefas', label: 'Tarefas' },
  { id: 'exames', label: 'Pedidos de Exame' },
  { id: 'vacinas', label: 'Vacinas' },
  { id: 'produtos', label: 'Produtos Utilizados' },
  { id: 'timeline', label: 'Linha do tempo' },
  { id: 'imagens', label: 'Imagens' },
  { id: 'arquivos', label: 'Arquivos' },
  { id: 'agendamentos', label: 'Agendamentos' },
  { id: 'recibos', label: 'Recibos' },
  { id: 'propostas', label: 'Propostas' },
];

const NAV_BY_ID = new Map(FEEGOW_PATIENT_NAV.map((item) => [item.id, item]));

export function feegowPatientNavLabel(sectionId: string): string {
  return NAV_BY_ID.get(sectionId)?.label ?? sectionId;
}

export function isFeegowPatientSectionId(value: string): boolean {
  return NAV_BY_ID.has(value);
}

export function feegowPatientRecordPath(patientId: string, sectionId: string): string {
  return `/recepcao/pacientes/${patientId}/${sectionId}`;
}

export function feegowPatientInsertPath(sectionId = 'dados-principais'): string {
  return sectionId === 'dados-principais'
    ? '/recepcao/pacientes/inserir'
    : `/recepcao/pacientes/inserir/${sectionId}`;
}

export type FeegowPatientListFilter = 'active' | 'inactive' | 'chart-search';

export function feegowPatientListPath(filter: FeegowPatientListFilter = 'active'): string {
  if (filter === 'inactive') return '/recepcao/pacientes/listar/inativos';
  if (filter === 'chart-search') return '/recepcao/pacientes/listar/prontuario';
  return '/recepcao/pacientes/listar';
}

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export type FeegowPatientRoute =
  | { mode: 'list'; listFilter: FeegowPatientListFilter }
  | { mode: 'insert'; section: string }
  | { mode: 'record'; patientId: string; section: string };

export function parseFeegowPatientRoute(pathname: string): FeegowPatientRoute | null {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  const base = '/recepcao/pacientes';

  if (!path.startsWith(base)) return null;

  const rest = path.slice(base.length).replace(/^\//, '');
  if (!rest) return null;

  if (rest === 'listar') return { mode: 'list', listFilter: 'active' };
  if (rest === 'listar/inativos') return { mode: 'list', listFilter: 'inactive' };
  if (rest === 'listar/prontuario') return { mode: 'list', listFilter: 'chart-search' };

  if (rest === 'inserir') return { mode: 'insert', section: 'dados-principais' };
  if (rest.startsWith('inserir/')) {
    const section = rest.slice('inserir/'.length);
    return { mode: 'insert', section: isFeegowPatientSectionId(section) ? section : 'dados-principais' };
  }

  const parts = rest.split('/');
  const [first, second, third] = parts;
  if (UUID_RE.test(first)) {
    const sectionCandidate = second === 'prontuario' ? third : second;
    const section = sectionCandidate && isFeegowPatientSectionId(sectionCandidate)
      ? sectionCandidate
      : 'dados-principais';
    return { mode: 'record', patientId: first, section };
  }

  return null;
}
