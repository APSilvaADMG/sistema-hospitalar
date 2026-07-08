import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type UserMissionDto, type UserMissionsDto } from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { ModuleTabs } from '../components/ModuleTabs';
import { PageHeader } from '../components/PageHeader';
import { dashboardTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';

function priorityLabel(p: string) {
  switch (p) {
    case 'Critica': return 'Crítica';
    case 'Alta': return 'Alta';
    case 'Normal': return 'Média';
    default: return 'Baixa';
  }
}

function priorityClass(p: string) {
  if (p === 'Critica' || p === 'Alta') return 'high';
  if (p === 'Normal') return 'medium';
  return 'low';
}

export function DashboardTasksPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [data, setData] = useState<UserMissionsDto | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.getMyMissions()
      .then(setData)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar missões.'));
  }, []);

  const tasks = useMemo(() => data?.missions ?? [], [data]);
  const high = data?.highPriority ?? 0;

  async function completeTask(task: UserMissionDto) {
    if (!task.isPendingItem) return;
    await api.completeMission(task.id);
    setData(await api.getMyMissions());
  }

  return (
    <>
      <PageHeader
        eyebrow="Dashboard"
        title={breadcrumb.title || 'Tarefas Pendentes'}
        subtitle="Missões personalizadas por perfil — recepção, enfermagem, almoxarifado e hotelaria."
      />

      <ModuleTabs basePath="/" tabs={dashboardTabs} />

      {error && <div className="alert alert-error">{error}</div>}

      <div className="kpi-grid">
        <KpiCard label="Missões" value={data?.total ?? '—'} variant="primary" />
        <KpiCard label="Prioridade alta" value={high} variant={high > 0 ? 'warning' : 'default'} />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Minhas missões</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Prioridade</th><th>Missão</th><th>Detalhe</th><th>Prazo</th><th>Ação</th></tr>
            </thead>
            <tbody>
              {tasks.map((t) => (
                <tr key={`${t.isPendingItem ? 'p' : 'm'}-${t.id}`}>
                  <td>
                    <span className={`badge priority-${priorityClass(t.priority)}`}>
                      {priorityLabel(t.priority)}
                    </span>
                  </td>
                  <td><strong>{t.title}</strong>{t.setor && <div style={{ fontSize: 12, color: 'var(--muted)' }}>{t.setor}</div>}</td>
                  <td>{t.description}</td>
                  <td>{t.dataLimite ? formatBrDateTime(t.dataLimite) : '—'}</td>
                  <td>
                    <div className="table-actions">
                      {t.linkDestino && <Link to={t.linkDestino} className="btn btn-secondary btn-sm">Abrir</Link>}
                      {t.isPendingItem && (
                        <button type="button" className="btn btn-sm" onClick={() => completeTask(t)}>
                          Concluir
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {tasks.length === 0 && (
                <tr>
                  <td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    {data ? 'Nenhuma missão pendente no momento.' : 'Carregando...'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
