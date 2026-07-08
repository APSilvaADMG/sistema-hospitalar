import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  type ProductDto,
  type ProductLotDto,
  type SectorConsumptionDto,
  type StockReplenishmentSuggestionDto,
  type WarehouseDashboardDto,
} from '../../../api/client';
import { formatBrDate } from '../../../utils/dateUtils';
import {
  printExpiringLotsReport,
  printStockPositionReport,
} from '../../../utils/printTemplates';

function KpiCard({
  value,
  label,
  tone = 'neutral',
  footer,
}: {
  value: string | number;
  label: string;
  tone?: 'green' | 'teal' | 'yellow' | 'red' | 'neutral';
  footer?: React.ReactNode;
}) {
  return (
    <div className={`feegow-warehouse-kpi feegow-warehouse-kpi-${tone}`}>
      <div className="feegow-warehouse-kpi-value">{value}</div>
      <div className="feegow-warehouse-kpi-label">{label}</div>
      {footer ? <div className="feegow-warehouse-kpi-footer">{footer}</div> : null}
    </div>
  );
}

export function FeegowWarehouseDashboard() {
  const [dashboard, setDashboard] = useState<WarehouseDashboardDto | null>(null);
  const [expiringLots, setExpiringLots] = useState<ProductLotDto[]>([]);
  const [lowStock, setLowStock] = useState<ProductDto[]>([]);
  const [consumption, setConsumption] = useState<SectorConsumptionDto[]>([]);
  const [replenishment, setReplenishment] = useState<StockReplenishmentSuggestionDto[]>([]);
  const [allProducts, setAllProducts] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const today = new Date();
      const from = new Date(today.getFullYear(), today.getMonth(), 1);
      const to = today;
      const fromStr = from.toISOString().slice(0, 10);
      const toStr = to.toISOString().slice(0, 10);

      const [dash, expiring, low, cons, restock, products] = await Promise.all([
        api.getWarehouseDashboard(),
        api.getWarehouseExpiringLots(30),
        api.getWarehouseLowStock(),
        api.getWarehouseConsumptionBySector(fromStr, toStr),
        api.getStockReplenishmentSuggestions(),
        api.getProducts('', false),
      ]);
      setDashboard(dash);
      setExpiringLots(expiring.slice(0, 10));
      setLowStock(low.slice(0, 10));
      setConsumption(cons.slice(0, 8));
      setReplenishment(restock.slice(0, 8));
      setAllProducts(products);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar dashboard do almoxarifado.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  if (loading && !dashboard) {
    return <div className="feegow-warehouse-panel">Carregando dashboard…</div>;
  }

  return (
    <div className="feegow-warehouse-page">
      <header className="feegow-warehouse-head">
        <div>
          <h1 className="feegow-warehouse-title">Almoxarifado</h1>
          <p className="feegow-warehouse-subtitle">Visão operacional de entradas, saídas e alertas</p>
        </div>
        <div className="feegow-warehouse-head-actions">
          <Link to="/estoque/entrada" className="feegow-warehouse-btn feegow-warehouse-btn-primary">+ Entrada NF</Link>
          <Link to="/estoque/saida" className="feegow-warehouse-btn">Saída</Link>
          <Link to="/estoque/movimentacoes" className="feegow-warehouse-btn feegow-warehouse-btn-ghost">Movimentações</Link>
          <button
            type="button"
            className="feegow-warehouse-btn feegow-warehouse-btn-ghost"
            onClick={() => printStockPositionReport(allProducts)}
          >
            Posição (PDF)
          </button>
          <button
            type="button"
            className="feegow-warehouse-btn feegow-warehouse-btn-ghost"
            onClick={() => void api.getWarehouseExpiringLots(30).then((lots) => printExpiringLotsReport(lots, 30))}
          >
            Lotes a vencer (PDF)
          </button>
          <button type="button" className="feegow-warehouse-btn feegow-warehouse-btn-ghost" onClick={() => void load()}>
            Atualizar
          </button>
        </div>
      </header>

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}

      {dashboard ? (
        <section className="feegow-warehouse-kpi-grid">
          <KpiCard value={dashboard.totalProducts} label="Produtos ativos" tone="teal" />
          <KpiCard
            value={dashboard.lowStockCount}
            label="Abaixo do mínimo"
            tone={dashboard.lowStockCount > 0 ? 'red' : 'green'}
            footer={<Link to="/estoque/listar?tipo=geral">Ver produtos</Link>}
          />
          <KpiCard
            value={dashboard.expiringLotsCount}
            label="Lotes a vencer (30d)"
            tone={dashboard.expiringLotsCount > 0 ? 'yellow' : 'green'}
            footer={<Link to="/estoque/lotes">Ver lotes</Link>}
          />
          <KpiCard value={dashboard.pendingRequisitions} label="Requisições pendentes" tone="neutral"
            footer={<Link to="/estoque/requisicoes">Abrir requisições</Link>}
          />
          <KpiCard value={dashboard.todayInboundQuantity} label="Entradas hoje (qtd)" tone="green" />
          <KpiCard value={dashboard.todayOutboundQuantity} label="Saídas hoje (qtd)" tone="neutral" />
        </section>
      ) : null}

      <div className="feegow-warehouse-columns">
        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head">
            <h2>Lotes próximos do vencimento</h2>
          </header>
          <div className="feegow-warehouse-table-wrap">
            <table className="feegow-warehouse-table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th>Lote</th>
                  <th>Validade</th>
                  <th>Saldo</th>
                </tr>
              </thead>
              <tbody>
                {expiringLots.map((lot) => (
                  <tr key={lot.id}>
                    <td>{lot.productName}</td>
                    <td>{lot.batchNumber}</td>
                    <td className={lot.isExpiringSoon ? 'feegow-warehouse-warn' : ''}>
                      {lot.expiryDate ? formatBrDate(lot.expiryDate) : '—'}
                    </td>
                    <td>{lot.quantityOnHand} {lot.productSku ? '' : ''}</td>
                  </tr>
                ))}
                {expiringLots.length === 0 ? (
                  <tr><td colSpan={4} className="feegow-warehouse-empty">Nenhum lote crítico.</td></tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>

        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head">
            <h2>Estoque baixo</h2>
          </header>
          <div className="feegow-warehouse-table-wrap">
            <table className="feegow-warehouse-table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th>SKU</th>
                  <th>Saldo</th>
                  <th>Mínimo</th>
                </tr>
              </thead>
              <tbody>
                {lowStock.map((product) => (
                  <tr key={product.id}>
                    <td>{product.name}</td>
                    <td>{product.sku}</td>
                    <td className="feegow-warehouse-warn">{product.quantityOnHand}</td>
                    <td>{product.minimumStock}</td>
                  </tr>
                ))}
                {lowStock.length === 0 ? (
                  <tr><td colSpan={4} className="feegow-warehouse-empty">Nenhum produto abaixo do mínimo.</td></tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      </div>

      <section className="feegow-warehouse-card">
        <header className="feegow-warehouse-card-head">
          <h2>Sugestões de reposição (IA)</h2>
        </header>
        <div className="feegow-warehouse-table-wrap">
          <table className="feegow-warehouse-table">
            <thead>
              <tr>
                <th>Produto</th>
                <th>Saldo</th>
                <th>Mínimo</th>
                <th>Consumo/dia</th>
                <th>Ruptura (dias)</th>
                <th>Recomendação</th>
              </tr>
            </thead>
            <tbody>
              {replenishment.map((row) => (
                <tr key={row.productId}>
                  <td>{row.productName}</td>
                  <td>{row.quantityOnHand}</td>
                  <td>{row.minimumStock}</td>
                  <td>{row.avgDailyConsumption ?? '—'}</td>
                  <td>{row.daysUntilStockout ?? '—'}</td>
                  <td>{row.recommendation}</td>
                </tr>
              ))}
              {replenishment.length === 0 ? (
                <tr><td colSpan={6} className="feegow-warehouse-empty">Estoque dentro dos parâmetros.</td></tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>

      <section className="feegow-warehouse-card">
        <header className="feegow-warehouse-card-head">
          <h2>Consumo por setor (mês atual)</h2>
        </header>
        <div className="feegow-warehouse-table-wrap">
          <table className="feegow-warehouse-table">
            <thead>
              <tr>
                <th>Setor</th>
                <th>Quantidade</th>
                <th>Movimentos</th>
              </tr>
            </thead>
            <tbody>
              {consumption.map((row) => (
                <tr key={row.sectorName}>
                  <td>{row.sectorName}</td>
                  <td>{row.totalQuantity}</td>
                  <td>{row.movementCount}</td>
                </tr>
              ))}
              {consumption.length === 0 ? (
                <tr><td colSpan={3} className="feegow-warehouse-empty">Sem consumo registrado no período.</td></tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
