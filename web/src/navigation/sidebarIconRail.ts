import type { NavIconName } from '../components/NavIcon';

export type SidebarIconRailItem = {
  path: string;
  icon: NavIconName;
  label: string;
  end?: boolean;
};

export const SIDEBAR_ICON_RAIL_ITEMS: SidebarIconRailItem[] = [
  { path: '/', icon: 'dashboard', label: 'Início', end: true },
  { path: '/recepcao', icon: 'users', label: 'Recepção' },
  { path: '/ambulatorio', icon: 'calendar', label: 'Ambulatório' },
  { path: '/emergencia', icon: 'siren', label: 'Pronto atendimento' },
  { path: '/pep', icon: 'stethoscope', label: 'PEP' },
  { path: '/internacao', icon: 'bed', label: 'Internação' },
  { path: '/faturamento', icon: 'wallet', label: 'Faturamento' },
  { path: '/relatorios', icon: 'file-text', label: 'Relatórios' },
  { path: '/guias', icon: 'file-text', label: 'Guias', end: true },
  { path: '/connect', icon: 'mail', label: 'Comunicação', end: true },
  { path: '/configuracoes', icon: 'wrench', label: 'Configurações' },
];
