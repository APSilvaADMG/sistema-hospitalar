import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  dietOrderStatusLabels,
  dietTypeLabels,
  mealPeriodLabels,
  type DietOrderDto,
  type HospitalizationDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { nutritionTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { Link, useLocation } from 'react-router-dom';
import { formatBrDate } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

const today = new Date().toISOString().slice(0, 10);

export function NutritionPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/nutricao');
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [orders, setOrders] = useState<DietOrderDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [form, setForm] = useState({
    hospitalizationId: '',
    dietType: 'Regular',
    mealPeriod: 'Lunch',
    mealDate: today,
    notes: '',
  });
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [mealFilter, setMealFilter] = useState('');
  const [search, setSearch] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    load();
    api.getHospitalizations().then(setHospitalizations).catch(console.error);
  }, []);

  async function load() {
    setOrders(await api.getDietOrders());
  }

  const active = hospitalizations.filter((h) => h.status === 1);

  const stats = useMemo(() => ({
    today: orders.filter((o) => o.mealDate.startsWith(today)).length,
    pending: orders.filter((o) => o.status === 'Pending' || o.status === 'InPreparation').length,
    delivered: orders.filter((o) => o.status === 'Delivered').length,
    hospitalized: active.length,
  }), [orders, active]);

  const filtered = useMemo(() => {
    return orders
      .filter((o) => {
        if (activeSection === 'producao') return o.status === 'InPreparation' || o.status === 'Pending';
        if (activeSection === 'distribuicao') return o.status === 'Ready' || o.status === 'Delivered';
        if (activeSection === 'dietas') return true;
        return true;
      })
      .filter((o) => !statusFilter || o.status === statusFilter)
      .filter((o) => !mealFilter || o.mealPeriod === mealFilter)
      .filter((o) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          o.patientName.toLowerCase().includes(term)
          || o.wardName.toLowerCase().includes(term)
          || o.bedNumber.toLowerCase().includes(term)
        );
      });
  }, [orders, statusFilter, mealFilter, search, activeSection]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    await api.createDietOrder(form);
    setSuccess('Dieta solicitada com sucesso.');
    setShowModal(false);
    setForm({ hospitalizationId: '', dietType: 'Regular', mealPeriod: 'Lunch', mealDate: today, notes: '' });
    await load();
  }

  async function markDelivered(id: string) {
    await api.updateDietOrderStatus(id, 'Delivered');
    setSuccess('Refeição marcada como entregue.');
    await load();
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Internação"
        title={activeSection ? breadcrumb.title : 'Nutrição Hospitalar'}
        subtitle="Prescrição de dietas por internação, refeição e acompanhamento de entregas."
      >
        {(activeSection === '' || activeSection === 'dietas') && (
        <button className="btn" type="button" onClick={() => setShowModal(true)}>
          + Nova dieta
        </button>
        )}
      </PageHeader>

      <ModuleNav basePath="/nutricao" tabs={nutritionTabs} />

      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Pedidos hoje" value={stats.today} variant="primary" />
        <KpiCard label="Pendentes / em preparo" value={stats.pending} variant="warning" />
        <KpiCard label="Entregues" value={stats.delivered} variant="success" />
        <KpiCard label="Pacientes internados" value={stats.hospitalized} variant="info" />
      </div>

      {activeSection === 'relatorios' && (
        <div className="card" style={{ marginTop: 16 }}>
          <Link to="/relatorios" className="btn btn-secondary">Relatórios de nutrição</Link>
        </div>
      )}

      {activeSection !== 'relatorios' && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">
          {activeSection === 'producao' && 'Produção — cozinha'}
          {activeSection === 'distribuicao' && 'Distribuição de refeições'}
          {(!activeSection || activeSection === 'dietas') && `Pedidos de dieta — ${filtered.length} registro(s)`}
        </div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="dietStatus">Status</label>
            <select id="dietStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(dietOrderStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-xs">
            <label htmlFor="mealPeriod">Refeição</label>
            <select id="mealPeriod" value={mealFilter} onChange={(e) => setMealFilter(e.target.value)}>
              <option value="">Todas</option>
              {Object.entries(mealPeriodLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="dietSearch">Buscar</label>
            <input
              id="dietSearch"
              placeholder="Paciente, ala ou leito..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Paciente</th><th>Leito</th><th>Dieta</th><th>Refeição</th><th>Data</th><th>Status</th><th></th></tr>
            </thead>
            <tbody>
              {filtered.map((o) => (
                <tr key={o.id}>
                  <td><strong>{o.patientName}</strong></td>
                  <td>{o.wardName} {o.bedNumber}</td>
                  <td>{dietTypeLabels[o.dietType]}</td>
                  <td>{mealPeriodLabels[o.mealPeriod]}</td>
                  <td>{formatBrDate(o.mealDate)}</td>
                  <td><span className="badge">{dietOrderStatusLabels[o.status]}</span></td>
                  <td>
                    {o.status !== 'Delivered' && (
                      <button className="btn btn-secondary btn-sm" type="button" onClick={() => markDelivered(o.id)}>
                        Entregar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhum pedido encontrado.
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
        title="Nova dieta"
        subtitle="Prescreva a dieta para um paciente internado."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleCreate}>
          <div className="form-field full">
            <label htmlFor="hospitalizationId">Internação *</label>
            <select id="hospitalizationId" value={form.hospitalizationId} onChange={(e) => setForm({ ...form, hospitalizationId: e.target.value })} required>
              <option value="">Selecione...</option>
              {active.map((h) => (
                <option key={h.id} value={h.id}>{h.patientName} — {h.wardName} Leito {h.bedNumber}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="dietType">Tipo de dieta</label>
            <select id="dietType" value={form.dietType} onChange={(e) => setForm({ ...form, dietType: e.target.value })}>
              {Object.entries(dietTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="mealPeriodSelect">Refeição</label>
            <select id="mealPeriodSelect" value={form.mealPeriod} onChange={(e) => setForm({ ...form, mealPeriod: e.target.value })}>
              {Object.entries(mealPeriodLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="mealDate">Data</label>
            <input id="mealDate" type="date" value={form.mealDate} onChange={(e) => setForm({ ...form, mealDate: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="notes">Observações</label>
            <input id="notes" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Solicitar dieta</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
