import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  laundryBatchStatusLabels,
  laundryOriginLabels,
  type LaundryBatchDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { laundryTabs } from '../navigation/moduleSections';
import { useAuth } from '../auth/AuthContext';

const statusFlow: Record<string, string> = {
  Collected: 'Washing',
  Washing: 'Drying',
  Drying: 'Delivered',
};

export function LaundryPage() {
  const { hasPermission } = useAuth();
  const [batches, setBatches] = useState<LaundryBatchDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [form, setForm] = useState({
    origin: 'Ward',
    originDetail: '',
    itemCount: 20,
    weightKg: 8,
    notes: '',
  });
  const [showModal, setShowModal] = useState(false);

  useEffect(() => { load(); }, []);

  async function load() {
    setBatches(await api.getLaundryBatches());
  }

  const stats = useMemo(() => ({
    total: batches.length,
    collected: batches.filter((b) => b.status === 'Collected').length,
    processing: batches.filter((b) => b.status === 'Washing' || b.status === 'Drying').length,
    delivered: batches.filter((b) => b.status === 'Delivered').length,
  }), [batches]);

  const filtered = useMemo(() => {
    return batches.filter((b) => !statusFilter || b.status === statusFilter);
  }, [batches, statusFilter]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    await api.createLaundryBatch({
      ...form,
      itemCount: Number(form.itemCount),
      weightKg: Number(form.weightKg),
      originDetail: form.originDetail || undefined,
      notes: form.notes || undefined,
    });
    setShowModal(false);
    await load();
  }

  async function advanceStatus(id: string, current: string) {
    const next = statusFlow[current];
    if (next) {
      await api.updateLaundryBatchStatus(id, next);
      await load();
    }
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito à recepção.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Infraestrutura"
        title="Lavanderia"
        subtitle="Controle de lotes de roupa hospitalar — coleta, lavagem e entrega."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>+ Novo lote</button>
      </PageHeader>

      <ModuleNav basePath="/lavanderia" tabs={laundryTabs} />

      <div className="kpi-grid">
        <KpiCard label="Lotes ativos" value={stats.total} variant="primary" />
        <KpiCard label="Coletados" value={stats.collected} variant="info" />
        <KpiCard label="Em processamento" value={stats.processing} variant="warning" />
        <KpiCard label="Entregues" value={stats.delivered} variant="success" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Lotes em processamento — {filtered.length} lote(s)</div>
        <FilterBar>
          <div className="filter-field w-lg">
            <label htmlFor="laundryStatus">Status</label>
            <select id="laundryStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(laundryBatchStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Lote</th><th>Origem</th><th>Itens</th><th>Status</th><th>Ações</th></tr></thead>
            <tbody>
              {filtered.map((b) => (
                <tr key={b.id}>
                  <td>{b.batchNumber}</td>
                  <td>{laundryOriginLabels[b.origin]} {b.originDetail ? `— ${b.originDetail}` : ''}</td>
                  <td>{b.itemCount} ({b.weightKg} kg)</td>
                  <td><span className="badge">{laundryBatchStatusLabels[b.status] ?? b.status}</span></td>
                  <td>
                    {statusFlow[b.status] && (
                      <button className="btn btn-secondary btn-sm" type="button" onClick={() => advanceStatus(b.id, b.status)}>
                        → {laundryBatchStatusLabels[statusFlow[b.status]]}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum lote</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal open={showModal} onClose={() => setShowModal(false)} title="Novo lote" width="md">
        <form onSubmit={handleCreate} className="form-grid">
          <div className="form-field">
            <label htmlFor="laundryOrigin">Origem</label>
            <select id="laundryOrigin" value={form.origin} onChange={(e) => setForm({ ...form, origin: e.target.value })}>
              {Object.entries(laundryOriginLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="laundryDetail">Origem detalhada</label>
            <input id="laundryDetail" value={form.originDetail} onChange={(e) => setForm({ ...form, originDetail: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="laundryItems">Itens</label>
            <input id="laundryItems" type="number" value={form.itemCount} onChange={(e) => setForm({ ...form, itemCount: Number(e.target.value) })} />
          </div>
          <div className="form-field">
            <label htmlFor="laundryWeight">Peso (kg)</label>
            <input id="laundryWeight" type="number" step="0.1" value={form.weightKg} onChange={(e) => setForm({ ...form, weightKg: Number(e.target.value) })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Registrar coleta</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
