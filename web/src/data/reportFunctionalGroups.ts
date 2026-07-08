/** Grupos funcionais do menu Relatórios (mapeamento para códigos do catálogo backend). */

export type FunctionalReportRef = {
  code: string;
  label: string;
};

export type ReportFunctionalGroup = {
  id: string;
  slug: string;
  label: string;
  description: string;
  reports: FunctionalReportRef[];
};

export const REPORT_FUNCTIONAL_GROUPS: ReportFunctionalGroup[] = [
  {
    id: 'pacientes',
    slug: 'pacientes',
    label: 'Pacientes',
    description: 'Cadastro, convênios, histórico clínico e indicadores',
    reports: [
      { code: 'admin.patients.registered', label: 'Pacientes cadastrados' },
      { code: 'admin.patients.by-city', label: 'Pacientes por cidade' },
      { code: 'admin.patients.by-insurance', label: 'Pacientes por convênio' },
      { code: 'admin.patients.active-inactive', label: 'Pacientes ativos e inativos' },
      { code: 'admin.patients.consultation-history', label: 'Histórico de consultas' },
      { code: 'admin.patients.hospitalization-history', label: 'Histórico de internações' },
      { code: 'pep.patient.history', label: 'Prontuários' },
      { code: 'admin.patients.indicators', label: 'Indicadores de pacientes' },
    ],
  },
  {
    id: 'agenda',
    slug: 'agenda',
    label: 'Agenda',
    description: 'Consultas, produtividade e faltas',
    reports: [
      { code: 'reception.appointments.scheduled', label: 'Consultas por período' },
      { code: 'reception.schedule.by-doctor', label: 'Consultas por médico' },
      { code: 'reception.schedule.by-specialty', label: 'Consultas por especialidade' },
      { code: 'reception.appointments.completed', label: 'Consultas realizadas' },
      { code: 'reception.appointments.cancelled', label: 'Consultas canceladas' },
      { code: 'reception.appointments.no-show', label: 'Pacientes faltosos' },
      { code: 'reception.wait.avg-time', label: 'Tempo médio de atendimento' },
      { code: 'admin.appointments.avg-time', label: 'Duração média de consulta' },
    ],
  },
  {
    id: 'estoque-farmacia',
    slug: 'estoque-farmacia',
    label: 'Estoque e Farmácia',
    description: 'Movimentações, inventário e consumo',
    reports: [
      { code: 'supply.entries', label: 'Entradas de produtos' },
      { code: 'supply.exits', label: 'Saídas de produtos' },
      { code: 'pharmacy.stock.movements', label: 'Movimentações' },
      { code: 'pharmacy.inventory', label: 'Inventário' },
      { code: 'supply.stock.minimum', label: 'Produtos abaixo do estoque mínimo' },
      { code: 'pharmacy.expiring-soon', label: 'Produtos próximos do vencimento' },
      { code: 'pharmacy.consumption.by-sector', label: 'Consumo por setor' },
      { code: 'pharmacy.consumption.by-patient', label: 'Consumo por paciente' },
      { code: 'supply.stock.minimum', label: 'Almoxarifado — estoque mínimo' },
      { code: 'pharmacy.abc-curve', label: 'Curva ABC — Farmácia' },
    ],
  },
  {
    id: 'financeiro',
    slug: 'financeiro',
    label: 'Financeiro',
    description: 'Contas, fluxo de caixa e demonstrativos',
    reports: [
      { code: 'fin.payables', label: 'Contas a pagar' },
      { code: 'fin.receivables', label: 'Contas a receber' },
      { code: 'fin.cashflow', label: 'Fluxo de caixa' },
      { code: 'fin.revenue.by-period', label: 'Receitas' },
      { code: 'fin.expenses.by-period', label: 'Despesas' },
      { code: 'fin.delinquency', label: 'Inadimplência' },
      { code: 'fin.dre', label: 'Centros de custo / DRE' },
      { code: 'fin.statement', label: 'Demonstrativo financeiro' },
      { code: 'fin.cash.sessions', label: 'Sessões de caixa' },
    ],
  },
  {
    id: 'faturamento',
    slug: 'faturamento',
    label: 'Faturamento',
    description: 'SUS, convênios, guias e glosas',
    reports: [
      { code: 'reg.ambulatory-production', label: 'Produção SUS ambulatorial' },
      { code: 'reg.hospital-production', label: 'Produção SUS hospitalar' },
      { code: 'ins.production.by-insurance', label: 'Produção por convênio' },
      { code: 'bill.revenue.by-procedure', label: 'Produção particular' },
      { code: 'bill.revenue.by-specialty', label: 'Receita por especialidade' },
      { code: 'ins.guides.issued', label: 'Guias emitidas' },
      { code: 'ins.guides.authorized', label: 'Guias autorizadas' },
      { code: 'ins.guides.glosas', label: 'Glosas' },
      { code: 'ins.glosas.by-reason', label: 'Glosas por motivo' },
      { code: 'ins.glosas.appeals', label: 'Recursos de glosa' },
      { code: 'bi.indicators.financial', label: 'Indicadores de faturamento' },
      { code: 'reg.apac', label: 'APAC' },
      { code: 'reg.aih', label: 'AIH' },
    ],
  },
  {
    id: 'internacao',
    slug: 'internacao',
    label: 'Internação',
    description: 'Ocupação, altas e indicadores assistenciais',
    reports: [
      { code: 'hosp.admissions.by-period', label: 'Internações por período' },
      { code: 'hosp.beds.occupancy', label: 'Taxa de ocupação' },
      { code: 'hosp.los.avg', label: 'Tempo médio de permanência' },
      { code: 'hosp.discharges', label: 'Altas' },
      { code: 'hosp.transfers.internal', label: 'Transferências' },
      { code: 'hosp.patients.current', label: 'Pacientes internados' },
      { code: 'bi.indicators.clinical', label: 'Indicadores assistenciais' },
      { code: 'hosp.deaths', label: 'Óbitos' },
    ],
  },
  {
    id: 'rh-gestao',
    slug: 'rh-gestao',
    label: 'RH e Gestão',
    description: 'Equipe, escalas e produtividade',
    reports: [
      { code: 'hr.employees.active', label: 'Funcionários cadastrados' },
      { code: 'hr.shifts', label: 'Escalas' },
      { code: 'hr.productivity', label: 'Produtividade' },
      { code: 'hr.schedules', label: 'Horários / escalas detalhadas' },
      { code: 'hr.overtime', label: 'Horas extras' },
      { code: 'admin.indicators.summary', label: 'Indicadores operacionais' },
      { code: 'reception.productivity', label: 'Produtividade da recepção' },
    ],
  },
  {
    id: 'indicadores',
    slug: 'indicadores',
    label: 'Indicadores e Dashboard',
    description: 'Visão executiva e evolução gerencial',
    reports: [
      { code: 'bi.revenue.monthly', label: 'Receita mensal' },
      { code: 'bi.revenue.daily', label: 'Evolução financeira diária' },
      { code: 'bi.occupancy-rate', label: 'Taxa de ocupação' },
      { code: 'admin.appointments.total', label: 'Atendimentos por período' },
      { code: 'bi.indicators.clinical', label: 'Indicadores assistenciais' },
      { code: 'bi.indicators.financial', label: 'Indicadores gerenciais' },
      { code: 'bi.hospitalizations', label: 'Internações (BI)' },
      { code: 'bi.medical-production', label: 'Produção médica' },
      { code: 'admin.beds.occupancy', label: 'Ocupação hospitalar' },
    ],
  },
];

