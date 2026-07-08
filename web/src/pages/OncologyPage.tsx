import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  chemotherapyStatusLabels,
  type ChemotherapySessionDto,
  type HealthInsuranceDto,
  type PatientDto,
  type ProfessionalDto,
} from '../api/client';
import { ClinicalGuideCaptureModal } from '../components/funi/ClinicalGuideCaptureModal';
import { buildChemoClinicalLabel } from '../utils/clinicalGuideWorkflow';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { ModuleNav } from '../components/ModuleNav';
import { oncologyTabs } from '../navigation/moduleSections';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

const emptyForm = {
  patientId: '',
  professionalId: '',
  protocolName: 'AC-T',
  drugRegimen: '',
  cycleNumber: 1,
  totalCycles: 4,
  scheduledAt: new Date().toISOString().slice(0, 16),
  notes: '',
};

export function OncologyPage() {
  const { hasPermission } = useAuth();
  const [sessions, setSessions] = useState<ChemotherapySessionDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [clinicalSession, setClinicalSession] = useState<ChemotherapySessionDto | null>(null);

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
    api.getProfessionals().then(setProfessionals).catch(console.error);
    api.getHealthInsurances().then((list) => setInsurances(Array.isArray(list) ? list : [])).catch(console.error);
  }, []);

  async function load() {
    setSessions(await api.getChemotherapySessions());
  }

  const stats = useMemo(() => ({
    total: sessions.length,
    scheduled: sessions.filter((s) => s.status === 'Scheduled').length,
    inPreparation: sessions.filter((s) => s.status === 'InPreparation').length,
    administered: sessions.filter((s) => s.status === 'Administered' || s.status === 'Completed').length,
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
          || s.protocolName.toLowerCase().includes(term)
          || s.professionalName.toLowerCase().includes(term)
          || s.drugRegimen.toLowerCase().includes(term)
        );
      });
  }, [sessions, statusFilter, search]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createChemotherapySession({
        ...form,
        cycleNumber: Number(form.cycleNumber),
        totalCycles: Number(form.totalCycles),
        scheduledAt: new Date(form.scheduledAt).toISOString(),
      });
      setSuccess('Sessão de quimioterapia agendada com sucesso.');
      setForm(emptyForm);
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao agendar sessão.');
    }
  }

  async function administer(id: string) {
    setError('');
    setSuccess('');
    try {
      await api.updateChemotherapyStatus(id, 'Administered');
      setSuccess('Quimioterapia administrada.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao administrar sessão.');
    }
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Especialidades"
        title="Oncologia"
        subtitle="Sessões de quimioterapia e protocolos antineoplásicos."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>
          + Nova sessão
        </button>
      </PageHeader>

      <ModuleNav basePath="/oncologia" tabs={oncologyTabs} contextId="oncology" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Total de sessões" value={stats.total} variant="primary" />
        <KpiCard label="Agendadas" value={stats.scheduled} variant="info" />
        <KpiCard label="Em preparo" value={stats.inPreparation} variant="warning" />
        <KpiCard label="Administradas" value={stats.administered} variant="success" />
        <KpiCard label="Canceladas" value={stats.cancelled} variant="danger" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Sessões de quimioterapia — {filtered.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="chemoStatus">Status</label>
            <select id="chemoStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(chemotherapyStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="chemoSearch">Buscar</label>
            <input
              id="chemoSearch"
              placeholder="Paciente, protocolo ou oncologista..."
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
                <th>Protocolo</th>
                <th>Esquema</th>
                <th>Ciclo</th>
                <th>Oncologista</th>
                <th>Agendada</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => (
                <tr key={s.id}>
                  <td><strong>{s.patientName}</strong></td>
                  <td>{s.protocolName}</td>
                  <td>{s.drugRegimen}</td>
                  <td>{s.cycleNumber}/{s.totalCycles}</td>
                  <td>{s.professionalName}</td>
                  <td>{formatBrDateTime(s.scheduledAt)}</td>
                  <td><span className="badge">{chemotherapyStatusLabels[s.status] ?? s.status}</span></td>
                  <td>
                    <div className="table-actions">
                      <button
                        className="btn btn-secondary btn-sm"
                        type="button"
                        onClick={() => setClinicalSession(s)}
                      >
                        Dados TISS
                      </button>
                      {s.status === 'Scheduled' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => administer(s.id)}>
                          Administrar
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
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
        title="Nova sessão de quimioterapia"
        subtitle="Agende uma sessão com protocolo e esquema medicamentoso."
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
            <label htmlFor="professionalId">Oncologista *</label>
            <select id="professionalId" required value={form.professionalId} onChange={(e) => setForm({ ...form, professionalId: e.target.value })}>
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="protocolName">Protocolo *</label>
            <input id="protocolName" required value={form.protocolName} onChange={(e) => setForm({ ...form, protocolName: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="drugRegimen">Esquema medicamentoso *</label>
            <input id="drugRegimen" required value={form.drugRegimen} onChange={(e) => setForm({ ...form, drugRegimen: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="cycleNumber">Ciclo</label>
            <input id="cycleNumber" type="number" min={1} value={form.cycleNumber} onChange={(e) => setForm({ ...form, cycleNumber: Number(e.target.value) })} />
          </div>
          <div className="form-field">
            <label htmlFor="totalCycles">Total de ciclos</label>
            <input id="totalCycles" type="number" min={1} value={form.totalCycles} onChange={(e) => setForm({ ...form, totalCycles: Number(e.target.value) })} />
          </div>
          <div className="form-field">
            <label htmlFor="scheduledAt">Data e hora *</label>
            <input id="scheduledAt" type="datetime-local" required value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Agendar quimioterapia</button>
          </div>
        </form>
      </Modal>

      {clinicalSession && (
        <ClinicalGuideCaptureModal
          open
          onClose={() => setClinicalSession(null)}
          guideType={17}
          patients={patients}
          insurances={insurances}
          patientId={clinicalSession.patientId}
          clinicalContext={{
            chemotherapySessionId: clinicalSession.id,
            label: buildChemoClinicalLabel(clinicalSession),
          }}
          onSaved={() => setSuccess('Dados clínicos da quimioterapia salvos. Gere a guia FUNI no faturamento.')}
        />
      )}
    </>
  );
}
