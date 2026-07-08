import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api,
  operatingRoomStatusLabels,
  surgeryStatusLabels,
  type HealthInsuranceDto,
  type OperatingRoomDto,
  type PatientDto,
  type ProfessionalDto,
  type SurgeryDto,
} from '../api/client';
import { ClinicalGuideCaptureModal } from '../components/funi/ClinicalGuideCaptureModal';
import { PatientWorkspaceShell } from '../components/patient-workspace/PatientWorkspaceShell';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { SurgerySectionPanels } from '../components/surgery/SurgerySectionPanels';
import { surgeryTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrDate, formatBrTime } from '../utils/dateUtils';

const emptyForm = {
  patientId: '',
  operatingRoomId: '',
  surgeonId: '',
  procedureName: '',
  scheduledAt: '',
  estimatedDurationMinutes: 60,
  notes: '',
};

export function SurgeryPage() {
  const { section } = useModuleSection('/centro-cirurgico');
  const [surgeries, setSurgeries] = useState<SurgeryDto[]>([]);
  const [rooms, setRooms] = useState<OperatingRoomDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [form, setForm] = useState(emptyForm);
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [clinicalSurgery, setClinicalSurgery] = useState<SurgeryDto | null>(null);

  async function load(selectedDate: string) {
    const [surgeryList, roomList, patientList, profList, insuranceList] = await Promise.all([
      api.getSurgeries(selectedDate),
      api.getOperatingRooms(),
      api.getPatients(undefined, 1),
      api.getProfessionals(),
      api.getHealthInsurances(),
    ]);
    setSurgeries(surgeryList);
    setRooms(roomList);
    setPatients(patientList.items);
    setProfessionals(profList);
    setInsurances(Array.isArray(insuranceList) ? insuranceList : []);
  }

  useEffect(() => {
    load(date).catch(console.error);
  }, [date]);

  const stats = useMemo(() => ({
    total: surgeries.length,
    scheduled: surgeries.filter((s) => s.status === 1).length,
    inProgress: surgeries.filter((s) => s.status === 2).length,
    completed: surgeries.filter((s) => s.status === 3).length,
    cancelled: surgeries.filter((s) => s.status === 4).length,
    availableRooms: rooms.filter((r) => r.status === 1).length,
  }), [surgeries, rooms]);

  const filtered = useMemo(() => {
    return surgeries
      .filter((s) => !statusFilter || s.status === Number(statusFilter))
      .filter((s) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          s.patientName.toLowerCase().includes(term)
          || s.procedureName.toLowerCase().includes(term)
          || s.surgeonName.toLowerCase().includes(term)
          || s.operatingRoomName.toLowerCase().includes(term)
        );
      });
  }, [surgeries, statusFilter, search]);

  function openNewModal() {
    setForm({ ...emptyForm, scheduledAt: `${date}T08:00` });
    setShowModal(true);
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createSurgery({
        ...form,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
        estimatedDurationMinutes: Number(form.estimatedDurationMinutes),
      });
      setSuccess('Cirurgia agendada com sucesso.');
      setForm(emptyForm);
      setShowModal(false);
      await load(date);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao agendar cirurgia.');
    }
  }

  async function handleStatusChange(id: string, status: number) {
    setError('');
    setSuccess('');
    try {
      await api.updateSurgeryStatus(id, status);
      setSuccess('Status atualizado.');
      await load(date);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao atualizar status.');
    }
  }

  async function handleUpdateChecklist(
    id: string,
    patch: Partial<{
      consentConfirmed: boolean;
      omsSignInCompleted: boolean;
      omsTimeOutCompleted: boolean;
      omsSignOutCompleted: boolean;
    }>,
  ) {
    setError('');
    try {
      await api.updateSurgerySafetyChecklist(id, patch);
      await load(date);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao atualizar checklist.');
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Volume 3 · Centro Cirúrgico"
        title="Centro Cirúrgico"
        subtitle="Agenda cirúrgica, checklist OMS e salas de operação."
      >
        <button className="btn" type="button" onClick={openNewModal}>
          + Nova cirurgia
        </button>
      </PageHeader>

      <ModuleNav basePath="/centro-cirurgico" tabs={surgeryTabs} contextId="surgery" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <PatientWorkspaceShell moduleId="surgery" patients={patients} hidePickerWhenSelected>

      <SurgerySectionPanels
        section={section}
        surgeries={surgeries}
        onUpdateChecklist={handleUpdateChecklist}
        onStatusChange={handleStatusChange}
      />

      {section === '' && (
      <>
      <div className="kpi-grid">
        <KpiCard label="Cirurgias do dia" value={stats.total} variant="primary" />
        <KpiCard label="Agendadas" value={stats.scheduled} variant="info" />
        <KpiCard label="Em andamento" value={stats.inProgress} variant="warning" />
        <KpiCard label="Concluídas" value={stats.completed} variant="success" />
        <KpiCard label="Canceladas" value={stats.cancelled} variant="danger" />
        <KpiCard label="Salas disponíveis" value={stats.availableRooms} variant="neutral" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Salas cirúrgicas — {rooms.length} sala(s)</div>
        <div className="card-panel-body">
          <div className="ward-overview-grid">
            {rooms.map((room) => (
              <div key={room.id} className="ward-overview-card">
                <div className="ward-overview-header">
                  <strong>{room.name}</strong>
                  <span className="badge">{operatingRoomStatusLabels[room.status]}</span>
                </div>
                {room.location && <span className="ward-code">{room.location}</span>}
              </div>
            ))}
            {rooms.length === 0 && (
              <p className="bula-empty">Nenhuma sala cadastrada.</p>
            )}
          </div>
        </div>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Agenda cirúrgica — {filtered.length} procedimento(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="surgeryDate">Data</label>
            <input
              id="surgeryDate"
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
            />
          </div>
          <div className="filter-field w-md">
            <label htmlFor="surgeryStatus">Status</label>
            <select id="surgeryStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(surgeryStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="surgerySearch">Buscar</label>
            <input
              id="surgerySearch"
              placeholder="Paciente, procedimento, cirurgião ou sala..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Horário</th>
                <th>Paciente</th>
                <th>Procedimento</th>
                <th>Sala</th>
                <th>Cirurgião</th>
                <th>Duração</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => (
                <tr key={s.id}>
                  <td>
                    <strong>{formatBrTime(s.scheduledAt)}</strong>
                    <div className="table-sub">{formatBrDate(s.scheduledAt)}</div>
                  </td>
                  <td><strong>{s.patientName}</strong></td>
                  <td>{s.procedureName}</td>
                  <td>{s.operatingRoomName}</td>
                  <td>{s.surgeonName}</td>
                  <td>{s.estimatedDurationMinutes} min</td>
                  <td><span className="badge">{surgeryStatusLabels[s.status]}</span></td>
                  <td>
                    <div className="table-actions">
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => setClinicalSurgery(s)}
                      >
                        Dados TISS
                      </button>
                      <select
                        className="appt-status-select"
                        value={s.status}
                        onChange={(e) => handleStatusChange(s.id, Number(e.target.value))}
                        title="Alterar status"
                      >
                        {Object.entries(surgeryStatusLabels).map(([v, l]) => (
                          <option key={v} value={v}>{l}</option>
                        ))}
                      </select>
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma cirurgia agendada para esta data.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      </>
      )}

      </PatientWorkspaceShell>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Nova cirurgia"
        subtitle="Agende um procedimento cirúrgico no centro cirúrgico."
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
            <label htmlFor="operatingRoomId">Sala cirúrgica *</label>
            <select id="operatingRoomId" required value={form.operatingRoomId} onChange={(e) => setForm({ ...form, operatingRoomId: e.target.value })}>
              <option value="">Selecione</option>
              {rooms.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="surgeonId">Cirurgião *</label>
            <select id="surgeonId" required value={form.surgeonId} onChange={(e) => setForm({ ...form, surgeonId: e.target.value })}>
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="procedureName">Procedimento *</label>
            <input id="procedureName" required value={form.procedureName} onChange={(e) => setForm({ ...form, procedureName: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="scheduledAt">Data e hora *</label>
            <input id="scheduledAt" type="datetime-local" required value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="estimatedDurationMinutes">Duração (min)</label>
            <input id="estimatedDurationMinutes" type="number" min={30} step={15} value={form.estimatedDurationMinutes} onChange={(e) => setForm({ ...form, estimatedDurationMinutes: Number(e.target.value) })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Agendar cirurgia</button>
          </div>
        </form>
      </Modal>

      {clinicalSurgery && (
        <ClinicalGuideCaptureModal
          open
          onClose={() => setClinicalSurgery(null)}
          guideType={2}
          patients={patients}
          insurances={insurances}
          patientId={clinicalSurgery.patientId}
          clinicalContext={{
            surgeryId: clinicalSurgery.id,
            label: `Cirurgia — ${clinicalSurgery.procedureName}`,
          }}
        />
      )}
    </>
  );
}
