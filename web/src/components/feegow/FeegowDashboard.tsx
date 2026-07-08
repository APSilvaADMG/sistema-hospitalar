import { type ReactNode } from 'react';

import { Link } from 'react-router-dom';

import {

  emergencyStatusLabels,

  triageUrgencyLabels,

  type OperationalDashboardDto,

} from '../../api/client';

import { AppointmentStatusBadge } from '../AppointmentStatusBadge';

import { DashboardAlertsPanel } from '../dashboard/DashboardAlertsPanel';

import { DashboardAppointmentStatusChart } from '../dashboard/DashboardAppointmentStatusChart';

import { DashboardHourlyChart } from '../dashboard/DashboardHourlyChart';

import { NavIcon } from '../NavIcon';

import { getInstitutionName } from '../../config/iasghBranding';
import {
  printDailyAgendaReport,
  printEmergencyQueueSummary,
  printOperationalHospitalizationSummary,
} from '../../utils/printTemplates';

import { formatBrLongDate, formatBrTime } from '../../utils/dateUtils';



type FeegowDashboardProps = {

  data: OperationalDashboardDto;

  onRefresh?: () => void;

  lastRefresh?: Date | null;

  scheduleDate?: string;

};



const quickActions = [

  { to: '/recepcao/pacientes/listar', label: 'Pacientes', icon: 'users' as const },

  { to: '/recepcao/agendamentos', label: 'Agenda', icon: 'calendar' as const },

  { to: '/emergencia', label: 'Sala de espera', icon: 'siren' as const },

  { to: '/internacao', label: 'Internação', icon: 'bed' as const },

  { to: '/laboratorio', label: 'Laboratório', icon: 'flask' as const },

  { to: '/imagem', label: 'Imagem', icon: 'scan' as const },

  { to: '/financeiro', label: 'Financeiro', icon: 'wallet' as const },

  { to: '/estoque/listar', label: 'Estoque', icon: 'package' as const },

];



const urgencyClass: Record<string, string> = {

  Emergency: 'feegow-dash-urgency-emergency',

  High: 'feegow-dash-urgency-high',

  Medium: 'feegow-dash-urgency-medium',

  Low: 'feegow-dash-urgency-low',

  NonUrgent: 'feegow-dash-urgency-nonurgent',

};



function formatCurrency(value: number) {

  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

}



function KpiCard({

  value,

  label,

  tone = 'neutral',

  footer,

}: {

  value: string | number;

  label: string;

  tone?: 'green' | 'teal' | 'yellow' | 'red' | 'neutral';

  footer?: ReactNode;

}) {

  return (

    <div className={`feegow-dash-kpi feegow-dash-kpi-${tone}`}>

      <div className="feegow-dash-kpi-value">{value}</div>

      <div className="feegow-dash-kpi-label">{label}</div>

      {footer ? <div className="feegow-dash-kpi-footer">{footer}</div> : null}

    </div>

  );

}



function BedMiniVisualization({ data }: { data: OperationalDashboardDto }) {

  const segments: { kind: 'free' | 'occupied' | 'cleaning' | 'blocked'; count: number }[] = [

    { kind: 'free', count: data.availableBeds },

    { kind: 'occupied', count: data.occupiedBeds },

    { kind: 'cleaning', count: data.cleaningBeds },

    { kind: 'blocked', count: data.maintenanceBeds },

  ];



  const cells = segments.flatMap(({ kind, count }) =>

    Array.from({ length: count }, (_, i) => ({ kind, key: `${kind}-${i}` })),

  );



  if (data.totalBeds === 0) {

    return <p className="feegow-dash-empty">Nenhum leito cadastrado</p>;

  }



  return (

    <div className="feegow-bed-viz">

      <div className="feegow-bed-viz-grid" role="img" aria-label={`Mapa de ${data.totalBeds} leitos`}>

        {cells.map((cell) => (

          <span key={cell.key} className={`feegow-bed-cell feegow-bed-cell-${cell.kind}`} />

        ))}

      </div>

      <div className="feegow-bed-viz-legend">

        <span><i className="feegow-bed-cell feegow-bed-cell-free" aria-hidden /> Livres {data.availableBeds}</span>

        <span><i className="feegow-bed-cell feegow-bed-cell-occupied" aria-hidden /> Ocupados {data.occupiedBeds}</span>

        <span><i className="feegow-bed-cell feegow-bed-cell-cleaning" aria-hidden /> Higienização {data.cleaningBeds}</span>

        <span><i className="feegow-bed-cell feegow-bed-cell-blocked" aria-hidden /> Manutenção {data.maintenanceBeds}</span>

        <span className="feegow-bed-viz-rate"><strong>{data.bedOccupancyRate}%</strong> ocupação</span>

      </div>

    </div>

  );

}



