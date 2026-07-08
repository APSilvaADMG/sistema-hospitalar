import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, type FinancialCashSessionDto } from '../../../api/client';
import { formatBrDateTime } from '../../../utils/dateUtils';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function isClosedSession(session: FinancialCashSessionDto): boolean {
  if (typeof session.status === 'number') return session.status !== 1;
  return session.status !== 'Open' && session.status !== 'Aberto';
}

export function FeegowFinanceClosingPanel() {
  const [sessions, setSessions] = useState<FinancialCashSessionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setSessions(await api.getFinancialCashSessions(100));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar fechamentos.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const closedSessions = useMemo(
    () => sessions.filter(isClosedSession),
    [sessions],
  );

  const summary = useMemo(() => {
    return closedSessions.reduce(
      (acc, session) => {
        acc.expected += session.expectedBalance;
        acc.informed += session.closingBalance ?? 0;
        acc.dayRevenue +=
          session.dayOperationalReceived - session.dayOperationalPaidOut;
        return acc;
      },
      { expected: 0, informed: 0, dayRevenue: 0 },
    );
  }, [closedSessions]);

  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead title="Fechamento de Data" />
      <p className="feegow-finance-lead" style={{ marginTop: 0, color: 'var(--muted)' }}>
        Histórico de fechamentos de caixa por data operacional. O saldo esperado é o caixa físico;
        a receita operacional do dia inclui todos os canais de pagamento.
      </p>

      <section className="feegow-finance-panel">
        {error && <div className="alert alert-error">{error}</div>}
        {loading && <p>Carregando fechamentos…</p>}

        {closedSessions.length > 0 && !loading ? (
          <div className="feegow-finance-cash-kpi-grid feegow-finance-closing-summary">
            <div className="feegow-finance-cash-kpi">
              <span>Total saldo esperado (físico)</span>
              <strong>{formatCurrency(summary.expected)}</strong>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Total saldo informado</span>
              <strong>{formatCurrency(summary.informed)}</strong>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Diferença acumulada</span>
              <strong style={{ color: summary.informed - summary.expected !== 0 ? 'var(--danger)' : undefined }}>
                {formatCurrency(summary.informed - summary.expected)}
              </strong>
            </div>
            <div className="feegow-finance-cash-kpi">
              <span>Receita operacional do dia (soma)</span>
              <strong>{formatCurrency(summary.dayRevenue)}</strong>
            </div>
          </div>
        ) : null}

        <div className="guides-table-wrap">
          <div className="guides-table-scroll">
            <table className="guides-data-table">
              <thead>
                <tr>
                  <th>Caixa</th>
                  <th>Abertura</th>
                  <th>Fechamento</th>
                  <th>Saldo inicial</th>
                  <th>Saldo esperado (físico)</th>
                  <th>Saldo informado</th>
                  <th>Diferença</th>
                  <th>Receita operacional do dia</th>
                </tr>
              </thead>
              <tbody>
                {closedSessions.map((session) => {
                  const diff = (session.closingBalance ?? 0) - session.expectedBalance;
                  const dayRevenue =
                    session.dayOperationalReceived - session.dayOperationalPaidOut;
                  return (
                    <tr key={session.id}>
                      <td><strong>{session.label}</strong></td>
                      <td>{formatBrDateTime(session.openedAt)}</td>
                      <td>{session.closedAt ? formatBrDateTime(session.closedAt) : '—'}</td>
                      <td>{formatCurrency(session.openingBalance)}</td>
                      <td>{formatCurrency(session.expectedBalance)}</td>
                      <td>{formatCurrency(session.closingBalance ?? 0)}</td>
                      <td style={{ color: diff !== 0 ? 'var(--danger)' : undefined }}>
                        {formatCurrency(diff)}
                      </td>
                      <td>{formatCurrency(dayRevenue)}</td>
                    </tr>
                  );
                })}
                {closedSessions.length === 0 && !loading && (
                  <tr>
                    <td colSpan={8} className="guides-table-empty">Nenhum fechamento registrado.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </section>
    </div>
  );
}