const GROUP_BY_SLUG = Object.fromEntries(
  REPORT_FUNCTIONAL_GROUPS.map((g) => [g.slug, g]),
) as Record<string, ReportFunctionalGroup>;

const CODE_TO_GROUP = new Map<string, ReportFunctionalGroup>();
for (const group of REPORT_FUNCTIONAL_GROUPS) {
  for (const report of group.reports) {
    CODE_TO_GROUP.set(report.code, group);
  }
}

export function getFunctionalGroupBySlug(slug: string | undefined): ReportFunctionalGroup | undefined {
  if (!slug) return undefined;
  return GROUP_BY_SLUG[slug];
}

export function filterCatalogByFunctionalGroup<T extends { code: string }>(
  catalog: T[],
  groupId: string | null,
): T[] {
  if (!groupId) return catalog;
  const group = REPORT_FUNCTIONAL_GROUPS.find((g) => g.id === groupId);
  if (!group) return catalog;
  const codes = new Set(group.reports.map((r) => r.code));
  return catalog.filter((item) => codes.has(item.code));
}

export function getFunctionalLabelForCode(code: string): string | undefined {
  for (const group of REPORT_FUNCTIONAL_GROUPS) {
    const hit = group.reports.find((r) => r.code === code);
    if (hit) return hit.label;
  }
  return undefined;
}

export function getGroupForCode(code: string): ReportFunctionalGroup | undefined {
  return CODE_TO_GROUP.get(code);
}
