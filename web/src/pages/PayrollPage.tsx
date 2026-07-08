import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  type GeneratePayrollRunRequest,
  type PayrollItemDto,
  type PayrollItemLineInputDto,
  type PayrollMonthlySummaryDto,
  type PayrollRunDto,
  type PayrollSlipDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { hrTabs } from '../navigation/moduleSections';
import { FeegowRhScreenLayout } from '../components/feegow/rh/FeegowRhScreenLayout';
import { printPayrollMonthlySummary, printPayrollSlip } from '../utils/printTemplates';

const today = new Date();

const PAYROLL_STATUS: Record<number, string> = {
  1: 'Rascunho',
  2: 'Gerada',
  3: 'Aprovada',
  4: 'Paga',
};

/** Códigos de eventos gerados automaticamente na folha (ver PayrollCalculationService). */
const PAYROLL_EVENT_CODES: { code: string; label: string; type: 'provento' | 'desconto' }[] = [
  { code: 'SAL', label: 'Salário base', type: 'provento' },
  { code: 'HE', label: 'Horas extras (plantões adicionais)', type: 'provento' },
  { code: 'AN', label: 'Adicional noturno', type: 'provento' },
  { code: 'INS', label: 'Adicional de insalubridade', type: 'provento' },
  { code: 'VR', label: 'Vale-refeição', type: 'provento' },
  { code: 'BON', label: 'Bônus', type: 'provento' },
  { code: 'FAL', label: 'Faltas (evento RH)', type: 'desconto' },
  { code: 'INSS', label: 'INSS progressivo', type: 'desconto' },
  { code: 'IRRF', label: 'IRRF', type: 'desconto' },
  { code: 'VT', label: 'Vale-transporte', type: 'desconto' },
  { code: 'PS', label: 'Plano de saúde', type: 'desconto' },
];

const defaultForm: GeneratePayrollRunRequest = {
  year: today.getFullYear(),
  month: today.getMonth() + 1,
  defaultBaseSalary: 3200,
  valeRefeicao: 450,
  valeTransportePercent: 6,
  healthPlanDiscount: 0,
  dependentCount: 0,
  notes: '',
};

