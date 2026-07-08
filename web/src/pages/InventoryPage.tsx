import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  productTypeLabels,
  stockMovementTypeLabels,
  type ProductDto,
  type StockMovementDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { inventoryTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { Link, useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';
import { FeegowInventoryWorkspacePage } from './FeegowInventoryWorkspacePage';

const emptyProduct = {
  name: '', sku: '', type: 1, unit: 'UN', minimumStock: 10, description: '',
  maximumStock: 0, averageSalePrice: 0,
};
const emptyInbound = { productId: '', quantity: 0, reason: 'Entrada de estoque', reference: '' };

export function InventoryPage() {
  const { appearance } = useAppearance();
  if (isFeegowBrand(appearance.brand)) {
    return <FeegowInventoryWorkspacePage />;
  }
  return <HospitalInventoryPage />;
}

function HospitalInventoryPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/estoque');
  const activeSection = section || 'inventario';

  const { hasPermission } = useAuth();
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [movements, setMovements] = useState<StockMovementDto[]>([]);
  const [search, setSearch] = useState('');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [productForm, setProductForm] = useState(emptyProduct);
  const [inboundForm, setInboundForm] = useState(emptyInbound);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showProductModal, setShowProductModal] = useState(false);
  const [showInboundModal, setShowInboundModal] = useState(false);

  async function load() {
    const [productList, movementList] = await Promise.all([
      api.getProducts(search, lowStockOnly),
      api.getStockMovements(),
    ]);
    setProducts(productList);
    setMovements(movementList);
  }

  useEffect(() => {
    load().catch(console.error);
  }, [lowStockOnly]);

  async function handleSearch(event: FormEvent) {
    event.preventDefault();
    await load();
  }

  async function handleCreateProduct(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createProduct(productForm);
      setSuccess('Produto cadastrado.');
      setProductForm(emptyProduct);
      setShowProductModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao cadastrar produto.');
    }
  }

  async function handleInbound(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.registerStockInbound({
        ...inboundForm,
        quantity: Number(inboundForm.quantity),
      });
      setSuccess('Entrada de estoque registrada.');
      setInboundForm(emptyInbound);
      setShowInboundModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na entrada de estoque.');
    }
  }

  const lowStockCount = products.filter((p) => p.isLowStock).length;

  const filteredMovements = movements.filter((m) => {
    if (activeSection === 'entradas') return m.type === 1;
    if (activeSection === 'saidas') return m.type === 2;
    if (activeSection === 'transferencias') return m.reason.toLowerCase().includes('transfer');
    return true;
  });

  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title={breadcrumb.title}
        subtitle="Controle de medicamentos e insumos hospitalares."
      >
        {hasPermission('warehouse.manage') && activeSection === 'entradas' && (
          <button className="btn btn-secondary" type="button" onClick={() => setShowInboundModal(true)}>+ Entrada</button>
        )}
        {hasPermission('warehouse.manage') && activeSection === 'inventario' && (
          <button className="btn" type="button" onClick={() => setShowProductModal(true)}>+ Produto</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/estoque" tabs={inventoryTabs} contextId="supply" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {lowStockCount > 0 && (
        <div className="alert alert-error">
          {lowStockCount} produto(s) abaixo do estoque mínimo.
        </div>
      )}

      <div className="kpi-grid">
        <KpiCard label="Produtos" value={products.length} variant="primary" />
        <KpiCard label="Estoque baixo" value={lowStockCount} variant={lowStockCount > 0 ? 'danger' : 'success'} />
        <KpiCard label="Movimentações" value={movements.length} variant="info" />
      </div>

      {activeSection === 'requisicoes' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Requisições de material</h3>
          <p>Solicite insumos ao almoxarifado central a partir dos setores assistenciais.</p>
          <Link to="/compras/solicitacoes" className="btn btn-secondary">Compras — solicitações</Link>
        </div>
      )}

      {activeSection === 'relatorios' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Relatórios de almoxarifado</h3>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 12 }}>
            <Link to="/relatorios/almoxarifado/entradas" className="btn btn-secondary">Entradas</Link>
            <Link to="/relatorios/almoxarifado/saidas" className="btn btn-secondary">Saídas</Link>
            <Link to="/relatorios/almoxarifado/inventario" className="btn btn-secondary">Inventário</Link>
          </div>
        </div>
      )}

      {activeSection === 'inventario' && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Produtos — {products.length} item(ns)</div>
        <FilterBar>
          <div className="filter-field grow">
            <label htmlFor="invSearch">Buscar</label>
            <input id="invSearch" placeholder="Produto ou SKU..." value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
          <div className="filter-field checkbox align-end">
            <label>
              <input type="checkbox" checked={lowStockOnly} onChange={(e) => setLowStockOnly(e.target.checked)} />
              Estoque baixo
            </label>
          </div>
          <div className="filter-field align-end">
            <button className="btn btn-secondary" type="button" onClick={handleSearch}>Buscar</button>
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Produto</th>
                <th>SKU</th>
                <th>Tipo</th>
                <th>Saldo</th>
                <th>Mínimo</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td>{p.sku}</td>
                  <td>{productTypeLabels[p.type]}</td>
                  <td>{p.quantityOnHand} {p.unit}</td>
                  <td>{p.minimumStock}</td>
                  <td>
                    {p.isLowStock
                      ? <span className="badge badge-danger">Estoque baixo</span>
                      : <span className="badge">OK</span>}
                  </td>
                </tr>
              ))}
              {products.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum produto encontrado.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {(activeSection === 'entradas' || activeSection === 'saidas' || activeSection === 'transferencias' || activeSection === 'inventario') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Movimentações — {filteredMovements.length} registro(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Produto</th>
                <th>Tipo</th>
                <th>Qtd</th>
                <th>Motivo</th>
              </tr>
            </thead>
            <tbody>
              {filteredMovements.map((m) => (
                <tr key={m.id}>
                  <td>{formatBrDateTime(m.createdAt)}</td>
                  <td>{m.productName}</td>
                  <td>{stockMovementTypeLabels[m.type]}</td>
                  <td>{m.quantity}</td>
                  <td>{m.reason}</td>
                </tr>
              ))}
              {filteredMovements.length === 0 && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma movimentação.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      <Modal open={showProductModal} onClose={() => setShowProductModal(false)} title="Cadastrar produto" width="md">
        <form className="form-grid" onSubmit={handleCreateProduct}>
          <div className="form-field">
            <label htmlFor="prodName">Nome *</label>
            <input id="prodName" required value={productForm.name} onChange={(e) => setProductForm({ ...productForm, name: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="prodSku">SKU *</label>
            <input id="prodSku" required value={productForm.sku} onChange={(e) => setProductForm({ ...productForm, sku: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="prodType">Tipo</label>
            <select id="prodType" value={productForm.type} onChange={(e) => setProductForm({ ...productForm, type: Number(e.target.value) })}>
              {Object.entries(productTypeLabels).map(([v, l]) => (
                <option key={v} value={v}>{l}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="prodUnit">Unidade *</label>
            <input id="prodUnit" required value={productForm.unit} onChange={(e) => setProductForm({ ...productForm, unit: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="prodMin">Estoque mínimo</label>
            <input id="prodMin" type="number" min={0} required value={productForm.minimumStock} onChange={(e) => setProductForm({ ...productForm, minimumStock: Number(e.target.value) })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowProductModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Salvar produto</button>
          </div>
        </form>
      </Modal>

      <Modal open={showInboundModal} onClose={() => setShowInboundModal(false)} title="Entrada de estoque" width="md">
        <form className="form-grid" onSubmit={handleInbound}>
          <div className="form-field">
            <label htmlFor="inProduct">Produto *</label>
            <select id="inProduct" required value={inboundForm.productId} onChange={(e) => setInboundForm({ ...inboundForm, productId: e.target.value })}>
              <option value="">Selecione</option>
              {products.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.sku})</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="inQty">Quantidade *</label>
            <input id="inQty" type="number" min={0.001} step={1} required value={inboundForm.quantity || ''} onChange={(e) => setInboundForm({ ...inboundForm, quantity: Number(e.target.value) })} />
          </div>
          <div className="form-field">
            <label htmlFor="inReason">Motivo *</label>
            <input id="inReason" required value={inboundForm.reason} onChange={(e) => setInboundForm({ ...inboundForm, reason: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="inRef">Referência</label>
            <input id="inRef" value={inboundForm.reference} onChange={(e) => setInboundForm({ ...inboundForm, reference: e.target.value })} placeholder="NF, lote..." />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowInboundModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Registrar entrada</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
