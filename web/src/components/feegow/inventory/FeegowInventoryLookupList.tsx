import { Link } from 'react-router-dom';
import type { InventoryLookupItemDto } from '../../../api/client';
import { TablePagination } from '../TablePagination';
import { feegowInventoryLookupInsertPath, type FeegowInventoryLookupConfigId } from './feegowInventoryNav';

type Props = {
  configId: FeegowInventoryLookupConfigId;
  title: string;
  fieldLabel: string;
  items: InventoryLookupItemDto[];
  selectedIds: Set<string>;
  onToggle: (id: string) => void;
  onToggleAll: () => void;
  onOpen: (id: string) => void;
  loading?: boolean;
  canManage?: boolean;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
};

export function FeegowInventoryLookupList({
  configId,
  title,
  fieldLabel,
  items,
  selectedIds,
  onToggle,
  onToggleAll,
  onOpen,
  loading,
  canManage,
  page,
  pageSize,
  totalCount,
  onPageChange,
}: Props) {
  const allSelected = items.length > 0 && items.every((item) => selectedIds.has(item.id));
  const showPagination = page != null && pageSize != null && totalCount != null && onPageChange != null;

  return (
    <div className="feegow-inventory-page">
      <header className="feegow-inventory-page-head">
        <div className="feegow-inventory-breadcrumb">
          <span>{title}</span>
          <span className="feegow-inventory-crumb-sep">/</span>
        </div>
        {canManage ? (
          <Link to={feegowInventoryLookupInsertPath(configId)} className="feegow-inventory-insert-btn">
            + INSERIR
          </Link>
        ) : null}
      </header>

      <section className="feegow-inventory-panel feegow-inventory-table-card">
        <div className="feegow-inventory-table-wrap">
          <table className="feegow-inventory-table feegow-inventory-lookup-table">
            <thead>
              <tr>
                <th className="feegow-inventory-lookup-check-col">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={onToggleAll}
                    aria-label="Selecionar todos"
                  />
                </th>
                <th>{fieldLabel}</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} onClick={() => onOpen(item.id)}>
                  <td className="feegow-inventory-lookup-check-col" onClick={(e) => e.stopPropagation()}>
                    <input
                      type="checkbox"
                      checked={selectedIds.has(item.id)}
                      onChange={() => onToggle(item.id)}
                      aria-label={`Selecionar ${item.name}`}
                    />
                  </td>
                  <td>{item.name}</td>
                </tr>
              ))}
              {!loading && items.length === 0 ? (
                <tr>
                  <td colSpan={2} className="feegow-inventory-table-empty">
                    Nenhum registro encontrado.
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
