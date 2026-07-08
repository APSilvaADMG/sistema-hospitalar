/** Menu superior e lateral contextual — espelhado dos prints Feegow Clinic. */

import { buildFeegowConnectChildren, buildFeegowConnectSideItems } from './connectMenu';
import { buildFeegowGuiasChildren, buildFeegowGuiasSideItems } from './guiasMenu';
import {
  buildFeegowFaturamentoSideItems,
  buildFeegowFaturamentoTopChildren,
} from '../components/feegow/faturamento/feegowFaturamentoNav';
import { buildFeegowRhSideItems, buildFeegowRhTopChildren } from '../components/feegow/rh/feegowRhNav';

export type FeegowTopItem = {
  id: string;
  label: string;
  path?: string;
  children?: { label: string; path: string; badge?: string }[];
};

export type FeegowSideItem = {
  id: string;
  label: string;
  path?: string;
  badge?: string;
  children?: { label: string; path: string }[];
};

export type FeegowSidePanel = {
  sectionLabel?: string;
  items: FeegowSideItem[];
  /** Bloco de local de atendimento (Sala de Espera, Agenda). */
  locationBlock?: boolean;
  /** Botões promocionais do dashboard. */
  promoButtons?: boolean;
};

export const FEEGOW_TOP_MENU: FeegowTopItem[] = [
  {
    id: 'agenda',
    label: 'Agenda',
    children: [
      { label: 'Diária', path: '/recepcao/agendamentos' },
      { label: 'Semanal', path: '/recepcao/agendamentos/semanal' },
      { label: 'Múltipla', path: '/recepcao/agendamentos/multipla' },
      { label: 'Check-in', path: '/recepcao/agendamentos/check-in' },
      { label: 'Confirmar agendamentos', path: '/recepcao/agendamentos/confirmar' },
      { label: 'Equipamentos Alocados', path: '/recepcao/agendamentos/equipamentos' },
      { label: 'Mapa de agenda', path: '/recepcao/agendamentos/mapa' },
    ],
  },
  {
    id: 'espera',
    label: 'Espera',
    path: '/emergencia',
    children: [{ label: 'Sala de Espera', path: '/emergencia' }],
  },
  {
    id: 'pacientes',
    label: 'Pacientes',
    children: [
      { label: 'Inserir', path: '/recepcao/pacientes/inserir' },
      { label: 'Listar', path: '/recepcao/pacientes/listar' },
      { label: 'Vacinação', path: '/recepcao/vacinacao' },
    ],
  },
  {
    id: 'estoque',
    label: 'Estoque',
    children: [
      { label: 'Inserir', path: '/estoque/inserir?tipo=geral' },
      { label: 'Listar', path: '/estoque/listar?tipo=geral' },
      { label: 'Dashboard Almoxarifado', path: '/estoque/dashboard' },
      { label: 'Entrada NF', path: '/estoque/entrada' },
      { label: 'Saída', path: '/estoque/saida' },
      { label: 'Kits de Produtos', path: '/estoque/kits' },
      { label: 'Medicamento por Convênio', path: '/estoque/config/medicamento-convenio' },
      { label: 'Farmácia por Ala', path: '/estoque/farmacia-ala' },
      { label: 'Requisição de estoque', path: '/estoque/requisicoes', badge: 'Novo' },
    ],
  },
  {
    id: 'operacoes',
    label: 'Operações',
    path: '/hotelaria',
    children: [
      { label: 'Hotelaria (NOC)', path: '/hotelaria' },
      { label: 'Resíduos e Coleta', path: '/residuos' },
      { label: 'Lavanderia', path: '/lavanderia' },
      { label: 'Transportes internos', path: '/transportes' },
    ],
  },
  {
    id: 'rh',
    label: 'RH',
    path: '/rh',
    children: buildFeegowRhTopChildren(),
  },
  {
    id: 'financeiro',
    label: 'Financeiro',
    path: '/financeiro',
    children: [
      { label: 'Visão Geral', path: '/financeiro' },
      { label: 'Honorários', path: '/financeiro/honorarios' },
      { label: 'Propostas', path: '/financeiro/propostas' },
      { label: 'Downloads', path: '/relatorios/downloads' },
    ],
  },
  {
    id: 'faturamento',
    label: 'Faturamento',
    path: '/faturamento-tiss',
    children: buildFeegowFaturamentoTopChildren(),
  },
  {
    id: 'relatorios',
    label: 'Relatórios',
    path: '/relatorios',
  },
  {
    id: 'guias',
    label: 'Guias',
    path: '/guias',
    children: buildFeegowGuiasChildren(),
  },
  {
    id: 'comunicacao',
    label: 'Comunicação',
    path: '/connect',
    children: buildFeegowConnectChildren(),
  },
];

