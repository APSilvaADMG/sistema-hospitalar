export type FeegowRhSection =
  | 'colaboradores'
  | 'folha'
  | 'escalas'
  | 'plantoes'
  | 'ferias'
  | 'treinamentos'
  | 'avaliacoes';

export const FEEGOW_RH_NAV_ITEMS: { id: FeegowRhSection; label: string; path: string }[] = [
  { id: 'colaboradores', label: 'Colaboradores', path: '/rh' },
  { id: 'folha', label: 'Folha de pagamento', path: '/rh/folha' },
  { id: 'escalas', label: 'Escalas', path: '/rh/escalas' },
  { id: 'plantoes', label: 'Plantões', path: '/rh/plantoes' },
  { id: 'ferias', label: 'Férias', path: '/rh/ferias' },
  { id: 'treinamentos', label: 'Treinamentos', path: '/rh/treinamentos' },
  { id: 'avaliacoes', label: 'Avaliações', path: '/rh/avaliacoes' },
];

export const FEEGOW_RH_SECTION_TITLES: Record<FeegowRhSection, string> = {
  colaboradores: 'Colaboradores',
  folha: 'Folha de pagamento',
  escalas: 'Escalas',
  plantoes: 'Plantões',
  ferias: 'Férias',
  treinamentos: 'Treinamentos',
  avaliacoes: 'Avaliações',
};

export function resolveFeegowRhSection(pathname: string): FeegowRhSection {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/rh';
  if (path === '/rh/folha') return 'folha';
  if (path === '/rh/escalas') return 'escalas';
  if (path === '/rh/plantoes') return 'plantoes';
  if (path === '/rh/ferias') return 'ferias';
  if (path === '/rh/treinamentos') return 'treinamentos';
  if (path === '/rh/avaliacoes') return 'avaliacoes';
  return 'colaboradores';
}

export function isFeegowRhRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  return path === '/rh' || path.startsWith('/rh/');
}

export function buildFeegowRhTopChildren(): { label: string; path: string }[] {
  return FEEGOW_RH_NAV_ITEMS.map((item) => ({ label: item.label, path: item.path }));
}

export function buildFeegowRhSideItems(): { id: string; label: string; path: string }[] {
  return FEEGOW_RH_NAV_ITEMS.map((item) => ({
    id: item.id,
    label: item.label,
    path: item.path,
  }));
}
