import { type FormEvent, useEffect, useRef, useState } from 'react';

import {
  api,
  type ConnectAiAskResponse,
  type ConnectAiQuickQueryDto,
} from '../../api/client';

type ChatMessage = {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  meta?: ConnectAiAskResponse;
  streaming?: boolean;
};

export function ConnectAiAssistantPanel() {
  const [quickQueries, setQuickQueries] = useState<ConnectAiQuickQueryDto[]>([]);
  const [question, setQuestion] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    api.getConnectAiQuickQueries().then(setQuickQueries).catch(console.error);
    return () => abortRef.current?.abort();
  }, []);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  function updateAssistantMessage(id: string, updater: (msg: ChatMessage) => ChatMessage) {
    setMessages((prev) => prev.map((m) => (m.id === id ? updater(m) : m)));
  }

  async function ask(q: string) {
    const trimmed = q.trim();
    if (!trimmed) return;

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    setError('');
    setQuestion('');

    const userMsg: ChatMessage = { id: crypto.randomUUID(), role: 'user', text: trimmed };
    const assistantId = crypto.randomUUID();
    const assistantMsg: ChatMessage = {
      id: assistantId,
      role: 'assistant',
      text: '',
      streaming: true,
    };

    setMessages((prev) => [...prev, userMsg, assistantMsg]);

    try {
      await api.askConnectAiStream(
        trimmed,
        (chunk) => {
          if (chunk.type === 'token' && chunk.text) {
            updateAssistantMessage(assistantId, (m) => ({
              ...m,
              text: m.text + chunk.text,
            }));
          } else if (chunk.type === 'done') {
            updateAssistantMessage(assistantId, (m) => ({
              ...m,
              text: m.text || chunk.text || '',
              streaming: false,
              meta: {
                question: trimmed,
                answer: m.text || chunk.text || '',
                intent: chunk.intent ?? 'unknown',
                data: chunk.data,
                usedLlm: chunk.usedLlm,
              },
            }));
          }
        },
        controller.signal,
      );
    } catch (err) {
      if (controller.signal.aborted) return;

      try {
        const answer = await api.askConnectAi(trimmed);
        updateAssistantMessage(assistantId, () => ({
          id: assistantId,
          role: 'assistant',
          text: answer.answer,
          meta: answer,
          streaming: false,
        }));
      } catch (fallbackErr) {
        setMessages((prev) => prev.filter((m) => m.id !== assistantId));
        setError(
          fallbackErr instanceof Error ? fallbackErr.message : 'Erro ao consultar assistente',
        );
      }
    } finally {
      setLoading(false);
      updateAssistantMessage(assistantId, (m) => ({ ...m, streaming: false }));
    }
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    ask(question).catch(console.error);
  }

  return (
    <div className="connect-panel" style={{ display: 'flex', flexDirection: 'column', minHeight: 420 }}>
      <p className="text-muted" style={{ marginTop: 0 }}>
        Assistente APSMed — consultas sobre comunicação, chamados, tarefas e guias (somente leitura, sem PHI).
      </p>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginBottom: '0.75rem' }}>
        {quickQueries.map((q) => (
          <button
            key={q.id}
            type="button"
            className="btn btn-secondary btn-sm"
            disabled={loading}
            onClick={() => ask(q.question).catch(console.error)}
          >
            {q.label}
          </button>
        ))}
      </div>

      <div
        style={{
          flex: 1,
          overflowY: 'auto',
          display: 'flex',
          flexDirection: 'column',
          gap: '0.75rem',
          padding: '0.75rem',
          marginBottom: '0.75rem',
          borderRadius: 8,
          border: '1px solid var(--border, #dde3ea)',
          background: 'var(--surface, #fff)',
          maxHeight: 360,
        }}
      >
        {messages.length === 0 ? (
          <p className="text-muted" style={{ margin: 0, fontSize: '0.9rem' }}>
            Faça uma pergunta ou use um atalho acima. Com Groq habilitado, respostas em linguagem natural com streaming.
          </p>
        ) : null}
        {messages.map((m) => (
          <div
            key={m.id}
            style={{
              alignSelf: m.role === 'user' ? 'flex-end' : 'flex-start',
              maxWidth: '85%',
              padding: '0.6rem 0.85rem',
              borderRadius: 10,
              background: m.role === 'user' ? 'var(--primary, #2563eb)' : 'var(--surface-elevated, #f5f7fa)',
              color: m.role === 'user' ? '#fff' : 'inherit',
            }}
          >
            <div style={{ whiteSpace: 'pre-wrap' }}>
              {m.text}
              {m.streaming ? <span className="text-muted">▌</span> : null}
            </div>
            {m.meta ? (
              <div className="text-muted" style={{ fontSize: '0.75rem', marginTop: '0.35rem', opacity: 0.85 }}>
                {m.meta.usedLlm ? 'IA · ' : ''}intenção: {m.meta.intent}
              </div>
            ) : null}
          </div>
        ))}
        {loading && messages.every((m) => !m.streaming) ? (
          <p className="text-muted" style={{ margin: 0 }}>Consultando…</p>
        ) : null}
        <div ref={bottomRef} />
      </div>

      {error ? <p className="text-danger">{error}</p> : null}

      <form onSubmit={handleSubmit} style={{ display: 'flex', gap: '0.5rem' }}>
        <input
          placeholder="Ex.: Quantas mensagens não lidas tenho?"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          style={{ flex: 1 }}
        />
        <button type="submit" className="btn btn-primary" disabled={loading}>
          Enviar
        </button>
      </form>
    </div>
  );
}
