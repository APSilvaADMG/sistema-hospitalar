import { formatAppointmentStatus, type BiStatusCountDto } from '../../api/client';

type DashboardAppointmentStatusChartProps = {
  data: BiStatusCountDto[];
};

const statusColors: Record<string, string> = {
  Scheduled: '#0288d1',
  Confirmed: '#1565c0',
  InProgress: '#ef6c00',
  Completed: '#2e7d32',
  Cancelled: '#94a3b8',
  NoShow: '#c62828',
  '1': '#0288d1',
  '2': '#1565c0',
  '3': '#ef6c00',
  '4': '#2e7d32',
  '5': '#94a3b8',
  '6': '#c62828',
};

function resolveLabel(label: string) {
  return formatAppointmentStatus(label);
}

export function DashboardAppointmentStatusChart({ data }: DashboardAppointmentStatusChartProps) {
  if (data.length === 0) {
    return <p style={{ color: 'var(--muted)', margin: 0 }}>Sem agendamentos no período.</p>;
  }

  const max = Math.max(...data.map((d) => d.count), 1);

  return (
    <div className="dashboard-status-chart">
      {data.map((item) => (
        <div key={item.label} className="dashboard-status-row">
          <span className="dashboard-status-label">{resolveLabel(item.label)}</span>
          <div className="dashboard-status-bar-track">
            <div
              className="dashboard-status-bar-fill"
              style={{
                width: `${(item.count / max) * 100}%`,
                background: statusColors[item.label] ?? 'var(--primary)',
              }}
            />
          </div>
          <span className="dashboard-status-count">{item.count}</span>
        </div>
      ))}
    </div>
  );
}
