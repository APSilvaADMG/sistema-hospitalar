import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  helpArticleTypeLabels,
  helpSuggestionStatusLabels,
  type HelpArticleDetailDto,
  type HelpArticleListItemDto,
  type HelpSuggestionDto,
} from '../../api/client';

export function HelpArticleDetailView({
  slug,
  onBack,
  onTrainingComplete,
}: {
  slug: string;
  onBack: () => void;
  onTrainingComplete?: () => void;
}) {
  const [article, setArticle] = useState<HelpArticleDetailDto | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    api.getHelpArticle(slug)
      .then(setArticle)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar artigo'));
  }, [slug]);

  async function completeTraining() {
    if (!article) return;
    await api.markHelpTrainingComplete(article.id);
    onTrainingComplete?.();
    setArticle({ ...article, trainingCompleted: true });
  }

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!article) return <p style={{ color: 'var(--muted)' }}>Carregando…</p>;

  return (
    <div>
      <button type="button" className="btn btn-secondary btn-sm" onClick={onBack} style={{ marginBottom: 12 }}>
        ← Voltar
      </button>
      <p style={{ fontSize: 12, color: 'var(--muted)', margin: '0 0 8px' }}>
        {article.categoryName} · {helpArticleTypeLabels[article.type]}
      </p>
      <h2 style={{ margin: '0 0 12px', fontSize: 20 }}>{article.title}</h2>
      {article.type === 'Video' && article.videoUrl ? (
        <div className="help-video-embed">
          <iframe
            src={article.videoUrl}
            title={article.title}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
          />
        </div>
      ) : null}
      <div className="help-article-detail">{article.content}</div>
      {article.type === 'Manual' && article.downloadUrl ? (
        <p style={{ marginTop: 16 }}>
          <Link to={article.downloadUrl} className="btn btn-secondary">
            Baixar manual
          </Link>
        </p>
      ) : null}
      {article.type === 'Training' && !article.trainingCompleted ? (
        <div style={{ marginTop: 16 }}>
          <button type="button" className="btn" onClick={() => completeTraining().catch(console.error)}>
            Marcar treinamento como concluído
          </button>
        </div>
      ) : null}
      {article.trainingCompleted ? (
        <p style={{ marginTop: 12, color: 'var(--success, #059669)' }}>✓ Treinamento concluído</p>
      ) : null}
    </div>
  );
}

export function HelpArticleList({
  items,
  onSelect,
  emptyLabel = 'Nenhum item encontrado.',
}: {
  items: HelpArticleListItemDto[];
  onSelect: (slug: string) => void;
  emptyLabel?: string;
}) {
  if (items.length === 0) return <p style={{ color: 'var(--muted)' }}>{emptyLabel}</p>;
  return (
    <div className="help-article-list">
      {items.map((item) => (
        <button key={item.id} type="button" className="help-article-row" onClick={() => onSelect(item.slug)}>
          <div>
            <strong>{item.title}</strong>
            {item.summary ? <small>{item.summary}</small> : null}
          </div>
          <span style={{ fontSize: 11, color: 'var(--muted)', whiteSpace: 'nowrap' }}>
            {helpArticleTypeLabels[item.type]}
          </span>
        </button>
      ))}
    </div>
  );
}

