import { Link } from 'react-router-dom';
import { formatBrDate, formatBrDateTime } from '../../../utils/dateUtils';
import {
  stockRequisitionPriorityLabels,
  stockRequisitionStatusLabels,
  type StockRequisitionDto,
} from '../../../api/client';
import { TablePagination } from '../TablePagination';

type Props = {
  requisitions: StockRequisitionDto[];
  statusFilter: number | '';
  priorityFilter: number | '';
  dueDateFilter: string;
  onStatusFilterChange: (value: number | '') => void;
  onPriorityFilterChange: (value: number | '') => void;
  onDueDateFilterChange: (value: string) => void;
  onSearch: () => void;
  onOpen: (id: string) => void;
  onDeny?: (id: string) => void;
  loading?: boolean;
  canCreate?: boolean;
  canManageWarehouse?: boolean;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
};

export function FeegowStockRequisitionList({
  requisitions,
  statusFilter,
  priorityFilter,
  dueDateFilter,
  onStatusFilterChange,
  onPriorityFilterChange,
  onDueDateFilterChange,
  onSearch,
  onOpen,
  onDeny,
  loading,
  canCreate,
  canManageWarehouse,
  page,
  pageSize,
  totalCount,
  onPageChange,
}: Props) {
  const showPagination = page != null && pageSize != null && totalCount != null && onPageChange != null;

  return (
    <div className="feegow-requisition-page">
      <header className="feegow-requisition-page-head">
        <div className="feegow-requisition-page-title-wrap">
          <h1 className="feegow-requisition-page-title">Requisição de Estoque</h1>
          <div className="feegow-requisition-page-title-icons" aria-hidden>
            <span>☰</span>
            <span>▦</span>
          </div>
        </div>
        {canCreate ? (
          <Link to="/estoque/requisicoes/inserir" className="feegow-requisition-insert-btn">
            + Inserir
          </Link>
        ) : null}
      </header>

      <section className="feegow-requisition-filter-card">
        <div className="feegow-requisition-filter-grid">
          <label className="feegow-requisition-field">
            <span>Status</span>
            <select
              value={statusFilter === '' ? '' : String(statusFilter)}
              onChange={(e) => onStatusFilterChange(e.target.value === '' ? '' : Number(e.target.value))}
            >
              <option value="">Selecione</option>
              {Object.entries(stockRequisitionStatusLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>

          <label className="feegow-requisition-field">
            <span>Prazo até</span>
            <div className="feegow-requisition-date-wrap">
              <span className="feegow-requisition-date-icon" aria-hidden>📅</span>
              <input
                type="date"
                value={dueDateFilter}
                onChange={(e) => onDueDateFilterChange(e.target.value)}
              />
            </div>
          </label>

          <label className="feegow-requisition-field">
            <span>Prioridade</span>
            <select
              value={priorityFilter === '' ? '' : String(priorityFilter)}
              onChange={(e) => onPriorityFilterChange(e.target.value === '' ? '' : Number(e.target.value))}
            >
              <option value="">Selecione</option>
              {Object.entries(stockRequisitionPriorityLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>

          <button type="button" className="feegow-requisition-search-btn" onClick={onSearch}>
            🔍 BUSCAR
          </button>
        </div>
      </section>

      <section className="feegow-requisition-table-card">
        <p className="feegow-requisition-count">{totalCount ?? requisitions.length} requisições</p>
        <div className="feegow-requisition-table-wrap">
          <table className="feegow-requisition-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Abertura</th>
                <th>Prazo</th>
                <th>Status</th>
                <th>Solicitante</th>
                <th>Produtos</th>
                <th>Localização destino</th>
                {canManageWarehouse ? <th>Ações</th> : null}
              </tr>
            </thead>
            <tbody>
              {requisitions.map((req) => (
                <tr key={req.id} className="feegow-requisition-table-row" onClick={() => onOpen(req.id)}>
                  <td>{req.sequenceNumber}</td>
                  <td>{formatBrDateTime(req.requestedAt)}</td>
                  <td>{req.dueDate ? formatBrDate(req.dueDate) : '—'}</td>
                  <td>{stockRequisitionStatusLabels[req.status] ?? '—'}</td>
                  <td>{req.requestedBy}</td>
                  <td>{req.itemCount}</td>
                  <td>{req.destinationLocation || '—'}</td>
                  {canManageWarehouse ? (
                    <td onClick={(e) => e.stopPropagation()}>
                      {(req.status === 1 || req.status === 2) && onDeny ? (
                        <button
                          type="button"
                          className="feegow-warehouse-btn feegow-warehouse-deny-btn"
                          onClick={() => onDeny(req.id)}
                        >
                          Negar
                        </button>
                      ) : '—'}
                    </td>
                  ) : null}
                </tr>
              ))}
              {!loading && requisitions.length === 0 ? (
                <tr>
                  <td colSpan={canManageWarehouse ? 8 : 7} className="feegow-requisition-table-empty">
                    Nenhuma requisição encontrada com os critérios selecionados.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {showPagination ? (
          <TablePagination
            page={page}
            pageSize={pageSize}
            totalCount={totalCount}
            onPageChange={onPageChange}
            loading={loading}
          />
        ) : null}
      </section>
    </div>
  );
}
