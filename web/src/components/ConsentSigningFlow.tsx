import { useRef, useState, type FormEvent } from 'react';
import type { ConsentTermDto } from '../api/client';
import { DigitalSignaturePad } from './DigitalSignaturePad';
import { formatBrDateTime } from '../utils/dateUtils';

type Props = {
  term: ConsentTermDto;
  defaultSignerName?: string;
  submitLabel?: string;
  saving?: boolean;
  onSubmit: (payload: {
    readAt: string;
    acknowledgedAt: string;
    signerName: string;
    signatureImage: string;
    notes?: string;
  }) => void | Promise<void>;
};

export function ConsentSigningFlow({
  term,
  defaultSignerName = '',
  submitLabel = 'Assinar e registrar consentimento',
  saving = false,
  onSubmit,
}: Props) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [readComplete, setReadComplete] = useState(false);
  const [readAt, setReadAt] = useState<string | null>(null);
  const [acknowledged, setAcknowledged] = useState(false);
  const [signerName, setSignerName] = useState(defaultSignerName);
  const [signatureImage, setSignatureImage] = useState<string | null>(null);
  const [notes, setNotes] = useState('');
  const [error, setError] = useState('');

  function handleScroll() {
    const el = scrollRef.current;
    if (!el || readComplete) return;
    const atBottom = el.scrollTop + el.clientHeight >= el.scrollHeight - 24;
    if (atBottom) {
      setReadComplete(true);
      setReadAt(new Date().toISOString());
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');

    if (!readAt) {
      setError('Role o termo até o final antes de dar ciência.');
      return;
    }
    if (!acknowledged) {
      setError('Marque que leu e compreendeu o termo (ciência).');
      return;
    }
    if (!signerName.trim()) {
      setError('Informe o nome de quem assina.');
      return;
    }
    if (!signatureImage) {
      setError('Desenhe a assinatura no campo abaixo.');
      return;
    }

    await onSubmit({
      readAt,
      acknowledgedAt: new Date().toISOString(),
      signerName: signerName.trim(),
      signatureImage,
      notes: notes.trim() || undefined,
    });
  }

  return (
    <form className="consent-signing-flow card-panel appt-panel" onSubmit={handleSubmit}>
      <div className="consent-signing-header">
        <div>
          <h3 style={{ margin: '0 0 4px' }}>{term.title}</h3>
          <p className="form-hint" style={{ margin: 0 }}>
            Versão {term.version} · vigente desde {formatBrDateTime(term.effectiveFrom)}
          </p>
        </div>
        <span className={`consent-step-badge${readComplete ? ' done' : ''}`}>1. Leitura</span>
      </div>

      <div className="consent-purposes">
        <strong>Finalidades:</strong> {term.purposes.join(' · ')}
      </div>

      <div
        ref={scrollRef}
        className="consent-term-scroll"
        onScroll={handleScroll}
        aria-label="Texto integral do termo de consentimento"
      >
        <p style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{term.content}</p>
      </div>

      {!readComplete && (
        <p className="form-hint consent-scroll-hint">
          Role até o final do termo para liberar a ciência e a assinatura.
        </p>
      )}

      <div className={`consent-signing-step${readComplete ? '' : ' disabled'}`}>
        <span className={`consent-step-badge${acknowledged ? ' done' : ''}`}>2. Ciência</span>
        <label className="pep-checkbox-label consent-ack-label">
          <input
            type="checkbox"
            checked={acknowledged}
            disabled={!readComplete}
            onChange={(e) => setAcknowledged(e.target.checked)}
          />
          Declaro que li integralmente este termo, compreendi seu conteúdo e dou ciência das informações acima.
        </label>
      </div>

      <div className={`consent-signing-step${acknowledged ? '' : ' disabled'}`}>
        <span className={`consent-step-badge${signatureImage ? ' done' : ''}`}>3. Assinatura</span>
        <div className="form-grid">
          <div className="form-field full">
            <label htmlFor="consent-signer-name">Nome completo de quem assina</label>
            <input
              id="consent-signer-name"
              value={signerName}
              disabled={!acknowledged}
              onChange={(e) => setSignerName(e.target.value)}
              required
            />
          </div>
          <div className="form-field full">
            <label>Assinatura digital</label>
            {acknowledged ? (
              <DigitalSignaturePad onChange={setSignatureImage} layoutKey={term.id} />
            ) : (
              <p className="form-hint">Disponível após marcar a ciência.</p>
            )}
          </div>
          <div className="form-field full">
            <label htmlFor="consent-notes">Observações (opcional)</label>
            <textarea
              id="consent-notes"
              rows={2}
              value={notes}
              disabled={!acknowledged}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="form-actions">
        <button
          className="btn"
          type="submit"
          disabled={saving || !readComplete || !acknowledged || !signatureImage || !signerName.trim()}
        >
          {saving ? 'Registrando…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