export function HelpSuggestionsPanel() {
  const [items, setItems] = useState<HelpSuggestionDto[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [module, setModule] = useState('');
  const [msg, setMsg] = useState('');
  const [error, setError] = useState('');

  const load = useCallback(() => {
    api.getMyHelpSuggestions().then(setItems).catch(console.error);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setMsg('');
    try {
      await api.createHelpSuggestion({ title, description, module: module || undefined });
      setTitle('');
      setDescription('');
      setModule('');
      setMsg('Sugestão enviada. Obrigado pelo feedback!');
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enviar sugestão');
    }
  }

  return (
    <div style={{ display: 'grid', gap: 20 }}>
      <form className="card form-grid" onSubmit={submit}>
        <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Enviar sugestão de melhoria</h3>
        <div className="form-field full">
          <label>Título</label>
          <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={256} />
        </div>
        <div className="form-field full">
          <label>Descrição</label>
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} required rows={4} />
        </div>
        <div className="form-field">
          <label>Módulo (opcional)</label>
          <input value={module} onChange={(e) => setModule(e.target.value)} placeholder="Ex.: Pacientes, TISS" />
        </div>
        {error ? <div className="alert alert-danger full">{error}</div> : null}
        {msg ? <div className="alert alert-success full">{msg}</div> : null}
        <div className="form-actions full">
          <button type="submit" className="btn">Enviar sugestão</button>
        </div>
      </form>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">Minhas sugestões</div>
        <div className="card-panel-body">
          {items.length === 0 ? (
            <p style={{ color: 'var(--muted)', margin: 0 }}>Você ainda não enviou sugestões.</p>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>Módulo</th>
                  <th>Status</th>
                  <th>Data</th>
                </tr>
              </thead>
              <tbody>
                {items.map((s) => (
                  <tr key={s.id}>
                    <td><strong>{s.title}</strong><br /><small>{s.description}</small></td>
                    <td>{s.module ?? '—'}</td>
                    <td>{helpSuggestionStatusLabels[s.status]}</td>
                    <td>{new Date(s.createdAt).toLocaleDateString('pt-BR')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}

export function HelpSearchBar({
  value,
  onChange,
  onSearch,
}: {
  value: string;
  onChange: (v: string) => void;
  onSearch: () => void;
}) {
  return (
    <form
      className="help-hub-search"
      onSubmit={(e) => {
        e.preventDefault();
        onSearch();
      }}
    >
      <input
        type="search"
        placeholder="Buscar na Central de Ajuda…"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
      <button type="submit" className="btn">Buscar</button>
    </form>
  );
}

export function useHelpSearch() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<HelpArticleListItemDto[]>([]);
  const [searched, setSearched] = useState(false);

  const search = useCallback(async (q?: string) => {
    const term = (q ?? query).trim();
    if (!term) {
      setResults([]);
      setSearched(false);
      return;
    }
    const res = await api.searchHelp(term);
    setResults(res.items);
    setSearched(true);
  }, [query]);

  return { query, setQuery, results, searched, search };
}

export function HelpAssistantChat({ route }: { route?: string }) {
  const [question, setQuestion] = useState('');
  const [messages, setMessages] = useState<{ role: 'user' | 'bot'; text: string; links?: HelpArticleListItemDto[] }[]>([]);
  const [loading, setLoading] = useState(false);

  async function ask(q: string) {
    const trimmed = q.trim();
    if (!trimmed) return;
    setLoading(true);
    setQuestion('');
    setMessages((prev) => [...prev, { role: 'user', text: trimmed }]);
    try {
      const res = await api.askHelp({ question: trimmed, route });
      setMessages((prev) => [...prev, { role: 'bot', text: res.answer, links: res.relatedArticles }]);
    } catch {
      setMessages((prev) => [...prev, { role: 'bot', text: 'Não foi possível consultar a base de ajuda.' }]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <div className="help-assistant-messages">
        {messages.length === 0 ? (
          <p style={{ fontSize: 13, color: 'var(--muted)', margin: 0 }}>
            Pergunte sobre pacientes, TISS, SUS, financeiro e outros módulos.
          </p>
        ) : null}
        {messages.map((m, i) => (
          <div key={i} className={`help-assistant-msg ${m.role}`}>
            {m.text}
            {m.links?.length ? (
              <ul style={{ margin: '8px 0 0', paddingLeft: 16, fontSize: 12 }}>
                {m.links.map((a) => (
                  <li key={a.id}>
                    <Link to={`/ajuda/base?artigo=${encodeURIComponent(a.slug)}`}>{a.title}</Link>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        ))}
        {loading ? <p style={{ fontSize: 12, color: 'var(--muted)' }}>Buscando…</p> : null}
      </div>
      <form
        className="help-assistant-form"
        onSubmit={(e) => {
          e.preventDefault();
          ask(question).catch(console.error);
        }}
      >
        <input
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="Sua dúvida…"
          disabled={loading}
        />
        <button type="submit" className="btn btn-sm" disabled={loading}>Enviar</button>
      </form>
    </>
  );
}
