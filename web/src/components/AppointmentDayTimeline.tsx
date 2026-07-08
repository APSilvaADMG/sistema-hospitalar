import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { appointmentStatusLabels, type AppointmentDto } from '../api/client';
import { AppointmentStatusBadge } from './AppointmentStatusBadge';
import { formatBrTime } from '../utils/dateUtils';

type AppointmentDayTimelineProps = {
  appointments: AppointmentDto[];
  date: string;
  startHour?: number;
  endHour?: number;
  slotMinutes?: number;
  professionalName?: string;
  roomLabel?: string;
  readOnly?: boolean;
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onClinicalData?: (appt: AppointmentDto) => void;
  onCreateAt?: (slotIso: string) => void;
};

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join('');
}

function pad2(n: number) {
  return String(n).padStart(2, '0');
}

function buildSlots(startHour: number, endHour: number, slotMinutes: number) {
  const slots: { key: string; label: string }[] = [];
  const totalStart = startHour * 60;
  const totalEnd = endHour * 60;
  for (let t = totalStart; t < totalEnd; t += slotMinutes) {
    const h = Math.floor(t / 60);
    const m = t % 60;
    const key = `${pad2(h)}:${pad2(m)}`;
    slots.push({ key, label: key });
  }
  return slots;
}

function appointmentSlotKey(appt: AppointmentDto, slotMinutes: number) {
  const d = new Date(appt.scheduledAt);
  const total = d.getHours() * 60 + d.getMinutes();
  const snapped = Math.floor(total / slotMinutes) * slotMinutes;
  const h = Math.floor(snapped / 60);
  const m = snapped % 60;
  return `${pad2(h)}:${pad2(m)}`;
}

export function AppointmentDayTimeline({
  appointments,
  date,
  startHour = 8,
  endHour = 18,
  slotMinutes = 30,
  professionalName,
  roomLabel,
  readOnly = false,
  canManage = false,
  onStatusChange,
  onClinicalData,
  onCreateAt,
}: AppointmentDayTimelineProps) {
  const slots = useMemo(
    () => buildSlots(startHour, endHour, slotMinutes),
    [startHour, endHour, slotMinutes],
  );

  const bySlot = useMemo(() => {
    const map = new Map<string, AppointmentDto[]>();
    for (const appt of appointments) {
      const key = appointmentSlotKey(appt, slotMinutes);
      const list = map.get(key) ?? [];
      list.push(appt);
      map.set(key, list);
    }
    return map;
  }, [appointments, slotMinutes]);

  const headerProfessional = professionalName ?? appointments[0]?.professionalName;
  const headerRoom = roomLabel ?? appointments[0]?.room;

  return (
    <div className="agenda-day-timeline">
      {(headerProfessional || headerRoom) && (
        <header className="agenda-timeline-header">
          {headerProfessional ? (
            <div className="agenda-timeline-prof">
              <span className="agenda-timeline-prof-avatar">{initials(headerProfessional)}</span>
              <div>
                <strong>{headerProfessional}</strong>
                {headerRoom ? <span className="agenda-timeline-room">Sala {headerRoom}</span> : null}
              </div>
            </div>
          ) : null}
          <span className="agenda-timeline-date">{date.split('-').reverse().join('/')}</span>
        </header>
      )}

      <div className="agenda-timeline-grid">
        {slots.map((slot) => {
          const items = bySlot.get(slot.key) ?? [];
          const slotIso = `${date}T${slot.key}`;

          return (
            <div key={slot.key} className={`agenda-timeline-row${items.length === 0 ? ' is-empty' : ''}`}>
              <div className="agenda-timeline-time">{slot.label}</div>
              <div className="agenda-timeline-slot">
                {items.length === 0 ? (
                  <div className="agenda-timeline-empty">
                    {canManage && onCreateAt ? (
                      <button
                        type="button"
                        className="agenda-timeline-empty-btn"
                        onClick={() => onCreateAt(slotIso)}
                      >
                        Horário livre
                      </button>
                    ) : (
                      <span className="agenda-timeline-empty-label">—</span>
                    )}
                  </div>
                ) : (
                  items.map((appt) => (
                    <article
                      key={appt.id}
                      className={`agenda-timeline-appt appt-card-status-${appt.status}`}
                    >
                      <span className={`agenda-timeline-status-dot status-${appt.status}`} aria-hidden />
                      <div className="agenda-timeline-appt-main">
                        <div className="agenda-timeline-appt-top">
                          <strong>{appt.patientName}</strong>
                          <span className="agenda-timeline-appt-time">{formatBrTime(appt.scheduledAt)}</span>
                        </div>
                        <div className="agenda-timeline-appt-meta">
                          <span>{appt.professionalName}</span>
                          {appt.specialtyName ? (
                            <>
                              <span className="appt-meta-dot">•</span>
                              <span>{appt.specialtyName}</span>
                            </>
                          ) : null}
                          {appt.room ? (
                            <>
                              <span className="appt-meta-dot">•</span>
                              <span>Sala {appt.room}</span>
                            </>
                          ) : null}
                          {appt.reason ? (
                            <>
                              <span className="appt-meta-dot">•</span>
                              <span>{appt.reason}</span>
                            </>
                          ) : null}
                        </div>
                      </div>
                      {!readOnly && (
                        <div className="agenda-timeline-appt-actions">
                          <AppointmentStatusBadge status={appt.status} />
                          {canManage && onStatusChange ? (
                            <select
                              className="appt-status-select"
                              value={appt.status}
                              onChange={(e) => onStatusChange(appt.id, Number(e.target.value))}
                              title="Alterar status"
                            >
                              {Object.entries(appointmentStatusLabels).map(([value, label]) => (
                                <option key={value} value={value}>{label}</option>
                              ))}
                            </select>
                          ) : null}
                          {onClinicalData ? (
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() => onClinicalData(appt)}
                            >
                              TISS
                            </button>
                          ) : null}
                          <Link
                            to={`/pacientes/${appt.patientId}/prontuario`}
                            className="btn btn-secondary btn-sm"
                          >
                            Prontuário
                          </Link>
                        </div>
                      )}
                    </article>
                  ))
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
