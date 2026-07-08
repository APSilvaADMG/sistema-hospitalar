import type { NavIconName } from '../components/NavIcon';
import type { UserRoleName } from '../api/client';
import type { MenuMegaGroupId } from './types';

export type MenuProfileId =
  | 'reception'
  | 'doctor'
  | 'nursing'
  | 'billing'
  | 'pharmacy'
  | 'warehouse'
  | 'operations'
  | 'admin'
  | 'it'
  | 'default';

export type MenuProfileShortcut = {
  label: string;
  path: string;
  icon: NavIconName;
  end?: boolean;
};

export type MenuProfileConfig = {
  id: MenuProfileId;
  label: string;
  /** Mega-grupo aberto ao entrar na home (/). */
  homeMegaGroup: MenuMegaGroupId;
  /** Seção expandida na home, quando aplicável. */
  homeSectionId: string | null;
  /** Mega-grupos que ficam recolhidos até o usuário abrir ou navegar. */
  collapsedByDefault: MenuMegaGroupId[];
  shortcuts: MenuProfileShortcut[];
};

const RECEPTION_SHORTCUTS: MenuProfileShortcut[] = [
  { label: 'Recepção', path: '/recepcao', icon: 'users', end: true },
  { label: 'Pacientes', path: '/recepcao/pacientes', icon: 'users' },
  { label: 'Agendamentos', path: '/recepcao/agendamentos', icon: 'calendar' },
  { label: 'Check-in', path: '/recepcao/check-in', icon: 'calendar' },
];

const DOCTOR_SHORTCUTS: MenuProfileShortcut[] = [
  { label: 'Evolução', path: '/pep/evolucao-medica', icon: 'stethoscope' },
  { label: 'Prescrição', path: '/pep/prescricao', icon: 'pill' },
  { label: 'Pronto Socorro', path: '/emergencia/atendimento-medico', icon: 'siren' },
  { label: 'Internação', path: '/internacao/admissao', icon: 'bed' },
];

const NURSING_SHORTCUTS: MenuProfileShortcut[] = [
  { label: 'Leito (PoC)', path: '/enfermagem/leito', icon: 'bed' },
  { label: 'Medicamentos', path: '/enfermagem/medicamentos', icon: 'pill' },
  { label: 'Sinais Vitais', path: '/enfermagem/sinais-vitais', icon: 'activity' },
  { label: 'Evolução Enf.', path: '/enfermagem/sae/evolucao', icon: 'clipboard' },
  { label: 'Leitos', path: '/internacao/leitos', icon: 'bed' },
];

const BILLING_SHORTCUTS: MenuProfileShortcut[] = [
  { label: 'Faturamento', path: '/faturamento', icon: 'wallet', end: true },
  { label: 'Guias', path: '/guias', icon: 'file-text', end: true },
  { label: 'Guias TISS', path: '/faturamento-tiss', icon: 'wallet' },
  { label: 'Saída', path: '/acesso-fisico', icon: 'lock' },
];

const PHARMACY_SHORTCUTS: MenuProfileShortcut[] = [
  { label: 'Dispensação', path: '/farmacia', icon: 'pill', end: true },
  { label: 'Solicitações', path: '/farmacia/solicitacoes', icon: 'clipboard' },
  { label: 'Estoque', path: '/farmacia/estoque', icon: 'boxes' },
];

const ADMIN_SHORTCUTS: MenuProfileShortcut[] = [
  { label: 'Dashboard', path: '/', icon: 'dashboard', end: true },
  { label: 'Indicadores', path: '/bi', icon: 'bar-chart' },
  { label: 'Recepção', path: '/recepcao', icon: 'users' },
  { label: 'Relatórios', path: '/relatorios', icon: 'file-text' },
];

