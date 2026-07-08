import { useMemo, useState } from 'react';
import { appointmentStatusLabel, isAppointmentStatus, normalizeAppointmentStatus, type AppointmentDto, type ProfessionalDto } from '../../../api/client';
import { formatBrTime } from '../../../utils/dateUtils';
import { FeegowAgendaFilterSidebar } from './FeegowAgendaFilterSidebar';
import { FeegowAgendaScreenLayout } from './FeegowAgendaScreenLayout';

type Props = {
  date: string;
  appointments: AppointmentDto[];
  professionals: ProfessionalDto[];
  onCheckIn: (id: string) => void;
  canManage?: boolean;
};

const CHECKIN_STATUS_OPTIONS = [
  { value: '1', label: 'Aguardando' },
  { value: '2', label: 'Confirmado' },
  { value: '3', label: 'Em atendimento' },
  { value: '4', label: 'Atendido' },
  { value: '5', label: 'Cancelado' },
  { value: '6', label: 'Faltou' },
];

function formatDisplayDate(dateStr: string) {
  const d = new Date(`${dateStr}T12:00:00`);
  return d.toLocaleDateString('pt-BR');
}

export function FeegowCheckInAgenda({
  date,
  appointments,
  professionals,
  onCheckIn,
  canManage,
}: Props) {
  const [statusFilter, setStatusFilter] = useState<string[]>(['1', '2', '3', '4', '5', '6']);
  const [patientFilter, setPatientFilter] = useState('');
  const [specialtyFilter, setSpecialtyFilter] = useState('');
  const [professionalFilter, setProfessionalFilter] = useState('');
  const [applied, setApplied] = useState(true);

  const specialties = useMemo(() => {
    const set = new Set(appointments.map((a) => a.specialtyName).filter(Boolean));
    return Array.from(set).sort();
  }, [appointments]);

  const rows = useMemo(() => {
    if (!applied) return [];
    return appointments
      .filter((a) => statusFilter.includes(String(a.status)))
      .filter((a) => !patientFilter || a.patientName.toLowerCase().includes(patientFilter.toLowerCase()))
      .filter((a) => !specialtyFilter || a.specialtyName === specialtyFilter)
      .filter((a) => !professionalFilter || a.professionalId === professionalFilter)
      .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt));
  }, [appointments, statusFilter, patientFilter, specialtyFilter, professionalFilter, applied]);

  const statusLabel = statusFilter.length === CHECKIN_STATUS_OPTIONS.length
    ? `${statusFilter.length} selecionado(s)`
    : `${statusFilter.length} selecionado(s)`;

  return (
    <FeegowAgendaScreenLayout sidebar={(
      <FeegowAgendaFilterSidebar
        onFilter={() => setApplied(true)}
        fields={[
          {
            id: 'checkin-status',
            label: 'STATUS',
            children: (
              <select
                id="checkin-status"
                value={statusFilter.join(',')}
                onChange={(e) => setStatusFilter(e.target.value ? e.target.value.split(',') : [])}
              >
                <option value={CHECKIN_STATUS_OPTIONS.map((o) => o.value).join(',')}>{statusLabel}</option>
                {CHECKIN_STATUS_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            ),
          },
          {
            id: 'checkin-patient',
            label: 'PACIENTE',
            children: (
              <input
                id="checkin-patient"
                value={patientFilter}
                onChange={(e) => setPatientFilter(e.target.value)}
              />
            ),
          },
          {
            id: 'checkin-specialty',
            label: 'ESPECIALIDADE',
            children: (
              <select id="checkin-specialty" value={specialtyFilter} onChange={(e) => setSpecialtyFilter(e.target.value)}>
                <option value="">SELECIONE</option>
                {specialties.map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            ),
          },
          {
            id: 'checkin-professional',
            label: 'PROFISSIONAL',
            children: (
              <select id="checkin-professional" value={professionalFilter} onChange={(e) => setProfessionalFilter(e.target.value)}>
                <option value="">SELECIONE</option>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            ),
          },
        ]}
      />
    )}
    >
      <div className="feegow-agenda-schedule feegow-checkin-agenda">
        <header className="feegow-agenda-schedule-head">
          <h1 className="feegow-agenda-schedule-title">
            Checkin
            <span className="feegow-title-check" aria-hidden>✓</span>
            <span className="feegow-checkin-date">{formatDisplayDate(date)}</span>
          </h1>
        </header>

        <div className="feegow-agenda-schedule-body feegow-checkin-table-wrap">
          <table className="feegow-agenda-data-table feegow-checkin-table">
            <thead>
              <tr>
                <th>Agendado</th>
                <th>Chegada</th>
                <th>Paciente</th>
                <th>Profissional</th>
                <th>Especialidade</th>
                <th>Procedimento</th>
                <th>Local</th>
                <th>Equipamento</th>
                <th>Tabela</th>
                <th>Valor/Convênio</th>
                {canManage ? <th /> : null}
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr>
                  <td colSpan={canManage ? 11 : 10} className="feegow-table-empty" />
                </tr>
              ) : (
                rows.map((a) => (
                  <tr key={a.id}>
                    <td>{formatBrTime(a.scheduledAt)}</td>
                    <td>{normalizeAppointmentStatus(a.status) >= 2 ? formatBrTime(a.scheduledAt) : '—'}</td>
                    <td>{a.patientName}</td>
                    <td>{a.professionalName}</td>
                    <td>{a.specialtyName}</td>
                    <td>{a.reason ?? 'Consulta'}</td>
                    <td>{a.room ?? 'CONSULTÓRIO 01'}</td>
                    <td>—</td>
                    <td>Particular</td>
                    <td>—</td>
                    {canManage ? (
                      <td>
                        {isAppointmentStatus(a.status, 1) ? (
                          <button type="button" className="btn btn-sm" onClick={() => onCheckIn(a.id)}>
                            Check-in
                          </button>
                        ) : (
                          <span className="feegow-muted">{appointmentStatusLabel(a.status)}</span>
                        )}
                      </td>
                    ) : null}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </FeegowAgendaScreenLayout>
  );
}
