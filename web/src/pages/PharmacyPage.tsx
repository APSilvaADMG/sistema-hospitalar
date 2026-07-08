import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useLocation, useSearchParams } from 'react-router-dom';
import {
  api,
  type PatientDto,
  type PharmacyDispensingDto,
  type ProductDto,
  type ProfessionalDto,
  type SpecialtyClinicalCatalogDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { pharmacyTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { formatBrDateTime } from '../utils/dateUtils';
import { SpecialtyCatalogPanel } from '../components/SpecialtyCatalogPanel';
import { useAuth } from '../auth/AuthContext';

export function PharmacyPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/farmacia');
  const activeSection = section || '';

  const { user } = useAuth();
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [catalog, setCatalog] = useState<SpecialtyClinicalCatalogDto | null>(null);
  const [dispensings, setDispensings] = useState<PharmacyDispensingDto[]>([]);
  const [form, setForm] = useState({
    patientId: '',
    productId: '',
    professionalId: user?.professionalId ?? '',
    quantity: 1,
    notes: '',
  });
  const [search, setSearch] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [reverseTarget, setReverseTarget] = useState<PharmacyDispensingDto | null>(null);
  const [reverseForm, setReverseForm] = useState({ quantity: '', reason: '' });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [searchParams, setSearchParams] = useSearchParams();

  async function load() {
    const [productList, patientList, dispensingList, profList] = await Promise.all([
      api.getProducts(undefined, false),
      api.getPatients(undefined, 1),
      api.getDispensings(),
      api.getProfessionals(),
    ]);
    setProducts(productList.filter((p) => p.type === 1));
    setPatients(patientList.items);
    setDispensings(dispensingList);
    setProfessionals(profList);
    if (form.professionalId) {
      setCatalog(await api.getClinicalCatalogByProfessional(form.professionalId));
    }
  }

  useEffect(() => {
    load().catch(console.error);
  }, []);

  useEffect(() => {
    if (searchParams.get('dispensar') !== '1') return;
    setShowModal(true);
    const next = new URLSearchParams(searchParams);
    next.delete('dispensar');
    setSearchParams(next, { replace: true });
  }, [searchParams, setSearchParams]);

  async function handleProfessionalChange(professionalId: string) {
    setForm((f) => ({ ...f, professionalId }));
    if (professionalId) {
      setCatalog(await api.getClinicalCatalogByProfessional(professionalId));
    } else {
      setCatalog(null);
    }
  }

  function handleMedCatalogSelect(medId: string, medName: string) {
    const med = catalog?.medications.find((m) => m.id === medId);
    const product = med?.productId
      ? products.find((p) => p.id === med.productId)
      : products.find((p) => p.name.toLowerCase().includes(medName.split(' ')[0].toLowerCase()));
    setForm((f) => ({
      ...f,
      productId: product?.id ?? f.productId,
      notes: med?.defaultDosage ?? f.notes,
    }));
  }

  async function handleDispense(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.dispenseMedication({
        patientId: form.patientId,
        productId: form.productId,
        quantity: Number(form.quantity),
        professionalId: form.professionalId || user?.professionalId,
        notes: form.notes || undefined,
      });
      setSuccess('Medicamento dispensado. Estoque atualizado.');
      setForm((f) => ({ ...f, patientId: '', productId: '', quantity: 1, notes: '' }));
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na dispensação.');
    }
  }

  async function handleReverse(event: FormEvent) {
    event.preventDefault();
    if (!reverseTarget) return;
    setError('');
    setSuccess('');
    const remaining = reverseTarget.quantity - (reverseTarget.reversedQuantity ?? 0);
    const qty = Number(reverseForm.quantity);
    if (!qty || qty > remaining) {
      setError(`Informe uma quantidade válida (máx. ${remaining}).`);
      return;
    }
    try {
      await api.reversePharmacyDispensing(reverseTarget.id, {
        quantity: qty,
        reason: reverseForm.reason || undefined,
      });
      setSuccess('Estorno registrado. Estoque atualizado.');
      setReverseTarget(null);
      setReverseForm({ quantity: '', reason: '' });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao estornar dispensação.');
    }
  }

  function openReverseModal(d: PharmacyDispensingDto) {
    const remaining = d.quantity - (d.reversedQuantity ?? 0);
    setReverseTarget(d);
    setReverseForm({ quantity: String(remaining), reason: '' });
  }

  const stats = useMemo(() => ({
    dispensings: dispensings.length,
    products: products.length,
    lowStock: products.filter((p) => p.isLowStock).length,
  }), [dispensings, products]);

  const filteredDispensings = useMemo(() => {
    if (!search.trim()) return dispensings;
    const term = search.toLowerCase();
    return dispensings.filter((d) =>
      d.patientName.toLowerCase().includes(term)
      || d.productName.toLowerCase().includes(term)
      || (d.professionalName?.toLowerCase().includes(term) ?? false),
    );
  }, [dispensings, search]);

  return (
    <>
      <PageHeader eyebrow="Diagnóstico" title={activeSection ? breadcrumb.title : 'Farmácia'} subtitle="Dispensação com catálogo de medicamentos por especialidade.">
        <Link to="/medicamentos" className="btn btn-secondary">Consultar bulário</Link>
        {(activeSection === '' || activeSection === 'solicitacoes') && (
          <button className="btn" type="button" onClick={() => setShowModal(true)}>
            + Dispensar medicamento
          </button>
        )}
      </PageHeader>

      <ModuleNav basePath="/farmacia" tabs={pharmacyTabs} contextId="pharmacy" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Dispensações" value={stats.dispensings} variant="primary" />
        <KpiCard label="Medicamentos em estoque" value={stats.products} variant="info" />
        <KpiCard label="Estoque baixo" value={stats.lowStock} variant="warning" />
      </div>

      {(activeSection === 'estoque' || activeSection === 'lotes' || activeSection === 'validades' || activeSection === 'inventario') && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">
            {activeSection === 'estoque' && 'Estoque de medicamentos'}
            {activeSection === 'lotes' && 'Lotes'}
            {activeSection === 'validades' && 'Validades'}
            {activeSection === 'inventario' && 'Inventário'}
          </div>
          <table className="data-table">
            <thead><tr><th>Medicamento</th><th>SKU</th><th>Saldo</th><th>Mínimo</th><th>Status</th></tr></thead>
            <tbody>
              {products.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td>{p.sku}</td>
                  <td>{p.quantityOnHand} {p.unit}</td>
                  <td>{p.minimumStock}</td>
                  <td>{p.isLowStock ? 'Baixo' : 'OK'}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Link to="/estoque" className="btn btn-secondary btn-sm" style={{ margin: 16 }}>Almoxarifado completo</Link>
        </div>
      )}

      {activeSection === 'relatorios' && (
        <div className="card" style={{ marginTop: 24 }}>
          <h3>Relatórios de farmácia</h3>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 12 }}>
            <Link to="/relatorios/farmacia/consumo" className="btn btn-secondary">Consumo</Link>
            <Link to="/relatorios/farmacia/estoque" className="btn btn-secondary">Estoque</Link>
            <Link to="/relatorios/farmacia/validades" className="btn btn-secondary">Validades</Link>
          </div>
        </div>
      )}

      {(activeSection === 'transferencias' || activeSection === 'devolucoes') && (
        <div className="card" style={{ marginTop: 24 }}>
          <h3>{activeSection === 'transferencias' ? 'Transferências entre setores' : 'Estornos / devoluções'}</h3>
          <p>
            {activeSection === 'transferencias'
              ? 'Utilize o almoxarifado para registrar movimentações de entrada e saída vinculadas à farmácia.'
              : 'Selecione uma dispensação na tabela abaixo e use Estornar para devolver ao estoque.'}
          </p>
          {activeSection === 'transferencias' ? (
            <Link to="/estoque/transferencias" className="btn btn-secondary">Abrir transferências</Link>
          ) : null}
        </div>
      )}

      {(activeSection === '' || activeSection === 'solicitacoes' || activeSection === 'devolucoes') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">
          {activeSection === 'solicitacoes' && 'Solicitações / dispensações'}
          {activeSection === 'devolucoes' && `Dispensações para estorno — ${filteredDispensings.length} registro(s)`}
          {!activeSection && `Dispensações recentes — ${filteredDispensings.length} registro(s)`}
        </div>
        <FilterBar>
          <div className="filter-field grow">
            <label htmlFor="pharmSearch">Buscar</label>
            <input
              id="pharmSearch"
              placeholder="Paciente, medicamento ou profissional..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Paciente</th>
                <th>Medicamento</th>
                <th>Qtd</th>
                <th>Estornado</th>
                <th>Profissional</th>
                {(activeSection === '' || activeSection === 'devolucoes') && <th>Ações</th>}
              </tr>
            </thead>
            <tbody>
              {filteredDispensings.map((d) => {
                const remaining = d.quantity - (d.reversedQuantity ?? 0);
                return (
                <tr key={d.id}>
                  <td>{formatBrDateTime(d.dispensedAt)}</td>
                  <td><strong>{d.patientName}</strong></td>
                  <td>{d.productName}</td>
                  <td>{d.quantity}</td>
                  <td>{d.reversedQuantity ?? 0}</td>
                  <td>{d.professionalName ?? '—'}</td>
                  {(activeSection === '' || activeSection === 'devolucoes') && (
                    <td>
                      {remaining > 0 ? (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => openReverseModal(d)}>
                          Estornar
                        </button>
                      ) : (
                        <span className="form-hint">Total estornado</span>
                      )}
                    </td>
                  )}
                </tr>
              );})}
              {filteredDispensings.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma dispensação registrada.
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
        title="Dispensar medicamento"
        subtitle="Selecione o médico para filtrar o catálogo por especialidade."
        width="lg"
      >
        <form onSubmit={handleDispense}>
          <div className="form-grid">
            <div className="form-field">
              <label htmlFor="pharmPatient">Paciente</label>
              <select id="pharmPatient" required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
                <option value="">Selecione</option>
                {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
            <div className="form-field">
              <label htmlFor="pharmProfessional">Médico / especialidade</label>
              <select id="pharmProfessional" value={form.professionalId} onChange={(e) => handleProfessionalChange(e.target.value)}>
                <option value="">Geral</option>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label htmlFor="pharmProduct">Medicamento (estoque)</label>
              <select id="pharmProduct" required value={form.productId} onChange={(e) => setForm({ ...form, productId: e.target.value })}>
                <option value="">Selecione</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>{p.name} — {p.quantityOnHand} {p.unit}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label htmlFor="pharmQty">Quantidade</label>
              <input id="pharmQty" type="number" min={0.001} step={1} required value={form.quantity} onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })} />
            </div>
            <div className="form-field full">
              <label htmlFor="pharmNotes">Posologia / observações</label>
              <input id="pharmNotes" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>

          {catalog && (
            <SpecialtyCatalogPanel
              specialtyName={catalog.specialtyName}
              labExams={[]}
              imagingProcedures={[]}
              medications={catalog.medications}
              onMedToggle={handleMedCatalogSelect}
              showLabs={false}
              showImaging={false}
            />
          )}

          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Dispensar</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={Boolean(reverseTarget)}
        onClose={() => setReverseTarget(null)}
        title="Estornar dispensação"
        subtitle={reverseTarget ? `${reverseTarget.productName} — ${reverseTarget.patientName}` : undefined}
      >
        {reverseTarget ? (
          <form onSubmit={handleReverse}>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="reverseQty">Quantidade a estornar</label>
                <input
                  id="reverseQty"
                  type="number"
                  min={0.001}
                  step={1}
                  required
                  value={reverseForm.quantity}
                  onChange={(e) => setReverseForm({ ...reverseForm, quantity: e.target.value })}
                />
              </div>
              <div className="form-field full">
                <label htmlFor="reverseReason">Motivo</label>
                <input
                  id="reverseReason"
                  value={reverseForm.reason}
                  onChange={(e) => setReverseForm({ ...reverseForm, reason: e.target.value })}
                  placeholder="Ex.: devolução do paciente, erro de dispensação"
                />
              </div>
            </div>
            <div className="form-field full modal-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setReverseTarget(null)}>Cancelar</button>
              <button className="btn" type="submit">Confirmar estorno</button>
            </div>
          </form>
        ) : null}
      </Modal>
    </>
  );
}
