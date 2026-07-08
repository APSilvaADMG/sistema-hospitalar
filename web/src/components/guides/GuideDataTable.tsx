import type { GuideHubListItemDto } from '../../api/client';
import { formatBrDate } from '../../utils/dateUtils';
import { GuideActionIcon, type GuideActionIconName } from './GuideActionIcon';

export type GuideRowAction =
  | 'view'
  | 'edit'
  | 'cancel'
  | 'print'
  | 'pdf'
  | 'duplicate'
  | 'authorize'
  | 'history';

type Props = {
  items: GuideHubListItemDto[];
  total: number;
  loading?: boolean;
  canWrite?: boolean;
  onAction: (action: GuideRowAction, item: GuideHubListItemDto) => void;
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
};

/** SUS: Draft=1, Submitted=2, Authorized=3, Billed=4, Glosa=5, Cancelled=6 */
/** TISS: Draft=1, Sent=2, Paid=3, Glosa=4, Cancelled=5 */

function isSus(row: GuideHubListItemDto) {
  return row.source === 'sus';
}

function canEdit(row: GuideHubListItemDto) {
  return row.status === 1;
}

function canCancel(row: GuideHubListItemDto) {
  if (isSus(row)) {
    return row.status === 1 || row.status === 2 || row.status === 3;
  }
  return row.status === 1 || row.status === 2 || row.status === 4;
}

function canPrint(row: GuideHubListItemDto) {
  return !isSus(row) && row.status !== 5;
}

function canDuplicate(row: GuideHubListItemDto) {
  return row.status === 1 || row.status === 2 || row.status === 3;
}

function canAuthorize(row: GuideHubListItemDto) {
  return row.status === 1 || row.status === 2;
}

function GuideActionButton({
  icon,
  label,
  onClick,
  variant,
}: {
  icon: GuideActionIconName;
  label: string;
  onClick: () => void;
  variant?: 'danger';
}) {
  return (
    <button
      type="button"
      className={`guides-action-icon-btn${variant === 'danger' ? ' guides-action-icon-btn--danger' : ''}`}
      title={label}
      aria-label={label}
      onClick={onClick}
    >
      <GuideActionIcon name={icon} />
    </button>
  );
}

function statusClass(row: GuideHubListItemDto) {
  const cancelled = isSus(row) ? row.status === 6 : row.status === 5;
  if (cancelled) return 'guides-status guides-status--cancelled';
  switch (row.status) {
    case 1:
      return 'guides-status guides-status--draft';
    case 2:
      return 'guides-status guides-status--sent';
    case 3:
    case 4:
      return 'guides-status guides-status--paid';
    case 5:
      return isSus(row) ? 'guides-status guides-status--glosa' : 'guides-status';
    default:
      return 'guides-status';
  }
}

export function GuideDataTable({
  items,
  total,
  loading,
  canWrite,
  onAction,
  page,
  pageSize,
  onPageChange,
}: Props) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div className="guides-table-wrap">
      <div className="guides-table-meta">
        <span>{total} guia(s) encontrada(s)</span>
        {loading && <span className="guides-table-loading">Atualizando…</span>}
      </div>
      <div className="guides-table-scroll">
        <table className="guides-data-table">
          <thead>
            <tr>
              <th>Nº guia</th>
              <th>Paciente</th>
              <th>Convênio</th>
              <th>Médico</th>
              <th>Especialidade</th>
              <th>Procedimento</th>
              <th>CID</th>
              <th>Emissão</th>
              <th>Autorização</th>
              <th>Status</th>
              <th>Unidade</th>
              <th>Tipo</th>
              <th>Origem</th>
              <th className="guides-col-actions">Ações</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && !loading && (
              <tr>
                <td colSpan={14} className="guides-empty">Nenhuma guia no período.</td>
              </tr>
            )}
            {items.map((row) => (
              <tr key={row.id}>
                <td><strong>{row.guideNumber}</strong></td>
                <td>{row.patientName}</td>
                <td>{row.healthInsuranceName}</td>
                <td>{row.requestingProfessionalName ?? '—'}</td>
                <td>{row.specialtyName ?? '—'}</td>
                <td className="guides-cell-truncate" title={row.procedureSummary ?? ''}>
                  {row.procedureSummary ?? '—'}
                </td>
                <td>{row.cid10Code ?? '—'}</td>
                <td>{formatBrDate(row.createdAt)}</td>
                <td>{row.authorizedAt ? formatBrDate(row.authorizedAt) : '—'}</td>
                <td>
                  <span className={statusClass(row)}>{row.statusLabel}</span>
                </td>
                <td>{row.serviceUnit}</td>
                <td>{row.guideTypeLabel}</td>
                <td>{row.source === 'sus' ? 'SUS' : 'TISS'}</td>
                <td className="guides-col-actions">
                  <div className="guides-row-actions" role="group" aria-label={`Ações da guia ${row.guideNumber}`}>
                    <GuideActionButton icon="view" label="Visualizar" onClick={() => onAction('view', row)} />
                    {canWrite && canEdit(row) && (
                      <GuideActionButton icon="edit" label="Editar" onClick={() => onAction('edit', row)} />
                    )}
                    {canWrite && canCancel(row) && (
                      <GuideActionButton icon="cancel" label="Cancelar" variant="danger" onClick={() => onAction('cancel', row)} />
                    )}
                    {canPrint(row) && (
                      <>
                        <GuideActionButton icon="print" label="Imprimir" onClick={() => onAction('print', row)} />
                        <GuideActionButton icon="pdf" label="Exportar PDF" onClick={() => onAction('pdf', row)} />
                      </>
                    )}
                    {canWrite && canDuplicate(row) && (
                      <GuideActionButton icon="duplicate" label="Duplicar" onClick={() => onAction('duplicate', row)} />
                    )}
                    {canWrite && canAuthorize(row) && (
                      <GuideActionButton icon="authorize" label="Autorizar" onClick={() => onAction('authorize', row)} />
                    )}
                    <GuideActionButton icon="history" label="Histórico" onClick={() => onAction('history', row)} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {totalPages > 1 && (
        <div className="guides-pagination">
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            Anterior
          </button>
          <span>Página {page} de {totalPages}</span>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            disabled={page >= totalPages}
            onClick={() => onPageChange(page + 1)}
          >
            Próxima
          </button>
        </div>
      )}
    </div>
  );
}
