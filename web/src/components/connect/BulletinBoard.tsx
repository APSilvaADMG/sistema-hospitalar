import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api, type BulletinPostDto, type CreateBulletinPostRequest } from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';
import { useAuth } from '../../auth/AuthContext';

export function BulletinBoard() {
  const { hasPermission } = useAuth();
  const canAdmin = hasPermission('connect.admin');
  const [posts, setPosts] = useState<BulletinPostDto[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [isPinned, setIsPinned] = useState(false);

  const load = useCallback(async () => {
    setError('');
    try {
      setPosts(await api.getConnectBulletinPosts());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar mural');
    }
  }, []);

  useEffect(() => {
    load().catch(console.error);
  }, [load]);

  async function markViewed(id: string) {
    await api.markConnectBulletinViewed(id);
    await load();
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    const payload: CreateBulletinPostRequest = {
      title: title.trim(),
      content: content.trim(),
      isPinned,
      publishNow: true,
    };
    await api.createConnectBulletinPost(payload);
    setShowForm(false);
    setTitle('');
    setContent('');
    setIsPinned(false);
    await load();
  }

  return (
    <div className="connect-panel">
      {error ? <p className="text-danger">{error}</p> : null}
      {canAdmin ? (
        <div style={{ marginBottom: '1rem' }}>
          <button type="button" className="btn btn-primary btn-sm" onClick={() => setShowForm((v) => !v)}>
            {showForm ? 'Cancelar' : 'Novo comunicado'}
          </button>
          {showForm ? (
            <form onSubmit={(e) => handleCreate(e).catch(console.error)} style={{ marginTop: '0.75rem' }}>
              <label>
                Título
                <input value={title} onChange={(e) => setTitle(e.target.value)} required />
              </label>
              <label>
                Conteúdo
                <textarea rows={4} value={content} onChange={(e) => setContent(e.target.value)} required />
              </label>
              <label style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                <input type="checkbox" checked={isPinned} onChange={(e) => setIsPinned(e.target.checked)} />
                Fixar no topo
              </label>
              <button type="submit" className="btn btn-primary btn-sm">
                Publicar
              </button>
            </form>
          ) : null}
        </div>
      ) : null}
      {posts.length === 0 ? (
        <p className="text-muted">Nenhum comunicado publicado.</p>
      ) : (
        <ul className="connect-mail-list">
          {posts.map((p) => (
            <li
              key={p.id}
              className="connect-mail-item"
              onClick={() => !p.isViewed && markViewed(p.id).catch(console.error)}
            >
              <div>
                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                  {p.isPinned ? <span className="connect-priority">Fixado</span> : null}
                  <strong>{p.title}</strong>
                  {!p.isViewed ? <span className="feegow-badge-novo">Novo</span> : null}
                </div>
                <div style={{ marginTop: '0.5rem', whiteSpace: 'pre-wrap' }}>{p.content}</div>
                <div className="text-muted" style={{ fontSize: '0.85rem', marginTop: '0.5rem' }}>
                  {p.authorName} · {p.publishedAt ? formatBrDateTime(p.publishedAt) : '—'} ·{' '}
                  {p.viewCount} visualizações
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
