export type FeegowFaturamentoInsertSlug = 'consulta' | 'spsadt' | 'honorarios';

export const FEEGOW_FATURAMENTO_INSERT_GUIDE_TYPES: Record<FeegowFaturamentoInsertSlug, number> = {
  consulta: 1,
  spsadt: 2,
  honorarios: 5,
};

export const FEEGOW_FATURAMENTO_INSERT_PATHS: Record<FeegowFaturamentoInsertSlug, string> = {
  consulta: '/faturamento-tiss/inserir/consulta',
  spsadt: '/faturamento-tiss/inserir/spsadt',
  honorarios: '/faturamento-tiss/inserir/honorarios',
};

export type FeegowFaturamentoSideGroup = {
  id: string;
  label: string;
  items: { id: string; label: string; path: string }[];
};

export const FEEGOW_FATURAMENTO_SIDE_GROUPS: FeegowFaturamentoSideGroup[] = [
  {
    id: 'guias-tiss',
    label: 'Guias TISS',
    items: [
      { id: 'tiss-painel', label: 'Painel / Buscar guias', path: '/faturamento-tiss' },
      { id: 'tiss-nova-consulta', label: 'Nova guia — Consulta', path: FEEGOW_FATURAMENTO_INSERT_PATHS.consulta },
      { id: 'tiss-nova-spsadt', label: 'Nova guia — SP/SADT', path: FEEGOW_FATURAMENTO_INSERT_PATHS.spsadt },
      { id: 'tiss-nova-honorarios', label: 'Nova guia — Honorários', path: FEEGOW_FATURAMENTO_INSERT_PATHS.honorarios },
      { id: 'tiss-funi', label: 'Catálogo FUNI', path: '/faturamento-tiss/guias-funi' },
    ],
  },
  {
    id: 'lotes-fechamento',
    label: 'Lotes e fechamento',
    items: [
      { id: 'tiss-lotes', label: 'Administrar lotes', path: '/faturamento-tiss/lotes' },
      { id: 'tiss-fechamento', label: 'Fechar lote', path: '/faturamento-tiss/fechamento' },
      { id: 'tiss-glosas', label: 'Glosas', path: '/faturamento-tiss/glosas' },
      { id: 'tiss-recursos', label: 'Recursos de glosa', path: '/faturamento-tiss/recursos-glosa' },
    ],
  },
  {
    id: 'autorizacoes-convenios',
    label: 'Autorizações e convênios',
    items: [
      { id: 'tiss-autorizacoes', label: 'Autorizações TISS', path: '/faturamento-tiss/autorizacoes' },
      { id: 'tpa', label: 'TPA', path: '/convenios/tpa' },
      { id: 'convenios', label: 'Convênios', path: '/convenios' },
    ],
  },
  {
    id: 'sus',
    label: 'SUS',
    items: [
      { id: 'sus-painel', label: 'Painel SUS', path: '/faturamento' },
      { id: 'sus-aih', label: 'AIH (SUS)', path: '/faturamento/sus/aih' },
      { id: 'sus-apac', label: 'APAC (SUS)', path: '/faturamento/sus/apac' },
      { id: 'sus-bpa', label: 'BPA (SUS)', path: '/faturamento/sus/bpa' },
      { id: 'sus-prod-amb', label: 'Produção Ambulatorial', path: '/faturamento/sus/producao-ambulatorial' },
    ],
  },
];

export function buildFeegowFaturamentoTopChildren(): { label: string; path: string }[] {
  return [
    { label: 'Inserir Guia de Consulta', path: FEEGOW_FATURAMENTO_INSERT_PATHS.consulta },
    { label: 'Inserir Guia de SP/SADT', path: FEEGOW_FATURAMENTO_INSERT_PATHS.spsadt },
    { label: 'Inserir Guia de Honorários', path: FEEGOW_FATURAMENTO_INSERT_PATHS.honorarios },
    { label: 'Buscar Guias', path: '/faturamento-tiss' },
    { label: 'Fechar Lote', path: '/faturamento-tiss/fechamento' },
    { label: 'Administrar Lotes', path: '/faturamento-tiss/lotes' },
    { label: 'TPA', path: '/convenios/tpa' },
    { label: 'Painel SUS', path: '/faturamento' },
  ];
}

export function buildFeegowFaturamentoSideItems(): {
  id: string;
  label: string;
  path?: string;
  children?: { label: string; path: string }[];
}[] {
  return FEEGOW_FATURAMENTO_SIDE_GROUPS.map((group) => ({
    id: group.id,
    label: group.label,
    children: group.items.map((item) => ({ label: item.label, path: item.path })),
  }));
}

export function resolveTissInsertGuideType(section: string): number | null {
  if (section === 'inserir/consulta') return FEEGOW_FATURAMENTO_INSERT_GUIDE_TYPES.consulta;
  if (section === 'inserir/spsadt') return FEEGOW_FATURAMENTO_INSERT_GUIDE_TYPES.spsadt;
  if (section === 'inserir/honorarios') return FEEGOW_FATURAMENTO_INSERT_GUIDE_TYPES.honorarios;
  return null;
}

export function isFeegowFaturamentoRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  return (
    path.startsWith('/faturamento-tiss')
    || path.startsWith('/faturamento')
    || path.startsWith('/convenios')
  );
}
