import { useEffect, useState } from 'react';
import { api, type AuditLogDto } from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { PageHeader } from '../components/PageHeader';
import { ModuleNav } from '../components/ModuleNav';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

export function AuditPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [logs, setLogs] = useState<AuditLogDto[]>([]);
  const [entityType, setEntityType] = useState('');

  useEffect(() => {
    if (!hasPermission('audit.read')) return;
    api.getAuditLogs(100, entityType || undefined).then(setLogs).catch(console.error);
  }, [hasPermission, entityType]);

  if (!hasPermission('audit.read')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title={breadcrumb.title || 'Auditoria / LGPD'}
        subtitle="Registro de ações sensíveis realizadas no sistema."
      />

      <ModuleNav
        basePath="/auditoria"
        tabs={[{ slug: '', label: 'Log de auditoria' }]}
        contextId="audit"
      />

      <div className="kpi-grid">
        <KpiCard label="Registros exibidos" value={logs.length} variant="primary" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Log de auditoria — {logs.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field grow">
            <label htmlFor="auditEntity">Filtrar entidade</label>
            <input
              id="auditEntity"
              value={entityType}
              onChange={(e) => setEntityType(e.target.value)}
              placeholder="ex: patients, emergency"
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Data</th><th>Usuário</th><th>Ação</th><th>Entidade</th><th>Detalhes</th><th>IP</th></tr>
            </thead>
            <tbody>
              {logs.map((l) => (
                <tr key={l.id}>
                  <td>{formatBrDateTime(l.createdAt)}</td>
                  <td>{l.userEmail}</td>
                  <td>{l.action}</td>
                  <td>{l.entityType}</td>
                  <td className="mono">{l.details}</td>
                  <td>{l.ipAddress ?? '—'}</td>
                </tr>
              ))}
              {logs.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum registro</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
