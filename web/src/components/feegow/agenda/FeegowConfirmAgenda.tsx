import { useMemo, useState } from 'react';
import { isAppointmentStatus, type AppointmentDto, type ProfessionalDto } from '../../../api/client';
import { formatBrTime } from '../../../utils/dateUtils';
import { FeegowAgendaFilterSidebar } from './FeegowAgendaFilterSidebar';
import { FeegowAgendaScreenLayout } from './FeegowAgendaScreenLayout';

type Props = {
  appointments: AppointmentDto[];
  professionals: ProfessionalDto[];
  onConfirm?: (id: string) => void;
  canManage?: boolean;
};

function addDays(dateStr: string, days: number) {
  const d = new Date(`${dateStr}T12:00:00`);
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

function formatBrDate(iso: string) {
  return new Date(iso).toLocaleDateString('pt-BR');
}

export function FeegowConfirmAgenda({
  appointments,
  professionals,
  onConfirm,
  canManage,
}: Props) {
  const today = new Date().toISOString().slice(0, 10);
  const [statusFilter, setStatusFilter] = useState('1');
  const [patientFilter, setPatientFilter] = useState('');
  const [professionalFilter, setProfessionalFilter] = useState('');
  const [procedureType, setProcedureType] = useState('');
  const [procedureGroup, setProcedureGroup] = useState('');
  const [dateFrom, setDateFrom] = useState(today);
  const [dateTo, setDateTo] = useState(addDays(today, 1));
  const [channel, setChannel] = useState('whatsapp');
  const [applied, setApplied] = useState(true);

  const rows = useMemo(() => {
    if (!applied) return [];
    return appointments
      .filter((a) => !statusFilter || String(a.status) === statusFilter)
      .filter((a) => !patientFilter || a.patientName.toLowerCase().includes(patientFilter.toLowerCase()))
      .filter((a) => !professionalFilter || a.professionalId === professionalFilter)
      .filter((a) => {
        const day = a.scheduledAt.slice(0, 10);
        return day >= dateFrom && day <= dateTo;
      })
      .filter((a) => !procedureType || (a.reason?.toLowerCase().includes(procedureType.toLowerCase()) ?? false))
      .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt));
  }, [appointments, statusFilter, patientFilter, professionalFilter, dateFrom, dateTo, procedureType, applied]);

  const pending = rows.filter((a) => isAppointmentStatus(a.status, 1)).length;
  const confirmed = rows.filter((a) => isAppointmentStatus(a.status, 2)).length;
  const progress = rows.length === 0 ? 0 : Math.round((confirmed / rows.length) * 100);

  return (
    <FeegowAgendaScreenLayout sidebar={(
      <FeegowAgendaFilterSidebar
        onFilter={() => setApplied(true)}
        fields={[
          {
            id: 'confirm-status',
            label: 'STATUS',
            children: (
              <select id="confirm-status" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="1">Marcado - não confirmado</option>
                <option value="2">Confirmado</option>
                <option value="5">Cancelado</option>
              </select>
            ),
          },
          {
            id: 'confirm-patient',
            label: 'PACIENTE',
            children: (
              <input id="confirm-patient" value={patientFilter} onChange={(e) => setPatientFilter(e.target.value)} />
            ),
          },
          {
            id: 'confirm-professional',
            label: 'PROFISSIONAL',
            children: (
              <select id="confirm-professional" value={professionalFilter} onChange={(e) => setProfessionalFilter(e.target.value)}>
                <option value="">SELECIONE</option>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            ),
          },
          {
            id: 'confirm-proc-type',
            label: 'TIPO DO PROCEDIMENTO',
            children: (
              <select id="confirm-proc-type" value={procedureType} onChange={(e) => setProcedureType(e.target.value)}>
                <option value="">SELECIONE</option>
                <option value="consulta">Consulta</option>
                <option value="retorno">Retorno</option>
                <option value="exame">Exame</option>
              </select>
            ),
          },
          {
            id: 'confirm-proc-group',
            label: 'GRUPO DE PROCEDIMENTO',
            children: (
              <select id="confirm-proc-group" value={procedureGroup} onChange={(e) => setProcedureGroup(e.target.value)}>
                <option value="">Selecione</option>
                <option value="ambulatorial">Ambulatorial</option>
              </select>
            ),
          },
          {
            id: 'confirm-from',
            label: 'DE',
            children: (
              <input id="confirm-from" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
            ),
          },
          {
            id: 'confirm-to',
            label: 'ATÉ',
            children: (
              <input id="confirm-to" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
            ),
          },
        ]}
      />
    )}
    >
      <div className="feegow-agenda-schedule feegow-confirm-agenda">
        <header className="feegow-agenda-schedule-head feegow-confirm-head">
          <div>
            <h1 className="feegow-agenda-schedule-title">
              Confirmação de agendamentos
              <span className="feegow-title-checks" aria-hidden>
                <span className="feegow-check-blue">✓</span>
                <span className="feegow-check-green">✓</span>
              </span>
            </h1>
            <div className="feegow-confirm-stats">
              <span>Agendamentos a confirmar: <strong>{pending}</strong></span>
              <span>Agendamentos confirmados: <strong>{confirmed}</strong></span>
            </div>
            <div className="feegow-confirm-progress" role="progressbar" aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100}>
              <div className="feegow-confirm-progress-bar" style={{ width: `${progress}%` }} />
            </div>
          </div>
          <label className="feegow-field">
            <span>Canal de confirmação</span>
            <select value={channel} onChange={(e) => setChannel(e.target.value)}>
              <option value="whatsapp">WhatsApp Desktop</option>
              <option value="sms">SMS</option>
              <option value="email">E-mail</option>
            </select>
          </label>
        </header>

        <div className="feegow-agenda-schedule-body feegow-confirm-table-wrap">
          <p className="feegow-confirm-count">{rows.length} agendamentos</p>
          <table className="feegow-agenda-data-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Paciente</th>
                <th>Celular</th>
                <th>Profissional</th>
                <th>Procedimento</th>
                <th>Local</th>
                <th>Valor/Convênio</th>
                <th>Observações</th>
                <th>Elegibilidade</th>
                {canManage ? <th /> : null}
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr><td colSpan={canManage ? 10 : 9} className="feegow-table-empty" /></tr>
              ) : (
                rows.map((a) => (
                  <tr key={a.id}>
                    <td>{formatBrDate(a.scheduledAt)} {formatBrTime(a.scheduledAt)}</td>
                    <td>{a.patientName}</td>
                    <td>—</td>
                    <td>{a.professionalName}</td>
                    <td>{a.reason ?? 'Consulta'}</td>
                    <td>{a.room ?? 'CONSULTÓRIO 01'}</td>
                    <td>—</td>
                    <td>{a.reason ?? '—'}</td>
                    <td>—</td>
                    {canManage && onConfirm ? (
                      <td>
                        {isAppointmentStatus(a.status, 1) ? (
                          <button type="button" className="btn btn-sm" onClick={() => onConfirm(a.id)}>Confirmar</button>
                        ) : null}
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
