import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  api,
  connectTicketCategoryLabels,
  connectTicketStatusLabels,
  messagePriorityLabels,
  type ConnectTicketCategory,
  type ConnectTicketDetailDto,
  type ConnectTicketListItemDto,
  type ConnectTicketStatus,
  type ConnectTicketSummaryDto,
  type MessagePriority,
  type UserListDto,
} from '../../api/client';
import { FilterBar } from '../FilterBar';
import { KpiCard } from '../KpiCard';
import { Modal } from '../Modal';
import { formatBrDateTime } from '../../utils/dateUtils';
import { subscribeConnectSlaAlert, subscribeConnectTicketRefresh } from '../../offline/connectRealtimeSync';

const statusFlow: Partial<Record<ConnectTicketStatus, ConnectTicketStatus>> = {
  Aberto: 'EmAndamento',
  EmAndamento: 'Aguardando',
  Aguardando: 'Resolvido',
};

export function TicketsPanel({
  canWrite,
  initialMyRequests = false,
}: {
  canWrite: boolean;
  initialMyRequests?: boolean;
}) {
  const [summary, setSummary] = useState<ConnectTicketSummaryDto | null>(null);
  const [items, setItems] = useState<ConnectTicketListItemDto[]>([]);
  const [selected, setSelected] = useState<ConnectTicketDetailDto | null>(null);
  const [users, setUsers] = useState<UserListDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [assignedToMe, setAssignedToMe] = useState(false);
  const [myRequests, setMyRequests] = useState(initialMyRequests);
  const [search, setSearch] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [comment, setComment] = useState('');
  const [assignUserId, setAssignUserId] = useState('');
  const [error, setError] = useState('');
  const [form, setForm] = useState({
    titulo: '',
    descricao: '',
    categoria: 'TI' as ConnectTicketCategory,
    prioridade: 'Normal' as MessagePriority,
    responsavelId: '',
  });

  const load = useCallback(async () => {
    setError('');
    try {
      const [s, list] = await Promise.all([
        api.getConnectTicketSummary(),
        api.getConnectTickets({
          status: (statusFilter || undefined) as ConnectTicketStatus | undefined,
          category: (categoryFilter || undefined) as ConnectTicketCategory | undefined,
          assignedToMe: assignedToMe || undefined,
          myRequests: myRequests || undefined,
          search: search || undefined,
        }),
      ]);
      setSummary(s);
      setItems(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar chamados');
    }
  }, [statusFilter, categoryFilter, assignedToMe, myRequests, search]);

  useEffect(() => {
    load().catch(console.error);
    api.getUsers().then(setUsers).catch(console.error);
  }, [load]);

  useEffect(() => {
    const refresh = () => load().catch(console.error);
    const unsubTicket = subscribeConnectTicketRefresh(refresh);
    const unsubSla = subscribeConnectSlaAlert(refresh);
    return () => {
      unsubTicket();
      unsubSla();
    };
  }, [load]);

  async function openDetail(id: string) {
    setSelected(await api.getConnectTicketDetail(id));
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    await api.createConnectTicket({
      titulo: form.titulo,
      descricao: form.descricao,
      categoria: form.categoria,
      prioridade: form.prioridade,
      responsavelId: form.responsavelId || undefined,
    });
    setShowModal(false);
    setForm({ titulo: '', descricao: '', categoria: 'TI', prioridade: 'Normal', responsavelId: '' });
    await load();
  }

  async function advanceStatus(id: string, current: ConnectTicketStatus) {
    const next = statusFlow[current];
    if (!next) return;
    await api.changeConnectTicketStatus(id, next);
    await load();
    if (selected?.id === id) await openDetail(id);
  }

  async function handleAssign() {
    if (!selected || !assignUserId) return;
    await api.assignConnectTicket(selected.id, assignUserId);
    await load();
    await openDetail(selected.id);
  }

  async function handleComment(e: FormEvent) {
    e.preventDefault();
    if (!selected || !comment.trim()) return;
    await api.addConnectTicketComment(selected.id, comment.trim());
    setComment('');
    await openDetail(selected.id);
  }

  const overdueCount = useMemo(() => items.filter((i) => i.isOverdue).length, [items]);

  return (
    <>
      {summary ? (
        <div className="kpi-grid">
          <KpiCard label="Abertos" value={summary.totalAbertos} variant="primary" />
          <KpiCard label="Em andamento" value={summary.totalEmAndamento} variant="info" />
          <KpiCard label="Aguardando" value={summary.totalAguardando} variant="warning" />
          <KpiCard label="SLA vencido" value={summary.totalVencidos} variant="danger" />
        </div>
      ) : null}

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Chamados internos — {items.length} registro(s){overdueCount ? ` · ${overdueCount} vencido(s)` : ''}</span>
          {canWrite ? (
            <button type="button" className="btn btn-sm" onClick={() => setShowModal(true)}>+ Novo chamado</button>
          ) : null}
        </div>
        <FilterBar>
          <div className="filter-field w-lg">
            <label htmlFor="ticketStatus">Status</label>
            <select id="ticketStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(connectTicketStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="ticketCategory">Categoria</label>
            <select id="ticketCategory" value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
              <option value="">Todas</option>
              {Object.entries(connectTicketCategoryLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field">
            <label>
              <input type="checkbox" checked={assignedToMe} onChange={(e) => setAssignedToMe(e.target.checked)} /> Atribuídos a mim
            </label>
          </div>
          <div className="filter-field">
            <label>
              <input type="checkbox" checked={myRequests} onChange={(e) => setMyRequests(e.target.checked)} /> Meus chamados
            </label>
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="ticketSearch">Busca</label>
            <input id="ticketSearch" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Protocolo ou título…" />
          </div>
        </FilterBar>
        {error ? <p className="text-danger" style={{ padding: '0 1rem' }}>{error}</p> : null}
        <div className="card-panel-body" style={{ padding: 0, display: 'grid', gridTemplateColumns: selected ? '1fr 1fr' : '1fr' }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Protocolo</th>
                <th>Título</th>
                <th>Categoria</th>
                <th>Status</th>
                <th>SLA</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {items.map((t) => (
                <tr key={t.id} className={t.isOverdue ? 'row-danger' : undefined}>
                  <td>{t.protocolo}</td>
                  <td>{t.titulo}</td>
                  <td>{connectTicketCategoryLabels[t.categoria] ?? t.categoria}</td>
                  <td>
                    <span className={`badge ${t.isOverdue ? 'badge-danger' : ''}`}>
                      {connectTicketStatusLabels[t.status] ?? t.status}
                    </span>
                  </td>
                  <td>{t.dueAt ? formatBrDateTime(t.dueAt) : '—'}</td>
                  <td>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => openDetail(t.id).catch(console.error)}>
                      Ver
                    </button>
                    {canWrite && statusFlow[t.status] ? (
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        style={{ marginLeft: 4 }}
                        onClick={() => advanceStatus(t.id, t.status).catch(console.error)}
                      >
                        → {connectTicketStatusLabels[statusFlow[t.status]!]}
                      </button>
                    ) : null}
                  </td>
                </tr>
              ))}
              {items.length === 0 ? (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum chamado</td></tr>
              ) : null}
            </tbody>
          </table>

          {selected ? (
            <div className="connect-panel" style={{ padding: '1rem', borderLeft: '1px solid var(--border)' }}>
              <h3 style={{ marginTop: 0 }}>{selected.protocolo} — {selected.titulo}</h3>
              <div className="text-muted" style={{ fontSize: '0.85rem', marginBottom: '0.75rem' }}>
                {connectTicketCategoryLabels[selected.categoria]} · {messagePriorityLabels[selected.prioridade]} ·{' '}
                Solicitante: {selected.solicitanteName}
                {selected.responsavelName ? ` · Responsável: ${selected.responsavelName}` : ''}
                {selected.dueAt ? ` · SLA: ${formatBrDateTime(selected.dueAt)}` : ''}
              </div>
              <div style={{ whiteSpace: 'pre-wrap', marginBottom: '1rem' }}>{selected.descricao}</div>

              {selected.comments.length > 0 ? (
                <div style={{ marginBottom: '1rem' }}>
                  <strong>Histórico</strong>
                  <ul style={{ paddingLeft: '1.2rem' }}>
                    {selected.comments.map((c) => (
                      <li key={c.id}>
                        <span className="text-muted">{c.userName} · {formatBrDateTime(c.createdAt)}</span>
                        <div>{c.content}</div>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}

              {canWrite ? (
                <>
                  <form onSubmit={handleComment} style={{ display: 'flex', gap: 8, marginBottom: '1rem' }}>
                    <input value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Adicionar comentário…" style={{ flex: 1 }} />
                    <button type="submit" className="btn btn-secondary btn-sm">Comentar</button>
                  </form>
                  <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                    <select value={assignUserId} onChange={(e) => setAssignUserId(e.target.value)}>
                      <option value="">Atribuir a…</option>
                      {users.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
                    </select>
                    <button type="button" className="btn btn-secondary btn-sm" disabled={!assignUserId} onClick={() => handleAssign().catch(console.error)}>
                      Atribuir
                    </button>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => setSelected(null)}>Fechar</button>
                  </div>
                </>
              ) : null}
            </div>
          ) : null}
        </div>
      </div>

      <Modal open={showModal} onClose={() => setShowModal(false)} title="Novo chamado" width="md">
        <form onSubmit={handleCreate} className="form-grid">
          <div className="form-field full">
            <label htmlFor="ticketTitle">Título</label>
            <input id="ticketTitle" required value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="ticketDesc">Descrição</label>
            <textarea id="ticketDesc" required rows={4} value={form.descricao} onChange={(e) => setForm({ ...form, descricao: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="ticketCat">Categoria</label>
            <select id="ticketCat" value={form.categoria} onChange={(e) => setForm({ ...form, categoria: e.target.value as typeof form.categoria })}>
              {Object.entries(connectTicketCategoryLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="ticketPri">Prioridade</label>
            <select id="ticketPri" value={form.prioridade} onChange={(e) => setForm({ ...form, prioridade: e.target.value as MessagePriority })}>
              {Object.entries(messagePriorityLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="ticketAssign">Responsável (opcional)</label>
            <select id="ticketAssign" value={form.responsavelId} onChange={(e) => setForm({ ...form, responsavelId: e.target.value })}>
              <option value="">Definir depois</option>
              {users.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
            </select>
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Abrir chamado</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
