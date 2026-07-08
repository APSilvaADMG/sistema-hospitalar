import { useMemo, useState } from 'react';
import type { ReportColumnDto } from '../../api/client';

type Row = Record<string, unknown>;

type Props = {
  columns: ReportColumnDto[];
  rows: Row[];
  pageSize?: number;
};

type SortDir = 'asc' | 'desc';

export function ReportDataTable({ columns, rows, pageSize = 25 }: Props) {
  const [quickSearch, setQuickSearch] = useState('');
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDir, setSortDir] = useState<SortDir>('asc');
  const [page, setPage] = useState(1);

  const filtered = useMemo(() => {
    const q = quickSearch.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter((row) =>
      columns.some((col) => String(row[col.key] ?? '').toLowerCase().includes(q)),
    );
  }, [rows, columns, quickSearch]);

  const sorted = useMemo(() => {
    if (!sortKey) return filtered;
    const copy = [...filtered];
    copy.sort((a, b) => {
      const av = String(a[sortKey] ?? '');
      const bv = String(b[sortKey] ?? '');
      const cmp = av.localeCompare(bv, 'pt-BR', { numeric: true, sensitivity: 'base' });
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return copy;
  }, [filtered, sortKey, sortDir]);

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const pageRows = sorted.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  function toggleSort(key: string) {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir('asc');
    }
    setPage(1);
  }

  return (
    <div className="reports-data-table-wrap">
      <div className="reports-result-filter-row">
        <div className="reports-filter-field search-grow">
          <label htmlFor="reportQuickSearch">Pesquisa rápida nos resultados</label>
          <input
            id="reportQuickSearch"
            type="search"
            placeholder="Filtrar linhas…"
            value={quickSearch}
            onChange={(e) => {
              setQuickSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <div className="reports-result-meta">
          {sorted.length} registro(s)
          {quickSearch ? ` · filtrado de ${rows.length}` : ''}
        </div>
      </div>

      <div className="table-responsive-wrap">
        <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable reports-sortable-table">
          <thead>
            <tr>
              <th style={{ width: 48 }}><div>#</div></th>
              {columns.map((col) => (
                <th key={col.key}>
                  <button
                    type="button"
                    className="reports-sort-btn"
                    onClick={() => toggleSort(col.key)}
                    title="Ordenar coluna"
                  >
                    {col.label}
                    {sortKey === col.key ? (sortDir === 'asc' ? ' ▲' : ' ▼') : ''}
                  </button>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pageRows.length === 0 ? (
              <tr>
                <td colSpan={columns.length + 1} className="dataTables_empty center">
                  Sem dados para exibir.
                </td>
              </tr>
            ) : (
              pageRows.map((row, i) => (
                <tr key={`${currentPage}-${i}`} className={i % 2 === 1 ? 'even' : undefined}>
                  <td>{(currentPage - 1) * pageSize + i + 1}</td>
                  {columns.map((col) => (
                    <td key={col.key}>{String(row[col.key] ?? '—')}</td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 ? (
        <div className="reports-pagination">
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            disabled={currentPage <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            Anterior
          </button>
          <span>
            Página {currentPage} de {totalPages}
          </span>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            disabled={currentPage >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          >
            Próxima
          </button>
        </div>
      ) : null}
    </div>
  );
}
