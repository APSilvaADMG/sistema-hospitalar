/** Rotas com layout próprio dentro do shell Feegow/IASGH. */

import { parseFeegowPatientRoute } from '../components/feegow/patients/feegowPatientNav';

export function isFeegowDashboardRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  return path === '/' || path.startsWith('/dashboard');
}

/** Telas que já trazem card/box completo — só título, sem card extra do shell. */
export function isFeegowPlainContentRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  return path.startsWith('/relatorios');
}

function normalizePath(pathname: string): string {
  return pathname.split('?')[0].replace(/\/$/, '') || '/';
}

/** Sala de Espera — menu Espera (print Feegow 16-31-04). */
export function isFeegowEsperaRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path === '/emergencia' || path.startsWith('/emergencia/') || path.startsWith('/espera');
}

/** Rotas de agenda (inclui sub-abas como /agendamentos/consultas). */
export function isFeegowAgendaRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path.includes('/agendamentos')
    || path.startsWith('/agenda')
    || path.includes('/ambulatorio/agenda');
}

export function isFeegowAgendaCheckInRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path.endsWith('/check-in') || path.includes('/check-in/');
}

/** Mapa de agenda — conteúdo em largura total, sem sidebar lateral da agenda. */
export function isFeegowAgendaMapRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path.includes('/agendamentos/mapa') || path.endsWith('/agenda/mapa');
}

/** Sidebar lateral da agenda (profissional, filtros, equipamento). */
export function isFeegowAgendaSidebarRoute(pathname: string): boolean {
  return isFeegowAgendaRoute(pathname)
    && !isFeegowAgendaMapRoute(pathname);
}

/** Sidebar lateral da sala de espera (local de atendimento). */
export function isFeegowEsperaSidebarRoute(pathname: string): boolean {
  return isFeegowEsperaRoute(pathname);
}

/** Telas Feegow de agenda — sem picker de paciente nem abas do hub. */
export function isFeegowDailyAgendaRoute(pathname: string): boolean {
  return isFeegowAgendaRoute(pathname);
}

/** Listagem de pacientes (print Feegow 16-36-28). */
export function isFeegowPatientListRoute(pathname: string): boolean {
  const route = parseFeegowPatientRoute(pathname);
  return route?.mode === 'list';
}

/** Cadastro — Dados Principais / inserir. */
export function isFeegowPatientInsertRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  if (path === '/recepcao/pacientes' || path === '/recepcao/pacientes/inserir') return true;
  const route = parseFeegowPatientRoute(pathname);
  return route?.mode === 'insert' && route.section === 'dados-principais';
}

/** Telas Feegow de estoque (inserir, listar). */
export function isFeegowInventoryRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path.startsWith('/estoque');
}

/** Sidebar lateral de estoque (tipos de itens + configurações). */
export function isFeegowInventorySidebarRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path.startsWith('/estoque') && !path.startsWith('/estoque/requisicoes');
}
/** Requisição de estoque — layout full width sem sidebar lateral. */
export function isFeegowRequisitionRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path.startsWith('/estoque/requisicoes');
}

/** Telas Feegow de pacientes (inserir, listar, prontuário). */
export function isFeegowPatientRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  if (path === '/recepcao/pacientes') return true;
  if (path === '/recepcao/vacinacao' || path.startsWith('/recepcao/vacinacao/')) return false;
  return parseFeegowPatientRoute(pathname) !== null;
}

/** Balcão de vacinação. */
export function isFeegowVaccinationRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path === '/recepcao/vacinacao' || path.startsWith('/recepcao/vacinacao/');
}

export function isFeegowSghcRoute(pathname: string): boolean {
  const path = normalizePath(pathname);
  return path === '/sghc' || path.startsWith('/sghc/');
}

export function isFeegowSghcSidebarRoute(pathname: string): boolean {
  return isFeegowSghcRoute(pathname);
}

/** Sidebar lateral nas telas de pacientes com prontuário (não na listagem). */
export function isFeegowPatientSidebarRoute(pathname: string): boolean {
  const route = parseFeegowPatientRoute(pathname);
  if (!route) return false;
  return route.mode === 'insert' || route.mode === 'record';
}
