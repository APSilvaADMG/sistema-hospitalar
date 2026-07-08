import { formatAppointmentStatus, normalizeAppointmentStatus } from '../api/client';

const statusClass: Record<number, string> = {
  1: 'status-scheduled',
  2: 'status-confirmed',
  3: 'status-in-progress',
  4: 'status-done',
  5: 'status-cancelled',
  6: 'status-no-show',
};

type AppointmentStatusBadgeProps = {
  status: number | string;
};

export function AppointmentStatusBadge({ status }: AppointmentStatusBadgeProps) {
  const code = normalizeAppointmentStatus(status);
  const label = formatAppointmentStatus(status);
  const cls = statusClass[code] ?? 'status-scheduled';

  return <span className={`appt-status ${cls}`}>{label}</span>;
}
