import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  dialysisStatusLabels,
  type DialysisSessionDto,
  type HospitalizationDto,
  type PatientDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { dialysisTabs } from '../navigation/moduleSections';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

const emptyForm = {
  patientId: '',
  hospitalizationId: '',
  machineNumber: 'DIA-01',
  scheduledAt: new Date().toISOString().slice(0, 16),
  dryWeightKg: '',
  nurseName: '',
  notes: '',
};

export function DialysisPage() {
  const { hasPermission } = useAuth();
  const [sessions, setSessions] = useState<DialysisSessionDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
    api.getHospitalizations().then(setHospitalizations).catch(console.error);
  }, []);

  async function load() {
    setSessions(await api.getDialysisSessions());
  }

  const activeHospitalizations = useMemo(
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
      .filter((s) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          s.patientName.toLowerCase().includes(term)
          || s.machineNumber.toLowerCase().includes(term)
          || (s.nurseName?.toLowerCase().includes(term) ?? false)
        );
      });
  }, [sessions, statusFilter, search]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createDialysisSession({
        patientId: form.patientId,
        hospitalizationId: form.hospitalizationId || undefined,
        machineNumber: form.machineNumber,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
        dryWeightKg: form.dryWeightKg ? Number(form.dryWeightKg) : undefined,
        nurseName: form.nurseName || undefined,
        notes: form.notes || undefined,
      });
      setSuccess('Sessão de diálise agendada com sucesso.');
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
      await api.updateDialysisSessionStatus(id, status);
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
        title="Diálise"
        subtitle="Agendamento e acompanhamento de sessões de hemodiálise."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>
          + Nova sessão
        </button>
      </PageHeader>

      <ModuleNav basePath="/dialise" tabs={dialysisTabs} />

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
        <div className="card-panel-header">Sessões de hemodiálise — {filtered.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="dialysisStatus">Status</label>
            <select id="dialysisStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(dialysisStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="dialysisSearch">Buscar</label>
            <input
              id="dialysisSearch"
              placeholder="Paciente, máquina ou enfermeiro(a)..."
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
                <th>Máquina</th>
                <th>Agendada</th>
                <th>Peso seco</th>
                <th>Enfermeiro(a)</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => (
                <tr key={s.id}>
                  <td><strong>{s.patientName}</strong></td>
                  <td>{s.machineNumber}</td>
                  <td>{formatBrDateTime(s.scheduledAt)}</td>
                  <td>{s.dryWeightKg != null ? `${s.dryWeightKg} kg` : '—'}</td>
                  <td>{s.nurseName ?? '—'}</td>
                  <td><span className="badge">{dialysisStatusLabels[s.status] ?? s.status}</span></td>
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
        title="Nova sessão de diálise"
        subtitle="Agende uma sessão de hemodiálise para o paciente."
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
              {activeHospitalizations.map((h) => (
                <option key={h.id} value={h.id}>{h.patientName} — {h.wardName}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="machineNumber">Máquina *</label>
            <input id="machineNumber" required value={form.machineNumber} onChange={(e) => setForm({ ...form, machineNumber: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="scheduledAt">Data e hora *</label>
            <input id="scheduledAt" type="datetime-local" required value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="dryWeightKg">Peso seco (kg)</label>
            <input id="dryWeightKg" type="number" step="0.1" value={form.dryWeightKg} onChange={(e) => setForm({ ...form, dryWeightKg: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="nurseName">Enfermeiro(a)</label>
            <input id="nurseName" value={form.nurseName} onChange={(e) => setForm({ ...form, nurseName: e.target.value })} />
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
