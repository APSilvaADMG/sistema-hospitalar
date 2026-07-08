export type FeegowFinanceSection =
  | 'resumo'
  | 'pagar-inserir'
  | 'pagar-listar'
  | 'pagar-despesas-fixas'
  | 'receber-inserir'
  | 'receber-listar'
  | 'caixas'
  | 'extratos'
  | 'repasses'
  | 'tef'
  | 'cheques'
  | 'cartoes'
  | 'fechamento'
  | 'propostas'
  | 'descontos'
  | 'honorarios'
  | 'recibos-diversos';

export const FEEGOW_FINANCE_SECTION_TITLES: Record<FeegowFinanceSection, string> = {
  resumo: 'painel principal',
  'pagar-inserir': 'Contas a Pagar',
  'pagar-listar': 'Contas a Pagar',
  'pagar-despesas-fixas': 'Despesas Fixas',
  'receber-inserir': 'Contas a Receber',
  'receber-listar': 'Contas a Receber',
  caixas: 'Caixas',
  extratos: 'Extratos',
  repasses: 'Repasses',
  tef: 'Transações TEF',
  cheques: 'Cheques',
  cartoes: 'Cartões',
  fechamento: 'Fechamento de Data',
  propostas: 'Propostas',
  descontos: 'Descontos Pendentes',
  honorarios: 'Honorários',
  'recibos-diversos': 'Recibos Diversos',
};

export function feegowFinanceListPath(kind: 'pagar' | 'receber'): string {
  return kind === 'pagar' ? '/financeiro/contas-a-pagar/listar' : '/financeiro/contas-a-receber/listar';
}

export function feegowFinanceInsertPath(kind: 'pagar' | 'receber'): string {
  return kind === 'pagar' ? '/financeiro/contas-a-pagar/inserir' : '/financeiro/contas-a-receber/inserir';
}

export function feegowFinanceSectionPath(section: FeegowFinanceSection): string {
  const map: Record<FeegowFinanceSection, string> = {
    resumo: '/financeiro/resumo',
    'pagar-inserir': '/financeiro/contas-a-pagar/inserir',
    'pagar-listar': '/financeiro/contas-a-pagar/listar',
    'pagar-despesas-fixas': '/financeiro/contas-a-pagar/despesas-fixas',
    'receber-inserir': '/financeiro/contas-a-receber/inserir',
    'receber-listar': '/financeiro/contas-a-receber/listar',
    caixas: '/financeiro/caixas',
    extratos: '/financeiro/extratos',
    repasses: '/financeiro/repasses',
    tef: '/financeiro/tef',
    cheques: '/financeiro/cheques',
    cartoes: '/financeiro/cartoes',
    fechamento: '/financeiro/fechamento',
    propostas: '/financeiro/propostas',
    descontos: '/financeiro/descontos',
    honorarios: '/financeiro/honorarios',
    'recibos-diversos': '/financeiro/recibos-diversos',
  };
  return map[section];
}

export function resolveFeegowFinanceSection(pathname: string): FeegowFinanceSection {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/financeiro';
  if (path === '/financeiro' || path === '/financeiro/resumo') return 'resumo';
  if (path === '/financeiro/contas-a-pagar/inserir') return 'pagar-inserir';
  if (path === '/financeiro/contas-a-pagar/listar') return 'pagar-listar';
  if (path === '/financeiro/contas-a-pagar/despesas-fixas') return 'pagar-despesas-fixas';
  if (path === '/financeiro/contas-a-receber/inserir') return 'receber-inserir';
  if (path === '/financeiro/contas-a-receber/listar') return 'receber-listar';
  if (path === '/financeiro/caixas') return 'caixas';
  if (path === '/financeiro/extratos') return 'extratos';
  if (path === '/financeiro/repasses') return 'repasses';
  if (path === '/financeiro/tef') return 'tef';
  if (path === '/financeiro/cheques') return 'cheques';
  if (path === '/financeiro/cartoes') return 'cartoes';
  if (path === '/financeiro/fechamento') return 'fechamento';
  if (path === '/financeiro/propostas') return 'propostas';
  if (path === '/financeiro/descontos') return 'descontos';
  if (path === '/financeiro/honorarios') return 'honorarios';
  if (path === '/financeiro/recibos-diversos') return 'recibos-diversos';
  return 'resumo';
}

export function isFeegowFinanceRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  return path === '/financeiro' || path.startsWith('/financeiro/');
}
