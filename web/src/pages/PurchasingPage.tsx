import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  api,
  purchaseOrderStatusLabels,
  purchasePriorityLabel,
  purchasePriorityValue,
  purchaseSectorLabel,
  purchaseSectorValue,
  type ProductDto,
  type PurchaseCreateSuggestionsDto,
  type PurchaseOrderDto,
  type PurchaseSuggestedItemDto,
  type SupplierDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { purchasingTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

type OrderLine = {
  key: string;
  productId: string;
  quantity: number;
  unitPrice: number;
};

function addDaysIso(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

function newLine(productId = '', quantity = 1, unitPrice = 0): OrderLine {
  return { key: crypto.randomUUID(), productId, quantity, unitPrice };
}

function linesFromSuggestions(items: PurchaseSuggestedItemDto[]): OrderLine[] {
  return items.map((item) => newLine(item.productId, item.suggestedQuantity, item.suggestedUnitPrice));
}

function mergeLines(existing: OrderLine[], incoming: OrderLine[]): OrderLine[] {
  const map = new Map(existing.map((line) => [line.productId, line]));
  for (const line of incoming) {
    if (!line.productId) continue;
    map.set(line.productId, line);
  }
  return Array.from(map.values());
}

const emptyOrderForm = {
  sector: 1,
  priority: 1,
  supplierId: '',
  requestedBy: '',
  justification: '',
  expectedAt: addDaysIso(7),
  notes: '',
};

export function PurchasingPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/compras');
  const activeSection = section || '';

  const { hasPermission, user } = useAuth();
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [orders, setOrders] = useState<PurchaseOrderDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [supplierForm, setSupplierForm] = useState({ name: '', cnpj: '', email: '', phone: '', contactName: '' });
  const [orderForm, setOrderForm] = useState(emptyOrderForm);
  const [orderLines, setOrderLines] = useState<OrderLine[]>([newLine()]);
  const [suggestions, setSuggestions] = useState<PurchaseCreateSuggestionsDto | null>(null);
  const [suggestionsLoading, setSuggestionsLoading] = useState(false);
  const [feedback, setFeedback] = useState('');
  const [error, setError] = useState('');
  const [showSupplierModal, setShowSupplierModal] = useState(false);
  const [showOrderModal, setShowOrderModal] = useState(false);
  const [creating, setCreating] = useState(false);

  useEffect(() => { load().catch(console.error); }, []);

  async function load() {
    const [s, o, p] = await Promise.all([api.getSuppliers(), api.getPurchaseOrders(), api.getProducts()]);
    setSuppliers(s);
    setOrders(o);
    setProducts(p);
  }

  const loadSuggestions = useCallback(async (sector: number, priority: number) => {
    setSuggestionsLoading(true);
    try {
      const data = await api.getPurchaseSuggestions(sector, priority);
      setSuggestions(data);
      setOrderForm((prev) => ({
        ...prev,
        sector,
        priority,
        supplierId: data.suggestedSupplierId ?? prev.supplierId,
        expectedAt: addDaysIso(data.suggestedDeliveryDays),
        justification: prev.justification || buildDefaultJustification(data),
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar sugestões');
    } finally {
      setSuggestionsLoading(false);
    }
  }, []);

  function buildDefaultJustification(data: PurchaseCreateSuggestionsDto): string {
    const sectorLabel = data.sectors.find((s) => purchaseSectorValue(s.sector) === purchaseSectorValue(data.selectedSector))?.label ?? 'Setor';
    const lowCount = data.lowStockItems.length;
    if (lowCount > 0) {
      return `Reposição solicitada por ${sectorLabel} — ${lowCount} item(ns) abaixo do estoque mínimo.`;
    }
    return `Pedido de materiais — ${sectorLabel}.`;
  }

  function openOrderModal() {
    const sector = 1;
    const priority = 1;
    setOrderForm({
      ...emptyOrderForm,
      sector,
      priority,
      requestedBy: user?.fullName ?? '',
      expectedAt: addDaysIso(7),
    });
    setOrderLines([newLine()]);
    setSuggestions(null);
    setError('');
    setShowOrderModal(true);
    void loadSuggestions(sector, priority);
  }

  function applySector(sector: number) {
    void loadSuggestions(sector, orderForm.priority);
  }

  function applyPriority(priority: number) {
    setOrderForm((prev) => ({ ...prev, priority }));
    void loadSuggestions(orderForm.sector, priority);
  }

  function addSuggestedItems(items: PurchaseSuggestedItemDto[]) {
    const incoming = linesFromSuggestions(items);
    setOrderLines((prev) => mergeLines(prev.filter((l) => l.productId), incoming));
  }

  function updateLine(key: string, patch: Partial<OrderLine>) {
    setOrderLines((prev) => prev.map((line) => {
      if (line.key !== key) return line;
      const updated = { ...line, ...patch };
      if (patch.productId) {
        const product = products.find((p) => p.id === patch.productId);
        const suggested = [...(suggestions?.lowStockItems ?? []), ...(suggestions?.kitItems ?? [])]
          .find((i) => i.productId === patch.productId);
        if (suggested && !patch.unitPrice && !patch.quantity) {
          updated.quantity = suggested.suggestedQuantity;
          updated.unitPrice = suggested.suggestedUnitPrice;
        } else if (product && updated.unitPrice === 0) {
          updated.unitPrice = suggested?.suggestedUnitPrice ?? 1;
        }
      }
      return updated;
    }));
  }

  function removeLine(key: string) {
    setOrderLines((prev) => (prev.length <= 1 ? prev : prev.filter((l) => l.key !== key)));
  }

  const orderTotal = useMemo(
    () => orderLines.reduce((sum, line) => sum + (line.quantity * line.unitPrice), 0),
    [orderLines],
  );

  const validLines = useMemo(
    () => orderLines.filter((l) => l.productId && l.quantity > 0 && l.unitPrice >= 0),
    [orderLines],
  );

  const stats = useMemo(() => ({
    suppliers: suppliers.length,
    orders: orders.length,
    draft: orders.filter((o) => o.status === 'Draft').length,
    pending: orders.filter((o) => o.status === 'Sent' || o.status === 'PartiallyReceived').length,
  }), [suppliers, orders]);

  const displayOrders = useMemo(() => {
    if (activeSection === 'recebimento') {
      return orders.filter((o) => o.status === 'Sent' || o.status === 'PartiallyReceived');
    }
    if (activeSection === 'solicitacoes' || activeSection === 'cotacoes') {
      return orders.filter((o) => o.status === 'Draft');
    }
    return orders;
  }, [orders, activeSection]);

  if (!hasPermission('warehouse.manage', 'reports.read', 'patients.create')) {
    return <div className="card">Acesso restrito à equipe administrativa.</div>;
  }

  async function handleSupplier(e: FormEvent) {
    e.preventDefault();
    await api.createSupplier(supplierForm);
    setSupplierForm({ name: '', cnpj: '', email: '', phone: '', contactName: '' });
    setFeedback('Fornecedor cadastrado.');
    setShowSupplierModal(false);
    await load();
  }

  async function handleOrder(e: FormEvent) {
    e.preventDefault();
    if (!orderForm.supplierId) {
      setError('Selecione o fornecedor.');
      return;
    }
    if (!orderForm.requestedBy.trim()) {
      setError('Informe o solicitante.');
      return;
    }
    if (validLines.length === 0) {
      setError('Adicione ao menos um item válido ao pedido.');
      return;
    }

    setCreating(true);
    setError('');
    try {
      await api.createPurchaseOrder({
        supplierId: orderForm.supplierId,
        sector: orderForm.sector,
        priority: orderForm.priority,
        requestedBy: orderForm.requestedBy.trim(),
        justification: orderForm.justification.trim() || undefined,
        expectedAt: orderForm.expectedAt
          ? new Date(`${orderForm.expectedAt}T12:00:00`).toISOString()
          : undefined,
        notes: orderForm.notes.trim() || undefined,
        items: validLines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
        })),
      });
      setFeedback('Pedido de compra criado com sucesso.');
      setShowOrderModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar pedido');
    } finally {
      setCreating(false);
    }
  }

  async function sendOrder(id: string) {
    await api.sendPurchaseOrder(id);
    setFeedback('Pedido enviado ao fornecedor.');
    await load();
  }

  async function receiveAll(order: PurchaseOrderDto) {
    await api.receivePurchaseOrder(order.id, {
      items: order.items
        .filter((i) => i.receivedQuantity < i.quantity)
        .map((i) => ({ itemId: i.id, quantity: i.quantity - i.receivedQuantity })),
    });
    setFeedback('Mercadorias recebidas — estoque atualizado.');
    await load();
  }

  const selectedSectorPreset = suggestions?.sectors.find(
    (s) => purchaseSectorValue(s.sector) === orderForm.sector,
  );

  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title={activeSection ? breadcrumb.title : 'Compras'}
        subtitle="Pedidos por setor com sugestão de itens em falta, kits e fornecedor automático."
      >
        {(activeSection === '' || activeSection === 'fornecedores') && (
          <button className="btn btn-secondary" type="button" onClick={() => setShowSupplierModal(true)}>+ Fornecedor</button>
        )}
        {(activeSection === '' || activeSection === 'solicitacoes' || activeSection === 'cotacoes') && (
          <button className="btn" type="button" onClick={openOrderModal}>+ Pedido</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/compras" tabs={purchasingTabs} contextId="supply" />

      {feedback && <div className="alert alert-success">{feedback}</div>}
      {error && !showOrderModal && <div className="alert alert-error">{error}</div>}

      <div className="kpi-grid">
        <KpiCard label="Fornecedores" value={stats.suppliers} variant="primary" />
        <KpiCard label="Pedidos totais" value={stats.orders} variant="info" />
        <KpiCard label="Rascunhos" value={stats.draft} variant="neutral" />
        <KpiCard label="Aguardando recebimento" value={stats.pending} variant="warning" />
      </div>

      {activeSection === 'contratos' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Contratos com fornecedores</h3>
          <p>Vincule pedidos recorrentes e SLAs de entrega aos fornecedores cadastrados.</p>
        </div>
      )}

      {(activeSection === '' || activeSection === 'fornecedores') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Fornecedores — {suppliers.length} cadastrado(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Nome</th><th>CNPJ</th><th>Contato</th></tr></thead>
            <tbody>
              {suppliers.map((s) => (
                <tr key={s.id}>
                  <td>{s.name}</td>
                  <td>{s.cnpj ?? '—'}</td>
                  <td>{s.contactName ?? s.email ?? s.phone ?? '—'}</td>
                </tr>
              ))}
              {suppliers.length === 0 && (
                <tr><td colSpan={3} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum fornecedor</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {(activeSection === '' || activeSection === 'solicitacoes' || activeSection === 'cotacoes' || activeSection === 'recebimento') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">
          {activeSection === 'recebimento' && 'Recebimento de mercadorias'}
          {activeSection === 'solicitacoes' && 'Solicitações de compra'}
          {activeSection === 'cotacoes' && 'Cotações em andamento'}
          {!activeSection && `Pedidos de compra — ${displayOrders.length} pedido(s)`}
        </div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Pedido</th>
                <th>Setor</th>
                <th>Prioridade</th>
                <th>Fornecedor</th>
                <th>Status</th>
                <th>Total</th>
                <th>Itens</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {displayOrders.map((o) => (
                <tr key={o.id}>
                  <td>
                    <strong>{o.orderNumber}</strong>
                    <div className="table-sub">{o.requestedBy}</div>
                  </td>
                  <td><span className="badge badge-muted">{purchaseSectorLabel(o.sector)}</span></td>
                  <td>
                    <span className={`badge ${purchasePriorityValue(o.priority) >= 2 ? 'badge-danger' : ''}`}>
                      {purchasePriorityLabel(o.priority)}
                    </span>
                  </td>
                  <td>{o.supplierName}</td>
                  <td><span className="badge">{purchaseOrderStatusLabels[o.status]}</span></td>
                  <td>R$ {o.totalAmount.toFixed(2)}</td>
                  <td>
                    {o.items.map((i) => (
                      <div key={i.id} className="table-sub">
                        {i.productName}: {i.receivedQuantity}/{i.quantity}
                      </div>
                    ))}
                    {o.justification && (
                      <div className="table-sub" style={{ marginTop: 4, fontStyle: 'italic' }}>{o.justification}</div>
                    )}
                  </td>
                  <td>
                    <div className="table-actions">
                      {o.status === 'Draft' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => sendOrder(o.id)}>Enviar</button>
                      )}
                      {(o.status === 'Sent' || o.status === 'PartiallyReceived') && (
                        <button className="btn btn-sm" type="button" onClick={() => receiveAll(o)}>Receber</button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {displayOrders.length === 0 && (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum pedido</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      <Modal open={showSupplierModal} onClose={() => setShowSupplierModal(false)} title="Cadastrar fornecedor" width="md">
        <form onSubmit={handleSupplier} className="form-grid">
          <div className="form-field">
            <label htmlFor="supName">Nome *</label>
            <input id="supName" value={supplierForm.name} onChange={(e) => setSupplierForm({ ...supplierForm, name: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="supCnpj">CNPJ</label>
            <input id="supCnpj" value={supplierForm.cnpj} onChange={(e) => setSupplierForm({ ...supplierForm, cnpj: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="supEmail">E-mail</label>
            <input id="supEmail" type="email" value={supplierForm.email} onChange={(e) => setSupplierForm({ ...supplierForm, email: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="supPhone">Telefone</label>
            <input id="supPhone" value={supplierForm.phone} onChange={(e) => setSupplierForm({ ...supplierForm, phone: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="supContact">Contato</label>
            <input id="supContact" value={supplierForm.contactName} onChange={(e) => setSupplierForm({ ...supplierForm, contactName: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowSupplierModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Cadastrar</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showOrderModal}
        onClose={() => setShowOrderModal(false)}
        title="Novo pedido de compra"
        subtitle="Selecione o setor solicitante — o sistema sugere itens, fornecedor e prazo."
        width="lg"
      >
        <form onSubmit={handleOrder} className="form-grid">
          {error && <div className="form-field full alert alert-error">{error}</div>}

          <div className="form-field full">
            <label htmlFor="poSector">Setor solicitante *</label>
            <select
              id="poSector"
              required
              value={orderForm.sector}
              onChange={(e) => applySector(Number(e.target.value))}
            >
              {(suggestions?.sectors ?? []).map((preset) => (
                <option key={purchaseSectorValue(preset.sector)} value={purchaseSectorValue(preset.sector)}>
                  {preset.label} — {preset.description}
                </option>
              ))}
            </select>
            {selectedSectorPreset && (
              <span className="field-hint">
                Prazo sugerido: {selectedSectorPreset.suggestedDeliveryDays} dias para este setor
              </span>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="poPriority">Prioridade *</label>
            <select
              id="poPriority"
              required
              value={orderForm.priority}
              onChange={(e) => applyPriority(Number(e.target.value))}
            >
              <option value={1}>Normal</option>
              <option value={2}>Urgente</option>
              <option value={3}>Crítica</option>
            </select>
          </div>

          <div className="form-field">
            <label htmlFor="poRequestedBy">Solicitante *</label>
            <input
              id="poRequestedBy"
              required
              value={orderForm.requestedBy}
              onChange={(e) => setOrderForm({ ...orderForm, requestedBy: e.target.value })}
              placeholder="Nome do responsável pelo setor"
            />
          </div>

          <div className="form-field">
            <label htmlFor="poSupplier">Fornecedor *</label>
            <select
              id="poSupplier"
              required
              value={orderForm.supplierId}
              onChange={(e) => setOrderForm({ ...orderForm, supplierId: e.target.value })}
            >
              <option value="">Selecione...</option>
              {(suggestions?.suppliers ?? suppliers).map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            {suggestions?.suggestedSupplierId && orderForm.supplierId === suggestions.suggestedSupplierId && (
              <span className="field-hint">Fornecedor sugerido automaticamente para este setor</span>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="poExpected">Previsão de entrega</label>
            <input
              id="poExpected"
              type="date"
              value={orderForm.expectedAt}
              onChange={(e) => setOrderForm({ ...orderForm, expectedAt: e.target.value })}
            />
          </div>

          <div className="form-field full">
            <label htmlFor="poJustification">Justificativa</label>
            <textarea
              id="poJustification"
              rows={2}
              value={orderForm.justification}
              onChange={(e) => setOrderForm({ ...orderForm, justification: e.target.value })}
              placeholder="Motivo da compra, urgência clínica, número de leitos afetados..."
            />
          </div>

          {suggestionsLoading && (
            <div className="form-field full">
              <span className="field-hint">Carregando sugestões do setor...</span>
            </div>
          )}

          {suggestions && (suggestions.lowStockItems.length > 0 || suggestions.kitItems.length > 0) && (
            <div className="form-field full purchase-suggestions">
              <div className="purchase-suggestions-header">
                <strong>Sugestões automáticas</strong>
                <div className="purchase-suggestion-actions">
                  {suggestions.lowStockItems.length > 0 && (
                    <button
                      className="btn btn-secondary btn-sm"
                      type="button"
                      onClick={() => addSuggestedItems(suggestions.lowStockItems)}
                    >
                      + Estoque baixo ({suggestions.lowStockItems.length})
                    </button>
                  )}
                  {suggestions.kitItems.length > 0 && (
                    <button
                      className="btn btn-secondary btn-sm"
                      type="button"
                      onClick={() => addSuggestedItems(suggestions.kitItems)}
                    >
                      + Kit do setor ({suggestions.kitItems.length})
                    </button>
                  )}
                </div>
              </div>
              <div className="purchase-suggestion-list">
                {[...suggestions.lowStockItems, ...suggestions.kitItems].slice(0, 6).map((item) => (
                  <button
                    key={`${item.sku}-${item.reason}`}
                    type="button"
                    className="purchase-suggestion-chip"
                    onClick={() => addSuggestedItems([item])}
                  >
                    <span>{item.productName}</span>
                    <small>
                      {item.isLowStock ? '⚠ ' : ''}
                      {item.suggestedQuantity} {item.unit} · R$ {item.suggestedUnitPrice.toFixed(2)}
                    </small>
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="form-field full">
            <div className="purchase-items-header">
              <label>Itens do pedido *</label>
              <button
                className="btn btn-secondary btn-sm"
                type="button"
                onClick={() => setOrderLines((prev) => [...prev, newLine()])}
              >
                + Linha
              </button>
            </div>
            <div className="purchase-items-list">
              {orderLines.map((line) => {
                const product = products.find((p) => p.id === line.productId);
                const lineTotal = line.quantity * line.unitPrice;
                return (
                  <div key={line.key} className="purchase-item-row">
                    <div className="form-field">
                      <label>Produto</label>
                      <select
                        value={line.productId}
                        onChange={(e) => updateLine(line.key, { productId: e.target.value })}
                      >
                        <option value="">Selecione...</option>
                        {products.map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.name} ({p.sku}) — estoque {p.quantityOnHand} {p.unit}
                            {p.isLowStock ? ' ⚠' : ''}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="form-field">
                      <label>Qtd</label>
                      <input
                        type="number"
                        min={1}
                        value={line.quantity}
                        onChange={(e) => updateLine(line.key, { quantity: Number(e.target.value) })}
                      />
                    </div>
                    <div className="form-field">
                      <label>Preço un.</label>
                      <input
                        type="number"
                        min={0}
                        step="0.01"
                        value={line.unitPrice}
                        onChange={(e) => updateLine(line.key, { unitPrice: Number(e.target.value) })}
                      />
                    </div>
                    <div className="purchase-item-total">
                      <span>R$ {lineTotal.toFixed(2)}</span>
                      {product && (
                        <small>Mín. {product.minimumStock} {product.unit}</small>
                      )}
                    </div>
                    <button
                      className="btn-icon purchase-item-remove"
                      type="button"
                      aria-label="Remover item"
                      onClick={() => removeLine(line.key)}
                    >
                      ×
                    </button>
                  </div>
                );
              })}
            </div>
            <div className="purchase-order-total">
              <strong>Total do pedido:</strong> R$ {orderTotal.toFixed(2)}
              <span>{validLines.length} item(ns)</span>
            </div>
          </div>

          <div className="form-field full">
            <label htmlFor="poNotes">Observações ao fornecedor</label>
            <textarea
              id="poNotes"
              rows={2}
              value={orderForm.notes}
              onChange={(e) => setOrderForm({ ...orderForm, notes: e.target.value })}
              placeholder="Horário de entrega, local de recebimento, contato..."
            />
          </div>

          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowOrderModal(false)}>Cancelar</button>
            <button className="btn" type="submit" disabled={creating || suggestionsLoading}>
              {creating ? 'Salvando...' : 'Criar pedido'}
            </button>
          </div>
        </form>
      </Modal>
    </>
  );
}
