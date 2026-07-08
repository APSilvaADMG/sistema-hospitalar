import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type ConnectConversationDetailDto,
  type ConnectConversationDto,
  type ConnectInboxSummaryDto,
} from '../../../api/client';
import { useAuth } from '../../../auth/AuthContext';
import { subscribeConnectInboxRefresh } from '../../../offline/connectRealtimeSync';
import { formatBrDateTime } from '../../../utils/dateUtils';

type InboxFilter = 'all' | 'human' | 'billing';

type Props = {
  initialFilter?: InboxFilter;
};

export function FeegowConnectInbox({ initialFilter = 'all' }: Props) {
  const { user, hasPermission } = useAuth();
  const canWrite = hasPermission('connect.write');
  const [filter, setFilter] = useState<InboxFilter>(initialFilter);
  const [summary, setSummary] = useState<ConnectInboxSummaryDto | null>(null);
  const [conversations, setConversations] = useState<ConnectConversationDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<ConnectConversationDetailDto | null>(null);
  const [reply, setReply] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const loadList = useCallback(async () => {
    setError('');
    try {
      const query = {
        limit: 80,
        awaitingHumanOnly: filter === 'human',
        queue: filter === 'billing' ? 'Billing' : undefined,
      };
      const [list, inboxSummary] = await Promise.all([
        api.getConnectConversations(query),
        api.getConnectInboxSummary(),
      ]);
      setConversations(list);
      setSummary(inboxSummary);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar inbox');
    }
  }, [filter]);

  const loadDetail = useCallback(async (id: string) => {
    try {
      const data = await api.getConnectConversation(id);
      setDetail(data);
      setSelectedId(id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao abrir conversa');
    }
  }, []);

  useEffect(() => {
    void loadList();
  }, [loadList]);

  useEffect(() => {
    return subscribeConnectInboxRefresh(() => {
      void loadList();
      if (selectedId) {
        void loadDetail(selectedId);
      }
    });
  }, [loadList, loadDetail, selectedId]);

  async function handleReply(event: FormEvent) {
    event.preventDefault();
    if (!canWrite || !selectedId || !reply.trim()) return;

    setSaving(true);
    setError('');
    try {
      await api.replyConnectConversation(selectedId, reply.trim());
      setReply('');
      await loadDetail(selectedId);
      await loadList();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enviar mensagem');
    } finally {
      setSaving(false);
    }
  }

  async function handleAssign() {
    if (!canWrite || !selectedId || !user?.userId) return;
    try {
      await api.assignConnectConversation(selectedId, { userId: user.userId, queue: 'Reception' });
      await loadDetail(selectedId);
      await loadList();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao assumir conversa');
    }
  }

  async function handleResolve() {
    if (!canWrite || !selectedId) return;
    try {
      await api.resolveConnectConversation(selectedId);
      setDetail(null);
      setSelectedId(null);
      await loadList();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao encerrar atendimento');
    }
  }

  return (
    <section className="feegow-connect-inbox">
      {error ? <div className="alert alert-error">{error}</div> : null}

      <div className="feegow-connect-inbox-kpis">
        <div className="feegow-connect-kpi">
          <span>Fila humana</span>
          <strong>{summary?.awaitingHuman ?? 0}</strong>
        </div>
        <div className="feegow-connect-kpi">
          <span>Em atendimento</span>
          <strong>{summary?.assignedOpen ?? 0}</strong>
        </div>
        <div className="feegow-connect-kpi">
          <span>Mensagens hoje</span>
          <strong>{summary?.messagesToday ?? 0}</strong>
        </div>
        <div className="feegow-connect-kpi warn">
          <span>Falhas hoje</span>
          <strong>{summary?.failedMessagesToday ?? 0}</strong>
        </div>
      </div>

      <div className="feegow-connect-inbox-filters">
        <button type="button" className={filter === 'all' ? 'is-active' : ''} onClick={() => setFilter('all')}>
          Todas
        </button>
        <button type="button" className={filter === 'human' ? 'is-active' : ''} onClick={() => setFilter('human')}>
          Fila humana
        </button>
        <button type="button" className={filter === 'billing' ? 'is-active' : ''} onClick={() => setFilter('billing')}>
          Cobrança
        </button>
      </div>

      <div className="feegow-connect-inbox-split">
        <div className="feegow-connect-list card">
          {conversations.length === 0 ? (
            <p className="form-hint">Nenhuma conversa neste filtro.</p>
          ) : null}
          {conversations.map((c) => (
            <button
              key={c.id}
              type="button"
              className={`feegow-connect-list-item${selectedId === c.id ? ' is-active' : ''}${c.botStep === 'AwaitingHuman' ? ' is-human' : ''}`}
              onClick={() => void loadDetail(c.id)}
            >
              <strong>{c.patientName ?? c.contactPhone}</strong>
              <span>{c.lastMessagePreview?.slice(0, 72) ?? '—'}</span>
              <small>
                {c.botStep}
                {c.assignedUserName ? ` · ${c.assignedUserName}` : ''}
                {c.lastMessageAt ? ` · ${formatBrDateTime(c.lastMessageAt)}` : ''}
              </small>
            </button>
          ))}
        </div>

        <div className="feegow-connect-thread card">
          {detail ? (
            <>
              <header className="feegow-connect-thread-head">
                <div>
                  <h3>{detail.conversation.patientName ?? detail.conversation.contactPhone}</h3>
                  <p>{detail.conversation.contactPhone} · {detail.conversation.botStep}</p>
                </div>
                {canWrite ? (
                  <div className="feegow-connect-thread-actions">
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => void handleAssign()}>
                      Assumir
                    </button>
                    <button type="button" className="btn btn-sm" onClick={() => void handleResolve()}>
                      Encerrar
                    </button>
                  </div>
                ) : null}
              </header>

              <div className="feegow-connect-messages">
                {detail.messages.map((m) => (
                  <div
                    key={m.id}
                    className={`feegow-connect-bubble${m.direction === 'Inbound' ? ' inbound' : ' outbound'}`}
                  >
                    <p>{m.body}</p>
                    <small>{formatBrDateTime(m.createdAt)} · {m.status}</small>
                  </div>
                ))}
              </div>

              {canWrite ? (
                <form className="feegow-connect-composer" onSubmit={handleReply}>
                  <textarea
                    value={reply}
                    onChange={(e) => setReply(e.target.value)}
                    placeholder="Digite a resposta para o paciente…"
                    rows={3}
                  />
                  <button type="submit" className="btn btn-sm" disabled={saving || !reply.trim()}>
                    {saving ? 'Enviando…' : 'Enviar WhatsApp'}
                  </button>
                </form>
              ) : null}
            </>
          ) : (
            <p className="form-hint">Selecione uma conversa para atender.</p>
          )}
        </div>
      </div>
    </section>
  );
}
