import type { DashboardHourlyStatDto } from '../../api/client';

export function DashboardHourlyChart({ data }: { data: DashboardHourlyStatDto[] }) {
  const max = Math.max(...data.map((d) => d.count), 1);

  if (data.length === 0) {
    return <p style={{ color: 'var(--muted)', margin: 0 }}>Sem atendimentos registrados hoje.</p>;
  }

  return (
    <div className="bar-chart dashboard-hourly-chart">
      {data.map((item) => (
        <div key={item.hour} className="bar-col" title={`${item.count} atendimento(s)`}>
          <div className="bar" style={{ height: `${(item.count / max) * 100}%` }} />
          <span>{String(item.hour).padStart(2, '0')}h</span>
        </div>
      ))}
    </div>
  );
}