function DashPanel({

  title,

  linkTo,

  linkLabel,

  meta,

  children,

  wide,

}: {

  title: string;

  linkTo?: string;

  linkLabel?: string;

  meta?: ReactNode;

  children: ReactNode;

  wide?: boolean;

}) {

  return (

    <section className={`feegow-dash-panel${wide ? ' feegow-dash-panel-wide' : ''}`}>

      <header className="feegow-dash-panel-head">

        <h3>{title}</h3>

        {meta ? <span className="feegow-dash-panel-meta">{meta}</span> : null}

        {linkTo ? <Link to={linkTo}>{linkLabel ?? 'Ver tudo'}</Link> : null}

      </header>

      <div className="feegow-dash-panel-body">{children}</div>

    </section>

  );

}



function RevenueExpenseChart({ data }: { data: OperationalDashboardDto }) {

  const maxMonthly = Math.max(

    ...data.revenueExpenseMonthly.map((m) => Math.max(m.revenue, m.expense)),

    1,

  );

  const totalRevenue = data.revenueExpenseMonthly.reduce((acc, m) => acc + m.revenue, 0);

  const totalExpense = data.revenueExpenseMonthly.reduce((acc, m) => acc + m.expense, 0);



  return (

    <>

      <div className="feegow-mini-bars feegow-mini-bars-tall">

        {data.revenueExpenseMonthly.map((m) => (

          <div key={m.monthLabel} className="feegow-mini-bar-col" title={`${m.monthLabel}: R ${m.revenue} / D ${m.expense}`}>

            <div className="feegow-mini-bar-stack">

              <div className="feegow-mini-bar revenue" style={{ height: `${(m.revenue / maxMonthly) * 100}%` }} />

              <div className="feegow-mini-bar expense" style={{ height: `${(m.expense / maxMonthly) * 100}%` }} />

            </div>

            <span>{m.monthLabel.slice(0, 3)}</span>

          </div>

        ))}

      </div>

      <div className="feegow-chart-legend">

        <span><i className="feegow-legend-dot revenue" aria-hidden /> Receita {formatCurrency(totalRevenue)}</span>

        <span><i className="feegow-legend-dot expense" aria-hidden /> Despesa {formatCurrency(totalExpense)}</span>

      </div>

    </>

  );

}



