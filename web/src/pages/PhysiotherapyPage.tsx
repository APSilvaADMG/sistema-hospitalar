import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  physiotherapyStatusLabels,
  physiotherapyTypeLabels,
  type HospitalizationDto,
  type PatientDto,
  type PhysiotherapySessionDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { physiotherapyTabs } from '../navigation/moduleSections';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

const emptyForm = {
  patientId: '',
  hospitalizationId: '',
  therapistName: '',
  sessionType: 'Respiratory',
  scheduledAt: new Date().toISOString().slice(0, 16),
  durationMinutes: 45,
  goals: '',
  notes: '',
};

export function PhysiotherapyPage() {
  const { hasPermission } = useAuth();
  const [sessions, setSessions] = useState<PhysiotherapySessionDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
    api.getHospitalizations().then(setHospitalizations).catch(console.error);
  }, []);

  async function load() {
    setSessions(await api.getPhysiotherapySessions());
  }

  const active = useMemo(
    () => hospitalizations.filter((h) => h.status === 1),
    [hospitalizations],
  );

  const stats = useMemo(() => ({
    total: sessions.length,
    scheduled: sessions.filter((s) => s.status === 'Scheduled').length,
    inProgress: sessions.filter((s) => s.status === 'InProgress').length,
    completed: sessions.filter((s) => s.status === 'Completed').length,
    cancelled: sessions.filter((s) => s.status === 'Cancelled').length,
  }), [sessions]);

  const filtered = useMemo(() => {
    return sessions
      .filter((s) => !statusFilter || s.status === statusFilter)
      .filter((s) => !typeFilter || s.sessionType === typeFilter)
      .filter((s) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          s.patientName.toLowerCase().includes(term)
          || s.therapistName.toLowerCase().includes(term)
          || (s.wardName?.toLowerCase().includes(term) ?? false)
        );
      });
  }, [sessions, statusFilter, typeFilter, search]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createPhysiotherapySession({
        ...form,
        hospitalizationId: form.hospitalizationId || undefined,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
        durationMinutes: Number(form.durationMinutes),
        goals: form.goals || undefined,
        notes: form.notes || undefined,
      });
      setSuccess('Sessão de fisioterapia agendada com sucesso.');
      setForm(emptyForm);
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao agendar sessão.');
    }
  }

  async function updateStatus(id: string, status: string) {
    setError('');
    setSuccess('');
    try {
      await api.updatePhysiotherapyStatus(id, status);
      setSuccess('Status atualizado.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao atualizar status.');
    }
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Especialidades"
        title="Fisioterapia"
        subtitle="Sessões de reabilitação motora, respiratória e neurológica."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>
          + Nova sessão
        </button>
      </PageHeader>

      <ModuleNav basePath="/fisioterapia" tabs={physiotherapyTabs} />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Total de sessões" value={stats.total} variant="primary" />
        <KpiCard label="Agendadas" value={stats.scheduled} variant="info" />
        <KpiCard label="Em andamento" value={stats.inProgress} variant="warning" />
        <KpiCard label="Concluídas" value={stats.completed} variant="success" />
        <KpiCard label="Canceladas" value={stats.cancelled} variant="danger" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Sessões de fisioterapia — {filtered.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="physioStatus">Status</label>
            <select id="physioStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(physiotherapyStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-md">
            <label htmlFor="physioType">Tipo</label>
            <select id="physioType" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(physiotherapyTypeLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="physioSearch">Buscar</label>
            <input
              id="physioSearch"
              placeholder="Paciente, fisioterapeuta ou ala..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Tipo</th>
                <th>Fisioterapeuta</th>
                <th>Agendada</th>
                <th>Duração</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => (
                <tr key={s.id}>
                  <td><strong>{s.patientName}</strong></td>
                  <td>{physiotherapyTypeLabels[s.sessionType]}</td>
                  <td>{s.therapistName}</td>
                  <td>{formatBrDateTime(s.scheduledAt)}</td>
                  <td>{s.durationMinutes} min</td>
                  <td><span className="badge">{physiotherapyStatusLabels[s.status] ?? s.status}</span></td>
                  <td>
                    <div className="table-actions">
                      {s.status === 'Scheduled' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => updateStatus(s.id, 'InProgress')}>
                          Iniciar
                        </button>
                      )}
                      {s.status === 'InProgress' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => updateStatus(s.id, 'Completed')}>
                          Concluir
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma sessão encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Nova sessão de fisioterapia"
        subtitle="Agende uma sessão de reabilitação para o paciente."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleCreate}>
          <div className="form-field">
            <label htmlFor="patientId">Paciente *</label>
            <select id="patientId" required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
              <option value="">Selecione</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="hospitalizationId">Internação</label>
            <select id="hospitalizationId" value={form.hospitalizationId} onChange={(e) => setForm({ ...form, hospitalizationId: e.target.value })}>
              <option value="">Opcional</option>
              {active.map((h) => <option key={h.id} value={h.id}>{h.patientName} — {h.wardName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="therapistName">Fisioterapeuta *</label>
            <input id="therapistName" required value={form.therapistName} onChange={(e) => setForm({ ...form, therapistName: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="sessionType">Tipo de sessão</label>
            <select id="sessionType" value={form.sessionType} onChange={(e) => setForm({ ...form, sessionType: e.target.value })}>
              {Object.entries(physiotherapyTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="scheduledAt">Data e hora *</label>
            <input id="scheduledAt" type="datetime-local" required value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="durationMinutes">Duração (min)</label>
            <input id="durationMinutes" type="number" min={15} step={15} value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: Number(e.target.value) })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Agendar sessão</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
