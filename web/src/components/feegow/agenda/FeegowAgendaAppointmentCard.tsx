import { Link } from 'react-router-dom';
import { appointmentStatusLabels, formatAppointmentStatus, normalizeAppointmentStatus, type AppointmentDto } from '../../../api/client';
import { AppointmentStatusBadge } from '../../AppointmentStatusBadge';
import { formatBrTime } from '../../../utils/dateUtils';

type Props = {
  appointment: AppointmentDto;
  compact?: boolean;
  showTimeInTitle?: boolean;
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onClinicalData?: (appt: AppointmentDto) => void;
};

export function FeegowAgendaAppointmentCard({
  appointment,
  compact = false,
  showTimeInTitle = false,
  canManage,
  onStatusChange,
  onClinicalData,
}: Props) {
  return (
    <div className={`feegow-agenda-appt${compact ? ' feegow-agenda-appt-compact' : ''}`}>
      <div className="feegow-agenda-appt-main">
        <strong>
          {showTimeInTitle ? `${formatBrTime(appointment.scheduledAt)} · ` : ''}
          {appointment.patientName}
        </strong>
        <span>
          {!showTimeInTitle ? `${formatBrTime(appointment.scheduledAt)} · ` : ''}
          {formatAppointmentStatus(appointment.status)}
        </span>
        {!compact && appointment.reason ? <small>{appointment.reason}</small> : null}
      </div>
      {!compact ? (
        <div className="feegow-agenda-appt-actions">
          <AppointmentStatusBadge status={appointment.status} />
          {canManage && onStatusChange ? (
            <select
              className="appt-status-select"
              value={normalizeAppointmentStatus(appointment.status)}
              onChange={(e) => onStatusChange(appointment.id, Number(e.target.value))}
            >
              {Object.entries(appointmentStatusLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          ) : null}
          {onClinicalData ? (
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => onClinicalData(appointment)}>
              TISS
            </button>
          ) : null}
          <Link to={`/pacientes/${appointment.patientId}/prontuario`} className="btn btn-secondary btn-sm">
            Prontuário
          </Link>
        </div>
      ) : null}
    </div>
  );
}