const PROFILES: Record<MenuProfileId, MenuProfileConfig> = {
  reception: {
    id: 'reception',
    label: 'Recepção',
    homeMegaGroup: 'clinical',
    homeSectionId: 'entrada',
    collapsedByDefault: ['security', 'management'],
    shortcuts: RECEPTION_SHORTCUTS,
  },
  doctor: {
    id: 'doctor',
    label: 'Assistencial',
    homeMegaGroup: 'clinical',
    homeSectionId: 'pep',
    collapsedByDefault: ['security', 'management', 'administrative'],
    shortcuts: DOCTOR_SHORTCUTS,
  },
  nursing: {
    id: 'nursing',
    label: 'Enfermagem',
    homeMegaGroup: 'clinical',
    homeSectionId: 'enfermagem',
    collapsedByDefault: ['security', 'management', 'administrative'],
    shortcuts: NURSING_SHORTCUTS,
  },
  billing: {
    id: 'billing',
    label: 'Faturamento',
    homeMegaGroup: 'administrative',
    homeSectionId: 'fechamento',
    collapsedByDefault: ['security'],
    shortcuts: BILLING_SHORTCUTS,
  },
  pharmacy: {
    id: 'pharmacy',
    label: 'Farmácia',
    homeMegaGroup: 'diagnostic',
    homeSectionId: 'farmacia',
    collapsedByDefault: ['security', 'management'],
    shortcuts: PHARMACY_SHORTCUTS,
  },
  warehouse: {
    id: 'warehouse',
    label: 'Suprimentos',
    homeMegaGroup: 'administrative',
    homeSectionId: 'suprimentos',
    collapsedByDefault: ['security', 'clinical'],
    shortcuts: [
      { label: 'Requisições', path: '/estoque/requisicoes', icon: 'boxes' },
      { label: 'Compras', path: '/compras', icon: 'cart' },
      { label: 'Inventário', path: '/estoque', icon: 'boxes', end: true },
    ],
  },
  operations: {
    id: 'operations',
    label: 'Operações',
    homeMegaGroup: 'diagnostic',
    homeSectionId: 'operacional',
    collapsedByDefault: ['security', 'management', 'administrative'],
    shortcuts: [
      { label: 'Transportes', path: '/transportes', icon: 'ambulance', end: true },
      { label: 'Hotelaria', path: '/hotelaria', icon: 'building' },
      { label: 'PS', path: '/emergencia', icon: 'siren' },
    ],
  },
  admin: {
    id: 'admin',
    label: 'Gestão',
    homeMegaGroup: 'management',
    homeSectionId: 'dashboard',
    collapsedByDefault: [],
    shortcuts: ADMIN_SHORTCUTS,
  },
  it: {
    id: 'it',
    label: 'TI / Sistema',
    homeMegaGroup: 'security',
    homeSectionId: 'integracoes',
    collapsedByDefault: [],
    shortcuts: [
      { label: 'Integrações', path: '/integracoes', icon: 'plug', end: true },
      { label: 'Configurações', path: '/configuracoes/parametros', icon: 'wrench' },
      { label: 'LGPD', path: '/seguranca-lgpd', icon: 'shield' },
    ],
  },
  default: {
    id: 'default',
    label: 'Equipe',
    homeMegaGroup: 'clinical',
    homeSectionId: 'entrada',
    collapsedByDefault: ['security'],
    shortcuts: [
      { label: 'Início', path: '/', icon: 'dashboard', end: true },
      { label: 'Recepção', path: '/recepcao', icon: 'users' },
      { label: 'Ambulatório', path: '/ambulatorio', icon: 'calendar' },
    ],
  },
};

export function resolveMenuProfile(options: {
  role?: UserRoleName;
  isAdmin: boolean;
  isAdminOrReception: boolean;
  hasPermission: (...permissions: string[]) => boolean;
}): MenuProfileConfig {
  const { role, isAdmin, hasPermission } = options;

  if (isAdmin || role === 'Admin' || role === 'HospitalDirector') {
    return PROFILES.admin;
  }
  if (role === 'IT') return PROFILES.it;
  if (role === 'Reception') return PROFILES.reception;
  if (role === 'Doctor') return PROFILES.doctor;
  if (role === 'Nurse' || role === 'NursingTechnician') return PROFILES.nursing;
  if (role === 'Billing' || role === 'Insurance') return PROFILES.billing;
  if (role === 'Pharmacy') return PROFILES.pharmacy;
  if (role === 'Warehouse') return PROFILES.warehouse;
  if (role === 'Porter' || role === 'Hospitality') return PROFILES.operations;
  if (role === 'Auditor') {
    return {
      ...PROFILES.admin,
      label: 'Auditoria',
      homeMegaGroup: 'administrative',
      homeSectionId: 'fechamento',
      shortcuts: [
        { label: 'Auditoria', path: '/faturamento/auditoria/pre-faturamento', icon: 'clipboard' },
        { label: 'Faturamento', path: '/faturamento', icon: 'wallet', end: true },
        { label: 'Relatórios', path: '/relatorios', icon: 'file-text' },
      ],
    };
  }

  if (hasPermission('billing.write', 'billing.read')) return PROFILES.billing;
  if (hasPermission('pharmacy.dispense')) return PROFILES.pharmacy;
  if (hasPermission('pep.write', 'pep.read')) return PROFILES.doctor;

  return PROFILES.default;
}

export function isMenuHomePath(pathname: string): boolean {
  return pathname === '/' || pathname === '/dashboard/assistencial';
}
