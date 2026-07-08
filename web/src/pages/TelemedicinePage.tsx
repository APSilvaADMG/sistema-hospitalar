import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  telemedicineStatusLabels,
  type PatientDto,
  type ProfessionalDto,
  type TelemedicineAppointmentDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDate, formatBrTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

function initials(name: string) {
  return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('');
}

function resolveMeetingUrl(appointmentId: string, meetingUrl?: string): string {
  if (meetingUrl && meetingUrl.includes('meet.google.com')) {
    return meetingUrl;
  }

  const alphabet = 'abcdefghijklmnopqrstuvwxyz';
  const hex = appointmentId.replace(/-/g, '');
  const chars: string[] = [];
  for (let i = 0; i < 10; i++) {
    const pos = (i * 2) % hex.length;
    const byte = Number.parseInt(hex.slice(pos, pos + 2), 16) || i;
    chars.push(alphabet[byte % alphabet.length]);
  }

  return `https://meet.google.com/${chars.slice(0, 3).join('')}-${chars.slice(3, 7).join('')}-${chars.slice(7, 10).join('')}`;
}

function openMeetingRoom(appointmentId: string, meetingUrl?: string) {
  const url = resolveMeetingUrl(appointmentId, meetingUrl);
  window.open(url, '_blank', 'noopener,noreferrer');
}

const statusClass: Record<string, string> = {
  Scheduled: 'status-scheduled',
  Waiting: 'status-confirmed',
  InProgress: 'status-in-progress',
  Completed: 'status-done',
  Cancelled: 'status-cancelled',
  NoShow: 'status-no-show',
};

export function TelemedicinePage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const canManage = hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage');

  const [appointments, setAppointments] = useState<TelemedicineAppointmentDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [form, setForm] = useState({
    patientId: '',
    professionalId: '',
    scheduledAt: new Date().toISOString().slice(0, 16),
    chiefComplaint: '',
    notes: '',
  });
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [professionalFilter, setProfessionalFilter] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
    api.getProfessionals().then(setProfessionals).catch(console.error);
  }, []);

  async function load() {
    setAppointments(await api.getTelemedicineAppointments());
  }

  const filtered = useMemo(() => appointments
    .filter((a) => !statusFilter || a.status === statusFilter)
    .filter((a) => !professionalFilter || a.professionalName === professionals.find((p) => p.id === professionalFilter)?.fullName)
    .filter((a) => {
      if (!search.trim()) return true;
      const term = search.toLowerCase();
      return a.patientName.toLowerCase().includes(term)
        || a.professionalName.toLowerCase().includes(term)
        || a.chiefComplaint.toLowerCase().includes(term);
    })
    .sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime()),
  [appointments, statusFilter, professionalFilter, search, professionals]);

  const stats = useMemo(() => ({
    total: appointments.length,
    scheduled: appointments.filter((a) => a.status === 'Scheduled' || a.status === 'Waiting').length,
    inProgress: appointments.filter((a) => a.status === 'InProgress').length,
    completed: appointments.filter((a) => a.status === 'Completed').length,
    cancelled: appointments.filter((a) => a.status === 'Cancelled' || a.status === 'NoShow').length,
  }), [appointments]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createTelemedicineAppointment({
        ...form,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
        notes: form.notes || undefined,
      });
      setShowModal(false);
      setSuccess('Teleconsulta agendada com sucesso.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao agendar');
    }
  }

  async function startConsultation(id: string) {
    await api.updateTelemedicineStatus(id, 'InProgress');
    setSuccess('Consulta iniciada.');
    await load();
  }

  async function completeConsultation(id: string) {
    await api.updateTelemedicineStatus(id, 'Completed');
    setSuccess('Consulta concluída.');
    await load();
  }

  if (!canManage) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Atendimento"
        title={breadcrumb.title || 'Telemedicina'}
        subtitle="Consultas remotas por vídeo com sala virtual e acompanhamento de status."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>
          + Nova teleconsulta
        </button>
      </PageHeader>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Total" value={stats.total} variant="primary" />
        <KpiCard label="Agendadas" value={stats.scheduled} variant="info" />
        <KpiCard label="Em consulta" value={stats.inProgress} variant="warning" />
        <KpiCard label="Concluídas" value={stats.completed} variant="success" />
        <KpiCard label="Canceladas / faltas" value={stats.cancelled} variant="danger" />
      </div>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">Teleconsultas — {filtered.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="tmStatus">Status</label>
            <select id="tmStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(telemedicineStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-xl">
            <label htmlFor="tmProf">Profissional</label>
            <select id="tmProf" value={professionalFilter} onChange={(e) => setProfessionalFilter(e.target.value)}>
              <option value="">Todos</option>
              {professionals.map((p) => (
                <option key={p.id} value={p.id}>{p.fullName}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="tmSearch">Buscar</label>
            <input
              id="tmSearch"
              placeholder="Paciente ou queixa..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>

        <div className="card-panel-body appt-panel-body">
          {filtered.length === 0 ? (
            <div className="appt-empty">
              <div className="appt-empty-icon">📹</div>
              <h3>Nenhuma teleconsulta</h3>
              <p>Agende uma nova consulta remota para começar.</p>
            </div>
          ) : (
            <div className="emergency-queue">
              {filtered.map((a) => (
                <article key={a.id} className="appt-card">
                  <div className="appt-card-time">
                    <span>{formatBrTime(a.scheduledAt)}</span>
                    <span className="appt-card-duration">
                      {formatBrDate(a.scheduledAt)}
                    </span>
                  </div>
                  <div className="appt-card-main">
                    <div className="appt-card-patient">
                      <div className="appt-avatar">{initials(a.patientName)}</div>
                      <div>
                        <strong>{a.patientName}</strong>
                        <span className="appt-card-reason">{a.chiefComplaint}</span>
                      </div>
                    </div>
                    <div className="appt-card-meta">
                      <span>{a.professionalName}</span>
                      <span className="appt-meta-dot">•</span>
                      <span>{a.specialtyName}</span>
                    </div>
                  </div>
                  <div className="appt-card-actions">
                    <span className={`appt-status ${statusClass[a.status] ?? 'status-scheduled'}`}>
                      {telemedicineStatusLabels[a.status] ?? a.status}
                    </span>
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => openMeetingRoom(a.id, a.meetingUrl)}
                    >
                      Abrir sala
                    </button>
                    {a.status === 'Scheduled' && (
                      <button className="btn btn-sm" type="button" onClick={() => startConsultation(a.id)}>Iniciar</button>
                    )}
                    {a.status === 'InProgress' && (
                      <button className="btn btn-sm" type="button" onClick={() => completeConsultation(a.id)}>Concluir</button>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Nova teleconsulta"
        subtitle="Agendamento de consulta remota por vídeo."
        width="lg"
      >
        <form onSubmit={handleCreate} className="form-grid">
          <div className="form-field">
            <label>Paciente *</label>
            <select value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })} required>
              <option value="">Selecione...</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Profissional *</label>
            <select value={form.professionalId} onChange={(e) => setForm({ ...form, professionalId: e.target.value })} required>
              <option value="">Selecione...</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Data e hora *</label>
            <input type="datetime-local" value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} required />
          </div>
          <div className="form-field">
            <label>Queixa principal *</label>
            <input value={form.chiefComplaint} onChange={(e) => setForm({ ...form, chiefComplaint: e.target.value })} required />
          </div>
          <div className="form-field full">
            <label>Observações</label>
            <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Agendar teleconsulta</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
