import { useCallback, useEffect, useState } from 'react';
import {
  api,
  type ConsentTermDto,
  type PatientConsentDto,
  type PatientConsentStatusDto,
  type PatientDto,
} from '../api/client';
import { ConsentSigningFlow } from './ConsentSigningFlow';
import { ConsentDocumentModal } from './ConsentDocumentModal';
import { formatBrDateTime } from '../utils/dateUtils';

type Props = {
  patientId?: string;
  patients?: PatientDto[];
  onPatientChange?: (patientId: string) => void;
  /** Portal do paciente — usa API do portal em vez da administração. */
  portalMode?: boolean;
  onSuccess?: (message: string) => void;
  onError?: (message: string) => void;
};

export function PatientConsentsPanel({
  patientId,
  patients,
  onPatientChange,
  portalMode = false,
  onSuccess,
  onError,
}: Props) {
  const [status, setStatus] = useState<PatientConsentStatusDto | null>(null);
  const [selectedTermId, setSelectedTermId] = useState('');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(false);
  const [viewConsentId, setViewConsentId] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!patientId) {
      setStatus(null);
      return;
    }
    setLoading(true);
    try {
      const data = portalMode
        ? await api.getPatientPortalConsentStatus(patientId)
        : await api.getPatientConsentStatus(patientId);
      setStatus(data);
      setSelectedTermId((prev) => {
        if (prev && data.pendingTerms.some((t) => t.id === prev)) return prev;
        return data.pendingTerms[0]?.id ?? '';
      });
    } catch (err) {
      onError?.(err instanceof Error ? err.message : 'Erro ao carregar consentimentos.');
    } finally {
      setLoading(false);
    }
  }, [patientId, portalMode, onError]);

  useEffect(() => {
    load().catch(console.error);
  }, [load]);

  const selectedTerm: ConsentTermDto | undefined = status?.pendingTerms.find((t) => t.id === selectedTermId)
    ?? status?.pendingTerms[0];

  const defaultSignerName = patients?.find((p) => p.id === patientId)?.fullName ?? '';

  async function handleSign(payload: {
    readAt: string;
    acknowledgedAt: string;
    signerName: string;
    signatureImage: string;
    notes?: string;
  }) {
    if (!patientId || !selectedTerm) return;
    setSaving(true);
    try {
      const body = {
        consentTermId: selectedTerm.id,
        purposes: selectedTerm.purposes,
        readAt: payload.readAt,
        acknowledgedAt: payload.acknowledgedAt,
        signerName: payload.signerName,
        signatureImage: payload.signatureImage,
        notes: payload.notes,
      };

      if (portalMode) {
        await api.signPatientPortalConsent(body, patientId);
      } else {
        await api.recordPatientConsent({ patientId, ...body });
      }

      onSuccess?.('Consentimento registrado com leitura, ciência e assinatura.');
      setSelectedTermId('');
      await load();
    } catch (err) {
      onError?.(err instanceof Error ? err.message : 'Erro ao registrar consentimento.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="patient-consents-panel">
      {onPatientChange && patients && (
        <div className="card form-grid" style={{ marginBottom: 16 }}>
          <div className="form-field grow-lg">
            <label htmlFor="consent-patient">Paciente</label>
            <select
              id="consent-patient"
              value={patientId ?? ''}
              onChange={(e) => onPatientChange(e.target.value)}
            >
              <option value="">Selecione o paciente</option>
              {(Array.isArray(patients) ? patients : []).map((p) => (
                <option key={p.id} value={p.id}>{p.fullName}</option>
              ))}
            </select>
          </div>
        </div>
      )}

      {!patientId && (
        <div className="card">
          <p className="form-hint" style={{ margin: 0 }}>Selecione um paciente para coletar consentimentos.</p>
        </div>
      )}

      {patientId && loading && !status && (
        <div className="card"><p className="form-hint" style={{ margin: 0 }}>Carregando termos…</p></div>
      )}

      {patientId && status && (
        <>
          {status.pendingTerms.length > 0 ? (
            <>
              {status.pendingTerms.length > 1 && (
                <div className="card form-grid" style={{ marginBottom: 16 }}>
                  <div className="form-field grow-lg">
                    <label htmlFor="consent-term">Termo pendente</label>
                    <select
                      id="consent-term"
                      value={selectedTerm?.id ?? ''}
                      onChange={(e) => setSelectedTermId(e.target.value)}
                    >
                      {status.pendingTerms.map((t) => (
                        <option key={t.id} value={t.id}>{t.title} (v{t.version})</option>
                      ))}
                    </select>
                  </div>
                </div>
              )}

              {selectedTerm && (
                <ConsentSigningFlow
                  key={selectedTerm.id}
                  term={selectedTerm}
                  defaultSignerName={defaultSignerName}
                  saving={saving}
                  onSubmit={handleSign}
                />
              )}
            </>
          ) : (
            <div className="card" style={{ marginBottom: 16 }}>
              <p className="form-hint" style={{ margin: 0 }}>
                Todos os termos vigentes foram assinados para este paciente.
              </p>
            </div>
          )}

          <ConsentHistoryTable
            consents={status.activeConsents}
            onView={(id) => setViewConsentId(id)}
          />
        </>
      )}

      <ConsentDocumentModal
        consentId={viewConsentId}
        portalMode={portalMode}
        patientId={patientId}
        onClose={() => setViewConsentId(null)}
      />
    </div>
  );
}

function ConsentHistoryTable({
  consents,
  onView,
}: {
  consents: PatientConsentDto[];
  onView: (id: string) => void;
}) {
  if (consents.length === 0) return null;

  return (
    <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
      <div className="card-panel-header">Histórico de consentimentos — {consents.length}</div>
      <div className="card-panel-body" style={{ padding: 0 }}>
        <table className="data-table">
          <thead>
            <tr>
              <th>Termo</th>
              <th>Assinante</th>
              <th>Leitura</th>
              <th>Ciência</th>
              <th>Assinatura</th>
              <th>Registrado</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {consents.map((c) => (
              <tr key={c.id}>
                <td>{c.termTitle} v{c.termVersion}</td>
                <td>{c.signerName ?? '—'}</td>
                <td>{c.readAt ? formatBrDateTime(c.readAt) : '—'}</td>
                <td>{c.acknowledgedAt ? formatBrDateTime(c.acknowledgedAt) : '—'}</td>
                <td>{c.hasSignature ? 'Sim' : 'Não'}</td>
                <td>{formatBrDateTime(c.grantedAt)}</td>
                <td>
                  <button type="button" className="btn btn-secondary btn-sm" onClick={() => onView(c.id)}>
                    Ver documento
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
