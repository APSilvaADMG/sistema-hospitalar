import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';

import { Link } from 'react-router-dom';

import {

  api,

  financialCategoryLabel,

  financialCategoryValue,

  financialDirectionValue,

  isFinancialOpen,

  paymentMethodLabel,

  paymentMethodValue,

  type FinancialAccountDto,

} from '../../../api/client';

import { formatBrDate } from '../../../utils/dateUtils';

import { TablePagination } from '../TablePagination';

import { FinancialStatusBadge } from './FinancialStatusBadge';

import { FeegowFinancePageHead } from './FeegowFinancePageHead';

import { feegowFinanceInsertPath, feegowFinanceListPath } from './feegowFinanceNav';



export type FinanceFilteredSection =

  | 'extratos'

  | 'repasses'

  | 'tef'

  | 'cheques'

  | 'cartoes'

  | 'propostas'

  | 'descontos'

  | 'honorarios';



type StatusFilter = 'all' | 'open' | 'settled';



type Props = {

  title: string;

  description?: string;

  direction?: 1 | 2;

  paymentMethod?: number;

  category?: number;

  partialOnly?: boolean;

  settledOnly?: boolean;

  searchInDescription?: string;

  defaultStatusFilter?: StatusFilter;

  section?: FinanceFilteredSection;

  allowConvertProposal?: boolean;

};



const PAGE_SIZE = 50;



const EMPTY_STATE: Record<FinanceFilteredSection, { message: string; cta?: string; insertHint?: string }> = {

  propostas: {

    message: 'Nenhuma proposta ou orçamento em aberto.',

    cta: 'Criar proposta',

    insertHint: 'Proposta — orçamento',

  },

  honorarios: {

    message: 'Nenhum honorário pendente.',

    cta: 'Novo lançamento',

  },

  tef: {

    message: 'Nenhuma transação TEF encontrada.',

  },

  cheques: {

    message: 'Nenhum cheque registrado.',

  },

  cartoes: {

    message: 'Nenhum lançamento com cartão de crédito.',

  },

  descontos: {

    message: 'Nenhuma conta com desconto parcial pendente.',

  },

  extratos: {

    message: 'Nenhuma movimentação quitada no período.',

  },

  repasses: {

    message: 'Nenhum repasse por transferência.',

  },

};



function formatCurrency(value: number): string {

  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

}



function accountListPath(row: FinancialAccountDto): string {

  return `${feegowFinanceListPath(financialDirectionValue(row.direction) === 2 ? 'pagar' : 'receber')}?conta=${row.id}`;

}



