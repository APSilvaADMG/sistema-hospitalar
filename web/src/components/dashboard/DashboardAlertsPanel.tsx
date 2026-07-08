import { Link } from 'react-router-dom';
import type { DashboardAlertDto } from '../../api/client';

const severityClass: Record<string, string> = {
  critical: 'alert-severity-critical',
  warning: 'alert-severity-warning',
  info: 'alert-severity-info',
};

export function DashboardAlertsPanel({ alerts }: { alerts: DashboardAlertDto[] }) {
  if (alerts.length === 0) {
    return (
      <div className="card dashboard-alerts-panel dashboard-alerts-empty">
        <h3>Alertas e pendências</h3>
        <p>Nenhum alerta crítico no momento.</p>
      </div>
    );
  }

  return (
    <div className="card dashboard-alerts-panel">
      <h3>Alertas e pendências</h3>
      <ul className="dashboard-alerts-list">
        {alerts.map((alert) => (
          <li key={alert.code} className={severityClass[alert.severity] ?? 'alert-severity-info'}>
            <div className="dashboard-alert-content">
              <strong>{alert.title}</strong>
              <span>{alert.message}</span>
            </div>
            {alert.linkPath ? (
              <Link to={alert.linkPath} className="btn btn-secondary btn-sm">
                Ver
              </Link>
            ) : null}
          </li>
        ))}
      </ul>
    </div>
  );
}