function formatBrl(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function HoleritePanel({ slip }: { slip: PayrollSlipDto }) {
  return (
    <div className="card" style={{ marginTop: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12, flexWrap: 'wrap' }}>
        <h3 style={{ marginTop: 0 }}>
          Holerite — {slip.item.employeeName}
          {slip.item.jobTitle ? ` (${slip.item.jobTitle})` : ''}
        </h3>
        <button type="button" className="btn btn-secondary btn-sm" onClick={() => printPayrollSlip(slip)}>
          Imprimir / PDF
        </button>
      </div>
      <p style={{ margin: '0 0 12px', color: 'var(--text-muted)' }}>
        {String(slip.month).padStart(2, '0')}/{slip.year} · {slip.item.departmentName}
      </p>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <h4>Proventos</h4>
          <table className="data-table">
            <thead>
              <tr><th>Cód.</th><th>Descrição</th><th>Valor</th></tr>
            </thead>
            <tbody>
              {slip.earnings.map((line) => (
                <tr key={line.id}>
                  <td><code>{line.code}</code></td>
                  <td>{line.description}</td>
                  <td>{formatBrl(line.amount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div>
          <h4>Descontos</h4>
          <table className="data-table">
            <thead>
              <tr><th>Cód.</th><th>Descrição</th><th>Valor</th></tr>
            </thead>
            <tbody>
              {slip.discounts.length === 0 ? (
                <tr><td colSpan={3}>Nenhum desconto</td></tr>
              ) : (
                slip.discounts.map((line) => (
                  <tr key={line.id}>
                    <td><code>{line.code}</code></td>
                    <td>{line.description}</td>
                    <td>{formatBrl(line.amount)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="kpi-grid" style={{ marginTop: 16 }}>
        <KpiCard label="Bruto" value={formatBrl(slip.item.grossAmount)} variant="info" />
        <KpiCard label="Descontos" value={formatBrl(slip.item.discountAmount)} variant="warning" />
        <KpiCard label="Líquido" value={formatBrl(slip.item.netAmount)} variant="success" />
        <KpiCard label="FGTS (empregador)" value={formatBrl(slip.totalFgtsEmployer)} variant="primary" />
      </div>
    </div>
  );
}

export function PayrollPage() {
  const [runs, setRuns] = useState<PayrollRunDto[]>([]);
  const [form, setForm] = useState<GeneratePayrollRunRequest>(defaultForm);
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const [selectedItem, setSelectedItem] = useState<PayrollItemDto | null>(null);
  const [slip, setSlip] = useState<PayrollSlipDto | null>(null);
  const [monthlySummary, setMonthlySummary] = useState<PayrollMonthlySummaryDto | null>(null);
  const [editLines, setEditLines] = useState<PayrollItemLineInputDto[]>([]);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  const selectedRun = useMemo(
    () => runs.find((r) => r.id === selectedRunId) ?? null,
    [runs, selectedRunId],
  );

  async function load() {
    setRuns(await api.getPayrollRuns());
  }

  useEffect(() => {
    load().catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar folha.'));
  }, []);

  useEffect(() => {
    if (!selectedRunId || !selectedItem) {
      setSlip(null);
      return;
    }
    api.getPayrollSlip(selectedRunId, selectedItem.employeeId)
      .then(setSlip)
      .catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar holerite.'));
  }, [selectedRunId, selectedItem]);

  useEffect(() => {
    if (!selectedRun) {
      setMonthlySummary(null);
      return;
    }
    api.getPayrollMonthlySummary(selectedRun.year, selectedRun.month)
      .then(setMonthlySummary)
      .catch(() => setMonthlySummary(null));
  }, [selectedRun]);

  useEffect(() => {
    if (!selectedItem) {
      setEditLines([]);
      return;
    }
    setEditLines(
      selectedItem.lines.map((line) => ({
        lineType: line.lineType,
        code: line.code,
        description: line.description,
        amount: line.amount,
      })),
    );
  }, [selectedItem]);

  const closedRuns = useMemo(
    () => runs
      .filter((r) => r.status >= 3)
      .sort((a, b) => b.year - a.year || b.month - a.month),
    [runs],
  );

  const totals = useMemo(() => {
    const latestRun = runs.length > 0
      ? [...runs].sort((a, b) => b.year - a.year || b.month - a.month)[0]
      : null;
    const currentPeriodRun = runs.find((r) => r.year === today.getFullYear() && r.month === today.getMonth() + 1);
    const approvedRuns = runs.filter((r) => r.status >= 3);
    return {
      totalRuns: runs.length,
      generated: runs.filter((r) => r.status >= 2).length,
      approved: approvedRuns.length,
      paid: runs.filter((r) => r.status >= 4).length,
      latestNet: latestRun?.totalNet ?? 0,
      currentEmployees: currentPeriodRun?.items.length ?? latestRun?.items.length ?? 0,
      totalFgts: latestRun?.totalFgtsEmployer ?? 0,
      avgNetPerEmployee: latestRun && latestRun.items.length > 0
        ? latestRun.totalNet / latestRun.items.length
        : 0,
    };
  }, [runs]);

  async function generate(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setOk('');
    await api.generatePayrollRun(form);
    setOk('Folha gerada com INSS, IRRF, FGTS e linhas de proventos/descontos.');
    await load();
  }

  async function saveItemLines() {
    if (!selectedRunId || !selectedItem) return;
    setError('');
    setOk('');
    try {
      const updated = await api.updatePayrollItemLines(selectedRunId, selectedItem.id, { lines: editLines });
      setOk('Proventos e descontos atualizados.');
      await load();
      setSelectedItem(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar linhas da folha.');
    }
  }

  async function closePayrollRun(run: PayrollRunDto) {
    const confirmed = window.confirm(
      `Fechar competência ${String(run.month).padStart(2, '0')}/${run.year}?\n\n`
      + 'Após o fechamento, proventos e descontos não poderão ser editados.',
    );
    if (!confirmed) return;
    await updateStatus(run, 3);
  }

  async function updateStatus(run: PayrollRunDto, status: number) {
    if (status === 4) {
      const confirmed = window.confirm(
        `Registrar pagamento da folha ${String(run.month).padStart(2, '0')}/${run.year}?\n\n`
        + `Total líquido: ${formatBrl(run.totalNet)}\n`
        + `Colaboradores: ${run.items.length}\n\n`
        + 'Será criada uma conta financeira consolidada.',
      );
      if (!confirmed) return;
    }

    setError('');
    setOk('');
    await api.updatePayrollRunStatus(run.id, {
      status,
      createFinancialAccountsWhenPaid: status === 4,
    });
    setOk(`Folha ${String(run.month).padStart(2, '0')}/${run.year} atualizada para ${PAYROLL_STATUS[status]}.`);
    await load();
  }

  return (
    <FeegowRhScreenLayout>
    <>
      <PageHeader
        eyebrow="RH"
        title="Folha de pagamento"
        subtitle="Geração mensal com INSS progressivo, IRRF, FGTS 8% e holerite por colaborador."
      />
      <ModuleNav basePath="/rh" tabs={hrTabs} contextId="humanResources" />
      {error && <div className="alert alert-error">{error}</div>}
      {ok && <div className="alert alert-success">{ok}</div>}

      <div className="kpi-grid">
        <KpiCard label="Última folha (líquido)" value={formatBrl(totals.latestNet)} variant="success" />
        <KpiCard label="Colaboradores (competência)" value={totals.currentEmployees} variant="primary" />
        <KpiCard label="Média líquida / colaborador" value={formatBrl(totals.avgNetPerEmployee)} variant="info" />
        <KpiCard label="FGTS empregador (última)" value={formatBrl(totals.totalFgts)} variant="warning" />
        <KpiCard label="Folhas fechadas" value={totals.approved} variant="neutral" />
        <KpiCard label="Folhas pagas" value={totals.paid} variant="success" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">Tipos de evento na folha</div>
        <div className="card-panel-body">
          <p style={{ margin: '0 0 12px', color: 'var(--text-muted)', fontSize: 14 }}>
            Códigos gerados automaticamente a partir de plantões, férias e parâmetros da competência.
          </p>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
            {PAYROLL_EVENT_CODES.map((ev) => (
              <span
                key={ev.code}
                className={`badge ${ev.type === 'provento' ? 'badge-success' : 'badge-warning'}`}
                title={ev.label}
              >
                <code>{ev.code}</code> — {ev.label}
              </span>
            ))}
          </div>
        </div>
      </div>

      <div className="card" style={{ marginBottom: 16, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16, flexWrap: 'wrap' }}>
        <div>
          <strong>Plantões noturnos</strong>
          <div style={{ color: 'var(--text-muted)', fontSize: 14 }}>
            Plantões noturnos impactam horas extras (HE) e adicional noturno (AN) na folha de pagamento.
          </div>
        </div>
        <Link className="btn btn-secondary" to="/rh/plantoes">Ver plantões →</Link>
      </div>

      <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={generate}>
        <h3 style={{ marginTop: 0 }}>Gerar folha mensal</h3>
        <div className="form-field">
          <label>Ano *</label>
          <input type="number" required value={form.year} onChange={(e) => setForm({ ...form, year: Number(e.target.value) })} />
        </div>
        <div className="form-field">
          <label>Mês *</label>
          <input type="number" min={1} max={12} required value={form.month} onChange={(e) => setForm({ ...form, month: Number(e.target.value) })} />
        </div>
        <div className="form-field">
          <label>Salário base padrão</label>
          <input type="number" step="0.01" value={form.defaultBaseSalary ?? ''} onChange={(e) => setForm({ ...form, defaultBaseSalary: Number(e.target.value) })} />
        </div>
        <div className="form-field">
          <label>Vale-refeição (R$)</label>
          <input type="number" step="0.01" value={form.valeRefeicao} onChange={(e) => setForm({ ...form, valeRefeicao: Number(e.target.value) })} />
        </div>
        <div className="form-field">
          <label>Vale-transporte (%)</label>
          <input type="number" step="0.01" value={form.valeTransportePercent} onChange={(e) => setForm({ ...form, valeTransportePercent: Number(e.target.value) })} />
        </div>
        <div className="form-field">
          <label>Plano de saúde (R$)</label>
          <input type="number" step="0.01" value={form.healthPlanDiscount} onChange={(e) => setForm({ ...form, healthPlanDiscount: Number(e.target.value) })} />
        </div>
        <div className="form-field">
          <label>Dependentes (IRRF)</label>
          <input type="number" min={0} value={form.dependentCount} onChange={(e) => setForm({ ...form, dependentCount: Number(e.target.value) })} />
        </div>
        <div className="form-field full">
          <label>Observações</label>
          <input value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        </div>
        <div className="form-actions"><button className="btn" type="submit">Gerar folha</button></div>
      </form>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">Execuções de folha</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Período</th>
                <th>Status</th>
                <th>Colaboradores</th>
                <th>Bruto</th>
                <th>Descontos</th>
                <th>Líquido</th>
                <th>FGTS</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {runs.map((r) => (
                <tr
                  key={r.id}
                  onClick={() => { setSelectedRunId(r.id); setSelectedItem(null); }}
                  style={{ cursor: 'pointer', background: selectedRunId === r.id ? 'var(--surface-alt)' : undefined }}
                >
                  <td>{String(r.month).padStart(2, '0')}/{r.year}</td>
                  <td>{PAYROLL_STATUS[r.status] ?? r.status}</td>
                  <td>{r.items.length}</td>
                  <td>{formatBrl(r.totalGross)}</td>
                  <td>{formatBrl(r.totalDiscounts)}</td>
                  <td>{formatBrl(r.totalNet)}</td>
                  <td>{formatBrl(r.totalFgtsEmployer)}</td>
                  <td onClick={(e) => e.stopPropagation()}>
                    {r.status === 2 && (
                      <>
                        <button type="button" className="btn btn-sm" onClick={() => closePayrollRun(r)}>Fechar</button>
                        {' '}
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => updateStatus(r, 3)}>Aprovar</button>
                      </>
                    )}
                    {r.status === 3 && (
                      <button type="button" className="btn btn-sm" onClick={() => updateStatus(r, 4)}>Pagar</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {selectedRun && monthlySummary ? (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
            <span>
              Resumo mensal — {String(selectedRun.month).padStart(2, '0')}/{selectedRun.year}
            </span>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => printPayrollMonthlySummary(monthlySummary, selectedRun)}
            >
              Imprimir resumo / PDF
            </button>
          </div>
          <div className="card-panel-body">
            <div className="kpi-grid" style={{ marginBottom: 16 }}>
              <KpiCard label="Colaboradores" value={monthlySummary.employeeCount} variant="primary" />
              <KpiCard label="Bruto total" value={formatBrl(monthlySummary.totalGross)} variant="info" />
              <KpiCard label="Líquido total" value={formatBrl(monthlySummary.totalNet)} variant="success" />
              <KpiCard label="FGTS empregador" value={formatBrl(monthlySummary.totalFgtsEmployer)} variant="warning" />
              <KpiCard label="De férias no mês" value={monthlySummary.employeesOnVacation} variant="neutral" />
              <KpiCard label="Plantões noturnos" value={monthlySummary.nightShiftsInMonth} variant="neutral" />
            </div>
            {monthlySummary.byDepartment.length > 0 ? (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Departamento</th>
                    <th>Colaboradores</th>
                    <th>Bruto</th>
                    <th>Líquido</th>
                  </tr>
                </thead>
                <tbody>
                  {monthlySummary.byDepartment.map((d) => (
                    <tr key={d.departmentName}>
                      <td>{d.departmentName}</td>
                      <td>{d.employeeCount}</td>
                      <td>{formatBrl(d.totalGross)}</td>
                      <td>{formatBrl(d.totalNet)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : null}
          </div>
        </div>
      ) : null}

      {selectedRun && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">
            Colaboradores — {String(selectedRun.month).padStart(2, '0')}/{selectedRun.year}
          </div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Cargo</th>
                  <th>Departamento</th>
                  <th>Bruto</th>
                  <th>Descontos</th>
                  <th>Líquido</th>
                  <th>Holerite</th>
                </tr>
              </thead>
              <tbody>
                {selectedRun.items.map((item) => (
                  <tr
                    key={item.id}
                    onClick={() => setSelectedItem(item)}
                    style={{ cursor: 'pointer', background: selectedItem?.id === item.id ? 'var(--surface-alt)' : undefined }}
                  >
                    <td>{item.employeeName}</td>
                    <td>{item.jobTitle ?? '—'}</td>
                    <td>{item.departmentName}</td>
                    <td>{formatBrl(item.grossAmount)}</td>
                    <td>{formatBrl(item.discountAmount)}</td>
                    <td>{formatBrl(item.netAmount)}</td>
                    <td onClick={(e) => e.stopPropagation()}>
                      <button type="button" className="btn btn-sm" onClick={() => setSelectedItem(item)}>Ver</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {selectedRun && selectedRun.status === 2 && selectedItem ? (
        <div className="card" style={{ marginTop: 16 }}>
          <h3 style={{ marginTop: 0 }}>Editar proventos/descontos — {selectedItem.employeeName}</h3>
          <p style={{ color: 'var(--text-muted)', marginTop: 0 }}>
            Ajuste manual antes do fechamento da competência. Plantões noturnos geram linhas HE/AN automaticamente.
          </p>
          <table className="data-table">
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Código</th>
                <th>Descrição</th>
                <th>Valor (R$)</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {editLines.map((line, index) => (
                <tr key={`${line.code}-${index}`}>
                  <td>
                    <select
                      value={line.lineType}
                      onChange={(e) => {
                        const next = [...editLines];
                        next[index] = { ...line, lineType: Number(e.target.value) };
                        setEditLines(next);
                      }}
                    >
                      <option value={1}>Provento</option>
                      <option value={2}>Desconto</option>
                    </select>
                  </td>
                  <td>
                    <input
                      list="payroll-codes"
                      value={line.code}
                      onChange={(e) => {
                        const next = [...editLines];
                        next[index] = { ...line, code: e.target.value };
                        setEditLines(next);
                      }}
                    />
                  </td>
                  <td>
                    <input
                      value={line.description}
                      onChange={(e) => {
                        const next = [...editLines];
                        next[index] = { ...line, description: e.target.value };
                        setEditLines(next);
                      }}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      step="0.01"
                      value={line.amount}
                      onChange={(e) => {
                        const next = [...editLines];
                        next[index] = { ...line, amount: Number(e.target.value) };
                        setEditLines(next);
                      }}
                    />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => setEditLines(editLines.filter((_, i) => i !== index))}
                    >
                      Remover
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <datalist id="payroll-codes">
            {PAYROLL_EVENT_CODES.map((ev) => (
              <option key={ev.code} value={ev.code}>{ev.label}</option>
            ))}
          </datalist>
          <div className="form-actions" style={{ marginTop: 12 }}>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setEditLines([...editLines, { lineType: 1, code: '', description: '', amount: 0 }])}
            >
              + Linha
            </button>
            <button type="button" className="btn" onClick={() => saveItemLines().catch(console.error)}>
              Salvar alterações
            </button>
          </div>
        </div>
      ) : null}

      {closedRuns.length > 0 ? (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Histórico de fechamentos</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Competência</th>
                  <th>Status</th>
                  <th>Fechado em</th>
                  <th>Pago em</th>
                  <th>Líquido</th>
                </tr>
              </thead>
              <tbody>
                {closedRuns.map((run) => (
                  <tr key={run.id}>
                    <td>{String(run.month).padStart(2, '0')}/{run.year}</td>
                    <td>{PAYROLL_STATUS[run.status] ?? run.status}</td>
                    <td>{run.approvedAt ? new Date(run.approvedAt).toLocaleString('pt-BR') : '—'}</td>
                    <td>{run.paidAt ? new Date(run.paidAt).toLocaleString('pt-BR') : '—'}</td>
                    <td>{formatBrl(run.totalNet)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}

      {slip && <HoleritePanel slip={slip} />}
    </>
    </FeegowRhScreenLayout>
  );
}