export function FeegowFinanceFilteredAccounts({

  title,

  description,

  direction,

  paymentMethod,

  category,

  partialOnly,

  settledOnly,

  searchInDescription,

  defaultStatusFilter = 'all',

  section,

  allowConvertProposal,

}: Props) {

  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState('');

  const [search, setSearch] = useState('');

  const [statusFilter, setStatusFilter] = useState<StatusFilter>(defaultStatusFilter);

  const [page, setPage] = useState(1);

  const [totalCount, setTotalCount] = useState(0);

  const [convertingId, setConvertingId] = useState<string | null>(null);

  const [actionMessage, setActionMessage] = useState('');



  const load = useCallback(async (targetPage: number) => {

    setLoading(true);

    setError('');

    try {

      const apiSearch = search.trim() || searchInDescription || undefined;

      if (direction) {

        const result = await api.getFinancialAccounts(

          undefined,

          apiSearch,

          targetPage,

          direction,

          PAGE_SIZE,

        );

        setAccounts(result.items);

        setTotalCount(result.totalCount);

      } else {

        const [receivable, payable] = await Promise.all([

          api.getFinancialAccounts(undefined, apiSearch, targetPage, 1, PAGE_SIZE),

          api.getFinancialAccounts(undefined, apiSearch, targetPage, 2, PAGE_SIZE),

        ]);

        setAccounts([...receivable.items, ...payable.items]);

        setTotalCount(receivable.totalCount + payable.totalCount);

      }

      setPage(targetPage);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar lançamentos.');

    } finally {

      setLoading(false);

    }

  }, [direction, search, searchInDescription]);



  useEffect(() => {

    void load(1);

  }, [load]);



  const filtered = useMemo(() => {

    let rows = accounts;



    if (direction) {

      rows = rows.filter((a) => financialDirectionValue(a.direction) === direction);

    }

    if (paymentMethod) {

      rows = rows.filter((a) => {

        const method = a.lastPaymentMethod ?? a.expectedPaymentMethod;

        return method !== undefined && paymentMethodValue(method) === paymentMethod;

      });

    }

    if (category) {

      rows = rows.filter((a) => financialCategoryValue(a.category) === category);

    }

    if (partialOnly) {

      rows = rows.filter((a) => a.paidAmount > 0 && a.balance > 0 && a.paidAmount < a.amount);

    }

    if (settledOnly) {

      rows = rows.filter((a) => a.balance <= 0);

    }

    if (searchInDescription) {

      const term = searchInDescription.toLowerCase();

      rows = rows.filter((a) =>

        a.description.toLowerCase().includes(term)

        || (a.notes ?? '').toLowerCase().includes(term),

      );

    }

    if (statusFilter === 'open') {

      rows = rows.filter((a) => isFinancialOpen(a.status) && a.balance > 0);

    } else if (statusFilter === 'settled') {

      rows = rows.filter((a) => a.balance <= 0);

    }

    if (search.trim()) {

      const term = search.trim().toLowerCase();

      rows = rows.filter((a) =>

        a.counterpartyDisplay.toLowerCase().includes(term)

        || a.description.toLowerCase().includes(term),

      );

    }



    return rows.sort((a, b) => (b.dueDate ?? b.createdAt).localeCompare(a.dueDate ?? a.createdAt));

  }, [accounts, category, direction, partialOnly, paymentMethod, search, searchInDescription, settledOnly, statusFilter]);



  const pagedRows = useMemo(() => {

    const start = (page - 1) * PAGE_SIZE;

    return filtered.slice(start, start + PAGE_SIZE);

  }, [filtered, page]);



  useEffect(() => {

    setPage(1);

  }, [search, statusFilter, category, direction, partialOnly, paymentMethod, searchInDescription, settledOnly]);



  const totalBalance = useMemo(

    () => filtered.reduce((sum, row) => sum + row.balance, 0),

    [filtered],

  );



  const emptyState = section ? EMPTY_STATE[section] : null;



  function handleFilter(event: FormEvent) {

    event.preventDefault();

    setPage(1);

    void load(1);

  }



  async function handleConvertProposal(accountId: string) {

    setConvertingId(accountId);

    setActionMessage('');

    try {

      const billing = await api.convertFinancialProposal(accountId);

      setActionMessage(`Faturamento criado: ${billing.description}`);

      await load(1);

    } catch (err) {

      setActionMessage(err instanceof Error ? err.message : 'Erro ao converter proposta.');

    } finally {

      setConvertingId(null);

    }

  }



  return (

    <div className="feegow-finance-page">

      <FeegowFinancePageHead title={title} />

      {description && (

        <p className="feegow-finance-lead" style={{ marginTop: 0, color: 'var(--muted)' }}>{description}</p>

      )}



      <section className="feegow-finance-panel">

        <form className="feegow-finance-filter-row" onSubmit={handleFilter}>

          <div className="form-field">

            <label htmlFor="ffaSearch">Buscar</label>

            <input

              id="ffaSearch"

              value={search}

              onChange={(e) => setSearch(e.target.value)}

              placeholder="Contraparte ou descrição..."

            />

          </div>

          <div className="form-field">

            <label htmlFor="ffaStatus">Situação</label>

            <select id="ffaStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}>

              <option value="all">Todas</option>

              <option value="open">Em aberto</option>

              <option value="settled">Quitadas</option>

            </select>

          </div>

          <button type="submit" className="btn btn-secondary">Atualizar</button>

        </form>



        {error && <div className="alert alert-error">{error}</div>}

        {actionMessage && <div className="alert alert-info">{actionMessage}</div>}



        <div className="feegow-finance-summary-strip">

          <span><strong>{filtered.length}</strong> lançamento(s)</span>

          <span>Saldo filtrado: <strong>{formatCurrency(totalBalance)}</strong></span>

          {loading && <span>Carregando…</span>}

        </div>



        <div className="guides-table-wrap">

          <div className="guides-table-scroll">

            <table className="guides-data-table">

              <thead>

                <tr>

                  <th>Vencimento</th>

                  <th>Contraparte</th>

                  <th>Descrição</th>

                  <th>Categoria</th>

                  <th>Forma</th>

                  <th>Situação</th>

                  <th>Valor</th>

                  <th>Saldo</th>

                  {allowConvertProposal ? <th>Ações</th> : null}

                </tr>

              </thead>

              <tbody>

                {pagedRows.map((row) => (

                  <tr key={row.id}>

                    <td>{row.dueDate ? formatBrDate(row.dueDate) : '—'}</td>

                    <td>{row.counterpartyDisplay}</td>

                    <td>

                      <Link to={accountListPath(row)} className="feegow-finance-row-link">

                        {row.description}

                      </Link>

                    </td>

                    <td>{financialCategoryLabel(row.category)}</td>

                    <td>{paymentMethodLabel(row.lastPaymentMethod ?? row.expectedPaymentMethod)}</td>

                    <td>

                      <FinancialStatusBadge status={row.status} />

                    </td>

                    <td>{formatCurrency(row.amount)}</td>

                    <td>{formatCurrency(row.balance)}</td>

                    {allowConvertProposal ? (

                      <td>

                        {row.balance > 0 && isFinancialOpen(row.status) ? (

                          <button

                            type="button"

                            className="btn btn-secondary btn-sm"

                            disabled={convertingId === row.id}

                            onClick={() => void handleConvertProposal(row.id)}

                          >

                            {convertingId === row.id ? 'Convertendo…' : 'Faturar'}

                          </button>

                        ) : (

                          '—'

                        )}

                      </td>

                    ) : null}

                  </tr>

                ))}

                {filtered.length === 0 && !loading && (

                  <tr>

                    <td colSpan={allowConvertProposal ? 9 : 8}>

                      <div className="feegow-finance-table-empty">

                        <p>{emptyState?.message ?? 'Nenhum lançamento encontrado.'}</p>

                        {emptyState?.cta && (

                          <Link

                            to={feegowFinanceInsertPath(direction === 2 ? 'pagar' : 'receber')}

                            className="btn btn-primary btn-sm"

                            state={emptyState.insertHint ? { description: emptyState.insertHint } : undefined}

                          >

                            {emptyState.cta}

                          </Link>

                        )}

                      </div>

                    </td>

                  </tr>

                )}

              </tbody>

            </table>

          </div>

        </div>



        <TablePagination

          page={page}

          pageSize={PAGE_SIZE}

          totalCount={statusFilter === 'all' && !search.trim() ? totalCount : filtered.length}

          onPageChange={setPage}

          loading={loading}

        />



        <div className="feegow-finance-actions" style={{ marginTop: 16 }}>

          <Link to={feegowFinanceInsertPath(direction === 2 ? 'pagar' : 'receber')} className="btn btn-primary btn-sm">

            Novo lançamento

          </Link>

          {section === 'propostas' && (

            <Link to="/faturamento-tiss" className="btn btn-secondary btn-sm" style={{ marginLeft: 8 }}>

              Faturamento TISS

            </Link>

          )}

        </div>

      </section>

    </div>

  );

}


