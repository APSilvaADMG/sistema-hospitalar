import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type FinancialSummaryDto } from '../../../api/client';
import { formatBrDate } from '../../../utils/dateUtils';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';
import { feegowFinanceInsertPath, feegowFinanceListPath, feegowFinanceSectionPath } from './feegowFinanceNav';

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function FeegowFinanceSummary() {
  const [summary, setSummary] = useState<FinancialSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [hideBalance, setHideBalance] = useState(false);
  const [balanceDate, setBalanceDate] = useState(() => new Date().toISOString().slice(0, 10));

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    api.getFinancialSummary()
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Erro ao carregar resumo.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, []);

  const generalBalance = summary
    ? summary.totalReceived - summary.totalPaidOut + summary.receivableOpen - summary.payableOpen
    : 0;

  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead title="painel principal" icon="📊" />

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}

      <section className="feegow-finance-panel feegow-finance-summary-balance">
        <div className="feegow-finance-summary-balance-head">
          <h2>Saldo Geral</h2>
          <button
            type="button"
            className="feegow-finance-eye-btn"
            onClick={() => setHideBalance((v) => !v)}
            aria-label={hideBalance ? 'Exibir saldo' : 'Ocultar saldo'}
          >
            {hideBalance ? '👁‍🗨' : '👁'}
          </button>
        </div>
        <div className="feegow-finance-summary-balance-row">
          <label className="feegow-finance-field-inline">
            <span>Referência</span>
            <input type="date" value={balanceDate} onChange={(e) => setBalanceDate(e.target.value)} />
          </label>
          <p className="feegow-finance-summary-balance-value">
            {loading ? '…' : hideBalance ? '••••••' : formatCurrency(generalBalance)}
          </p>
        </div>
      </section>

      <div className="feegow-finance-kpi-grid">
        <article className="feegow-finance-kpi-card">
          <span className="feegow-finance-kpi-label">A receber em aberto</span>
          <strong>{loading || !summary ? '—' : formatCurrency(summary.receivableOpen)}</strong>
          <Link to={feegowFinanceListPath('receber')}>Ver contas</Link>
        </article>
        <article className="feegow-finance-kpi-card">
          <span className="feegow-finance-kpi-label">A pagar em aberto</span>
          <strong>{loading || !summary ? '—' : formatCurrency(summary.payableOpen)}</strong>
          <Link to={feegowFinanceListPath('pagar')}>Ver contas</Link>
        </article>
        <article className="feegow-finance-kpi-card">
          <span className="feegow-finance-kpi-label">Propostas em aberto</span>
          <strong>{loading || !summary ? '—' : summary.openProposalsCount}</strong>
          <span className="feegow-finance-kpi-sub">
            {loading || !summary ? '—' : formatCurrency(summary.openProposalsBalance)}
          </span>
          <Link to={feegowFinanceSectionPath('propostas')}>Ver propostas</Link>
        </article>
        <article className="feegow-finance-kpi-card">
          <span className="feegow-finance-kpi-label">Honorários pendentes</span>
          <strong>{loading || !summary ? '—' : summary.openHonorariosCount}</strong>
          <span className="feegow-finance-kpi-sub">
            {loading || !summary ? '—' : formatCurrency(summary.openHonorariosBalance)}
          </span>
          <Link to={feegowFinanceSectionPath('honorarios')}>Ver honorários</Link>
        </article>
        <article className="feegow-finance-kpi-card">
          <span className="feegow-finance-kpi-label">Recebido no mês</span>
          <strong>{loading || !summary ? '—' : formatCurrency(summary.receivedThisMonth)}</strong>
        </article>
        <article className="feegow-finance-kpi-card">
          <span className="feegow-finance-kpi-label">Pago no mês</span>
          <strong>{loading || !summary ? '—' : formatCurrency(summary.paidOutThisMonth)}</strong>
        </article>
      </div>

      <section className="feegow-finance-panel">
        <header className="feegow-finance-panel-head">
          <h3>Atalhos</h3>
          <span className="feegow-finance-panel-sub">{formatBrDate(balanceDate)}</span>
        </header>
        <div className="feegow-finance-shortcuts">
          <Link to={feegowFinanceInsertPath('pagar')} className="feegow-finance-shortcut-btn">
            + Conta a pagar
          </Link>
          <Link to={feegowFinanceInsertPath('receber')} className="feegow-finance-shortcut-btn">
            + Conta a receber
          </Link>
          <Link to={feegowFinanceSectionPath('propostas')} className="feegow-finance-shortcut-btn is-secondary">
            Propostas
          </Link>
          <Link to={feegowFinanceSectionPath('honorarios')} className="feegow-finance-shortcut-btn is-secondary">
            Honorários
          </Link>
          <Link to={feegowFinanceSectionPath('tef')} className="feegow-finance-shortcut-btn is-secondary">
            TEF
          </Link>
          <Link to={feegowFinanceSectionPath('cheques')} className="feegow-finance-shortcut-btn is-secondary">
            Cheques
          </Link>
          <Link to={feegowFinanceListPath('pagar')} className="feegow-finance-shortcut-btn is-secondary">
            Listar a pagar
          </Link>
          <Link to={feegowFinanceListPath('receber')} className="feegow-finance-shortcut-btn is-secondary">
            Listar a receber
          </Link>
          <Link to={feegowFinanceSectionPath('caixas')} className="feegow-finance-shortcut-btn is-secondary">
            Caixas
          </Link>
        </div>
      </section>
    </div>
  );
}
