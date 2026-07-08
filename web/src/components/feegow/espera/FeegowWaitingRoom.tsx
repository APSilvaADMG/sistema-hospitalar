import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import type { AppointmentDto, EmergencyVisitDto, ProfessionalDto } from '../../../api/client';
import { formatEmergencyVisitStatus, normalizeAppointmentStatus } from '../../../api/client';
import { AppointmentStatusBadge } from '../../AppointmentStatusBadge';
import { formatBrTime } from '../../../utils/dateUtils';

type StatusFilterKey = 'waiting-in-care' | 'waiting' | 'in-care' | 'all';

type SortKey = 'scheduled' | 'arrival' | 'patient';

export type WaitingRoomStats = {
  waiting: number;
  inCare: number;
  completedToday: number;
  avgWaitMinutes: number;
  byRoom: RoomSummary[];
};

export type RoomSummary = {
  room: string;
  waiting: number;
  inCare: number;
  nextPatient?: string;
  nextTime?: string;
};

type Props = {
  appointments: AppointmentDto[];
  emergencyVisits: EmergencyVisitDto[];
  professionals: ProfessionalDto[];
  selectedDate: string;
  onDateChange: (date: string) => void;
  stats: WaitingRoomStats;
  statusFilter: StatusFilterKey;
  onStatusFilterChange: (value: StatusFilterKey) => void;
  sortBy: SortKey;
  onSortByChange: (value: SortKey) => void;
  professionalFilter: string;
  onProfessionalFilterChange: (value: string) => void;
  specialtyFilter: string;
  onSpecialtyFilterChange: (value: string) => void;
  onRefresh: () => void;
  loading?: boolean;
  canManage?: boolean;
  onCallPatient?: (id: string) => void;
  onFinishCare?: (id: string) => void;
  onPrintPsQueue?: () => void;
  onPrintDailyAgenda?: () => void;
};

const STATUS_FILTER_LABELS: Record<StatusFilterKey, string> = {
  'waiting-in-care': 'Aguardando, Em Atendimento',
  waiting: 'Aguardando',
  'in-care': 'Em Atendimento',
  all: 'Todos os status',
};

function statusesForFilter(key: StatusFilterKey): number[] {
  switch (key) {
    case 'waiting':
      return [1, 2];
    case 'in-care':
      return [3];
    case 'all':
      return [1, 2, 3, 4];
    default:
      return [1, 2, 3];
  }
}

function urgencyLabel(urgency: string): string {
  const map: Record<string, string> = {
    Emergency: 'Emergência',
    High: 'Alta',
    Medium: 'Média',
    Low: 'Baixa',
  };
  return map[urgency] ?? urgency;
}