const OPERACOES_SIDE: FeegowSidePanel = {
  sectionLabel: 'OPERAÇÕES',
  items: [
    { id: 'hotelaria', label: 'Hotelaria (NOC)', path: '/hotelaria' },
    { id: 'residuos', label: 'Resíduos e Coleta', path: '/residuos' },
    { id: 'lavanderia', label: 'Lavanderia', path: '/lavanderia' },
    { id: 'transportes', label: 'Transportes internos', path: '/transportes' },
    { id: 'ambulancias', label: 'Ambulâncias', path: '/ambulancias' },
  ],
};

const RH_SIDE: FeegowSidePanel = {
  sectionLabel: 'RECURSOS HUMANOS',
  items: buildFeegowRhSideItems(),
};

const FINANCEIRO_SIDE: FeegowSidePanel = {
  sectionLabel: 'FINANCEIRO',
  items: [
    { id: 'resumo', label: 'Resumo', path: '/financeiro' },
    {
      id: 'pagar',
      label: 'Contas a Pagar',
      children: [
        { label: 'Inserir', path: '/financeiro/contas-a-pagar/inserir' },
        { label: 'Listar', path: '/financeiro/contas-a-pagar/listar' },
        { label: 'Despesas Fixas', path: '/financeiro/contas-a-pagar/despesas-fixas' },
      ],
    },
    {
      id: 'receber',
      label: 'Contas a Receber',
      children: [
        { label: 'Inserir', path: '/financeiro/contas-a-receber/inserir' },
        { label: 'Listar', path: '/financeiro/contas-a-receber/listar' },
      ],
    },
    { id: 'caixas', label: 'Caixas', path: '/financeiro/caixas' },
    { id: 'tpa', label: 'TPA', path: '/convenios/tpa' },
    { id: 'recibos-diversos', label: 'Recibos Diversos', path: '/financeiro/recibos-diversos' },
    { id: 'auditoria', label: 'Auditoria', path: '/auditoria', badge: 'Novo' },
    { id: 'extratos', label: 'Extratos', path: '/financeiro/extratos' },
    { id: 'repasses', label: 'Repasses', path: '/financeiro/repasses' },
    { id: 'tef', label: 'Transações TEF', path: '/financeiro/tef' },
    { id: 'cheques', label: 'Cheques', path: '/financeiro/cheques' },
    { id: 'cartoes', label: 'Cartões', path: '/financeiro/cartoes' },
    { id: 'fechamento', label: 'Fechamento de Data', path: '/financeiro/fechamento' },
    { id: 'propostas', label: 'Propostas', path: '/financeiro/propostas' },
    { id: 'descontos', label: 'Descontos Pendentes', path: '/financeiro/descontos' },
    { id: 'honorarios', label: 'Honorários', path: '/financeiro/honorarios' },
    { id: 'rh-folha', label: '↗ Folha de pagamento (RH)', path: '/rh/folha' },
  ],
};

const FATURAMENTO_SIDE: FeegowSidePanel = {
  sectionLabel: 'FATURAMENTO',
  items: buildFeegowFaturamentoSideItems(),
};

const GUIAS_SIDE: FeegowSidePanel = {
  sectionLabel: 'GUIAS',
  items: buildFeegowGuiasSideItems(),
};

const COMUNICACAO_SIDE: FeegowSidePanel = {
  sectionLabel: 'COMUNICAÇÃO',
  items: buildFeegowConnectSideItems(),
};

const RELATORIOS_SIDE: FeegowSidePanel = {
  sectionLabel: 'RELATÓRIOS',
  items: [
    {
      id: 'pacientes',
      label: 'Pacientes',
      children: [{ label: 'Por Perfil', path: '/relatorios' }],
    },
    {
      id: 'lab',
      label: 'Integração Laboratorial',
      children: [
        { label: 'Mapa Laboratório', path: '/relatorios' },
        { label: 'Mapeamento de Coletas', path: '/relatorios' },
        { label: 'Conferência de Amostras', path: '/relatorios' },
        { label: 'Relatório de Recoletas', path: '/relatorios' },
      ],
    },
    {
      id: 'agenda',
      label: 'Agenda',
      children: [
        { label: 'Agendamentos e Atendimento', path: '/relatorios/atendimento' },
        { label: 'Taxa de Ocupação', path: '/relatorios' },
      ],
    },
    {
      id: 'faturamento',
      label: 'Faturamento',
      children: [
        { label: 'Produção - Analítico', path: '/relatorios' },
        { label: 'Produção Externa', path: '/relatorios' },
        { label: 'Guias Pagas', path: '/relatorios' },
        { label: 'Vendas - Particular', path: '/relatorios' },
        { label: 'TPA', path: '/relatorios?q=tpa' },
        { label: 'Folha', path: '/relatorios?q=folha' },
        { label: 'Ambulância', path: '/relatorios?q=ambulancia' },
        { label: 'Patologia', path: '/relatorios?q=patologia' },
      ],
    },
  ],
};

