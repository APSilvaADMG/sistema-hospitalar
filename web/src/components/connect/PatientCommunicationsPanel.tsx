import { useCallback, useEffect, useState } from 'react';
import { api, type ConnectContextMessageDto } from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';

export function PatientCommunicationsPanel({ patientId }: { patientId: string }) {
  const [messages, setMessages] = useState<ConnectContextMessageDto[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setMessages(await api.getConnectPatientContextMessages(patientId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar comunicações');
    } finally {
      setLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    load().catch(console.error);
  }, [load]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <p className="text-muted" style={{ margin: 0 }}>
          Mensagens internas do APSMed Connect vinculadas a este paciente.
        </p>
        <button type="button" className="btn btn-secondary btn-sm" onClick={() => load().catch(console.error)}>
          Atualizar
        </button>
      </div>

      {error ? <p className="text-danger">{error}</p> : null}
      {loading ? <p className="text-muted">Carregando…</p> : null}

      {!loading && messages.length === 0 ? (
        <p className="text-muted">Nenhuma comunicação vinculada a este paciente.</p>
      ) : (
        <div style={{ display: 'grid', gap: '0.75rem' }}>
          {messages.map((m) => (
            <article key={m.id} className="connect-panel" style={{ padding: '0.75rem 1rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem' }}>
                <strong>{m.subject}</strong>
                <span className="text-muted" style={{ fontSize: '0.85rem' }}>
                  {formatBrDateTime(m.createdAt)}
                </span>
              </div>
              <div className="text-muted" style={{ fontSize: '0.85rem', margin: '0.25rem 0' }}>
                De {m.senderName} · {m.priority}
              </div>
              <div style={{ whiteSpace: 'pre-wrap' }}>{m.content}</div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
