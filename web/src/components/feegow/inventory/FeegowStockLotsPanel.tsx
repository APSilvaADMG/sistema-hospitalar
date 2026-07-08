import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type ProductLotDto } from '../../../api/client';
import { formatBrDate } from '../../../utils/dateUtils';
import { TablePagination } from '../TablePagination';
import { printExpiringLotsReport } from '../../../utils/printTemplates';

const PAGE_SIZE = 25;

type LotFilter = 'all' | 'expiring' | 'expired';

function lotTone(lot: ProductLotDto): string {
  if (!lot.expiryDate) return '';
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const expiry = new Date(`${lot.expiryDate}T00:00:00`);
  if (expiry < today) return 'feegow-inventory-lot-expired';
  if (lot.isExpiringSoon) return 'feegow-inventory-lot-expiring';
  return '';
}

export function FeegowStockLotsPanel() {
  const [lots, setLots] = useState<ProductLotDto[]>([]);
  const [filter, setFilter] = useState<LotFilter>('all');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const expiringWithinDays = filter === 'expiring' ? 30 : undefined;
      const data = await api.getWarehouseLots(undefined, expiringWithinDays);
      setLots(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar lotes.');
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    void load();
  }, [load]);

  const filtered = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const term = search.trim().toLowerCase();

    return lots.filter((lot) => {
      if (filter === 'expired') {
        if (!lot.expiryDate) return false;
        const expiry = new Date(`${lot.expiryDate}T00:00:00`);
        if (expiry >= today) return false;
      }

      if (!term) return true;
      return (
        lot.productName.toLowerCase().includes(term)
        || lot.productSku.toLowerCase().includes(term)
        || lot.batchNumber.toLowerCase().includes(term)
        || (lot.locationName?.toLowerCase().includes(term) ?? false)
      );
    });
  }, [lots, filter, search]);

  const paged = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, page]);

  useEffect(() => {
    setPage(1);
  }, [filter, search]);

  return (
    <div className="feegow-warehouse-page">
      <header className="feegow-warehouse-head">
        <div>
          <h1 className="feegow-warehouse-title">Lotes</h1>
          <p className="feegow-warehouse-subtitle">Controle por lote, validade e localização (FEFO)</p>
        </div>
        <div className="feegow-warehouse-head-actions">
          <button
            type="button"
            className="feegow-warehouse-btn feegow-warehouse-btn-ghost"
            onClick={() => printExpiringLotsReport(filtered, filter === 'expiring' ? 30 : 90)}
          >
            Imprimir / PDF
          </button>
          <button type="button" className="feegow-warehouse-btn feegow-warehouse-btn-ghost" onClick={() => void load()}>
            Atualizar
          </button>
        </div>
      </header>

      {error ? <div className="alert alert-error">{error}</div> : null}

      <div className="feegow-inventory-filters card-panel" style={{ marginBottom: 16, padding: 12 }}>
        <div className="form-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Produto, SKU, lote ou local"
            />
          </div>
          <div className="form-field">
            <label>Filtro de validade</label>
            <select value={filter} onChange={(e) => setFilter(e.target.value as LotFilter)}>
              <option value="all">Todos com saldo</option>
              <option value="expiring">Vencendo em 30 dias</option>
              <option value="expired">Vencidos</option>
            </select>
          </div>
        </div>
      </div>

      <div className="feegow-inventory-table-wrap card-panel" style={{ padding: 0 }}>
        <table className="feegow-inventory-table data-table">
          <thead>
            <tr>
              <th>Produto</th>
              <th>SKU</th>
              <th>Lote</th>
              <th>Validade</th>
              <th>Saldo</th>
              <th>Local</th>
              <th>Fabricante</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {paged.map((lot) => (
              <tr key={lot.id} className={lotTone(lot)}>
                <td>{lot.productName}</td>
                <td>{lot.productSku}</td>
                <td>{lot.batchNumber}</td>
                <td>{formatBrDate(lot.expiryDate)}</td>
                <td>{lot.quantityOnHand}</td>
                <td>{lot.locationName || '—'}</td>
                <td>{lot.manufacturer || '—'}</td>
                <td>
                  <Link to={`/estoque/inserir?tipo=geral&id=${lot.productId}`} className="feegow-warehouse-btn feegow-warehouse-btn-ghost">
                    Ver produto
                  </Link>
                </td>
              </tr>
            ))}
            {!loading && paged.length === 0 ? (
              <tr>
                <td colSpan={8} className="feegow-inventory-table-empty">
                  Nenhum lote encontrado para o filtro selecionado.
                </td>
              </tr>
            ) : null}
          </tbody>
        </table>
      </div>

      <TablePagination
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={filtered.length}
        onPageChange={setPage}
        loading={loading}
      />
    </div>
  );
}