export function FeegowWaitingRoom({
  appointments,
  emergencyVisits,
  professionals,
  selectedDate,
  onDateChange,
  stats,
  statusFilter,
  onStatusFilterChange,
  sortBy,
  onSortByChange,
  professionalFilter,
  onProfessionalFilterChange,
  specialtyFilter,
  onSpecialtyFilterChange,
  onRefresh,
  loading,
  canManage,
  onCallPatient,
  onFinishCare,
  onPrintPsQueue,
  onPrintDailyAgenda,
}: Props) {
  const specialties = useMemo(() => {
    const set = new Set(appointments.map((a) => a.specialtyName).filter(Boolean));
    return Array.from(set).sort();
  }, [appointments]);

  const rows = useMemo(() => {
    const allowed = new Set(statusesForFilter(statusFilter));
    return appointments
      .filter((a) => allowed.has(normalizeAppointmentStatus(a.status)))
      .filter((a) => !professionalFilter || a.professionalId === professionalFilter)
      .filter((a) => !specialtyFilter || a.specialtyName === specialtyFilter)
      .sort((a, b) => {
        if (sortBy === 'patient') {
          return a.patientName.localeCompare(b.patientName, 'pt-BR');
        }
        if (sortBy === 'arrival') {
          return a.scheduledAt.localeCompare(b.scheduledAt);
        }
        return new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime();
      });
  }, [appointments, statusFilter, professionalFilter, specialtyFilter, sortBy]);

  const isEmpty = appointments.length === 0 && emergencyVisits.length === 0;

  return (
    <div className="feegow-waiting-room">
      <header className="feegow-waiting-head">
        <div className="feegow-waiting-title-row">
          <h1 className="feegow-waiting-title">Sala de Espera</h1>
          <p className="feegow-waiting-crumb">
            <span className="feegow-agenda-cal-icon" aria-hidden>🕐</span>
            <span className="feegow-crumb-sep">/</span>
            pacientes aguardando
          </p>
        </div>
        <div className="feegow-waiting-head-filters">
          <label className="feegow-field">
            <span>Data</span>
            <input type="date" value={selectedDate} onChange={(e) => onDateChange(e.target.value)} />
          </label>
          <label className="feegow-field">
            <span>Status</span>
            <select
              value={statusFilter}
              onChange={(e) => onStatusFilterChange(e.target.value as StatusFilterKey)}
            >
              {(Object.keys(STATUS_FILTER_LABELS) as StatusFilterKey[]).map((key) => (
                <option key={key} value={key}>{STATUS_FILTER_LABELS[key]}</option>
              ))}
            </select>
          </label>
          <label className="feegow-field feegow-waiting-sort-field">
            <span>Ordenar por</span>
            <select value={sortBy} onChange={(e) => onSortByChange(e.target.value as SortKey)}>
              <option value="scheduled">Horário Agendado</option>
              <option value="arrival">Horário de chegada</option>
              <option value="patient">Nome do paciente</option>
            </select>
          </label>
          {onPrintDailyAgenda ? (
            <button type="button" className="btn btn-secondary btn-sm" onClick={onPrintDailyAgenda}>
              Imprimir agenda
            </button>
          ) : null}
          {onPrintPsQueue ? (
            <button type="button" className="btn btn-secondary btn-sm" onClick={onPrintPsQueue}>
              Imprimir fila PS
            </button>
          ) : null}
        </div>
      </header>

      <div className="feegow-waiting-kpi-grid">
        <article className="feegow-waiting-kpi">
          <span className="feegow-waiting-kpi-value">{stats.waiting}</span>
          <span className="feegow-waiting-kpi-label">Aguardando</span>
        </article>
        <article className="feegow-waiting-kpi feegow-waiting-kpi-care">
          <span className="feegow-waiting-kpi-value">{stats.inCare}</span>
          <span className="feegow-waiting-kpi-label">Em atendimento</span>
        </article>
        <article className="feegow-waiting-kpi feegow-waiting-kpi-done">
          <span className="feegow-waiting-kpi-value">{stats.completedToday}</span>
          <span className="feegow-waiting-kpi-label">Finalizados hoje</span>
        </article>
        <article className="feegow-waiting-kpi feegow-waiting-kpi-time">
          <span className="feegow-waiting-kpi-value">{stats.avgWaitMinutes} min</span>
          <span className="feegow-waiting-kpi-label">Tempo médio estimado</span>
        </article>
      </div>

      {stats.byRoom.length > 0 ? (
        <section className="feegow-waiting-room-panel">
          <h2 className="feegow-waiting-panel-title">Resumo por sala / consultório</h2>
          <div className="feegow-waiting-room-grid">
            {stats.byRoom.map((room) => (
              <article key={room.room} className="feegow-waiting-room-card">
                <h3>{room.room}</h3>
                <div className="feegow-waiting-room-counts">
                  <span><strong>{room.waiting}</strong> aguardando</span>
                  <span><strong>{room.inCare}</strong> em atendimento</span>
                </div>
                {room.nextPatient ? (
                  <p className="feegow-waiting-room-next">
                    Próximo: {room.nextPatient}
                    {room.nextTime ? ` — ${room.nextTime}` : ''}
                  </p>
                ) : (
                  <p className="feegow-waiting-room-next feegow-waiting-room-next-empty">Sem pacientes na fila</p>
                )}
              </article>
            ))}
          </div>
        </section>
      ) : null}

      <div className="feegow-waiting-card">
        <div className="feegow-waiting-toolbar">
          <button
            type="button"
            className="feegow-waiting-refresh"
            onClick={onRefresh}
            title="Atualizar"
            aria-label="Atualizar lista"
            disabled={loading}
          >
            ↻
          </button>
          <div className="feegow-waiting-toolbar-filters">
            <label className="feegow-field">
              <span>Profissional</span>
              <select
                value={professionalFilter}
                onChange={(e) => onProfessionalFilterChange(e.target.value)}
              >
                <option value="">Selecione o profissional…</option>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            </label>
            <label className="feegow-field">
              <span>Especialidade</span>
              <select
                value={specialtyFilter}
                onChange={(e) => onSpecialtyFilterChange(e.target.value)}
              >
                <option value="">Todas as especialidades…</option>
                {specialties.map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            </label>
          </div>
        </div>

        {isEmpty ? (
          <div className="feegow-waiting-empty">
            <p>Nenhum agendamento ou paciente de PS para esta data.</p>
            <button type="button" className="feegow-waiting-reload-btn" onClick={onRefresh} disabled={loading}>
              Recarregar
            </button>
          </div>
        ) : rows.length === 0 ? (
          <p className="feegow-agenda-empty-hint feegow-waiting-empty-inline">
            Nenhum paciente aguardando com os filtros selecionados.
          </p>
        ) : (
          <div className="feegow-waiting-table-wrap">
            <table className="feegow-agenda-data-table feegow-waiting-table">
              <thead>
                <tr>
                  <th>Sala</th>
                  <th>Horário agendado</th>
                  <th>Chegada</th>
                  <th>Paciente</th>
                  <th>Profissional</th>
                  <th>Especialidade</th>
                  <th>Status</th>
                  {canManage ? <th /> : null}
                </tr>
              </thead>
              <tbody>
                {rows.map((appt) => (
                  <tr key={appt.id}>
                    <td>{appt.room ?? '—'}</td>
                    <td>{formatBrTime(appt.scheduledAt)}</td>
                    <td>{normalizeAppointmentStatus(appt.status) >= 2 ? formatBrTime(appt.scheduledAt) : '—'}</td>
                    <td>
                      <Link to={`/pacientes/${appt.patientId}/prontuario`}>{appt.patientName}</Link>
                    </td>
                    <td>{appt.professionalName}</td>
                    <td>{appt.specialtyName}</td>
                    <td><AppointmentStatusBadge status={appt.status} /></td>
                    {canManage ? (
                      <td className="feegow-waiting-actions">
                        {(normalizeAppointmentStatus(appt.status) === 1 || normalizeAppointmentStatus(appt.status) === 2) && onCallPatient ? (
                          <button type="button" className="btn btn-sm" onClick={() => onCallPatient(appt.id)}>
                            Chamar
                          </button>
                        ) : null}
                        {normalizeAppointmentStatus(appt.status) === 3 && onFinishCare ? (
                          <button type="button" className="btn btn-secondary btn-sm" onClick={() => onFinishCare(appt.id)}>
                            Finalizar
                          </button>
                        ) : null}
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {emergencyVisits.length > 0 ? (
        <section className="feegow-waiting-card feegow-waiting-ps-panel">
          <header className="feegow-waiting-ps-head">
            <h2>Pronto-Socorro — fila de triagem</h2>
            <span className="feegow-waiting-ps-badge">{emergencyVisits.length} aguardando</span>
          </header>
          <div className="feegow-waiting-table-wrap">
            <table className="feegow-agenda-data-table feegow-waiting-table">
              <thead>
                <tr>
                  <th>Chegada</th>
                  <th>Paciente</th>
                  <th>Queixa</th>
                  <th>Urgência</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {emergencyVisits.map((visit) => (
                  <tr key={visit.id}>
                    <td>{formatBrTime(visit.arrivedAt)}</td>
                    <td>
                      <Link to={`/pacientes/${visit.patientId}/prontuario`}>{visit.patientName}</Link>
                    </td>
                    <td>{visit.chiefComplaint}</td>
                    <td><span className={`feegow-ps-urgency feegow-ps-urgency-${visit.urgency.toLowerCase()}`}>{urgencyLabel(visit.urgency)}</span></td>
                    <td>
                      <span className="appt-status status-in-progress">{formatEmergencyVisitStatus(visit.status)}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </div>
  );
}

export type { StatusFilterKey, SortKey };

const AVG_CONSULTATION_MINUTES = 25;
const MAX_DISPLAY_WAIT_MINUTES = 90;

function estimatePatientWaitMinutes(
  appt: AppointmentDto,
  queuePosition: number,
  nowMs: number,
): number {
  const status = normalizeAppointmentStatus(appt.status);
  if (status === 2) {
    const elapsed = Math.max(0, (nowMs - new Date(appt.scheduledAt).getTime()) / 60_000);
    return Math.min(elapsed, MAX_DISPLAY_WAIT_MINUTES);
  }

  return Math.min(queuePosition * AVG_CONSULTATION_MINUTES, MAX_DISPLAY_WAIT_MINUTES);
}

export function computeWaitingRoomStats(appointments: AppointmentDto[]): WaitingRoomStats {
  const waiting = appointments.filter((a) => {
    const s = normalizeAppointmentStatus(a.status);
    return s === 1 || s === 2;
  }).length;
  const inCare = appointments.filter((a) => normalizeAppointmentStatus(a.status) === 3).length;
  const completedToday = appointments.filter((a) => normalizeAppointmentStatus(a.status) === 4).length;

  const now = Date.now();
  const waitingAppts = appointments.filter((a) => {
    const s = normalizeAppointmentStatus(a.status);
    return s === 1 || s === 2;
  });
  const sortedWaiting = [...waitingAppts].sort(
    (a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime(),
  );
  const avgWaitMinutes = sortedWaiting.length === 0
    ? 0
    : Math.round(
        sortedWaiting.reduce(
          (sum, appt, index) => sum + estimatePatientWaitMinutes(appt, index + 1, now),
          0,
        ) / sortedWaiting.length,
      );

  const roomMap = new Map<string, { waiting: number; inCare: number; next?: AppointmentDto }>();
  for (const appt of appointments) {
    const room = appt.room?.trim() || 'Sem sala';
    const entry = roomMap.get(room) ?? { waiting: 0, inCare: 0, next: undefined };
    const s = normalizeAppointmentStatus(appt.status);
    if (s === 1 || s === 2) {
      entry.waiting += 1;
      if (!entry.next || new Date(appt.scheduledAt) < new Date(entry.next.scheduledAt)) {
        entry.next = appt;
      }
    } else if (s === 3) {
      entry.inCare += 1;
    }
    roomMap.set(room, entry);
  }

  const byRoom: RoomSummary[] = Array.from(roomMap.entries())
    .map(([room, data]) => ({
      room,
      waiting: data.waiting,
      inCare: data.inCare,
      nextPatient: data.next?.patientName,
      nextTime: data.next ? formatBrTime(data.next.scheduledAt) : undefined,
    }))
    .sort((a, b) => a.room.localeCompare(b.room, 'pt-BR'));

  return { waiting, inCare, completedToday, avgWaitMinutes, byRoom };
}
