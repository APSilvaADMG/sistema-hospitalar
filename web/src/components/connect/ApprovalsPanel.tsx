import { type FormEvent, useCallback, useEffect, useState } from 'react';
import {
  api,
  workflowInstanceStatusLabels,
  workflowStepStatusLabels,
  workflowTypeLabels,
  type UserListDto,
  type WorkflowInstanceDetailDto,
  type WorkflowInstanceListItemDto,
  type WorkflowSummaryDto,
  type WorkflowType,
} from '../../api/client';
import { FilterBar } from '../FilterBar';
import { KpiCard } from '../KpiCard';
import { Modal } from '../Modal';
import { formatBrDateTime } from '../../utils/dateUtils';

export function ApprovalsPanel({ canWrite, canApprove }: { canWrite: boolean; canApprove: boolean }) {
  const [summary, setSummary] = useState<WorkflowSummaryDto | null>(null);
  const [items, setItems] = useState<WorkflowInstanceListItemDto[]>([]);
  const [selected, setSelected] = useState<WorkflowInstanceDetailDto | null>(null);
  const [users, setUsers] = useState<UserListDto[]>([]);
  const [pendingForMe, setPendingForMe] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [justificativa, setJustificativa] = useState('');
  const [error, setError] = useState('');
  const [form, setForm] = useState({
    tipo: 'SolicitacaoCompra' as WorkflowType,
    titulo: '',
    descricao: '',
    referencia: '',
    aprovadorIds: [] as string[],
  });

  const load = useCallback(async () => {
    setError('');
    try {
      const [s, list] = await Promise.all([
        api.getConnectApprovalSummary(),
        api.getConnectApprovals({ pendingForMe: pendingForMe || undefined }),
      ]);
      setSummary(s);
      setItems(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar aprovações');
    }
  }, [pendingForMe]);

  useEffect(() => {
    load().catch(console.error);
    api.getUsers().then(setUsers).catch(console.error);
  }, [load]);

  async function openDetail(id: string) {
    setSelected(await api.getConnectApprovalDetail(id));
    setJustificativa('');
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (form.aprovadorIds.length === 0) {
      setError('Selecione ao menos um aprovador.');
      return;
    }
    await api.createConnectApproval({
      tipo: form.tipo,
      titulo: form.titulo,
      descricao: form.descricao,
      referencia: form.referencia || undefined,
      aprovadorIds: form.aprovadorIds,
    });
    setShowModal(false);
    setForm({ tipo: 'SolicitacaoCompra', titulo: '', descricao: '', referencia: '', aprovadorIds: [] });
    await load();
  }

  async function handleApprove() {
    if (!selected) return;
    await api.approveConnectApproval(selected.id, justificativa || undefined);
    await load();
    await openDetail(selected.id);
  }

  async function handleReject() {
    if (!selected) return;
    await api.rejectConnectApproval(selected.id, justificativa || 'Rejeitado');
    await load();
    await openDetail(selected.id);
  }

  const pendingStep = selected?.steps.find((s) => s.status === 'Pendente');

  return (
    <>
      {summary ? (
        <div className="kpi-grid">
          <KpiCard label="Pendentes para mim" value={summary.pendentesParaMim} variant="warning" />
          <KpiCard label="Minhas solicitações" value={summary.minhasPendentes} variant="primary" />
          <KpiCard label="Aprovadas (mês)" value={summary.aprovadasMes} variant="success" />
          <KpiCard label="Rejeitadas (mês)" value={summary.rejeitadasMes} variant="danger" />
        </div>
      ) : null}

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Workflow de aprovações — {items.length} registro(s)</span>
          {canWrite ? (
            <button type="button" className="btn btn-sm" onClick={() => setShowModal(true)}>+ Nova solicitação</button>
          ) : null}
        </div>
        <FilterBar>
          <div className="filter-field">
            <label>
              <input type="checkbox" checked={pendingForMe} onChange={(e) => setPendingForMe(e.target.checked)} /> Pendentes para mim
            </label>
          </div>
        </FilterBar>
        {error ? <p className="text-danger" style={{ padding: '0 1rem' }}>{error}</p> : null}
        <div className="card-panel-body" style={{ padding: 0, display: 'grid', gridTemplateColumns: selected ? '1fr 1fr' : '1fr' }}>
          <table className="data-table">
            <thead>
              <tr><th>Tipo</th><th>Título</th><th>Solicitante</th><th>Status</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {items.map((i) => (
                <tr key={i.id} className={i.pendingForMe ? 'row-warning' : undefined}>
                  <td>{workflowTypeLabels[i.tipo] ?? i.tipo}</td>
                  <td>{i.titulo}</td>
                  <td>{i.solicitanteName}</td>
                  <td>
                    <span className={`badge ${i.pendingForMe ? 'badge-warning' : ''}`}>
                      {workflowInstanceStatusLabels[i.status] ?? i.status}
                    </span>
                  </td>
                  <td>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => openDetail(i.id).catch(console.error)}>
                      Ver
                    </button>
                  </td>
                </tr>
              ))}
              {items.length === 0 ? (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma solicitação</td></tr>
              ) : null}
            </tbody>
          </table>

          {selected ? (
            <div className="connect-panel" style={{ padding: '1rem', borderLeft: '1px solid var(--border)' }}>
              <h3 style={{ marginTop: 0 }}>{selected.titulo}</h3>
              <div className="text-muted" style={{ fontSize: '0.85rem', marginBottom: '0.75rem' }}>
                {workflowTypeLabels[selected.tipo]} · {selected.solicitanteName} · {formatBrDateTime(selected.createdAt)}
                {selected.referencia ? ` · Ref: ${selected.referencia}` : ''}
              </div>
              <div style={{ whiteSpace: 'pre-wrap', marginBottom: '1rem' }}>{selected.descricao}</div>

              <strong>Etapas</strong>
              <ol style={{ paddingLeft: '1.2rem' }}>
                {selected.steps.map((s) => (
                  <li key={s.id}>
                    {s.aprovadorName} — {workflowStepStatusLabels[s.status] ?? s.status}
                    {s.justificativa ? ` (${s.justificativa})` : ''}
                  </li>
                ))}
              </ol>

              {canApprove && selected.status === 'Pendente' && pendingStep ? (
                <div style={{ marginTop: '1rem' }}>
                  <textarea
                    rows={2}
                    placeholder="Justificativa (obrigatória para rejeição)…"
                    value={justificativa}
                    onChange={(e) => setJustificativa(e.target.value)}
                    style={{ width: '100%', marginBottom: 8 }}
                  />
                  <div style={{ display: 'flex', gap: 8 }}>
                    <button type="button" className="btn btn-sm" onClick={() => handleApprove().catch(console.error)}>Aprovar</button>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleReject().catch(console.error)}>Rejeitar</button>
                  </div>
                </div>
              ) : null}

              <button type="button" className="btn btn-secondary btn-sm" style={{ marginTop: '1rem' }} onClick={() => setSelected(null)}>
                Fechar
              </button>
            </div>
          ) : null}
        </div>
      </div>

      <Modal open={showModal} onClose={() => setShowModal(false)} title="Nova solicitação de aprovação" width="md">
        <form onSubmit={handleCreate} className="form-grid">
          <div className="form-field">
            <label htmlFor="wfType">Tipo</label>
            <select id="wfType" value={form.tipo} onChange={(e) => setForm({ ...form, tipo: e.target.value as WorkflowType })}>
              {Object.entries(workflowTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="wfRef">Referência</label>
            <input id="wfRef" value={form.referencia} onChange={(e) => setForm({ ...form, referencia: e.target.value })} placeholder="Ex: NF-1234" />
          </div>
          <div className="form-field full">
            <label htmlFor="wfTitle">Título</label>
            <input id="wfTitle" required value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="wfDesc">Descrição</label>
            <textarea id="wfDesc" required rows={3} value={form.descricao} onChange={(e) => setForm({ ...form, descricao: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="wfApprovers">Aprovadores (ordem de aprovação)</label>
            <select
              id="wfApprovers"
              multiple
              size={5}
              value={form.aprovadorIds}
              onChange={(e) => setForm({
                ...form,
                aprovadorIds: Array.from(e.target.selectedOptions).map((o) => o.value),
              })}
            >
              {users.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
            </select>
            <span className="text-muted" style={{ fontSize: '0.8rem' }}>Segure Ctrl para selecionar múltiplos.</span>
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Enviar solicitação</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
