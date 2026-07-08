import { useEffect, useMemo, useState } from 'react';

import { Link } from 'react-router-dom';

import {

  api,

  type FinancialAccountDto,

  type FinancialSummaryDto,

  financialDirectionValue,

} from '../../api/client';

import { ModuleNav } from '../../components/ModuleNav';

import {

  FINANCIAL_FUNCTIONAL_GROUPS,

  getFinancialGroupBySlug,

} from '../../data/financialFunctionalGroups';

import { financialTabs } from '../../navigation/moduleSections';

import { useModuleSection } from '../../navigation/useModuleSection';

import { FinancialPage } from '../FinancialPage';



const GROUP_PATHS: Record<string, string> = {

  receber: '/financeiro/receber/convenios',

  pagar: '/financeiro/pagar/fornecedores',

  tesouraria: '/financeiro/tesouraria/caixa',

  fiscal: '/financeiro/fiscal/notas',

  cobrancas: '/financeiro/cobrancas',

  'recibos-diversos': '/financeiro/recibos-diversos',

  boletos: '/financeiro/boletos',

};



function money(value: number) {

  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

}



function FinancialHubOverview() {

  const { section } = useModuleSection('/financeiro');

  const sectionGroup = getFinancialGroupBySlug(section || undefined);

  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(

    () => sectionGroup?.id ?? null,

  );

  const [summary, setSummary] = useState<FinancialSummaryDto | null>(null);

  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);

  const [loading, setLoading] = useState(true);



  const activeGroup = useMemo(

    () => FINANCIAL_FUNCTIONAL_GROUPS.find((g) => g.id === selectedGroupId) ?? null,

    [selectedGroupId],

  );



  useEffect(() => {

    const group = getFinancialGroupBySlug(section || undefined);

    if (group) setSelectedGroupId(group.id);

  }, [section]);



  useEffect(() => {

    setLoading(true);

    Promise.all([

      api.getFinancialSummary(),

      api.getFinancialAccounts(undefined, undefined, 1, 1),

      api.getFinancialAccounts(undefined, undefined, 1, 2),

    ])

      .then(([sum, receivable, payable]) => {

        setSummary(sum);

        setAccounts([...receivable.items, ...payable.items]);

      })

      .catch(console.error)

      .finally(() => setLoading(false));

  }, []);



  const groupRows = useMemo(() => {

    if (!selectedGroupId) {

      return accounts

        .filter((a) => a.balance > 0)

        .slice(0, 15);

    }

    if (selectedGroupId === 'receber') {

      return accounts.filter((a) => financialDirectionValue(a.direction) === 1).slice(0, 15);

    }

    if (selectedGroupId === 'pagar') {

      return accounts.filter((a) => financialDirectionValue(a.direction) === 2).slice(0, 15);

    }

    if (selectedGroupId === 'recibos-diversos') {

      return accounts

        .filter((a) => (a.description + (a.notes ?? '')).toLowerCase().includes('recibo'))

        .slice(0, 15);

    }

    return accounts.slice(0, 15);

  }, [accounts, selectedGroupId]);



  const groupOpenTotal = useMemo(

    () => groupRows.reduce((sum, row) => sum + row.balance, 0),

    [groupRows],

  );



  return (

    <div className="page-content guides-bayanno-page">

      <ModuleNav basePath="/financeiro" tabs={financialTabs} contextId="financial" />



      <div className="box">

        <div className="box-content padded">

          <div className="guides-toolbar">

            <h2 style={{ margin: 0, flex: 1 }}>Gestão Financeira — Hub</h2>

            <Link to="/financeiro/receber/convenios" className="btn btn-secondary">Receber</Link>

            <Link to="/financeiro/pagar/fornecedores" className="btn btn-secondary">Pagar</Link>

            <Link to="/relatorios/financeiro" className="btn btn-secondary">Relatórios</Link>

          </div>



          {summary && (

            <div className="guides-kpi-row">

              <div className="guides-kpi-card">

                <strong>{money(summary.receivableOpen)}</strong>

                <span>A receber (aberto)</span>

              </div>

              <div className="guides-kpi-card">

                <strong>{money(summary.payableOpen)}</strong>

                <span>A pagar (aberto)</span>

              </div>

              <div className="guides-kpi-card">

                <strong>{money(summary.receivedThisMonth)}</strong>

                <span>Recebido no mês</span>

              </div>

              <div className="guides-kpi-card">

                <strong>{money(summary.paidOutThisMonth)}</strong>

                <span>Pago no mês</span>

              </div>

              <Link to="/financeiro/propostas" className="guides-kpi-card guides-kpi-card-link">

                <strong>{summary.openProposalsCount}</strong>

                <span>Propostas em aberto</span>

                <small>{money(summary.openProposalsBalance)}</small>

              </Link>

              <Link to="/financeiro/honorarios" className="guides-kpi-card guides-kpi-card-link">

                <strong>{summary.openHonorariosCount}</strong>

                <span>Honorários pendentes</span>

                <small>{money(summary.openHonorariosBalance)}</small>

              </Link>

            </div>

          )}



          <div className="guides-bayanno-layout">

            <nav className="guides-module-nav" aria-label="Grupos financeiros">

              <div className="guides-module-nav-head">Grupos funcionais</div>

              <button

                type="button"

                className={`guides-module-btn${selectedGroupId === null ? ' active' : ''}`}

                onClick={() => setSelectedGroupId(null)}

              >

                Visão consolidada

                <span className="guides-module-btn-desc">Receitas, despesas e saldo</span>

              </button>

              {FINANCIAL_FUNCTIONAL_GROUPS.map((group) => (

                <button

                  key={group.id}

                  type="button"

                  className={`guides-module-btn${selectedGroupId === group.id ? ' active' : ''}`}

                  onClick={() => setSelectedGroupId(group.id)}

                >

                  {group.label}

                  <span className="guides-module-btn-desc">{group.description}</span>

                </button>

              ))}

            </nav>



            <div className="guides-main-panel">

              <div className="guides-table-meta" style={{ marginBottom: 12 }}>

                <span>

                  {activeGroup?.label ?? 'Consolidado'} — {groupRows.length} lançamento(s) em destaque

                </span>

                <span>Saldo em aberto: <strong>{money(groupOpenTotal)}</strong></span>

                {loading && <span className="guides-table-loading">Carregando…</span>}

              </div>



              <div className="guides-table-wrap">

                <div className="guides-table-scroll">

                  <table className="guides-data-table">

                    <thead>

                      <tr>

                        <th>Contraparte</th>

                        <th>Descrição</th>

                        <th>Vencimento</th>

                        <th>Saldo</th>

                      </tr>

                    </thead>

                    <tbody>

                      {groupRows.map((row) => (

                        <tr key={row.id}>

                          <td>{row.counterpartyDisplay}</td>

                          <td>{row.description}</td>

                          <td>{row.dueDate?.slice(0, 10) ?? '—'}</td>

                          <td>{money(row.balance)}</td>

                        </tr>

                      ))}

                      {groupRows.length === 0 && !loading && (

                        <tr>

                          <td colSpan={4} className="guides-table-empty">Nenhum lançamento em aberto.</td>

                        </tr>

                      )}

                    </tbody>

                  </table>

                </div>

              </div>



              <div style={{ marginTop: 16, display: 'flex', gap: 8, flexWrap: 'wrap' }}>

                <Link to="/financeiro" className="btn btn-primary btn-sm">

                  Abrir visão geral completa

                </Link>

                {selectedGroupId && GROUP_PATHS[selectedGroupId] && (

                  <Link to={GROUP_PATHS[selectedGroupId]} className="btn btn-secondary btn-sm">

                    Ir para {activeGroup?.label}

                  </Link>

                )}

              </div>

            </div>

          </div>

        </div>

      </div>

    </div>

  );

}



export function FinancialHubPage() {

  const { section } = useModuleSection('/financeiro');

  if (section === 'hub') {

    return <FinancialHubOverview />;

  }

  return <FinancialPage />;

}

