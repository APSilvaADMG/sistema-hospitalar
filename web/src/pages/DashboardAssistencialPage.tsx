import { useEffect, useState } from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import {
  api,
  emergencyStatusLabels,
  triageUrgencyCssClass,
  triageUrgencyLabels,
  type OperationalDashboardDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { DashboardHourlyChart } from '../components/dashboard/DashboardHourlyChart';
import { occupancyRateTone, OpsDashKpi } from '../components/dashboard/OpsDashKpi';
import { ModuleTabs } from '../components/ModuleTabs';
import { PageHeader } from '../components/PageHeader';
import { dashboardTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { formatBrTime } from '../utils/dateUtils';

const REFRESH_MS = 30_000;

export function DashboardAssistencialPage() {
  const { hasPermission, hasRole } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [data, setData] = useState<OperationalDashboardDto | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  const isPatient = hasRole('Patient');
  const isStaff = !isPatient && hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage');

  async function loadDashboard() {
    try {
      const result = await api.getOperationalDashboard();
      setData(result);
      setError('');
      setLastRefresh(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar dashboard assistencial');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!isStaff) {
      setLoading(false);
      return;
    }
    loadDashboard();
    const timer = window.setInterval(loadDashboard, REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [isStaff]);

  if (isPatient) return <Navigate to="/portal-paciente" replace />;
  if (!isStaff) return <div className="card">Acesso restrito à equipe assistencial.</div>;
  if (loading && !data) return <div className="card">Carregando painel assistencial...</div>;
  if (error && !data) return <div className="alert alert-error">{error}</div>;
  if (!data) return null;

  const psTone = data.emergencyWaiting > 5 ? 'red' : data.emergencyWaiting > 0 ? 'yellow' : 'green';
  const waitTone = data.averageEmergencyWaitMinutes > 60 ? 'red' : data.averageEmergencyWaitMinutes > 30 ? 'yellow' : 'teal';

  return (
    <>
      <PageHeader
        eyebrow="TELA-002 · Dashboard Assistencial"
        title={breadcrumb.title || 'Operação assistencial'}
        subtitle="Fluxo de pacientes, tempos de espera e classificação de risco — atualização automática a cada 30s."
      >
        <div className="dashboard-header-actions">
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => loadDashboard()}>
            Atualizar
          </button>
        </div>
      </PageHeader>

      <ModuleTabs basePath="/" tabs={dashboardTabs} />

      <div className="ops-dashboard">
        {lastRefresh && (
          <p className="dashboard-meta">
            Última atualização: {formatBrTime(lastRefresh.toISOString())}
            {data.generatedAt ? ` · Servidor: ${formatBrTime(data.generatedAt)}` : ''}
          </p>
        )}

        <section aria-label="Indicadores assistenciais">
          <h2 className="feegow-dash-section-label">Pronto-socorro e triagem</h2>
          <div className="feegow-dash-kpi-grid feegow-dash-kpi-grid-primary" style={{ marginTop: 12 }}>
            <OpsDashKpi
              value={data.emergencyWaiting}
              label="Pacientes aguardando (PS)"
              tone={psTone}
              footer={<Link to="/emergencia">Abrir PS</Link>}
            />
            <OpsDashKpi
              value={`${Math.round(data.averageEmergencyWaitMinutes)} min`}
              label="Tempo médio de espera"
              tone={waitTone}
            />
            <OpsDashKpi
              value={data.emergencySlaViolations}
              label="SLA ultrapassado"
              tone={data.emergencySlaViolations > 0 ? 'red' : 'neutral'}
            />
            <OpsDashKpi value={data.triageEmergencyToday} label="Triagem vermelha hoje" tone="red" />
            <OpsDashKpi value={data.emergencyInCare} label="PS em atendimento" tone="teal" />
            <OpsDashKpi value={data.emergencyCritical} label="PS críticos" tone="red" />
          </div>
        </section>

        <section aria-label="Capacidade e diagnóstico">
          <div className="feegow-dash-kpi-grid feegow-dash-kpi-grid-secondary">
            <OpsDashKpi
              value={`${data.bedOccupancyRate}%`}
              label="Ocupação de leitos"
              tone={occupancyRateTone(data.bedOccupancyRate)}
              footer={<span>{data.occupiedBeds}/{data.totalBeds} leitos</span>}
            />
            <OpsDashKpi value={data.triageToday} label="Triagens hoje" tone="teal" />
            <OpsDashKpi
              value={data.labOrdersPending}
              label="Lab pendente"
              tone={data.labOrdersPending > 0 ? 'yellow' : 'green'}
              footer={<Link to="/laboratorio">Laboratório</Link>}
            />
            <OpsDashKpi
              value={data.imagingStudiesPending}
              label="Imagem pendente"
              tone={data.imagingStudiesPending > 0 ? 'yellow' : 'green'}
              footer={<Link to="/imagem">Imagem</Link>}
            />
          </div>
        </section>

        <div className="ops-dash-columns">
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Fluxo de pacientes por hora</div>
            <div className="card-panel-body">
              <DashboardHourlyChart data={data.hourlyAttendances} />
            </div>
          </div>

          <div className="card-panel appt-panel">
            <div className="card-panel-header">
              Fila do pronto-socorro
              <span className="feegow-dash-panel-meta" style={{ marginLeft: 8, fontWeight: 500, color: 'var(--muted)' }}>
                {data.emergencyQueue.length} paciente(s)
              </span>
            </div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <div className="feegow-dash-table-wrap">
                <table className="feegow-dash-table">
                  <thead>
                    <tr><th>Chegada</th><th>Paciente</th><th>Queixa</th><th>Urgência</th><th>Status</th></tr>
                  </thead>
                  <tbody>
                    {data.emergencyQueue.map((v) => (
                      <tr key={v.id}>
                        <td className="feegow-dash-table-time">{formatBrTime(v.arrivedAt)}</td>
                        <td>{v.patientName}</td>
                        <td className="feegow-dash-table-muted" title={v.chiefComplaint}>
                          {v.chiefComplaint.length > 36 ? `${v.chiefComplaint.slice(0, 36)}…` : v.chiefComplaint}
                        </td>
                        <td>
                          <span className={`badge ${triageUrgencyCssClass(v.urgency)}`}>
                            {triageUrgencyLabels[v.urgency] ?? v.urgency}
                          </span>
                        </td>
                        <td>{emergencyStatusLabels[v.status] ?? v.status}</td>
                      </tr>
                    ))}
                    {data.emergencyQueue.length === 0 && (
                      <tr>
                        <td colSpan={5} className="feegow-dash-empty">
                          Fila vazia — {emergencyStatusLabels.Aguardando}
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
            <div className="card-panel-footer">
              <Link to="/emergencia" className="btn btn-secondary btn-sm">Abrir emergência</Link>
              <Link to="/emergencia/classificacao-risco" className="btn btn-secondary btn-sm">Triagem</Link>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
