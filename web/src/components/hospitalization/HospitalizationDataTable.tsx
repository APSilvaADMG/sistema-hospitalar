import { Link } from 'react-router-dom';
import type { HospitalizationHubListItemDto } from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';

export type HospitalizationRowAction = 'view' | 'pep' | 'admit';

type Props = {
  items: HospitalizationHubListItemDto[];
  total: number;
  loading?: boolean;
  canManage?: boolean;
  onAction: (action: HospitalizationRowAction, item: HospitalizationHubListItemDto) => void;
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
};

function statusClass(item: HospitalizationHubListItemDto) {
  if (item.itemType === 'request') {
    if (item.status === 1) return 'guides-status guides-status--draft';
    if (item.status === 2) return 'guides-status guides-status--sent';
    if (item.status === 3) return 'guides-status guides-status--cancelled';
    return 'guides-status';
  }
  switch (item.status) {
    case 1:
      return 'guides-status guides-status--sent';
    case 2:
      return 'guides-status guides-status--paid';
    case 3:
      return 'guides-status guides-status--draft';
    default:
      return 'guides-status';
  }
}

export function HospitalizationDataTable({
  items,
  total,
  loading,
  canManage,
  onAction,
  page,
  pageSize,
  onPageChange,
}: Props) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div className="guides-table-wrap">
      <div className="guides-table-meta">
        <span>{total} registro(s) encontrado(s)</span>
        {loading && <span className="guides-table-loading">Atualizando…</span>}
      </div>
      <div className="guides-table-scroll">
        <table className="guides-data-table">
          <thead>
            <tr>
              <th>Tipo</th>
              <th>Paciente</th>
              <th>Ala / Leito</th>
              <th>Médico</th>
              <th>Modalidade</th>
              <th>Data</th>
              <th>Permanência</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={`${item.itemType}-${item.id}`}>
                <td>{item.itemType === 'request' ? 'Solicitação' : 'Internação'}</td>
                <td>
                  <strong>{item.patientName}</strong>
                  {item.hasSusAih && (
                    <span className="guides-tag" style={{ marginLeft: 6 }}>AIH</span>
                  )}
                </td>
                <td>
                  {item.wardName ?? '—'}
                  {item.bedNumber ? ` / ${item.bedNumber}` : ''}
                </td>
                <td>{item.professionalName ?? '—'}</td>
                <td>{item.modalityLabel ?? '—'}</td>
                <td>{formatBrDateTime(item.eventAt)}</td>
                <td>{item.daysHospitalized != null ? `${item.daysHospitalized}d` : '—'}</td>
                <td><span className={statusClass(item)}>{item.statusLabel}</span></td>
                <td>
                  <div className="guides-row-actions">
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => onAction('pep', item)}
                    >
                      PEP
                    </button>
                    {item.itemType === 'request' && canManage && item.status === 2 && (
                      <button
                        type="button"
                        className="btn btn-sm"
                        onClick={() => onAction('admit', item)}
                      >
                        Admitir
                      </button>
                    )}
                    {item.itemType === 'hospitalization' && (
                      <Link
                        to={`/internacao/leitos?internacao=${item.id}`}
                        className="btn btn-secondary btn-sm"
                      >
                        Leito
                      </Link>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {items.length === 0 && !loading && (
              <tr>
                <td colSpan={9} className="guides-table-empty">
                  Nenhum registro encontrado para os filtros selecionados.
                </td>
              </tr>
            )}
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
