type Props = {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  loading?: boolean;
};

export function TablePagination({ page, pageSize, totalCount, onPageChange, loading }: Props) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);
  const atFirst = page <= 1;
  const atLast = page >= totalPages;

  if (totalCount === 0 && !loading) {
    return null;
  }

  return (
    <div className="feegow-table-pagination" aria-label="Paginação da tabela">
      <span className="feegow-table-pagination-range">
        {start}–{end} de {totalCount} registros
      </span>
      <div className="feegow-table-pagination-controls">
        <button
          type="button"
          className="feegow-table-pagination-btn"
          disabled={atFirst || loading}
          onClick={() => onPageChange(page - 1)}
        >
          Anterior
        </button>
        <span className="feegow-table-pagination-page">
          Página {page} de {totalPages}
        </span>
        <button
          type="button"
          className="feegow-table-pagination-btn"
          disabled={atLast || loading}
          onClick={() => onPageChange(page + 1)}
        >
          Próxima
        </button>
      </div>
    </div>
  );
}
