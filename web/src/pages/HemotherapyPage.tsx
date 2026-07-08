import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  bloodComponentLabels,
  bloodTypeLabels,
  bloodUnitStatusLabels,
  transfusionStatusLabels,
  type BloodUnitDto,
  type PatientDto,
  type ProfessionalDto,
  type TransfusionRequestDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { hemotherapyTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { Link, useLocation } from 'react-router-dom';
import { formatBrDate } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

const emptyForm = {
  patientId: '',
  requestingProfessionalId: '',
  hospitalizationId: '',
  bloodTypeRequired: 'OPositive',
  component: 'PackedRedCells',
  unitsRequested: 1,
  notes: '',
};

type DonorRecord = { id: string; name: string; bloodType: string; at: string };

export function HemotherapyPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/hemoterapia');
  const activeSection = section || 'estoque';

  const { hasPermission } = useAuth();
  const canManage = hasPermission('patients.create', 'reports.read');
  const canCreate = hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage');
  const [units, setUnits] = useState<BloodUnitDto[]>([]);
  const [requests, setRequests] = useState<TransfusionRequestDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [showModal, setShowModal] = useState(false);
  const [unitStatusFilter, setUnitStatusFilter] = useState('');
  const [requestStatusFilter, setRequestStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [donors, setDonors] = useState<DonorRecord[]>([]);
  const [donorForm, setDonorForm] = useState({ name: '', bloodType: 'OPositive' });

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
    api.getProfessionals().then(setProfessionals).catch(console.error);
  }, []);

  async function load() {
    const [u, r] = await Promise.all([api.getBloodUnits(), api.getTransfusionRequests()]);
    setUnits(u);
    setRequests(r);
  }

  const availableUnits = useMemo(
    () => units.filter((u) => u.status === 'Available'),
    [units],
  );

  const stats = useMemo(() => ({
    totalUnits: units.length,
    available: units.filter((u) => u.status === 'Available').length,
    reserved: units.filter((u) => u.status === 'Reserved').length,
    requests: requests.length,
    pending: requests.filter((r) => r.status === 'Requested' || r.status === 'Matched').length,
    transfused: requests.filter((r) => r.status === 'Transfused').length,
  }), [units, requests]);

  const filteredUnits = useMemo(() => {
    return units.filter((u) => !unitStatusFilter || u.status === unitStatusFilter);
  }, [units, unitStatusFilter]);

  const filteredRequests = useMemo(() => {
    return requests
      .filter((r) => !requestStatusFilter || r.status === requestStatusFilter)
      .filter((r) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          r.patientName.toLowerCase().includes(term)
          || (r.bloodUnitCode?.toLowerCase().includes(term) ?? false)
        );
      });
  }, [requests, requestStatusFilter, search]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createTransfusionRequest({
        ...form,
        hospitalizationId: form.hospitalizationId || undefined,
        unitsRequested: Number(form.unitsRequested),
      });
      setSuccess('Solicitação de transfusão criada com sucesso.');
      setForm(emptyForm);
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar solicitação.');
    }
  }

  async function matchRequest(requestId: string, bloodUnitId: string) {
    setError('');
    setSuccess('');
    try {
      await api.matchTransfusion(requestId, bloodUnitId);
      setSuccess('Bolsa compatibilizada com a solicitação.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao compatibilizar bolsa.');
    }
  }

  async function completeRequest(id: string) {
    setError('');
    setSuccess('');
    try {
      await api.completeTransfusion(id);
      setSuccess('Transfusão concluída.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao concluir transfusão.');
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Especialidades"
        title={breadcrumb.title}
        subtitle="Banco de sangue, bolsas disponíveis e solicitações de transfusão."
      >
        {canCreate && activeSection === 'transfusoes' && (
          <button className="btn" type="button" onClick={() => setShowModal(true)}>
            + Nova solicitação
          </button>
        )}
      </PageHeader>

      <ModuleNav basePath="/hemoterapia" tabs={hemotherapyTabs} contextId="hemotherapy" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Bolsas em estoque" value={stats.totalUnits} variant="primary" />
        <KpiCard label="Disponíveis" value={stats.available} variant="success" />
        <KpiCard label="Reservadas" value={stats.reserved} variant="warning" />
        <KpiCard label="Solicitações" value={stats.requests} variant="info" />
        <KpiCard label="Pendentes" value={stats.pending} variant="warning" />
        <KpiCard label="Transfundidas" value={stats.transfused} variant="success" />
      </div>

      {activeSection === 'doadores' && (
        <>
          <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={(e) => {
            e.preventDefault();
            if (!donorForm.name.trim()) return;
            setDonors((p) => [{ id: crypto.randomUUID(), name: donorForm.name, bloodType: donorForm.bloodType, at: new Date().toISOString() }, ...p]);
            setDonorForm({ name: '', bloodType: 'OPositive' });
            setSuccess('Doador registrado.');
          }}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Cadastro de doadores</h3>
            <div className="form-field"><label>Nome</label><input value={donorForm.name} onChange={(e) => setDonorForm({ ...donorForm, name: e.target.value })} required /></div>
            <div className="form-field"><label>Tipo sanguíneo</label>
              <select value={donorForm.bloodType} onChange={(e) => setDonorForm({ ...donorForm, bloodType: e.target.value })}>
                {Object.entries(bloodTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div className="form-actions"><button className="btn" type="submit">Registrar doador</button></div>
          </form>
          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <table className="data-table">
              <thead><tr><th>Data</th><th>Doador</th><th>Tipo</th></tr></thead>
              <tbody>
                {donors.map((d) => <tr key={d.id}><td>{formatBrDate(d.at)}</td><td>{d.name}</td><td>{bloodTypeLabels[d.bloodType]}</td></tr>)}
                {donors.length === 0 && <tr><td colSpan={3} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Nenhum doador.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}

      {activeSection === 'relatorios' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Relatórios de hemoterapia</h3>
          <Link to="/relatorios" className="btn btn-secondary">Central de relatórios</Link>
        </div>
      )}

      {(activeSection === 'estoque' || activeSection === 'hemocomponentes') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">
          {activeSection === 'hemocomponentes' ? 'Hemocomponentes' : `Bolsas em estoque — ${filteredUnits.length} unidade(s)`}
        </div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="unitStatus">Status</label>
            <select id="unitStatus" value={unitStatusFilter} onChange={(e) => setUnitStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(bloodUnitStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Código</th>
                <th>Tipo sanguíneo</th>
                <th>Componente</th>
                <th>Volume</th>
                <th>Validade</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {filteredUnits.map((u) => (
                <tr key={u.id}>
                  <td><strong>{u.unitCode}</strong></td>
                  <td>{bloodTypeLabels[u.bloodType]}</td>
                  <td>{bloodComponentLabels[u.component]}</td>
                  <td>{u.volumeMl} ml</td>
                  <td>{formatBrDate(u.expiresAt)}</td>
                  <td><span className="badge">{bloodUnitStatusLabels[u.status] ?? u.status}</span></td>
                </tr>
              ))}
              {filteredUnits.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma bolsa encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {activeSection === 'transfusoes' && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Solicitações de transfusão — {filteredRequests.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="requestStatus">Status</label>
            <select id="requestStatus" value={requestStatusFilter} onChange={(e) => setRequestStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(transfusionStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="transfusionSearch">Buscar</label>
            <input
              id="transfusionSearch"
              placeholder="Paciente ou código da bolsa..."
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
                <th>Tipo / Componente</th>
                <th>Unidades</th>
                <th>Status</th>
                <th>Bolsa</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filteredRequests.map((r) => (
                <tr key={r.id}>
                  <td><strong>{r.patientName}</strong></td>
                  <td>{bloodTypeLabels[r.bloodTypeRequired]} — {bloodComponentLabels[r.component]}</td>
                  <td>{r.unitsRequested}</td>
                  <td><span className="badge">{transfusionStatusLabels[r.status] ?? r.status}</span></td>
                  <td>{r.bloodUnitCode ?? '—'}</td>
                  <td>
                    <div className="table-actions">
                      {canManage && r.status === 'Requested' && availableUnits
                        .filter((u) => u.bloodType === r.bloodTypeRequired && u.component === r.component)
                        .map((u) => (
                          <button key={u.id} className="btn btn-secondary btn-sm" type="button" onClick={() => matchRequest(r.id, u.id)}>
                            {u.unitCode}
                          </button>
                        ))}
                      {r.status === 'Matched' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => completeRequest(r.id)}>
                          Transfundir
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filteredRequests.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma solicitação encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Nova solicitação de transfusão"
        subtitle="Solicite componentes sanguíneos para um paciente."
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
            <label htmlFor="requestingProfessionalId">Médico solicitante *</label>
            <select id="requestingProfessionalId" required value={form.requestingProfessionalId} onChange={(e) => setForm({ ...form, requestingProfessionalId: e.target.value })}>
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="bloodTypeRequired">Tipo sanguíneo</label>
            <select id="bloodTypeRequired" value={form.bloodTypeRequired} onChange={(e) => setForm({ ...form, bloodTypeRequired: e.target.value })}>
              {Object.entries(bloodTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="component">Componente</label>
            <select id="component" value={form.component} onChange={(e) => setForm({ ...form, component: e.target.value })}>
              {Object.entries(bloodComponentLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Solicitar transfusão</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
