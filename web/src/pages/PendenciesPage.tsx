import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type PendencyDto, type PendencySummaryDto } from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { formatBrDateTime } from '../utils/dateUtils';
import { subscribeHubNotificationRefresh } from '../offline/connectRealtimeSync';

const MODULE_OPTIONS = [
  { value: '', label: 'Todos os módulos' },
  { value: 'Inventory', label: 'Estoque' },
  { value: 'Guides', label: 'Guias' },
  { value: 'System', label: 'Sistema' },
  { value: 'Mail', label: 'E-mail' },
  { value: 'Tickets', label: 'Chamados' },
  { value: 'Tasks', label: 'Tarefas' },
];

const PRIORITY_OPTIONS = [
  { value: '', label: 'Todas as prioridades' },
  { value: 'Critica', label: 'Crítica' },
  { value: 'Alta', label: 'Alta' },
  { value: 'Normal', label: 'Normal' },
  { value: 'Baixa', label: 'Baixa' },
];

const priorityClass: Record<string, string> = {
  Critica: 'urgency-emergency',
  Alta: 'urgency-high',
  Normal: 'urgency-medium',
  Baixa: 'urgency-low',
};

export function PendenciesPage() {
  const [items, setItems] = useState<PendencyDto[]>([]);
  const [summary, setSummary] = useState<PendencySummaryDto | null>(null);
  const [modulo, setModulo] = useState('');
  const [prioridade, setPrioridade] = useState('');
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setError('');
    try {
      const [list, sum] = await Promise.all([
        api.getPendencies(modulo || undefined),
        api.getPendenciesSummary(),
      ]);
      setItems(list);
      setSummary(sum);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar pendências');
    }
  }, [modulo]);

  useEffect(() => {
    load().catch(console.error);
    return subscribeHubNotificationRefresh(() => load().catch(console.error));
  }, [load]);

  const filtered = useMemo(() => {
    if (!prioridade) return items;
    return items.filter((p) => p.prioridade === prioridade);
  }, [items, prioridade]);

  return (
    <div className="feegow-page-content" style={{ padding: '1.5rem' }}>
      <h1>Centro de Pendências</h1>
      <p className="text-muted">
        Itens que exigem sua atenção — sincronizados de chamados, e-mail, guias, estoque e fluxos clínicos.
      </p>

      {summary && (
        <div className="kpi-grid" style={{ marginTop: '1rem' }}>
          <KpiCard label="Total abertas" value={summary.abertas} variant="primary" />
          <KpiCard label="Vencidas" value={summary.vencidas} variant="danger" />
          <KpiCard label="Críticas / altas" value={summary.criticas} variant="warning" />
          <KpiCard label="Total em fila" value={summary.total} variant="neutral" />
        </div>
      )}

      <div className="filter-bar" style={{ marginTop: '1rem', display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
        <label>
          Módulo{' '}
          <select className="form-control" value={modulo} onChange={(e) => setModulo(e.target.value)}>
            {MODULE_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </label>
        <label>
          Prioridade{' '}
          <select className="form-control" value={prioridade} onChange={(e) => setPrioridade(e.target.value)}>
            {PRIORITY_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </label>
        <button type="button" className="btn btn-secondary btn-sm" style={{ alignSelf: 'flex-end' }} onClick={() => load()}>
          Atualizar
        </button>
      </div>

      {error ? <p className="text-danger">{error}</p> : null}
      {!error && filtered.length === 0 ? <p className="text-muted">Nenhuma pendência aberta.</p> : null}
      <ul className="connect-mail-list" style={{ marginTop: '1rem' }}>
        {filtered.map((p) => (
          <li key={p.id} className={`connect-mail-item${p.prioridade === 'Critica' ? ' unread' : ''}`}>
            <div>
              <strong>{p.titulo}</strong>
              <div style={{ marginTop: '0.25rem' }}>
                <span className={`badge ${priorityClass[p.prioridade] ?? ''}`}>{p.prioridade}</span>
                {' '}
                <span className="text-muted">{p.modulo}</span>
              </div>
              <div style={{ marginTop: '0.25rem' }}>{p.descricao}</div>
              <div className="text-muted" style={{ fontSize: '0.85rem' }}>
                {formatBrDateTime(p.dataAbertura)}
                {p.dataLimite ? ` · limite ${formatBrDateTime(p.dataLimite)}` : ''}
                {p.setor ? ` · ${p.setor}` : ''}
              </div>
            </div>
            {p.linkDestino ? (
              <Link to={p.linkDestino} className="btn btn-sm btn-outline-primary">
                Abrir
              </Link>
            ) : null}
          </li>
        ))}
      </ul>
    </div>
  );
}