const PACIENTES_SIDE: FeegowSidePanel = {
  sectionLabel: 'PACIENTES',
  items: [
    { id: 'inserir', label: 'Inserir', path: '/recepcao/pacientes/inserir' },
    { id: 'listar', label: 'Listar', path: '/recepcao/pacientes/listar' },
    { id: 'vacinacao', label: 'Vacinação', path: '/recepcao/vacinacao' },
  ],
};

const DASHBOARD_SIDE: FeegowSidePanel = {
  promoButtons: true,
  sectionLabel: 'MÓDULOS',
  items: [
    { id: 'caixas', label: 'Caixas', path: '/financeiro/caixas' },
    { id: 'vacinacao', label: 'Vacinação', path: '/recepcao/vacinacao' },
    { id: 'farmacia-ala', label: 'Farmácia por Ala', path: '/estoque/farmacia-ala' },
    { id: 'kits', label: 'Kits de Produtos', path: '/estoque/kits' },
    { id: 'med-convenio', label: 'Medicamento por Convênio', path: '/estoque/config/medicamento-convenio' },
    { id: 'hotelaria', label: 'Hotelaria', path: '/hotelaria' },
    { id: 'residuos', label: 'Resíduos', path: '/residuos' },
    { id: 'rh', label: 'Recursos Humanos', path: '/rh' },
  ],
};

const ESPERA_SIDE: FeegowSidePanel = {
  locationBlock: true,
  items: [],
};

const AGENDA_SIDE: FeegowSidePanel = {
  locationBlock: true,
  items: [],
};

export function resolveFeegowSidePanel(pathname: string): FeegowSidePanel {
  const path = pathname.split('?')[0];
  if (path === '/' || path.startsWith('/dashboard')) return DASHBOARD_SIDE;
  if (path.startsWith('/connect')) return COMUNICACAO_SIDE;
  if (path.startsWith('/ajuda')) {
    return {
      sectionLabel: 'AJUDA',
      items: [
        { id: 'inicio', label: 'Início', path: '/ajuda' },
        { id: 'faq', label: 'FAQ', path: '/ajuda/faq' },
        { id: 'chamados', label: 'Suporte', path: '/ajuda/chamados' },
        { id: 'sugestoes', label: 'Sugestões', path: '/ajuda/sugestoes' },
      ],
    };
  }
  if (path.startsWith('/guias')) return GUIAS_SIDE;
  if (
    path.startsWith('/faturamento-tiss')
    || path.startsWith('/faturamento')
    || path.startsWith('/convenios')
  ) {
    return FATURAMENTO_SIDE;
  }
  if (path.startsWith('/rh')) return RH_SIDE;
  if (
    path.startsWith('/hotelaria')
    || path.startsWith('/residuos')
    || path.startsWith('/lavanderia')
    || path.startsWith('/transportes')
    || path.startsWith('/ambulancias')
  ) {
    return OPERACOES_SIDE;
  }
  if (path.startsWith('/financeiro')) return FINANCEIRO_SIDE;
  if (path.startsWith('/relatorios')) return RELATORIOS_SIDE;
  if (path.startsWith('/emergencia') || path.includes('espera')) return ESPERA_SIDE;
  if (path.startsWith('/agenda') || path.includes('agendamento')) return AGENDA_SIDE;
  if (path.startsWith('/estoque') && !path.startsWith('/estoque/requisicoes')) {
    return { items: [] };
  }
  if (
    path.startsWith('/recepcao/pacientes')
    || path.startsWith('/recepcao/vacinacao')
    || path.startsWith('/pacientes')
  ) {
    return PACIENTES_SIDE;
  }
  return { items: [] };
}

export function isFeegowTopItemActive(pathname: string, item: FeegowTopItem): boolean {
  const path = pathname.split('?')[0];
  if (item.path && (path === item.path || path.startsWith(`${item.path}/`))) return true;
  return item.children?.some((c) => path === c.path || path.startsWith(`${c.path}/`)) ?? false;
}
