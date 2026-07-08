import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api, type FinancialCashSessionDto } from '../../../api/client';
import { formatBrDateTime } from '../../../utils/dateUtils';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function sessionStatusLabel(status: number | string): string {
  if (typeof status === 'number') return status === 1 ? 'Aberto' : 'Fechado';
  if (status === 'Open' || status === 'Aberto') return 'Aberto';
  return 'Fechado';
}

export function FeegowFinanceCashSessions() {
  const [openSession, setOpenSession] = useState<FinancialCashSessionDto | null>(null);
  const [sessions, setSessions] = useState<FinancialCashSessionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [openForm, setOpenForm] = useState({ label: 'Caixa principal', openingBalance: '0' });
  const [closeForm, setCloseForm] = useState({ closingBalance: '', notes: '' });

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [open, history] = await Promise.all([
        api.getOpenFinancialCashSession(),
        api.getFinancialCashSessions(50),
      ]);
      setOpenSession(open);
      setSessions(history);
      if (open) {
        setCloseForm((prev) => ({
          ...prev,
          closingBalance: String(open.expectedBalance),
        }));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar caixas.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleOpen(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await api.openFinancialCashSession({
        label: openForm.label.trim() || 'Caixa principal',
        openingBalance: Number(openForm.openingBalance) || 0,
      });
      setSuccess('Caixa aberto com sucesso.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao abrir caixa.');
    } finally {
      setSaving(false);
    }
  }

  async function handleClose(event: FormEvent) {
    event.preventDefault();
    if (!openSession) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await api.closeFinancialCashSession(openSession.id, {
        closingBalance: Number(closeForm.closingBalance) || 0,
        notes: closeForm.notes.trim() || undefined,
      });
      setSuccess('Caixa fechado com sucesso.');
      setCloseForm({ closingBalance: '', notes: '' });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao fechar caixa.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead title="Caixas" icon="🏧" />

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}

      {openSession ? (
        <section className="feegow-finance-panel feegow-finance-cash-open">
          <header className="feegow-finance-panel-head">
            <h3>Caixa aberto — {openSession.label}</h3>
            <span className="feegow-finance-panel-sub">
              desde {formatBrDateTime(openSession.openedAt)}
            </span>
          </header>
          <p className="feegow-finance-cash-hint">
            O saldo esperado reflete apenas o caixa físico: fundo de troco + dinheiro recebido − dinheiro pago (sangrias).
            Pix e cartões entram no movimento da recepção e na receita operacional do dia, mas não alteram o saldo em espécie.
          </p>
          <div className="feegow-finance-cash-kpi-grid">
            <div className="feegow-finance-cash-kpi">
              <span>Saldo inicial (fundo)</span>
              <strong>{formatCurrency(openSession.openingBalance)}</strong>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Recebido no caixa (dinheiro)</span>
              <strong>{formatCurrency(openSession.cashReceived)}</strong>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Pago em dinheiro (sangrias)</span>
              <strong>{formatCurrency(openSession.cashPaidOut)}</strong>
            </div>
            <div className="feegow-finance-cash-kpi feegow-finance-cash-kpi--highlight">
              <span>Saldo esperado (físico)</span>
              <strong>{formatCurrency(openSession.expectedBalance)}</strong>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Movimento recepção (dinheiro + Pix + cartões)</span>
              <strong>
                {formatCurrency(openSession.counterReceived - openSession.counterPaidOut)}
              </strong>
              <small>
                Entradas {formatCurrency(openSession.counterReceived)} · Saídas{' '}
                {formatCurrency(openSession.counterPaidOut)}
              </small>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Receita operacional do dia (todos os canais)</span>
              <strong>
                {formatCurrency(
                  openSession.dayOperationalReceived - openSession.dayOperationalPaidOut,
                )}
              </strong>
              <small>
                Entradas {formatCurrency(openSession.dayOperationalReceived)} · Saídas{' '}
                {formatCurrency(openSession.dayOperationalPaidOut)}
              </small>
            </div>
          </div>
          <form className="feegow-finance-cash-close-form" onSubmit={(e) => { void handleClose(e); }}>
            <label>
              Saldo informado no fechamento (contagem física)
              <input
                type="number"
                step="0.01"
                required
                value={closeForm.closingBalance}
                onChange={(e) => setCloseForm({ ...closeForm, closingBalance: e.target.value })}
              />
            </label>
            <label className="feegow-finance-field-wide">
              Observações
              <textarea
                rows={2}
                value={closeForm.notes}
                onChange={(e) => setCloseForm({ ...closeForm, notes: e.target.value })}
              />
            </label>
            <button type="submit" className="feegow-finance-filter-btn" disabled={saving}>
              Fechar caixa
            </button>
          </form>
        </section>
      ) : (
        <section className="feegow-finance-panel">
          <header className="feegow-finance-panel-head">
            <h3>Abrir caixa</h3>
          </header>
          <form className="feegow-finance-form-grid" onSubmit={(e) => { void handleOpen(e); }}>
            <label>
              Identificação
              <input
                type="text"
                value={openForm.label}
                onChange={(e) => setOpenForm({ ...openForm, label: e.target.value })}
              />
            </label>
            <label>
              Saldo inicial
              <input
                type="number"
                step="0.01"
                value={openForm.openingBalance}
                onChange={(e) => setOpenForm({ ...openForm, openingBalance: e.target.value })}
              />
            </label>
            <div className="feegow-finance-field-wide">
              <button type="submit" className="feegow-finance-filter-btn" disabled={saving}>
                Abrir caixa
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="feegow-finance-panel feegow-finance-table-card">
        <header className="feegow-finance-panel-head">
          <h3>Histórico de sessões</h3>
        </header>
        <div className="feegow-finance-table-wrap">
          <table className="feegow-finance-table">
            <thead>
              <tr>
                <th>Caixa</th>
                <th>Abertura</th>
                <th>Fechamento</th>
                <th>Inicial</th>
                <th>Esperado (físico)</th>
                <th>Informado</th>
                <th>Receita dia</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {sessions.map((session) => (
                <tr key={session.id}>
                  <td>{session.label}</td>
                  <td>{formatBrDateTime(session.openedAt)}</td>
                  <td>{session.closedAt ? formatBrDateTime(session.closedAt) : '—'}</td>
                  <td>{formatCurrency(session.openingBalance)}</td>
                  <td>{formatCurrency(session.expectedBalance)}</td>
                  <td>{session.closingBalance != null ? formatCurrency(session.closingBalance) : '—'}</td>
                  <td>
                    {formatCurrency(
                      session.dayOperationalReceived - session.dayOperationalPaidOut,
                    )}
                  </td>
                  <td>{sessionStatusLabel(session.status)}</td>
                </tr>
              ))}
              {!loading && sessions.length === 0 ? (
                <tr>
                  <td colSpan={8} className="feegow-finance-table-empty">
                    Nenhuma sessão registrada.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {loading ? <p className="feegow-finance-loading">Carregando...</p> : null}
      </section>
    </div>
  );
}
