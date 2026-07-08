import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import {
  api,
  emergencyStatusLabels,
  triageUrgencyCssClass,
  triageUrgencyLabels,
  type OperationalDashboardDto,
  type ProfessionalDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { BayannoActionTiles } from '../components/dashboard/BayannoActionTiles';
import { BayannoAreaTop } from '../components/dashboard/BayannoAreaTop';
import { BayannoDashboardLower } from '../components/dashboard/BayannoDashboardLower';
import { DashboardAlertsPanel } from '../components/dashboard/DashboardAlertsPanel';
import { DashboardAppointmentStatusChart } from '../components/dashboard/DashboardAppointmentStatusChart';
import { DashboardHourlyChart } from '../components/dashboard/DashboardHourlyChart';
import { DateNavigator } from '../components/DateNavigator';
import { FilterBar } from '../components/FilterBar';
import { getBayannoDashboardTilesForRole } from '../config/bayannoDashboardTiles';
import { loadHospitalParams } from '../config/clinicOnDoctorProfile';
import { filterPathsByModules } from '../config/moduleVisibility';
import { useAppearance } from '../theme/AppearanceProvider';
import { isClinicShellBrand, isFeegowBrand } from '../theme/appearanceConfig';
import { FeegowDashboard } from '../components/feegow/FeegowDashboard';
import { usePersistedFilters } from '../hooks/usePersistedFilters';
import { FILTER_STORAGE_KEYS } from '../utils/persistedFilters';
import { HealthCampaignsPanel } from '../components/HealthCampaignsPanel';
import { AppointmentStatusBadge } from '../components/AppointmentStatusBadge';
import { KpiCard } from '../components/KpiCard';
import { MonthBirthdaysPanel } from '../components/MonthBirthdaysPanel';
import { NavIcon } from '../components/NavIcon';
import { ModuleTabs } from '../components/ModuleTabs';
import { PageHeader } from '../components/PageHeader';
import { dashboardTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { exportDashboardCsv } from '../utils/dashboardExport';
import {
  printDailyAgendaReport,
  printEmergencyQueueSummary,
  printOperationalHospitalizationSummary,
} from '../utils/printTemplates';
import { formatBrTime } from '../utils/dateUtils';

const REFRESH_MS = 30_000;

const quickActions = [
  { to: '/recepcao/pacientes', label: 'Pacientes', icon: 'users' as const },
  { to: '/recepcao/agendamentos', label: 'Agendamentos', icon: 'calendar' as const },
  { to: '/emergencia', label: 'Emergência', icon: 'siren' as const },
  { to: '/internacao', label: 'Internação', icon: 'bed' as const },
  { to: '/laboratorio', label: 'Laboratório', icon: 'flask' as const },
  { to: '/financeiro', label: 'Financeiro', icon: 'wallet' as const },
  { to: '/dashboard/assistencial', label: 'Painel Assistencial', icon: 'stethoscope' as const },
  { to: '/bi', label: 'BI', icon: 'bar-chart' as const },
];

function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function DashboardPage() {
  const { hasPermission, hasRole, user } = useAuth();
  const { appearance } = useAppearance();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [data, setData] = useState<OperationalDashboardDto | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const { filters, patch } = usePersistedFilters(FILTER_STORAGE_KEYS.dashboard, {
    date: new Date().toISOString().slice(0, 10),
    professionalId: '',
  });

  const isPatient = hasRole('Patient');
  const isStaff = !isPatient && hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage');
  const modules = loadHospitalParams().modules;
  const useFeegowDashboard = isFeegowBrand(appearance.brand);
  const useClinicDashboard = isClinicShellBrand(appearance.brand) && !useFeegowDashboard;
  const bayannoTileRows = useMemo(() => {
    if (!useClinicDashboard || !user) return [];
    const roleTiles = getBayannoDashboardTilesForRole(user.role);
    return roleTiles
      .map((row) => filterPathsByModules(row, modules))
      .filter((row) => row.length > 0);
  }, [useClinicDashboard, modules, pathname, user]);
  const visibleQuickActions = useMemo(
    () => filterPathsByModules(quickActions, modules),
    [modules, pathname],
  );

  const loadDashboard = useCallback(async () => {
    try {
      const result = await api.getOperationalDashboard({
        date: filters.date,
        professionalId: filters.professionalId || undefined,
      });
      const safe = {
        ...result,
        revenueExpenseMonthly: result.revenueExpenseMonthly ?? [],
        weeklyCalendar: result.weeklyCalendar ?? [],
        departmentRevenue: result.departmentRevenue ?? [],
      };
      setData(safe);
      setError('');
      setLastRefresh(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar dashboard');
    } finally {
      setLoading(false);
    }
  }, [filters.date, filters.professionalId]);

  useEffect(() => {
    api.getProfessionals().then(setProfessionals).catch(console.error);
  }, []);

  useEffect(() => {
    if (!isStaff) {
      setLoading(false);
      return;
    }
    loadDashboard();
    const timer = window.setInterval(loadDashboard, REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [isStaff, loadDashboard]);

  if (isPatient) return <Navigate to="/portal-paciente" replace />;
  if (!isStaff) return <div className="card">Acesso restrito à equipe do hospital.</div>;
  if (loading && !data) return <div className="card">Carregando indicadores...</div>;
  if (error && !data) return <div className="alert alert-error">{error}</div>;
  if (!data) return null;

  if (useFeegowDashboard) {
    return (
      <FeegowDashboard
        data={data}
        onRefresh={loadDashboard}
        lastRefresh={lastRefresh}
        scheduleDate={filters.date}
      />
    );
  }

  if (useClinicDashboard) {
    return (
      <>
        <BayannoAreaTop
          title={breadcrumb.title || 'Painel administrativo'}
          data={data}
        />
        {bayannoTileRows.length > 0 && <BayannoActionTiles rows={bayannoTileRows} />}
        <BayannoDashboardLower alerts={data.alerts} scheduleDate={filters.date} />
      </>
    );
  }

  const maxMonthly = Math.max(...data.monthlyAppointments.map((m) => m.count), 1);
  const maxSpecialty = Math.max(...data.productionBySpecialty.map((s) => s.count), 1);

  return (
    <>
      <PageHeader
        eyebrow="TELA-001 · Dashboard Executivo"
        title={breadcrumb.title || 'Visão geral operacional'}
        subtitle="Indicadores em tempo real — atualização automática a cada 30 segundos."

      >
        <div className="dashboard-header-actions">
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => loadDashboard()}>
            Atualizar
          </button>
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => exportDashboardCsv(data)}>
            Exportar CSV
          </button>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={() => printDailyAgendaReport(filters.date, data.appointmentsTodayList, {
              total: data.appointmentsToday,
            })}
          >
            Agenda do dia
          </button>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
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
            className="btn btn-secondary btn-sm"
            onClick={() => printOperationalHospitalizationSummary(data)}
          >
            Internação
          </button>
          <Link to="/bi" className="btn btn-sm">Abrir BI</Link>
        </div>
      </PageHeader>

      <ModuleTabs basePath="/" tabs={dashboardTabs} />

      <FilterBar>
        <div className="filter-field w-md">
          <label htmlFor="dashDate">Data da agenda</label>
          <DateNavigator date={filters.date} onChange={(value) => patch({ date: value })} />
        </div>
        <div className="filter-field w-xl">
          <label htmlFor="dashProf">Profissional</label>
          <select
            id="dashProf"
            value={filters.professionalId}
            onChange={(e) => patch({ professionalId: e.target.value })}
          >
            <option value="">Todos</option>
            {professionals.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </select>
        </div>
      </FilterBar>

      {lastRefresh && (
        <p className="dashboard-meta">
          Última atualização: {formatBrTime(lastRefresh.toISOString())}
          {data.generatedAt ? ` · Servidor: ${formatBrTime(data.generatedAt)}` : ''}
        </p>
      )}

      <DashboardAlertsPanel alerts={data.alerts} />

      <div className="dashboard-section-label">Indicadores principais</div>
      <div className="kpi-grid">
        <KpiCard label="Pacientes ativos" value={data.totalPatients} variant="primary" />
        <KpiCard label="Atendimentos hoje" value={data.attendancesToday} variant="info" />
        <KpiCard label="Internações ativas" value={data.activeHospitalizations} variant="success" />
        <KpiCard label="Leitos ocupados" value={`${data.occupiedBeds}/${data.totalBeds}`} variant="neutral" />
        <KpiCard label="Leitos disponíveis" value={data.availableBeds} variant="success" />
        <KpiCard label="Cirurgias hoje" value={data.surgeriesToday} variant="warning" />
        <KpiCard label="Receita do dia" value={formatCurrency(data.revenueToday)} variant="success" />
        <KpiCard
          label="Ocupação hospitalar"
          value={`${data.bedOccupancyRate}%`}
          variant={data.bedOccupancyRate >= 90 ? 'danger' : data.bedOccupancyRate >= 75 ? 'warning' : 'neutral'}
        />
      </div>

      <div className="dashboard-section-label">Mapa de leitos (resumo)</div>
      <div className="bed-status-legend">
        <span className="bed-legend-item bed-legend-free">Livre {data.availableBeds}</span>
        <span className="bed-legend-item bed-legend-occupied">Ocupado {data.occupiedBeds}</span>
        <span className="bed-legend-item bed-legend-cleaning">Higienização {data.cleaningBeds}</span>
        <span className="bed-legend-item bed-legend-blocked">Manutenção {data.maintenanceBeds}</span>
        <Link to="/internacao/leitos" className="btn btn-secondary btn-sm">Mapa completo</Link>
      </div>

      <div className="dashboard-section-label">Urgência e triagem</div>
      <div className="kpi-grid kpi-grid-6">
        <KpiCard label="PS — aguardando" value={data.emergencyWaiting} variant="warning" />
        <KpiCard label="PS — em atendimento" value={data.emergencyInCare} variant="info" />
        <KpiCard label="PS — críticos" value={data.emergencyCritical} variant="danger" />
        <KpiCard label="SLA ultrapassado" value={data.emergencySlaViolations} variant="danger" />
        <KpiCard label="Triagens hoje" value={data.triageToday} variant="primary" />
        <KpiCard label="Notificações" value={data.unreadNotifications} variant="neutral" />
      </div>

      <div className="dashboard-section-label">Diagnóstico, financeiro e infraestrutura</div>
      <div className="kpi-grid kpi-grid-6">
        <KpiCard label="Lab pendente" value={data.labOrdersPending} variant="warning" />
        <KpiCard label="Imagem pendente" value={data.imagingStudiesPending} variant="warning" />
        <KpiCard label="Receita do mês" value={formatCurrency(data.revenueThisMonth)} variant="success" />
        <KpiCard label="A receber" value={formatCurrency(data.revenuePending)} variant="danger" />
        <KpiCard label="Estoque baixo" value={data.lowStockProducts} variant="danger" />
        <KpiCard label="Falhas integração" value={data.integrationFailures} variant="warning" />
      </div>

      {!useClinicDashboard && (
        <div className="card" style={{ marginTop: 20 }}>
          <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Acesso rápido</h2>
          <div className="action-grid">
            {visibleQuickActions.map((action) => (
              <Link key={action.to} to={action.to} className="action-tile">
                <NavIcon name={action.icon} />
                <span>{action.label}</span>
              </Link>
            ))}
          </div>
        </div>
      )}

      <div className="grid-2" style={{ marginTop: 20 }}>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Status da agenda ({filters.date.split('-').reverse().join('/')})</div>
          <div className="card-panel-body">
            <DashboardAppointmentStatusChart data={data.appointmentStatusBreakdown ?? []} />
          </div>
        </div>

        <div className="card-panel appt-panel">
          <div className="card-panel-header">Atendimentos por hora</div>
          <div className="card-panel-body">
            <DashboardHourlyChart data={data.hourlyAttendances} />
          </div>
        </div>

        <div className="card-panel appt-panel">
          <div className="card-panel-header">Produção por especialidade (hoje)</div>
          <div className="card-panel-body">
            <div className="bar-chart dashboard-specialty-chart">
              {data.productionBySpecialty.map((s) => (
                <div key={s.label} className="bar-col" title={`${s.count}`}>
                  <div className="bar bar-specialty" style={{ height: `${(s.count / maxSpecialty) * 100}%` }} />
                  <span>{s.label.length > 10 ? `${s.label.slice(0, 10)}…` : s.label}</span>
                </div>
              ))}
              {data.productionBySpecialty.length === 0 && (
                <p style={{ color: 'var(--muted)', margin: 0 }}>Sem produção registrada hoje.</p>
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="grid-2" style={{ marginTop: 20 }}>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">
            Agendamentos de hoje — {data.appointmentsTodayList.length} exibido(s)
          </div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Horário</th><th>Paciente</th><th>Profissional</th><th>Status</th></tr>
              </thead>
              <tbody>
                {data.appointmentsTodayList.map((a) => (
                  <tr key={a.id}>
                    <td>{formatBrTime(a.scheduledAt)}</td>
                    <td>{a.patientName}</td>
                    <td>{a.professionalName} · {a.specialtyName}</td>
                    <td><AppointmentStatusBadge status={a.status} /></td>
                  </tr>
                ))}
                {data.appointmentsTodayList.length === 0 && (
                  <tr>
                    <td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                      Nenhum agendamento para hoje
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="card-panel-footer">
            <Link to="/agendamentos" className="btn btn-secondary btn-sm">Ver agenda completa</Link>
          </div>
        </div>

        <div className="card-panel appt-panel">
          <div className="card-panel-header">
            Fila do pronto-socorro — {data.emergencyQueue.length} paciente(s)
          </div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Chegada</th><th>Paciente</th><th>Queixa</th><th>Urgência</th></tr>
              </thead>
              <tbody>
                {data.emergencyQueue.map((v) => (
                  <tr key={v.id}>
                    <td>{formatBrTime(v.arrivedAt)}</td>
                    <td>{v.patientName}</td>
                    <td>{v.chiefComplaint.length > 40 ? `${v.chiefComplaint.slice(0, 40)}...` : v.chiefComplaint}</td>
                    <td>
                      <span className={`badge ${triageUrgencyCssClass(v.urgency)}`}>
                        {triageUrgencyLabels[v.urgency] ?? v.urgency}
                      </span>
                    </td>
                  </tr>
                ))}
                {data.emergencyQueue.length === 0 && (
                  <tr>
                    <td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                      Fila vazia — {emergencyStatusLabels.Aguardando}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="card-panel-footer">
            <Link to="/emergencia" className="btn btn-secondary btn-sm">Abrir emergência</Link>
            <Link to="/dashboard/assistencial" className="btn btn-secondary btn-sm">Painel assistencial</Link>
          </div>
        </div>
      </div>

      <div className="grid-2" style={{ marginTop: 20 }}>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Agendamentos — últimos 6 meses</div>
          <div className="card-panel-body">
            <div className="bar-chart">
              {data.monthlyAppointments.map((m) => (
                <div key={m.label} className="bar-col">
                  <div className="bar" style={{ height: `${(m.count / maxMonthly) * 100}%` }} title={`${m.count}`} />
                  <span>{m.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="card-panel appt-panel">
          <div className="card-panel-header">Laboratório por status</div>
          <div className="card-panel-body">
            <ul className="bi-list">
              {data.labOrdersByStatus.map((s) => (
                <li key={s.label}>
                  <span>{s.label}</span>
                  <strong>{s.count}</strong>
                </li>
              ))}
              {data.labOrdersByStatus.length === 0 && <li>Nenhum pedido de laboratório</li>}
            </ul>
          </div>
        </div>
      </div>

      <div className="grid-2" style={{ marginTop: 20 }}>
        <div>
          <div className="dashboard-section-label">Campanhas de saúde</div>
          <HealthCampaignsPanel />
        </div>
        <div>
          <div className="dashboard-section-label">Aniversariantes do mês</div>
          <MonthBirthdaysPanel birthdays={data.monthBirthdays} />
        </div>
      </div>
    </>
  );
}
