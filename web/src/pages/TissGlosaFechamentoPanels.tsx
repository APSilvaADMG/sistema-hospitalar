import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  glosaContestationLabels,
  type TissGlosaDto,
  type TissGuideDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { formatBrDateTime } from '../utils/dateUtils';

function money(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

type GlosaRow = TissGlosaDto & {
  guideId: string;
  guideNumber: string;
  patientName: string;
  healthInsuranceName: string;
};

type Props = {
  tab: 'glosas' | 'recursos' | 'fechamento';
  guides: TissGuideDto[];
  canManage: boolean;
  onReload: () => Promise<void>;
  onMessage: (error: string, success: string) => void;
};

export function TissGlosaFechamentoPanels({ tab, guides, canManage, onReload, onMessage }: Props) {
  const [search, setSearch] = useState('');
  const [showResolved, setShowResolved] = useState(false);
  const [contestNotes, setContestNotes] = useState('');
  const [contestingGlosa, setContestingGlosa] = useState<GlosaRow | null>(null);

  const glosaRows = useMemo(() => {
    const rows: GlosaRow[] = [];
    for (const g of guides) {
      for (const gl of g.glosas) {
        rows.push({
          ...gl,
          guideId: g.id,
          guideNumber: g.guideNumber,
          patientName: g.patientName,
          healthInsuranceName: g.healthInsuranceName,
        });
      }
    }
    return rows
      .filter((r) => showResolved || !r.isResolved)
      .filter((r) => {
        if (tab === 'recursos' && (r.isResolved || r.contestationStatus !== 0)) return false;
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return r.guideNumber.toLowerCase().includes(term)
          || r.patientName.toLowerCase().includes(term)
          || r.reason.toLowerCase().includes(term)
          || r.healthInsuranceName.toLowerCase().includes(term);
      })
      .sort((a, b) => (a.isResolved === b.isResolved ? b.glosaAmount - a.glosaAmount : a.isResolved ? 1 : -1));
  }, [guides, search, showResolved, tab]);

  const draftGuides = useMemo(
    () => guides.filter((g) => g.status === 1),
    [guides],
  );

  const fechamentoStats = useMemo(() => ({
    pendingClose: draftGuides.filter((g) => !g.accountClosedAt).length,
    readyToSend: draftGuides.filter((g) => g.accountClosedAt).length,
    openGlosas: guides.flatMap((g) => g.glosas).filter((g) => !g.isResolved).length,
    openGlosaAmount: guides.flatMap((g) => g.glosas).filter((g) => !g.isResolved).reduce((s, g) => s + g.glosaAmount, 0),
  }), [guides, draftGuides]);

  async function runAction(action: () => Promise<unknown>, success: string) {
    onMessage('', '');
    try {
      await action();
      onMessage('', success);
      await onReload();
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Erro na operação.', '');
    }
  }

  if (tab === 'fechamento') {
    return (
      <>
        <div className="kpi-grid">
          <KpiCard label="Rascunhos pendentes" value={fechamentoStats.pendingClose} variant="warning" />
          <KpiCard label="Prontos para envio" value={fechamentoStats.readyToSend} variant="success" />
          <KpiCard label="Glosas abertas" value={fechamentoStats.openGlosas} variant="info" />
          <KpiCard label="Valor glosado" value={money(fechamentoStats.openGlosaAmount)} variant="warning" />
        </div>

        <div className="card-panel appt-panel" style={{ marginTop: 20 }}>
          <div className="card-panel-header">Fechamento de contas — RN-028</div>
          <div className="card-panel-body">
            <p className="form-hint" style={{ marginTop: 0 }}>
              Audite os itens TUSS e feche a conta antes de enviar a guia ao convênio ou gerar AIH SUS.
            </p>
          </div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Guia</th>
                  <th>Paciente</th>
                  <th>Convênio</th>
                  <th>Itens</th>
                  <th>Total</th>
                  <th>Conta</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {draftGuides.map((g) => {
                  const audited = g.items.filter((i) => i.isAudited).length;
                  return (
                    <tr key={g.id}>
                      <td><strong>{g.guideNumber}</strong></td>
                      <td>{g.patientName}</td>
                      <td>{g.healthInsuranceName}</td>
                      <td>{audited}/{g.items.length} auditado(s)</td>
                      <td>{money(g.totalAmount)}</td>
                      <td>
                        {g.accountClosedAt
                          ? <span className="badge badge-success">Fechada {formatBrDateTime(g.accountClosedAt)}</span>
                          : <span className="badge badge-warning">Aberta</span>}
                      </td>
                      <td>
                        <div className="table-actions">
                          {canManage && !g.accountClosedAt && (
                            <button
                              type="button"
                              className="btn btn-sm"
                              onClick={() => runAction(() => api.closeTissGuideAccount(g.id), 'Conta fechada — itens auditados.')}
                            >
                              Fechar conta
                            </button>
                          )}
                          {canManage && g.accountClosedAt && (
                            <button
                              type="button"
                              className="btn btn-sm"
                              onClick={() => runAction(() => api.sendTissGuide(g.id), 'Guia enviada ao convênio.')}
                            >
                              Enviar
                            </button>
                          )}
                          <Link to="/faturamento-tiss" className="btn btn-secondary btn-sm">Ver guia</Link>
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {draftGuides.length === 0 && (
                  <tr>
                    <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                      Nenhuma guia em rascunho aguardando fechamento.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </>
    );
  }

  const openAmount = glosaRows.filter((r) => !r.isResolved).reduce((s, r) => s + r.glosaAmount, 0);

  return (
    <>
      <div className="kpi-grid">
        <KpiCard label={tab === 'recursos' ? 'Recursos pendentes' : 'Glosas listadas'} value={glosaRows.length} variant="primary" />
        <KpiCard label="Valor em aberto" value={money(openAmount)} variant="warning" />
        <KpiCard label="Guias com glosa" value={new Set(glosaRows.map((r) => r.guideId)).size} variant="info" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 20 }}>
        <div className="card-panel-header">
          {tab === 'recursos' ? 'Central de recursos de glosa' : 'Central de glosas TISS'}
        </div>
        <FilterBar>
          <div className="filter-field grow">
            <label htmlFor="glosaSearch">Buscar</label>
            <input
              id="glosaSearch"
              placeholder="Guia, paciente, convênio ou motivo..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          {tab === 'glosas' && (
            <div className="filter-field checkbox align-end">
              <label>
                <input type="checkbox" checked={showResolved} onChange={(e) => setShowResolved(e.target.checked)} />
                {' '}Incluir resolvidas
              </label>
            </div>
          )}
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Guia</th>
                <th>Paciente</th>
                <th>Convênio</th>
                <th>Item</th>
                <th>Motivo</th>
                <th>Valor</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {glosaRows.map((gl) => (
                <tr key={gl.id}>
                  <td><strong>{gl.guideNumber}</strong></td>
                  <td>{gl.patientName}</td>
                  <td>{gl.healthInsuranceName}</td>
                  <td>{gl.itemDescription ?? 'Guia inteira'}</td>
                  <td>{gl.reason}{gl.ansGlosaCode ? ` (${gl.ansGlosaCode})` : ''}</td>
                  <td>{money(gl.glosaAmount)}</td>
                  <td>
                    {gl.isResolved
                      ? 'Resolvida'
                      : glosaContestationLabels[gl.contestationStatus] ?? 'Aberta'}
                  </td>
                  <td>
                    {canManage && !gl.isResolved && (
                      <div className="table-actions">
                        {tab === 'glosas' && (
                          <>
                            <button
                              type="button"
                              className="btn btn-sm"
                              onClick={() => runAction(() => api.resolveTissGlosa(gl.id), 'Glosa resolvida.')}
                            >
                              Resolver
                            </button>
                            {gl.contestationStatus === 0 && (
                              <button
                                type="button"
                                className="btn btn-secondary btn-sm"
                                onClick={() => setContestingGlosa(gl)}
                              >
                                Recurso
                              </button>
                            )}
                          </>
                        )}
                        {tab === 'recursos' && gl.contestationStatus === 0 && (
                          <button
                            type="button"
                            className="btn btn-sm"
                            onClick={() => setContestingGlosa(gl)}
                          >
                            Registrar recurso
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
              {glosaRows.length === 0 && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    {tab === 'recursos' ? 'Nenhum recurso de glosa pendente.' : 'Nenhuma glosa encontrada.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={!!contestingGlosa}
        title="Recurso de glosa"
        onClose={() => { setContestingGlosa(null); setContestNotes(''); }}
      >
        <form
          className="form-grid"
          onSubmit={(e) => {
            e.preventDefault();
            if (!contestingGlosa) return;
            runAction(
              () => api.contestTissGlosa(contestingGlosa.id, { contestationNotes: contestNotes }),
              'Recurso de glosa registrado.',
            ).then(() => { setContestingGlosa(null); setContestNotes(''); });
          }}
        >
          <div className="form-field full">
            <label>Guia {contestingGlosa?.guideNumber} — {contestingGlosa?.reason}</label>
            <textarea required rows={4} value={contestNotes} onChange={(e) => setContestNotes(e.target.value)} />
          </div>
          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setContestingGlosa(null)}>Cancelar</button>
            <button type="submit" className="btn">Enviar recurso</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
