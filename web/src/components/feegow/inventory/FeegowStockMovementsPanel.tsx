import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, type ProductDto, type StockMovementDto } from '../../../api/client';
import { FeegowProductMovements } from './FeegowProductMovements';
import { printStockMovementsReport } from '../../../utils/printTemplates';

const MOVEMENT_TYPES = [
  { value: '', label: 'Todos os tipos' },
  { value: '1', label: 'Entrada' },
  { value: '2', label: 'Saída' },
  { value: '3', label: 'Ajuste / transferência' },
] as const;

function defaultFromDate(): string {
  const d = new Date();
  d.setDate(d.getDate() - 30);
  return d.toISOString().slice(0, 10);
}

function todayDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function FeegowStockMovementsPanel() {
  const [movements, setMovements] = useState<StockMovementDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [productId, setProductId] = useState('');
  const [search, setSearch] = useState('');
  const [type, setType] = useState('');
  const [from, setFrom] = useState(defaultFromDate);
  const [to, setTo] = useState(todayDate);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api.getProducts().then(setProducts).catch(() => setProducts([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const list = await api.getStockMovements({
        productId: productId || undefined,
        search: search.trim() || undefined,
        type: type ? Number(type) : undefined,
        from,
        to,
        limit: 500,
      });
      setMovements(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar movimentações.');
    } finally {
      setLoading(false);
    }
  }, [productId, search, type, from, to]);

  useEffect(() => {
    void load();
  }, [load]);

  const summary = useMemo(() => {
    const inbound = movements.filter((m) => m.type === 1).reduce((s, m) => s + m.quantity, 0);
    const outbound = movements.filter((m) => m.type === 2).reduce((s, m) => s + m.quantity, 0);
    return { inbound, outbound, total: movements.length };
  }, [movements]);

  return (
    <div className="feegow-warehouse-page">
      <header className="feegow-warehouse-head">
        <div>
          <h1 className="feegow-warehouse-title">Movimentações</h1>
          <p className="feegow-warehouse-subtitle">Histórico de entradas, saídas e ajustes do almoxarifado</p>
        </div>
        <div className="feegow-warehouse-head-actions">
          <button
            type="button"
            className="feegow-warehouse-btn feegow-warehouse-btn-ghost"
            onClick={() => printStockMovementsReport(movements, from, to, summary)}
          >
            Imprimir / PDF
          </button>
          <button type="button" className="feegow-warehouse-btn feegow-warehouse-btn-ghost" onClick={() => void load()}>
            Atualizar
          </button>
        </div>
      </header>

      {error ? <div className="alert alert-error">{error}</div> : null}

      <div className="feegow-warehouse-kpi-grid" style={{ marginBottom: 16 }}>
        <div className="feegow-warehouse-kpi feegow-warehouse-kpi-teal">
          <div className="feegow-warehouse-kpi-value">{summary.total}</div>
          <div className="feegow-warehouse-kpi-label">Registros no período</div>
        </div>
        <div className="feegow-warehouse-kpi feegow-warehouse-kpi-green">
          <div className="feegow-warehouse-kpi-value">+{summary.inbound}</div>
          <div className="feegow-warehouse-kpi-label">Entradas (un.)</div>
        </div>
        <div className="feegow-warehouse-kpi feegow-warehouse-kpi-red">
          <div className="feegow-warehouse-kpi-value">-{summary.outbound}</div>
          <div className="feegow-warehouse-kpi-label">Saídas (un.)</div>
        </div>
      </div>

      <div className="feegow-inventory-filters card-panel" style={{ marginBottom: 16, padding: 12 }}>
        <div className="form-grid">
          <div className="form-field">
            <label>Produto</label>
            <select value={productId} onChange={(e) => setProductId(e.target.value)}>
              <option value="">Todos</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>Tipo</label>
            <select value={type} onChange={(e) => setType(e.target.value)}>
              {MOVEMENT_TYPES.map((opt) => (
                <option key={opt.value || 'all'} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>De</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="form-field">
            <label>Até</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
          <div className="form-field full">
            <label>Busca livre</label>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Produto, SKU, lote, NF, motivo…"
            />
          </div>
        </div>
      </div>

      <FeegowProductMovements movements={movements} loading={loading} showProductColumn />
    </div>
  );
}
