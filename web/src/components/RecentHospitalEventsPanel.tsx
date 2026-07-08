import { useEffect, useState } from 'react';
import {
  api,
  hospitalEventStatusLabels,
  hospitalEventTypeLabels,
  type HospitalEventLogDto,
} from '../api/client';
import { formatBrDateTime } from '../utils/dateUtils';

type Props = {
  limit?: number;
  title?: string;
};

export function RecentHospitalEventsPanel({ limit = 20, title = 'Eventos recentes' }: Props) {
  const [events, setEvents] = useState<HospitalEventLogDto[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    api.getRecentHospitalEvents(limit)
      .then(setEvents)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Erro ao carregar eventos.'));
  }, [limit]);

  return (
    <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
      <div className="card-panel-header">{title}</div>
      <div className="card-panel-body" style={{ padding: 0 }}>
        {error && <div className="alert alert-error" style={{ margin: 16 }}>{error}</div>}
        <table className="data-table">
          <thead>
            <tr>
              <th>Quando</th>
              <th>Evento</th>
              <th>Status</th>
              <th>Entidade</th>
            </tr>
          </thead>
          <tbody>
            {events.map((e) => (
              <tr key={e.id}>
                <td>{formatBrDateTime(e.createdAt)}</td>
                <td>
                  <strong>{hospitalEventTypeLabels[e.eventType] ?? e.eventType}</strong>
                  {e.errorMessage && (
                    <div style={{ fontSize: 12, color: 'var(--danger)' }}>{e.errorMessage}</div>
                  )}
                </td>
                <td>
                  <span className={`badge ${e.status === 'Failed' ? 'badge-danger' : e.status === 'Processed' ? 'badge-success' : 'badge-warning'}`}>
                    {hospitalEventStatusLabels[e.status] ?? e.status}
                  </span>
                </td>
                <td style={{ color: 'var(--muted)', fontSize: 13 }}>
                  {e.relatedEntityType ? `${e.relatedEntityType}` : '—'}
                </td>
              </tr>
            ))}
            {events.length === 0 && !error && (
              <tr>
                <td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                  Nenhum evento registrado.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