export function FeegowDashboard({ data, onRefresh, lastRefresh, scheduleDate }: FeegowDashboardProps) {

  const institution = getInstitutionName();

  const todayLabel = formatBrLongDate(new Date());

  const occupancyTone = data.bedOccupancyRate >= 90 ? 'red' : data.bedOccupancyRate >= 75 ? 'yellow' : 'green';

  const alertCount = data.alerts.length;

  const psTone = data.emergencyWaiting > 5 ? 'red' : data.emergencyWaiting > 0 ? 'yellow' : 'green';

  const agendaDate = scheduleDate ?? new Date().toISOString().slice(0, 10);



  return (

    <div className="feegow-dashboard">

      <header className="feegow-dash-head">

        <div>

          <p className="feegow-dash-eyebrow">Início / Painel principal</p>

          <h1 className="feegow-dash-title">{institution}</h1>

          <p className="feegow-dash-subtitle">{todayLabel}</p>

        </div>

        <div className="feegow-dash-head-actions">

          {lastRefresh ? (

            <span className="feegow-dash-meta">

              Atualizado às {formatBrTime(lastRefresh.toISOString())}

            </span>

          ) : null}

          {onRefresh ? (

            <button type="button" className="feegow-dash-btn" onClick={onRefresh}>

              Atualizar

            </button>

          ) : null}

          <button
            type="button"
            className="feegow-dash-btn"
            onClick={() => printDailyAgendaReport(agendaDate, data.appointmentsTodayList, {
              total: data.appointmentsToday,
            })}
          >
            Agenda do dia
          </button>

          <button
            type="button"
            className="feegow-dash-btn"
            onClick={() => printEmergencyQueueSummary(data.emergencyQueue, {
              waiting: data.emergencyWaiting,
              inCare: data.emergencyInCare,
              critical: data.emergencyCritical,
              avgWaitMinutes: data.averageEmergencyWaitMinutes,
              slaViolations: data.emergencySlaViolations,
            })}
          >
            Fila PS
          </button>

          <button
            type="button"
            className="feegow-dash-btn"
            onClick={() => printOperationalHospitalizationSummary(data)}
          >
            Internação
          </button>

          <Link to="/bi" className="feegow-dash-btn feegow-dash-btn-primary">BI</Link>

        </div>

      </header>



      <section className="feegow-dash-kpi-grid feegow-dash-kpi-grid-primary" aria-label="Indicadores principais">

        <KpiCard value={data.emergencyWaiting} label="PS aguardando" tone={psTone} footer={<Link to="/emergencia">Abrir PS</Link>} />

        <KpiCard value={data.appointmentsToday} label="Agenda hoje" tone="teal" footer={<Link to="/recepcao/agendamentos">Ver agenda</Link>} />

        <KpiCard value={data.attendancesToday} label="Atendimentos hoje" tone="green" />

        <KpiCard value={`${data.bedOccupancyRate}%`} label="Ocupação de leitos" tone={occupancyTone} footer={<span>{data.occupiedBeds}/{data.totalBeds} leitos</span>} />

        <KpiCard value={formatCurrency(data.revenueToday)} label="Receita do dia" tone="green" />

        <KpiCard value={alertCount} label="Alertas ativos" tone={alertCount > 0 ? 'red' : 'neutral'} />

      </section>



      <section className="feegow-dash-quick" aria-label="Acesso rápido">

        <h2 className="feegow-dash-section-label">Acesso rápido</h2>

        <nav className="feegow-dash-quick-grid">

          {quickActions.map((action) => (

            <Link key={action.to} to={action.to} className="feegow-dash-quick-tile">

              <span className="feegow-dash-quick-icon" aria-hidden>

                <NavIcon name={action.icon} />

              </span>

              <span className="feegow-dash-quick-label">{action.label}</span>

            </Link>

          ))}

        </nav>

      </section>



      {data.alerts.length > 0 ? <DashboardAlertsPanel alerts={data.alerts} /> : null}



      <section className="feegow-dash-section" aria-labelledby="feegow-dash-activity-heading">

        <h2 id="feegow-dash-activity-heading" className="feegow-dash-section-label">Atividade recente</h2>

        <div className="feegow-dash-columns">

          <DashPanel

            title="Agenda de hoje"

            linkTo="/recepcao/agendamentos"

            meta={`${data.appointmentsTodayList.length} agendamento(s)`}

          >

            <div className="feegow-dash-table-wrap">

              <table className="feegow-dash-table">

                <thead>

                  <tr><th>Horário</th><th>Paciente</th><th>Profissional</th><th>Status</th></tr>

                </thead>

                <tbody>

                  {data.appointmentsTodayList.slice(0, 8).map((a) => (

                    <tr key={a.id}>

                      <td className="feegow-dash-table-time">{formatBrTime(a.scheduledAt)}</td>

                      <td>{a.patientName}</td>

                      <td className="feegow-dash-table-muted">{a.professionalName}</td>

                      <td><AppointmentStatusBadge status={a.status} /></td>

                    </tr>

                  ))}

                  {data.appointmentsTodayList.length === 0 ? (

                    <tr>

                      <td colSpan={4} className="feegow-dash-empty">Nenhum agendamento para hoje</td>

                    </tr>

                  ) : null}

                </tbody>

              </table>

            </div>

          </DashPanel>



          <DashPanel

            title="Fila do pronto-socorro"

            linkTo="/emergencia"

            linkLabel="Abrir PS"

            meta={`${data.emergencyQueue.length} paciente(s)`}

          >

            <div className="feegow-dash-table-wrap">

              <table className="feegow-dash-table">

                <thead>

                  <tr><th>Chegada</th><th>Paciente</th><th>Urgência</th></tr>

                </thead>

                <tbody>

                  {data.emergencyQueue.slice(0, 8).map((v) => (

                    <tr key={v.id}>

                      <td className="feegow-dash-table-time">{formatBrTime(v.arrivedAt)}</td>

                      <td>{v.patientName}</td>

                      <td>

                        <span className={`feegow-dash-urgency ${urgencyClass[v.urgency] ?? ''}`}>

                          {triageUrgencyLabels[v.urgency] ?? v.urgency}

                        </span>

                      </td>

                    </tr>

                  ))}

                  {data.emergencyQueue.length === 0 ? (

                    <tr>

                      <td colSpan={3} className="feegow-dash-empty">

                        Fila vazia — {emergencyStatusLabels.Aguardando}

                      </td>

                    </tr>

                  ) : null}

                </tbody>

              </table>

            </div>

          </DashPanel>

        </div>

      </section>



      <section className="feegow-dash-section" aria-labelledby="feegow-dash-clinical-heading">

        <h2 id="feegow-dash-clinical-heading" className="feegow-dash-section-label">Assistencial</h2>

        <div className="feegow-dash-columns feegow-dash-columns-3">

          <DashPanel title="Status da agenda (hoje)">

            <DashboardAppointmentStatusChart data={data.appointmentStatusBreakdown ?? []} />

          </DashPanel>

          <DashPanel title="Atendimentos por hora">

            <DashboardHourlyChart data={data.hourlyAttendances} />

          </DashPanel>

          <DashPanel title="Mapa de leitos" linkTo="/internacao/leitos" linkLabel="Mapa completo">

            <BedMiniVisualization data={data} />

          </DashPanel>

        </div>

      </section>



      <section className="feegow-dash-section" aria-labelledby="feegow-dash-finance-heading">

        <h2 id="feegow-dash-finance-heading" className="feegow-dash-section-label">Financeiro</h2>

        <div className="feegow-dash-columns">

          <DashPanel title="Resumo financeiro" linkTo="/financeiro" linkLabel="Abrir financeiro">

            <ul className="feegow-finance-summary">

              <li>

                <div className="feegow-finance-summary-label">

                  <span>A receber em aberto</span>

                  <small>{data.financialAccountsOpen} título(s)</small>

                </div>

                <strong>{formatCurrency(data.revenuePending)}</strong>

              </li>

              <li>

                <div className="feegow-finance-summary-label">

                  <span>A pagar em aberto</span>

                  <small>{data.payableAccountsOpen} título(s)</small>

                </div>

                <strong>{formatCurrency(data.payablePending)}</strong>

              </li>

              <li>

                <span>Recebido no mês</span>

                <strong className="feegow-finance-positive">{formatCurrency(data.revenueThisMonth)}</strong>

              </li>

              <li>

                <span>Pago no mês</span>

                <strong>{formatCurrency(data.expenseThisMonth)}</strong>

              </li>

              {data.overdueReceivable > 0 ? (

                <li className="feegow-finance-summary-overdue">

                  <span>Vencidos a receber</span>

                  <strong>{formatCurrency(data.overdueReceivable)}</strong>

                </li>

              ) : null}

              {data.overduePayable > 0 ? (

                <li className="feegow-finance-summary-overdue">

                  <span>Vencidos a pagar</span>

                  <strong>{formatCurrency(data.overduePayable)}</strong>

                </li>

              ) : null}

            </ul>

            <div className="feegow-dash-panel-actions">

              <Link to="/financeiro/contas-a-receber/listar" className="feegow-dash-btn feegow-dash-btn-sm">A receber</Link>

              <Link to="/financeiro/contas-a-pagar/listar" className="feegow-dash-btn feegow-dash-btn-sm">A pagar</Link>

              <Link to="/faturamento-tiss" className="feegow-dash-btn feegow-dash-btn-sm">TISS</Link>

            </div>

          </DashPanel>



          <DashPanel title="Receita x despesa (12 meses)">

            <RevenueExpenseChart data={data} />

          </DashPanel>

        </div>



        {data.departmentRevenue.length > 0 ? (

          <div className="feegow-department-tiles">

            {data.departmentRevenue.map((d) => (

              <div key={d.departmentCode} className="feegow-department-tile">

                <span>{d.departmentLabel}</span>

                <strong>{formatCurrency(d.amount)}</strong>

              </div>

            ))}

          </div>

        ) : null}

      </section>



      <section className="feegow-dash-section" aria-labelledby="feegow-dash-ops-heading">

        <h2 id="feegow-dash-ops-heading" className="feegow-dash-section-label">Operações</h2>

        <div className="feegow-dash-kpi-grid feegow-dash-kpi-grid-secondary">

          <KpiCard value={data.activeHospitalizations} label="Internações ativas" tone="teal" footer={<Link to="/internacao">Internação</Link>} />

          <KpiCard value={data.surgeriesToday} label="Cirurgias hoje" tone="yellow" footer={<Link to="/centro-cirurgico">Centro cirúrgico</Link>} />

          <KpiCard value={data.emergencyInCare} label="PS em atendimento" tone="teal" />

          <KpiCard value={data.emergencyCritical} label="PS críticos" tone="red" />

          <KpiCard value={data.labOrdersPending} label="Lab pendente" tone={data.labOrdersPending > 0 ? 'yellow' : 'green'} footer={<Link to="/laboratorio">Laboratório</Link>} />

          <KpiCard value={data.imagingStudiesPending} label="Imagem pendente" tone={data.imagingStudiesPending > 0 ? 'yellow' : 'green'} footer={<Link to="/imagem">Imagem</Link>} />

          <KpiCard value={data.lowStockProducts} label="Estoque abaixo do mínimo" tone={data.lowStockProducts > 0 ? 'red' : 'green'} footer={<Link to="/estoque/listar">Estoque</Link>} />

          <KpiCard value={data.integrationFailures} label="Falhas de integração" tone={data.integrationFailures > 0 ? 'red' : 'green'} />

        </div>

      </section>

    </div>

  );

}


