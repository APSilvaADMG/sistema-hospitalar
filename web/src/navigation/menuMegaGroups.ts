import type { NavIconName } from '../components/NavIcon';

import type { MenuMegaGroupId } from './types';



export type MenuMegaGroupMeta = {

  id: MenuMegaGroupId;

  title: string;

  subtitle: string;

  color: string;

  accent: string;

  icon: NavIconName;

  order: number;

};



/** Macro-grupos alinhados ao fluxo hospitalar e às funções de apoio. */

export const MENU_MEGA_GROUPS: MenuMegaGroupMeta[] = [

  {

    id: 'management',

    title: 'Visão Geral',

    subtitle: 'Dashboard · Indicadores · Alertas',

    color: '#546e7a',

    accent: '#eceff1',

    icon: 'dashboard',

    order: 1,

  },

  {

    id: 'clinical',

    title: 'Jornada do Paciente',

    subtitle: 'Entrada → Atendimento → Internação → Alta',

    color: '#1565c0',

    accent: '#e3f2fd',

    icon: 'heart-pulse',

    order: 2,

  },

  {

    id: 'diagnostic',

    title: 'Apoio ao Atendimento',

    subtitle: 'Exames · Farmácia · Nutrição · Logística',

    color: '#2e7d32',

    accent: '#e8f5e9',

    icon: 'flask',

    order: 3,

  },

  {

    id: 'administrative',

    title: 'Administrativo',

    subtitle: 'Faturamento · Financeiro · Suprimentos · RH',

    color: '#f9a825',

    accent: '#fff8e1',

    icon: 'briefcase',

    order: 4,

  },

  {

    id: 'security',

    title: 'Sistema',

    subtitle: 'Integrações · Segurança · LGPD · Configurações',

    color: '#c62828',

    accent: '#ffebee',

    icon: 'shield',

    order: 5,

  },

];



export function getMegaGroupMeta(id: MenuMegaGroupId): MenuMegaGroupMeta {

  return MENU_MEGA_GROUPS.find((g) => g.id === id) ?? MENU_MEGA_GROUPS[0];

}



/** Permissões mínimas para exibir uma seção inteira do menu (qualquer uma basta). */

export const SECTION_PERMISSIONS: Partial<Record<string, string[]>> = {

  pep: ['pep.read'],

  enfermagem: ['pep.read'],

  farmacia: ['pharmacy.dispense', 'warehouse.manage'],

  suprimentos: ['warehouse.manage'],

  laboratorio: ['pep.read', 'pep.write'],

  imagem: ['pep.read', 'pep.write'],

  fechamento: ['billing.read', 'billing.write'],

  guias: ['billing.read'],

  comunicacao: ['connect.read'],

  financeiro: ['billing.read'],

  rh: ['reports.read', 'patients.create'],

  'seguranca-lgpd': ['audit.read', 'security.manage', 'lgpd.manage', 'lgpd.consent.manage', 'lgpd.subject_requests', 'incidents.manage'],

  operacional: ['transport.operate', 'cleaning.operate', 'transport.manage', 'cleaning.manage'],

  gestao: ['reports.read'],

  relatorios: ['reports.read'],

  ccih: ['pep.read'],

};


