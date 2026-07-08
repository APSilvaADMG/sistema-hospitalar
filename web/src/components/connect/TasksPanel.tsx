import { type FormEvent, useCallback, useEffect, useState } from 'react';
import {
  api,
  connectTaskStatusLabels,
  messagePriorityLabels,
  type ConnectTaskDetailDto,
  type ConnectTaskListItemDto,
  type ConnectTaskStatus,
  type ConnectTaskSummaryDto,
  type MessagePriority,
  type UserListDto,
} from '../../api/client';
import { FilterBar } from '../FilterBar';
import { KpiCard } from '../KpiCard';
import { Modal } from '../Modal';
import { formatBrDateTime } from '../../utils/dateUtils';
import { subscribeConnectTaskRefresh } from '../../offline/connectRealtimeSync';

const statusFlow: Partial<Record<ConnectTaskStatus, ConnectTaskStatus>> = {
  Aberta: 'EmAndamento',
  EmAndamento: 'Concluida',
};

export function TasksPanel({ canWrite }: { canWrite: boolean }) {
  const [summary, setSummary] = useState<ConnectTaskSummaryDto | null>(null);
  const [items, setItems] = useState<ConnectTaskListItemDto[]>([]);
  const [selected, setSelected] = useState<ConnectTaskDetailDto | null>(null);
  const [users, setUsers] = useState<UserListDto[]>([]);
  const [scope, setScope] = useState<'all' | 'mine' | 'delegated'>('all');
  const [statusFilter, setStatusFilter] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [error, setError] = useState('');
  const [form, setForm] = useState({
    titulo: '',
    descricao: '',
    responsavelId: '',
    prazo: '',
    prioridade: 'Normal' as MessagePriority,
  });

  const load = useCallback(async () => {
    setError('');
    try {
      const [s, list] = await Promise.all([
        api.getConnectTaskSummary(),
        api.getConnectTasks({ scope, status: (statusFilter || undefined) as ConnectTaskStatus | undefined }),
      ]);
      setSummary(s);
      setItems(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar tarefas');
    }
  }, [scope, statusFilter]);

  useEffect(() => {
    load().catch(console.error);
    api.getUsers().then(setUsers).catch(console.error);
  }, [load]);

  useEffect(() => subscribeConnectTaskRefresh(() => load().catch(console.error)), [load]);

  async function openDetail(id: string) {
    setSelected(await api.getConnectTaskDetail(id));
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    await api.createConnectTask({
      titulo: form.titulo,
      descricao: form.descricao,
      responsavelId: form.responsavelId || undefined,
      prazo: form.prazo ? new Date(form.prazo).toISOString() : undefined,
      prioridade: form.prioridade,
    });
    setShowModal(false);
    setForm({ titulo: '', descricao: '', responsavelId: '', prazo: '', prioridade: 'Normal' });
    await load();
  }

  async function advanceStatus(id: string, current: ConnectTaskStatus) {
    const next = statusFlow[current];
    if (!next) return;
    await api.changeConnectTaskStatus(id, next);
    await load();
    if (selected?.id === id) await openDetail(id);
  }

  return (
    <>
      {summary ? (
        <div className="kpi-grid">
          <KpiCard label="Minhas abertas" value={summary.minhasAbertas} variant="primary" />
          <KpiCard label="Delegadas" value={summary.delegadasAbertas} variant="info" />
          <KpiCard label="Vencidas" value={summary.vencidas} variant="danger" />
          <KpiCard label="Concluídas (mês)" value={summary.concluidasMes} variant="success" />
        </div>
      ) : null}

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Gestão de tarefas — {items.length} registro(s)</span>
          {canWrite ? (
            <button type="button" className="btn btn-sm" onClick={() => setShowModal(true)}>+ Nova tarefa</button>
          ) : null}
        </div>
        <FilterBar>
          <div className="filter-field w-lg">
            <label htmlFor="taskScope">Escopo</label>
            <select id="taskScope" value={scope} onChange={(e) => setScope(e.target.value as typeof scope)}>
              <option value="all">Todas (minhas + delegadas)</option>
              <option value="mine">Minhas tarefas</option>
              <option value="delegated">Delegadas por mim</option>
            </select>
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="taskStatus">Status</label>
            <select id="taskStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(connectTaskStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
        </FilterBar>
        {error ? <p className="text-danger" style={{ padding: '0 1rem' }}>{error}</p> : null}
        <div className="card-panel-body" style={{ padding: 0, display: 'grid', gridTemplateColumns: selected ? '1fr 1fr' : '1fr' }}>
          <table className="data-table">
            <thead>
              <tr><th>Título</th><th>Responsável</th><th>Prazo</th><th>Status</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {items.map((t) => (
                <tr key={t.id} className={t.isOverdue ? 'row-danger' : undefined}>
                  <td>{t.titulo}</td>
                  <td>{t.responsavelName ?? '—'}</td>
                  <td>{t.prazo ? formatBrDateTime(t.prazo) : '—'}</td>
                  <td>
                    <span className={`badge ${t.isOverdue ? 'badge-danger' : ''}`}>
                      {connectTaskStatusLabels[t.status] ?? t.status}
                    </span>
                  </td>
                  <td>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => openDetail(t.id).catch(console.error)}>Ver</button>
                    {canWrite && statusFlow[t.status] ? (
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        style={{ marginLeft: 4 }}
                        onClick={() => advanceStatus(t.id, t.status).catch(console.error)}
                      >
                        → {connectTaskStatusLabels[statusFlow[t.status]!]}
                      </button>
                    ) : null}
                  </td>
                </tr>
              ))}
              {items.length === 0 ? (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma tarefa</td></tr>
              ) : null}
            </tbody>
          </table>

          {selected ? (
            <div className="connect-panel" style={{ padding: '1rem', borderLeft: '1px solid var(--border)' }}>
              <h3 style={{ marginTop: 0 }}>{selected.titulo}</h3>
              <div className="text-muted" style={{ fontSize: '0.85rem', marginBottom: '0.75rem' }}>
                Criador: {selected.criadorName} · Responsável: {selected.responsavelName ?? '—'} ·{' '}
                {messagePriorityLabels[selected.prioridade]}
                {selected.prazo ? ` · Prazo: ${formatBrDateTime(selected.prazo)}` : ''}
              </div>
              <div style={{ whiteSpace: 'pre-wrap' }}>{selected.descricao}</div>
              <button type="button" className="btn btn-secondary btn-sm" style={{ marginTop: '1rem' }} onClick={() => setSelected(null)}>
                Fechar
              </button>
            </div>
          ) : null}
        </div>
      </div>

      <Modal open={showModal} onClose={() => setShowModal(false)} title="Nova tarefa" width="md">
        <form onSubmit={handleCreate} className="form-grid">
          <div className="form-field full">
            <label htmlFor="taskTitle">Título</label>
            <input id="taskTitle" required value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="taskDesc">Descrição</label>
            <textarea id="taskDesc" required rows={3} value={form.descricao} onChange={(e) => setForm({ ...form, descricao: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="taskAssign">Responsável</label>
            <select id="taskAssign" value={form.responsavelId} onChange={(e) => setForm({ ...form, responsavelId: e.target.value })}>
              <option value="">Eu mesmo</option>
              {users.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="taskDue">Prazo</label>
            <input id="taskDue" type="datetime-local" value={form.prazo} onChange={(e) => setForm({ ...form, prazo: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="taskPri">Prioridade</label>
            <select id="taskPri" value={form.prioridade} onChange={(e) => setForm({ ...form, prioridade: e.target.value as MessagePriority })}>
              {Object.entries(messagePriorityLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Criar tarefa</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
