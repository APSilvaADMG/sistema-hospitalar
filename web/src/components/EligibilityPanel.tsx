import { useState } from 'react';
import { api, type EligibilityCheckDto } from '../api/client';
import { formatBrDateTime } from '../utils/dateUtils';

type Props = {
  patientId?: string;
  healthInsuranceId?: string;
  cardNumber?: string;
  compact?: boolean;
};

export function EligibilityPanel({ patientId, healthInsuranceId, cardNumber, compact }: Props) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<EligibilityCheckDto | null>(null);

  const canCheck = Boolean(patientId && healthInsuranceId);

  async function handleCheck() {
    if (!patientId || !healthInsuranceId) {
      setError('Selecione paciente e convênio antes de verificar elegibilidade.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      const check = await api.checkEligibility({
        patientId,
        healthInsuranceId,
        cardNumber: cardNumber || undefined,
      });
      setResult(check);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na verificação de elegibilidade.');
      setResult(null);
    } finally {
      setLoading(false);
    }
  }

  const statusClass = result?.status === 1
    ? 'badge-success'
    : result?.status === 2
      ? 'badge-danger'
      : 'badge-warning';

  const statusLabel = result?.status === 1
    ? 'Elegível'
    : result?.status === 2
      ? 'Não elegível'
      : result?.status === 3
        ? 'Pendente'
        : result?.status === 4
          ? 'Erro'
          : '';

  return (
    <div className={compact ? 'eligibility-panel compact' : 'card-panel appt-panel'}>
      {!compact && <div className="card-panel-header">Elegibilidade do convênio</div>}
      <div className={compact ? '' : 'card-panel-body'}>
        <p style={{ margin: compact ? '0 0 8px' : undefined, color: 'var(--muted)', fontSize: 13 }}>
          Consulta online ao operador (TISS). Recomendado antes de agendamentos com plano de saúde.
        </p>
        <button
          type="button"
          className="btn btn-secondary btn-sm"
          disabled={!canCheck || loading}
          onClick={handleCheck}
        >
          {loading ? 'Verificando...' : 'Verificar elegibilidade'}
        </button>
        {error && <div className="alert alert-error" style={{ marginTop: 12 }}>{error}</div>}
        {result && (
          <div style={{ marginTop: 12, padding: 12, background: 'var(--surface-muted)', borderRadius: 8 }}>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
              <span className={`badge ${statusClass}`}>{statusLabel}</span>
              <strong>{result.healthInsuranceName}</strong>
              {result.planName && <span style={{ color: 'var(--muted)' }}>· {result.planName}</span>}
            </div>
            {result.coverageSummary && (
              <p style={{ margin: '8px 0 0', fontSize: 13 }}>{result.coverageSummary}</p>
            )}
            {result.responseMessage && (
              <p style={{ margin: '4px 0 0', fontSize: 12, color: 'var(--muted)' }}>{result.responseMessage}</p>
            )}
            <p style={{ margin: '8px 0 0', fontSize: 12, color: 'var(--muted)' }}>
              Verificado em {formatBrDateTime(result.createdAt)}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
