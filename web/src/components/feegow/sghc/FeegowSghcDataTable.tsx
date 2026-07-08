import { Link } from 'react-router-dom';
import type { SghcColumn, SghcRow } from './sghcScreenData';

type Props = {
  columns: SghcColumn[];
  rows: SghcRow[];
  loading: boolean;
  error: string;
  summary?: string;
  moduleLink?: string | null;
  onRefresh: () => void;
};

export function FeegowSghcDataTable({
  columns,
  rows,
  loading,
  error,
  summary,
  moduleLink,
  onRefresh,
}: Props) {
  return (
    <div className="feegow-patient-card feegow-sghc-table-card">
      <div className="feegow-sghc-data-toolbar">
        <div>
          {summary ? <p className="feegow-sghc-data-summary">{summary}</p> : null}
          <p className="form-hint">Dados carregados da API do sistema hospitalar.</p>
        </div>
        <div className="feegow-sghc-data-actions">
          <button type="button" className="btn btn-secondary btn-sm" onClick={onRefresh} disabled={loading}>
            {loading ? 'Atualizando…' : 'Atualizar'}
          </button>
          {moduleLink ? (
            <Link to={moduleLink} className="btn btn-sm">
              Módulo completo
            </Link>
          ) : null}
        </div>
      </div>

      {error ? <div className="alert alert-error feegow-sghc-data-alert">{error}</div> : null}

      <div className="table-wrap">
        <table className="data-table feegow-sghc-table">
          <thead>
            <tr>
              {columns.map((col) => (
                <th key={col.key}>{col.label}</th>
              ))}
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {loading && rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length + 1} className="feegow-sghc-empty-row">
                  Carregando registros…
                </td>
              </tr>
            ) : null}
            {!loading && rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length + 1} className="feegow-sghc-empty-row">
                  Nenhum registro encontrado.
                </td>
              </tr>
            ) : null}
            {rows.map((row) => (
              <tr key={row.id}>
                {columns.map((col) => (
                  <td key={`${row.id}-${col.key}`}>{row.cells[col.key] ?? '—'}</td>
                ))}
                <td>
                  {row.link ? (
                    <Link to={row.link} className="btn btn-secondary btn-sm">
                      Abrir
                    </Link>
                  ) : (
                    '—'
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
