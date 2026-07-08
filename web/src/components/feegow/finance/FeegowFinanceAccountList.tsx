import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  api,
  financialCategoryLabel,
  financialCategoryLabels,
  financialDirectionValue,
  type FinancialAccountDto,
} from '../../../api/client';
import { formatBrDate } from '../../../utils/dateUtils';
import { TablePagination } from '../TablePagination';
import { FinancialStatusBadge } from './FinancialStatusBadge';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';
import { feegowFinanceInsertPath, feegowFinanceListPath } from './feegowFinanceNav';

type Props = {
  direction: 1 | 2;
};

const PAGE_SIZE = 50;

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

const EXIBIR_OPTIONS = [
  { value: 'todas', label: 'Todas' },
  { value: 'abertas', label: 'Em aberto' },
  { value: 'pagas', label: 'Quitadas' },
];

export function FeegowFinanceAccountList({ direction }: Props) {
  const kind = direction === 2 ? 'pagar' : 'receber';
  const title = direction === 2 ? 'Contas a Pagar' : 'Contas a Receber';
  const counterpartyLabel = direction === 2 ? 'Pagar a' : 'Receber de';

  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [exibir, setExibir] = useState('todas');
  const [counterpartySearch, setCounterpartySearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [invoiceFilter, setInvoiceFilter] = useState('');
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [searchParams, setSearchParams] = useSearchParams();
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const load = useCallback(async (targetPage: number) => {
    setLoading(true);
    setError('');
    try {
      const result = await api.getFinancialAccounts(
        undefined,
        counterpartySearch || undefined,
        targetPage,
        direction,
        PAGE_SIZE,
      );
      setAccounts(result.items);
      setTotalCount(result.totalCount);
      setPage(result.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar contas.');
    } finally {
      setLoading(false);
    }
  }, [counterpartySearch, direction]);

  useEffect(() => {
    void load(1);
  }, [load]);

  useEffect(() => {
    const contaId = searchParams.get('conta');
    if (!contaId || accounts.length === 0) return;
    setSelectedIds(new Set([contaId]));
    setCounterpartySearch(accounts.find((a) => a.id === contaId)?.counterpartyDisplay ?? '');
    const next = new URLSearchParams(searchParams);
    next.delete('conta');
    setSearchParams(next, { replace: true });
  }, [accounts, searchParams, setSearchParams]);

  const filtered = useMemo(() => {
    let rows = accounts.filter((a) => financialDirectionValue(a.direction) === direction);

    if (dateFrom) {
      rows = rows.filter((a) => {
        const d = a.dueDate ?? a.createdAt;
        return d && d.slice(0, 10) >= dateFrom;
      });
    }
    if (dateTo) {
      rows = rows.filter((a) => {
        const d = a.dueDate ?? a.createdAt;
        return d && d.slice(0, 10) <= dateTo;
      });
    }
    if (exibir === 'abertas') {
      rows = rows.filter((a) => a.balance > 0);
    } else if (exibir === 'pagas') {
      rows = rows.filter((a) => a.balance <= 0);
    }
    if (categoryFilter) {
      rows = rows.filter((a) => String(financialCategoryLabel(a.category)) === categoryFilter);
    }
    if (invoiceFilter.trim()) {
      const term = invoiceFilter.trim().toLowerCase();
      rows = rows.filter((a) => (a.notes ?? '').toLowerCase().includes(term));
    }

    return rows;
  }, [accounts, categoryFilter, dateFrom, dateTo, direction, exibir, invoiceFilter]);

  const categoryOptions = useMemo(() => {
    const set = new Set<string>();
    accounts.forEach((a) => set.add(financialCategoryLabel(a.category)));
    return Array.from(set).sort((a, b) => a.localeCompare(b, 'pt-BR'));
  }, [accounts]);

  function handleFilter(event: FormEvent) {
    event.preventDefault();
    setPage(1);
    void load(1);
  }

  function toggleAll() {
    if (filtered.every((row) => selectedIds.has(row.id))) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(filtered.map((row) => row.id)));
    }
  }

  function toggleOne(id: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  const allSelected = filtered.length > 0 && filtered.every((row) => selectedIds.has(row.id));

  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead
        title={title}
        listPath={feegowFinanceListPath(kind)}
        insertPath={feegowFinanceInsertPath(kind)}
      />

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}

      <form className="feegow-finance-filters" onSubmit={handleFilter}>
        <div className="feegow-finance-filter-row">
          <label>
            De
            <input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
          </label>
          <label>
            Até
            <input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
          </label>
          <label>
            Exibir
            <select value={exibir} onChange={(e) => setExibir(e.target.value)}>
              {EXIBIR_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </label>
          <button type="submit" className="feegow-finance-filter-btn">Filtrar</button>
        </div>
        <div className="feegow-finance-filter-row">
          <label>
            {counterpartyLabel}
            <input
              type="search"
              value={counterpartySearch}
              onChange={(e) => setCounterpartySearch(e.target.value)}
              placeholder="Buscar..."
            />
          </label>
          <label>
            Categoria
            <select value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
              <option value="">Todas</option>
              {categoryOptions.map((cat) => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
          </label>
          <label>
            Nota Fiscal
            <input
              type="search"
              value={invoiceFilter}
              onChange={(e) => setInvoiceFilter(e.target.value)}
              placeholder="Nº ou referência"
            />
          </label>
          <label>
            Limitar Tipo
            <select disabled>
              <option>Todos</option>
            </select>
          </label>
        </div>
      </form>

      <section className="feegow-finance-panel feegow-finance-table-card">
        <div className="feegow-finance-table-toolbar">
          <select className="feegow-finance-actions-select" defaultValue="">
            <option value="" disabled>Ações</option>
            <option value="export">Exportar selecionados</option>
          </select>
          <div className="feegow-finance-table-tools">
            <button type="button" className="feegow-finance-icon-btn" aria-label="Imprimir">🖨</button>
            <button type="button" className="feegow-finance-icon-btn" aria-label="Excel">📊</button>
          </div>
        </div>
        <div className="feegow-finance-table-wrap">
          <table className="feegow-finance-table">
            <thead>
              <tr>
                <th className="feegow-finance-check-col">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={toggleAll}
                    aria-label="Selecionar todos"
                  />
                </th>
                <th>Data</th>
                <th>Conta</th>
                <th>Plano de Contas</th>
                <th>Descrição</th>
                <th>Situação</th>
                <th>Nota Fiscal</th>
                <th>Valor</th>
                <th>{direction === 2 ? 'Pago' : 'Recebido'}</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((row) => (
                <tr key={row.id}>
                  <td className="feegow-finance-check-col">
                    <input
                      type="checkbox"
                      checked={selectedIds.has(row.id)}
                      onChange={() => toggleOne(row.id)}
                      aria-label={`Selecionar ${row.description}`}
                    />
                  </td>
                  <td>{formatBrDate(row.dueDate ?? row.createdAt)}</td>
                  <td>{row.counterpartyDisplay}</td>
                  <td>{financialCategoryLabel(row.category)}</td>
                  <td>{row.description}</td>
                  <td><FinancialStatusBadge status={row.status} /></td>
                  <td>{row.notes?.match(/NF[:\s]*[\w-]+/i)?.[0] ?? '—'}</td>
                  <td>{formatCurrency(row.amount)}</td>
                  <td>{formatCurrency(row.paidAmount)}</td>
                </tr>
              ))}
              {!loading && filtered.length === 0 ? (
                <tr>
                  <td colSpan={9} className="feegow-finance-table-empty">
                    Nenhuma conta encontrada.{' '}
                    <Link to={feegowFinanceInsertPath(kind)}>Criar lançamento</Link>
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {loading ? <p className="feegow-finance-loading">Carregando...</p> : null}
        <TablePagination
          page={page}
          pageSize={PAGE_SIZE}
          totalCount={totalCount}
          onPageChange={(nextPage) => {
            setSelectedIds(new Set());
            void load(nextPage);
          }}
          loading={loading}
        />
      </section>

      <datalist id="feegow-finance-categories">
        {Object.values(financialCategoryLabels).map((label) => (
          <option key={label} value={label} />
        ))}
      </datalist>
    </div>
  );
}
