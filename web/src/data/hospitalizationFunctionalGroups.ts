/** Grupos funcionais do hub de Internação. */

export type HospitalizationQuickLink = {
  label: string;
  path: string;
  description?: string;
};

export type HospitalizationFunctionalGroup = {
  id: string;
  slug: string;
  label: string;
  description: string;
  quickLinks?: HospitalizationQuickLink[];
};

export const HOSPITALIZATION_FUNCTIONAL_GROUPS: HospitalizationFunctionalGroup[] = [
  {
    id: 'ativas',
    slug: 'ativas',
    label: 'Internações ativas',
    description: 'Pacientes internados no momento, por ala e modalidade',
    quickLinks: [
      { label: 'Mapa de leitos', path: '/internacao/leitos' },
      { label: 'UTI', path: '/uti' },
    ],
  },
  {
    id: 'admissao',
    slug: 'admissao',
    label: 'Admissão',
    description: 'Solicitações aprovadas e admissões recentes',
    quickLinks: [
      { label: 'Fluxo de admissão', path: '/internacao/admissao' },
      { label: 'Emergência', path: '/emergencia' },
    ],
  },
  {
    id: 'solicitacoes',
    slug: 'solicitacoes',
    label: 'Solicitações',
    description: 'Pedidos pendentes e aguardando efetivação',
    quickLinks: [
      { label: 'Nova solicitação', path: '/internacao/admissao' },
      { label: 'Regulação', path: '/regulacao' },
    ],
  },
  {
    id: 'sus-aih',
    slug: 'sus-aih',
    label: 'SUS / AIH',
    description: 'Internações SUS com dados de AIH e faturamento',
    quickLinks: [
      { label: 'AIH — Faturamento SUS', path: '/faturamento/sus/aih' },
      { label: 'Integrações SIH', path: '/integracoes-gov/sih' },
      { label: 'Guias de internação', path: '/guias/internacao' },
    ],
  },
  {
    id: 'transferencias',
    slug: 'transferencias',
    label: 'Transferências',
    description: 'Movimentações entre leitos e alas',
    quickLinks: [
      { label: 'Transferências', path: '/internacao/transferencias' },
      { label: 'Transporte interno', path: '/transportes' },
    ],
  },
  {
    id: 'altas',
    slug: 'altas',
    label: 'Altas',
    description: 'Altas hospitalares no período selecionado',
    quickLinks: [
      { label: 'Altas', path: '/internacao/altas' },
      { label: 'Hotelaria', path: '/hotelaria' },
    ],
  },
  {
    id: 'obitos',
    slug: 'obitos',
    label: 'Óbitos',
    description: 'Registros de óbito durante internação',
    quickLinks: [
      { label: 'Óbitos', path: '/internacao/obitos' },
      { label: 'Relatório óbitos', path: '/relatorios/internacao' },
    ],
  },
];

const GROUP_BY_SLUG = Object.fromEntries(
  HOSPITALIZATION_FUNCTIONAL_GROUPS.map((g) => [g.slug, g]),
) as Record<string, HospitalizationFunctionalGroup>;

export function getHospitalizationGroupBySlug(
  slug: string | undefined,
): HospitalizationFunctionalGroup | undefined {
  if (!slug) return undefined;
  return GROUP_BY_SLUG[slug];
}

export const HOSPITALIZATION_STATUS_OPTIONS = [
  { value: '', label: 'Todos' },
  { value: '1', label: 'Ativa' },
  { value: '2', label: 'Alta' },
  { value: '3', label: 'Transferida' },
] as const;

export const HOSPITALIZATION_MODALITY_OPTIONS = [
  { value: '', label: 'Todas' },
  { value: '1', label: 'Particular' },
  { value: '2', label: 'Convênio' },
  { value: '3', label: 'SUS' },
  { value: '4', label: 'Mista' },
] as const;
