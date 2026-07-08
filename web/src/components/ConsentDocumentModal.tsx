import { useEffect, useState } from 'react';
import { api, type PatientConsentDetailDto } from '../api/client';
import { Modal } from './Modal';
import { formatBrDateTime } from '../utils/dateUtils';

type Props = {
  consentId: string | null;
  portalMode?: boolean;
  patientId?: string;
  onClose: () => void;
};

export function ConsentDocumentModal({ consentId, portalMode = false, patientId, onClose }: Props) {
  const [detail, setDetail] = useState<PatientConsentDetailDto | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!consentId) {
      setDetail(null);
      return;
    }
    setLoading(true);
    setError('');
    const load = portalMode
      ? api.getPatientPortalConsentDetail(consentId, patientId)
      : api.getPatientConsentDetail(consentId);
    load
      .then(setDetail)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar documento.'))
      .finally(() => setLoading(false));
  }, [consentId, portalMode, patientId]);

  return (
    <Modal
      open={!!consentId}
      onClose={onClose}
      title="Documento assinado"
      subtitle={detail ? `${detail.termTitle} · v${detail.termVersion}` : 'Termo de consentimento'}
      width="lg"
    >
      {loading && <p className="form-hint">Carregando documento…</p>}
      {error && <div className="alert alert-error">{error}</div>}
      {detail && (
        <div className="consent-document-view">
          <div className="consent-document-meta">
            <div><strong>Paciente:</strong> {detail.patientName}</div>
            <div><strong>Assinante:</strong> {detail.signerName ?? '—'}</div>
            <div><strong>Leitura:</strong> {detail.readAt ? formatBrDateTime(detail.readAt) : '—'}</div>
            <div><strong>Ciência:</strong> {detail.acknowledgedAt ? formatBrDateTime(detail.acknowledgedAt) : '—'}</div>
            <div><strong>Registrado:</strong> {formatBrDateTime(detail.grantedAt)}</div>
            {detail.recordedByName && <div><strong>Registrado por:</strong> {detail.recordedByName}</div>}
            {detail.ipAddress && <div><strong>IP:</strong> {detail.ipAddress}</div>}
            {detail.revokedAt && <div><strong>Revogado:</strong> {formatBrDateTime(detail.revokedAt)}</div>}
          </div>

          <div className="consent-document-section">
            <h4>Finalidades</h4>
            <p>{detail.purposes.join(' · ')}</p>
          </div>

          <div className="consent-document-section">
            <h4>Texto do termo</h4>
            <div className="consent-term-scroll consent-document-body">
              <p style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{detail.termContent}</p>
            </div>
          </div>

          {detail.notes && (
            <div className="consent-document-section">
              <h4>Observações</h4>
              <p>{detail.notes}</p>
            </div>
          )}

          <div className="consent-document-section">
            <h4>Assinatura digital</h4>
            {detail.signatureImage ? (
              <img
                src={detail.signatureImage}
                alt={`Assinatura de ${detail.signerName ?? 'titular'}`}
                className="consent-signature-preview"
              />
            ) : (
              <p className="form-hint">Assinatura não disponível neste registro.</p>
            )}
          </div>

          <div className="form-actions no-print">
            <button type="button" className="btn btn-secondary" onClick={() => window.print()}>
              Imprimir / PDF
            </button>
            <button type="button" className="btn" onClick={onClose}>Fechar</button>
          </div>
        </div>
      )}
    </Modal>
  );
}
