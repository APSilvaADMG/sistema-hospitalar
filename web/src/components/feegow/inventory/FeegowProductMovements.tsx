import { useEffect, useMemo, useState } from 'react';
import { formatBrDate, formatBrDateTime } from '../../../utils/dateUtils';
import type { StockMovementDto } from '../../../api/client';
import { TablePagination } from '../TablePagination';

const PAGE_SIZE = 50;

type Props = {
  productId?: string;
  movements: StockMovementDto[];
  loading?: boolean;
  showProductColumn?: boolean;
};

function formatQuantity(type: number, quantity: number): string {
  const prefix = type === 2 ? '-' : '+';
  return `${prefix}${quantity}`;
}

function formatCurrency(value?: number): string {
  if (value == null || value <= 0) return '—';
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function FeegowProductMovements({ productId, movements, loading, showProductColumn }: Props) {
  const [page, setPage] = useState(1);

  const pagedMovements = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return movements.slice(start, start + PAGE_SIZE);
  }, [movements, page]);

  useEffect(() => {
    setPage(1);
  }, [movements]);

  if (!productId && !showProductColumn) {
    return (
      <section className="feegow-inventory-panel feegow-inventory-movements-panel feegow-inventory-empty-panel">
        <h2 className="feegow-inventory-movements-title">Movimentação</h2>
        <p>Salve o produto na aba Cadastro para visualizar movimentações.</p>
      </section>
    );
  }

  return (
    <section className="feegow-inventory-panel feegow-inventory-movements-panel">
      <h2 className="feegow-inventory-movements-title">Movimentação</h2>
      <div className="feegow-inventory-table-wrap">
        <table className="feegow-inventory-table feegow-inventory-movements-table">
          <thead>
            <tr>
              {showProductColumn ? <th>Produto</th> : null}
              <th>Quant.</th>
              <th>Data</th>
              <th>Paciente/Fornecedor</th>
              <th>Responsável</th>
              <th>Usuário</th>
              <th>Lote</th>
              <th>Código</th>
              <th>Validade</th>
              <th>NF</th>
              <th>Valor Unit.</th>
              <th>Conta</th>
            </tr>
          </thead>
          <tbody>
            {pagedMovements.map((movement) => (
              <tr key={movement.id} className="feegow-inventory-table-row-static">
                {showProductColumn ? <td>{movement.productName}</td> : null}
                <td>{formatQuantity(movement.type, movement.quantity)}</td>
                <td>{formatBrDateTime(movement.createdAt)}</td>
                <td>{movement.patientOrSupplier || movement.reference || '—'}</td>
                <td>{movement.responsibleName || '—'}</td>
                <td>{movement.userName || '—'}</td>
                <td>{movement.batchNumber || '—'}</td>
                <td>{movement.individualCode || '—'}</td>
                <td>{formatBrDate(movement.expiryDate)}</td>
                <td>{movement.invoiceNumber || '—'}</td>
                <td>{formatCurrency(movement.unitPrice)}</td>
                <td>{movement.account || movement.reason || '—'}</td>
              </tr>
            ))}
            {!loading && movements.length === 0 ? (
              <tr>
                <td colSpan={showProductColumn ? 12 : 11} className="feegow-inventory-table-empty">
                  Nenhuma movimentação registrada.
                </td>
              </tr>
            ) : null}
          </tbody>
        </table>
      </div>
      <TablePagination
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={movements.length}
        onPageChange={setPage}
        loading={loading}
      />
    </section>
  );
}

