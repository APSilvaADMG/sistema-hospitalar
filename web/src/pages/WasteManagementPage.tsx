import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  wasteStatusLabels,
  wasteTypeLabels,
  type WasteCollectionDto,
  type WasteDashboardDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { formatBrDateTime } from '../utils/dateUtils';

const emptyForm = {
  wasteType: 'Infectious',
  sectorName: '',
  quantityKg: 1,
  containerCode: '',
  collectedBy: '',
  manifestNumber: '',
  notes: '',
};

export function WasteManagementPage() {
  const { hasPermission } = useAuth();
  const [dashboard, setDashboard] = useState<WasteDashboardDto | null>(null);
  const [collections, setCollections] = useState<WasteCollectionDto[]>([]);
  const [typeFilter, setTypeFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [form, setForm] = useState(emptyForm);
  const [showModal, setShowModal] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => { load(); }, []);

  async function load() {
    const [dash, list] = await Promise.all([
      api.getWasteDashboard(),
      api.getWasteCollections(),
    ]);
    setDashboard(dash);
    setCollections(list);
  }

  const filtered = useMemo(() => collections.filter((c) => {
    if (typeFilter && c.wasteType !== typeFilter) return false;
    if (statusFilter && c.status !== statusFilter) return false;
    return true;
  }), [collections, typeFilter, statusFilter]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.createWasteCollection({
        ...form,
        quantityKg: Number(form.quantityKg),
        manifestNumber: form.manifestNumber || undefined,
        notes: form.notes || undefined,
      });
      setShowModal(false);
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar coleta.');
    }
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito à operação hospitalar.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Operação hospitalar"
        title="Resíduos hospitalares"
        subtitle="Gestão de coleta, armazenamento e destinação — ANVISA RDC 222/2018."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>+ Nova coleta</button>
      </PageHeader>

      <div className="kpi-grid">
        <KpiCard label="Coletas registradas" value={dashboard?.totalCollections ?? '—'} variant="primary" />
        <KpiCard label="Volume total (kg)" value={dashboard ? dashboard.totalKg.toFixed(1) : '—'} variant="info" />
        <KpiCard
          label="Tipos distintos"
          value={dashboard?.byType.length ?? '—'}
          variant="default"
        />
        <KpiCard
          label="Infectantes (kg)"
          value={dashboard?.byType.find((k) => k.wasteType === 'Infectious')?.totalKg.toFixed(1) ?? '0'}
          variant="warning"
        />
      </div>

      {dashboard && dashboard.byType.length > 0 && (
        <div className="kpi-grid" style={{ marginTop: 8 }}>
          {dashboard.byType.map((k) => (
            <KpiCard
              key={k.wasteType}
              label={wasteTypeLabels[k.wasteType] ?? k.wasteType}
              value={`${k.count} · ${k.totalKg.toFixed(1)} kg`}
            />
          ))}
        </div>
      )}

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Coletas — {filtered.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-lg">
            <label htmlFor="wasteType">Tipo</label>
            <select id="wasteType" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(wasteTypeLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="wasteStatus">Status</label>
            <select id="wasteStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(wasteStatusLabels).map(([k, v]) => (
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
                <th>Tipo</th>
                <th>Setor</th>
                <th>Qtd (kg)</th>
                <th>Recipiente</th>
                <th>Coletado em</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((c) => (
                <tr key={c.id}>
                  <td>{c.code}</td>
                  <td>{wasteTypeLabels[c.wasteType] ?? c.wasteType}</td>
                  <td>{c.sectorName}</td>
                  <td>{c.quantityKg.toFixed(2)}</td>
                  <td>{c.containerCode}</td>
                  <td>{formatBrDateTime(c.collectedAt)}</td>
                  <td><span className="badge">{wasteStatusLabels[c.status] ?? c.status}</span></td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma coleta encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal open={showModal} title="Registrar coleta de resíduo" onClose={() => setShowModal(false)}>
        <form onSubmit={handleCreate} className="form-stack">
          {error && <div className="alert alert-error">{error}</div>}
          <div className="form-row">
            <label>Tipo de resíduo</label>
            <select value={form.wasteType} onChange={(e) => setForm({ ...form, wasteType: e.target.value })} required>
              {Object.entries(wasteTypeLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-row">
            <label>Setor</label>
            <input value={form.sectorName} onChange={(e) => setForm({ ...form, sectorName: e.target.value })} required />
          </div>
          <div className="form-row">
            <label>Quantidade (kg)</label>
            <input type="number" step="0.1" min="0.1" value={form.quantityKg}
              onChange={(e) => setForm({ ...form, quantityKg: Number(e.target.value) })} required />
          </div>
          <div className="form-row">
            <label>Código do recipiente</label>
            <input value={form.containerCode} onChange={(e) => setForm({ ...form, containerCode: e.target.value })} required />
          </div>
          <div className="form-row">
            <label>Responsável pela coleta</label>
            <input value={form.collectedBy} onChange={(e) => setForm({ ...form, collectedBy: e.target.value })} required />
          </div>
          <div className="form-row">
            <label>Manifesto (opcional)</label>
            <input value={form.manifestNumber} onChange={(e) => setForm({ ...form, manifestNumber: e.target.value })} />
          </div>
          <div className="form-row">
            <label>Observações</label>
            <textarea value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} rows={2} />
          </div>
          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Salvar</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
