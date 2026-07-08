import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type PatientDto,
  type ProductDto,
  type WardDto,
  type WardStockBalanceDto,
  type WardStockMovementDto,
} from '../../../api/client';
import { formatBrDateTime } from '../../../utils/dateUtils';

const MOVEMENT_LABELS: Record<number, string> = {
  1: 'Entrada (central)',
  2: 'Saída',
  3: 'Dispensação',
  4: 'Ajuste',
};

type Props = {
  embedded?: boolean;
};

export function FeegowWardPharmacy({ embedded }: Props) {
  const [wards, setWards] = useState<WardDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [balances, setBalances] = useState<WardStockBalanceDto[]>([]);
  const [movements, setMovements] = useState<WardStockMovementDto[]>([]);
  const [wardFilter, setWardFilter] = useState('');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [transferForm, setTransferForm] = useState({
    wardId: '',
    productId: '',
    quantity: '',
    reference: '',
    notes: '',
  });
  const [dispenseForm, setDispenseForm] = useState({
    wardId: '',
    productId: '',
    patientId: '',
    quantity: '',
    notes: '',
  });

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [wardList, productList, balanceList, movementList] = await Promise.all([
        api.getWards(),
        api.getProducts(),
        api.getWardPharmacyBalances(wardFilter || undefined, lowStockOnly),
        api.getWardPharmacyMovements({ wardId: wardFilter || undefined }),
      ]);
      setWards(wardList);
      setProducts(productList.filter((p) => p.type === 1 || p.type === 2));
      setBalances(balanceList);
      setMovements(movementList);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar farmácia por ala.');
    } finally {
      setLoading(false);
    }
  }, [lowStockOnly, wardFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(() => setPatients([]));
  }, []);

  async function handleTransfer(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await api.transferWardPharmacyStock({
        wardId: transferForm.wardId,
        productId: transferForm.productId,
        quantity: Number(transferForm.quantity),
        reference: transferForm.reference.trim() || undefined,
        notes: transferForm.notes.trim() || undefined,
      });
      setSuccess('Transferência registrada.');
      setTransferForm({ wardId: '', productId: '', quantity: '', reference: '', notes: '' });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na transferência.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDispense(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await api.dispenseWardPharmacyStock({
        wardId: dispenseForm.wardId,
        productId: dispenseForm.productId,
        patientId: dispenseForm.patientId,
        quantity: Number(dispenseForm.quantity),
        notes: dispenseForm.notes.trim() || undefined,
      });
      setSuccess('Dispensação registrada.');
      setDispenseForm({ wardId: '', productId: '', patientId: '', quantity: '', notes: '' });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na dispensação.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className={embedded ? 'feegow-inventory-ward-pharmacy' : 'feegow-inventory-page'}>
      {!embedded ? (
        <header className="feegow-inventory-page-head">
          <div className="feegow-inventory-breadcrumb">
            <span>Estoque</span>
            <span className="feegow-inventory-crumb-sep">/</span>
            <span>💊 Farmácia por Ala</span>
          </div>
        </header>
      ) : null}

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}

      <form className="feegow-finance-filters" onSubmit={(e) => { e.preventDefault(); void load(); }}>
        <div className="feegow-finance-filter-row">
          <label>
            Ala
            <select value={wardFilter} onChange={(e) => setWardFilter(e.target.value)}>
              <option value="">Todas</option>
              {wards.map((w) => (
                <option key={w.id} value={w.id}>{w.name}</option>
              ))}
            </select>
          </label>
          <label className="feegow-finance-field-inline">
            <span>Apenas estoque baixo</span>
            <input
              type="checkbox"
              checked={lowStockOnly}
              onChange={(e) => setLowStockOnly(e.target.checked)}
            />
          </label>
          <button type="submit" className="feegow-finance-filter-btn">Filtrar</button>
        </div>
      </form>

      <div className="feegow-inventory-ward-pharmacy-grid">
        <section className="feegow-finance-panel">
          <header className="feegow-finance-panel-head">
            <h3>Transferir do estoque central</h3>
          </header>
          <form className="feegow-finance-form-grid" onSubmit={(e) => { void handleTransfer(e); }}>
            <label>
              Ala
              <select
                required
                value={transferForm.wardId}
                onChange={(e) => setTransferForm({ ...transferForm, wardId: e.target.value })}
              >
                <option value="">Selecione</option>
                {wards.map((w) => (
                  <option key={w.id} value={w.id}>{w.name}</option>
                ))}
              </select>
            </label>
            <label>
              Produto
              <select
                required
                value={transferForm.productId}
                onChange={(e) => setTransferForm({ ...transferForm, productId: e.target.value })}
              >
                <option value="">Selecione</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>{p.name} ({p.sku})</option>
                ))}
              </select>
            </label>
            <label>
              Quantidade
              <input
                type="number"
                min="0.001"
                step="0.001"
                required
                value={transferForm.quantity}
                onChange={(e) => setTransferForm({ ...transferForm, quantity: e.target.value })}
              />
            </label>
            <label>
              Referência
              <input
                type="text"
                value={transferForm.reference}
                onChange={(e) => setTransferForm({ ...transferForm, reference: e.target.value })}
              />
            </label>
            <div className="feegow-finance-field-wide">
              <button type="submit" className="feegow-finance-filter-btn" disabled={saving}>
                Transferir
              </button>
            </div>
          </form>
        </section>

        <section className="feegow-finance-panel">
          <header className="feegow-finance-panel-head">
            <h3>Dispensar ao paciente</h3>
          </header>
          <form className="feegow-finance-form-grid" onSubmit={(e) => { void handleDispense(e); }}>
            <label>
              Ala
              <select
                required
                value={dispenseForm.wardId}
                onChange={(e) => setDispenseForm({ ...dispenseForm, wardId: e.target.value })}
              >
                <option value="">Selecione</option>
                {wards.map((w) => (
                  <option key={w.id} value={w.id}>{w.name}</option>
                ))}
              </select>
            </label>
            <label>
              Produto
              <select
                required
                value={dispenseForm.productId}
                onChange={(e) => setDispenseForm({ ...dispenseForm, productId: e.target.value })}
              >
                <option value="">Selecione</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </label>
            <label>
              Paciente
              <select
                required
                value={dispenseForm.patientId}
                onChange={(e) => setDispenseForm({ ...dispenseForm, patientId: e.target.value })}
              >
                <option value="">Selecione</option>
                {patients.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            </label>
            <label>
              Quantidade
              <input
                type="number"
                min="0.001"
                step="0.001"
                required
                value={dispenseForm.quantity}
                onChange={(e) => setDispenseForm({ ...dispenseForm, quantity: e.target.value })}
              />
            </label>
            <div className="feegow-finance-field-wide">
              <button type="submit" className="feegow-finance-filter-btn" disabled={saving}>
                Dispensar
              </button>
            </div>
          </form>
        </section>
      </div>

      <section className="feegow-finance-panel feegow-finance-table-card">
        <header className="feegow-finance-panel-head">
          <h3>Saldo por ala</h3>
        </header>
        <div className="feegow-finance-table-wrap">
          <table className="feegow-finance-table">
            <thead>
              <tr>
                <th>Ala</th>
                <th>Produto</th>
                <th>SKU</th>
                <th>Saldo</th>
                <th>Mínimo</th>
                <th>Unidade</th>
              </tr>
            </thead>
            <tbody>
              {balances.map((row) => (
                <tr key={row.id} className={row.isLowStock ? 'feegow-inventory-row-low' : undefined}>
                  <td>{row.wardName}</td>
                  <td>{row.productName}</td>
                  <td>{row.productSku}</td>
                  <td>{row.quantityOnHand}</td>
                  <td>{row.minimumStock}</td>
                  <td>{row.unit}</td>
                </tr>
              ))}
              {!loading && balances.length === 0 ? (
                <tr>
                  <td colSpan={6} className="feegow-finance-table-empty">
                    Nenhum saldo na ala. Transfira produtos do estoque central.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>

      <section className="feegow-finance-panel feegow-finance-table-card">
        <header className="feegow-finance-panel-head">
          <h3>Movimentações recentes</h3>
        </header>
        <div className="feegow-finance-table-wrap">
          <table className="feegow-finance-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Ala</th>
                <th>Produto</th>
                <th>Tipo</th>
                <th>Qtd</th>
                <th>Paciente</th>
              </tr>
            </thead>
            <tbody>
              {movements.map((m) => (
                <tr key={m.id}>
                  <td>{formatBrDateTime(m.movementDate)}</td>
                  <td>{m.wardName}</td>
                  <td>{m.productName}</td>
                  <td>{MOVEMENT_LABELS[typeof m.movementType === 'number' ? m.movementType : 1] ?? '—'}</td>
                  <td>{m.quantity} {m.unit}</td>
                  <td>{m.patientName ?? '—'}</td>
                </tr>
              ))}
              {!loading && movements.length === 0 ? (
                <tr>
                  <td colSpan={6} className="feegow-finance-table-empty">
                    Sem movimentações.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {loading ? <p className="feegow-finance-loading">Carregando...</p> : null}
      </section>
    </div>
  );
}
