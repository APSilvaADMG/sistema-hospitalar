import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  ambulanceStatusLabels,
  dispatchStatusLabels,
  type AmbulanceDispatchDto,
  type AmbulanceDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { ambulanceTabs } from '../navigation/moduleSections';
import { useAuth } from '../auth/AuthContext';

export function AmbulancePage() {
  const { hasPermission } = useAuth();
  const [fleet, setFleet] = useState<AmbulanceDto[]>([]);
  const [dispatches, setDispatches] = useState<AmbulanceDispatchDto[]>([]);
  const [form, setForm] = useState({ patientName: '', pickupAddress: '', destination: '', notes: '' });
  const [statusFilter, setStatusFilter] = useState('');
  const [showModal, setShowModal] = useState(false);

  useEffect(() => { load(); }, []);

  async function load() {
    const [f, d] = await Promise.all([api.getAmbulanceFleet(), api.getAmbulanceDispatches()]);
    setFleet(f);
    setDispatches(d);
  }

  const stats = useMemo(() => ({
    fleet: fleet.length,
    available: fleet.filter((a) => a.status === 'Available').length,
    inUse: fleet.filter((a) => a.status !== 'Available').length,
    activeDispatches: dispatches.filter((d) => d.status !== 'Completed' && d.status !== 'Cancelled').length,
  }), [fleet, dispatches]);

  const filtered = useMemo(() => {
    return dispatches.filter((d) => !statusFilter || d.status === statusFilter);
  }, [dispatches, statusFilter]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    await api.createAmbulanceDispatch(form);
    setForm({ patientName: '', pickupAddress: '', destination: '', notes: '' });
    setShowModal(false);
    await load();
  }

  async function assign(dispatchId: string, ambulanceId: string) {
    await api.assignAmbulance(dispatchId, ambulanceId);
    await load();
  }

  async function advanceStatus(dispatchId: string, status: string) {
    await api.updateAmbulanceDispatchStatus(dispatchId, status);
    await load();
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Infraestrutura"
        title="Ambulâncias / SAMU"
        subtitle="Frota e despachos de remoção."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>+ Nova remoção</button>
      </PageHeader>

      <ModuleNav basePath="/ambulancias" tabs={ambulanceTabs} />

      <div className="kpi-grid">
        <KpiCard label="Frota total" value={stats.fleet} variant="primary" />
        <KpiCard label="Disponíveis" value={stats.available} variant="success" />
        <KpiCard label="Em operação" value={stats.inUse} variant="warning" />
        <KpiCard label="Despachos ativos" value={stats.activeDispatches} variant="info" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Frota — {fleet.length} veículo(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Código</th><th>Placa</th><th>Status</th></tr></thead>
            <tbody>
              {fleet.map((a) => (
                <tr key={a.id}>
                  <td><strong>{a.code}</strong></td>
                  <td>{a.plate}</td>
                  <td><span className="badge">{ambulanceStatusLabels[a.status]}</span></td>
                </tr>
              ))}
              {fleet.length === 0 && (
                <tr><td colSpan={3} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma ambulância</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Despachos — {filtered.length} remoção(ões)</div>
        <FilterBar>
          <div className="filter-field w-lg">
            <label htmlFor="dispatchStatus">Status</label>
            <select id="dispatchStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(dispatchStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Paciente</th><th>Origem → Destino</th><th>Status</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {filtered.map((d) => (
                <tr key={d.id}>
                  <td><strong>{d.patientName}</strong></td>
                  <td>
                    {d.pickupAddress} → {d.destination}
                  </td>
                  <td><span className="badge">{dispatchStatusLabels[d.status]}</span></td>
                  <td>
                    <div className="table-actions">
                      {d.status === 'Requested' && (
                        <select
                          className="btn-sm"
                          onChange={(e) => e.target.value && assign(d.id, e.target.value)}
                          defaultValue=""
                        >
                          <option value="">Despachar...</option>
                          {fleet.filter((a) => a.status === 'Available').map((a) => (
                            <option key={a.id} value={a.id}>{a.code}</option>
                          ))}
                        </select>
                      )}
                      {d.status === 'Dispatched' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => advanceStatus(d.id, 'Transporting')}>Em transporte</button>
                      )}
                      {d.status === 'Transporting' && (
                        <button className="btn btn-sm" type="button" onClick={() => advanceStatus(d.id, 'Completed')}>Concluir</button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr><td colSpan={4} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum despacho</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal open={showModal} onClose={() => setShowModal(false)} title="Nova remoção" width="md">
        <form onSubmit={handleCreate} className="form-grid">
          <div className="form-field">
            <label htmlFor="ambPatient">Nome do paciente *</label>
            <input id="ambPatient" value={form.patientName} onChange={(e) => setForm({ ...form, patientName: e.target.value })} required />
          </div>
          <div className="form-field full">
            <label htmlFor="ambPickup">Endereço de origem *</label>
            <input id="ambPickup" value={form.pickupAddress} onChange={(e) => setForm({ ...form, pickupAddress: e.target.value })} required />
          </div>
          <div className="form-field full">
            <label htmlFor="ambDest">Destino *</label>
            <input id="ambDest" value={form.destination} onChange={(e) => setForm({ ...form, destination: e.target.value })} required />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Solicitar ambulância</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
