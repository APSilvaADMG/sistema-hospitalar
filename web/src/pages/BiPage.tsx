import { useCallback, useEffect, useState } from 'react';
import { api, type BiDashboardDto } from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { biTabs, dashboardTabs } from '../navigation/moduleSections';
import { ModuleTabs } from '../components/ModuleTabs';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';
import { printBiReport, type BiPrintSection } from '../utils/printTemplates';

function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function formatPercent(value: number) {
  const sign = value > 0 ? '+' : '';
  return `${sign}${value.toFixed(1)}%`;
}

function BiBarChart({
  data,
  valueKey,
  formatValue,
}: {
  data: { label: string; count: number; amount?: number }[];
  valueKey: 'count' | 'amount';
  formatValue?: (v: number) => string;
}) {
  const values = data.map((d) => (valueKey === 'amount' ? (d.amount ?? 0) : d.count));
  const max = Math.max(...values, 1);

  return (
    <div className="bar-chart">
      {data.map((item) => {
        const value = valueKey === 'amount' ? (item.amount ?? 0) : item.count;
        const display = formatValue ? formatValue(value) : String(value);
        return (
          <div key={item.label} className="bar-col">
            <span className="bar-value">{display}</span>
            <div className="bar" style={{ height: `${(value / max) * 100}%` }} title={display} />
            <span>{item.label}</span>
          </div>
        );
      })}
    </div>
  );
}

function BiProgressList({
  items,
  showAmount,
}: {
  items: { label: string; count: number; amount?: number }[];
  showAmount?: boolean;
}) {
  const max = Math.max(...items.map((i) => i.count), 1);
  return (
    <ul className="bi-progress-list">
      {items.map((item) => (
        <li key={item.label}>
          <div className="bi-progress-head">
            <span>{item.label}</span>
            <strong>
              {item.count}
              {showAmount && item.amount != null ? ` · ${formatCurrency(item.amount)}` : ''}
            </strong>
          </div>
          <div className="bi-progress-track">
            <div className="bi-progress-fill" style={{ width: `${(item.count / max) * 100}%` }} />
          </div>
        </li>
      ))}
      {items.length === 0 && <li className="bi-empty">Sem dados</li>}
    </ul>
  );
}

function BiCategoryList({ items }: { items: { label: string; amount: number; count: number }[] }) {
  const max = Math.max(...items.map((i) => i.amount), 1);
  return (
    <ul className="bi-progress-list">
      {items.map((item) => (
        <li key={item.label}>
          <div className="bi-progress-head">
            <span>{item.label}</span>
            <strong>{formatCurrency(item.amount)} <small>({item.count})</small></strong>
          </div>
          <div className="bi-progress-track">
            <div className="bi-progress-fill bi-progress-revenue" style={{ width: `${(item.amount / max) * 100}%` }} />
          </div>
        </li>
      ))}
      {items.length === 0 && <li className="bi-empty">Sem receita no mês</li>}
    </ul>
  );
}

export function BiPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/bi');
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [data, setData] = useState<BiDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(() => {
    setLoading(true);
    setError('');
    api.getBiDashboard()
      .then(setData)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar BI'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => { load(); }, [load]);

  if (!hasPermission('reports.read')) {
    return <div className="card">Acesso restrito à equipe administrativa.</div>;
  }

  if (loading && !data) {
    return <div className="card">Carregando indicadores...</div>;
  }

  if (error && !data) {
    return (
      <div className="card">
        <p>{error}</p>
        <button className="btn" type="button" onClick={load}>Tentar novamente</button>
      </div>
    );
  }

  if (!data) return null;

  const revenueTrend = data.revenueGrowthPercent;
  const generatedLabel = formatBrDateTime(data.generatedAt);

  const isOverview = !activeSection;
  const isOccupancy = activeSection === 'ocupacao';
  const isPermanencia = activeSection === 'permanencia';
  const isGiro = activeSection === 'giro-leitos';
  const isCustos = activeSection === 'custos';
  const isInadimplencia = activeSection === 'inadimplencia';
  const isProdMedica = activeSection === 'producao-medica';
  const isProdHospitalar = activeSection === 'producao-hospitalar';
  const isFaturamento = activeSection === 'faturamento';

  const showFinancial = isOverview || isCustos || isInadimplencia || isFaturamento;
  const showRevenueKpis = isOverview || isFaturamento;
  const showExpenseKpis = isOverview || isCustos;
  const showDefaultKpis = isOverview || isInadimplencia;
  const showOperational = isOverview || isOccupancy || isPermanencia || isGiro || isProdMedica || isProdHospitalar;
  const showOccupancyKpis = isOverview || isOccupancy || isPermanencia || isGiro || isProdHospitalar;
  const showRevenueCharts = isOverview || isFaturamento || isCustos;
  const showExpenseChart = isOverview || isCustos;
  const showOccupancy = isOverview || isOccupancy || isPermanencia || isGiro || isProdHospitalar;
  const showProduction = isOverview || isProdMedica || isProdHospitalar;
  const showTissPanels = isOverview || isFaturamento;
  const showErPanel = isOverview || isProdHospitalar || isOccupancy;

  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title={activeSection ? breadcrumb.title : 'Business Intelligence'}
        subtitle={`Painel gerencial com finanças, operação, faturamento TISS e estoque. Atualizado em ${generatedLabel}.`}
      >
        <button className="btn btn-secondary" type="button" onClick={load} disabled={loading}>
          {loading ? 'Atualizando...' : 'Atualizar'}
        </button>
        <button className="btn btn-secondary" type="button" onClick={() => printBiReport(data, activeSection as BiPrintSection)}>
          Imprimir relatório
        </button>
      </PageHeader>

      <ModuleTabs basePath="/" tabs={dashboardTabs} />
      <ModuleNav basePath="/bi" tabs={biTabs} contextId="businessIntelligence" />

      {error && <div className="alert alert-error">{error}</div>}

      {(isPermanencia || isGiro) && (
        <>
          <div className="bi-section-label">{isPermanencia ? 'Permanência' : 'Giro de leitos'}</div>
          <div className="kpi-grid">
            {isPermanencia && (
              <>
                <KpiCard label="Permanência média (dias)" value={data.averageLengthOfStayDays} variant="primary" />
                <KpiCard label="Altas no mês" value={data.dischargesThisMonth} variant="success" />
              </>
            )}
            {isGiro && (
              <>
                <KpiCard label="Giro (internações/leito)" value={data.bedTurnoverRate} variant="primary" />
                <KpiCard label="Giro mensal estimado" value={data.monthlyBedTurnover} variant="info" />
                <KpiCard label="Internações no mês" value={data.monthlyHospitalizations.at(-1)?.count ?? 0} variant="neutral" />
              </>
            )}
          </div>
        </>
      )}

      {showExpenseKpis && !isOverview && (
        <>
          <div className="bi-section-label">Custos</div>
          <div className="kpi-grid">
            <KpiCard label="Despesas do mês" value={formatCurrency(data.expenseThisMonth)} variant="warning" />
            <KpiCard
              label="vs mês anterior"
              value={formatPercent(data.expenseGrowthPercent)}
              variant={data.expenseGrowthPercent <= 0 ? 'success' : 'danger'}
            />
            <KpiCard label="Despesas mês anterior" value={formatCurrency(data.expenseLastMonth)} variant="neutral" />
          </div>
        </>
      )}

      {showDefaultKpis && !isOverview && (
        <>
          <div className="bi-section-label">Inadimplência</div>
          <div className="kpi-grid">
            <KpiCard label="Vencido a receber" value={formatCurrency(data.overdueReceivable)} variant="danger" />
            <KpiCard label="Títulos vencidos" value={data.overdueReceivableCount} variant="warning" />
            <KpiCard label="Inadimplência (% do aberto)" value={`${data.defaultRatePercent}%`} variant="danger" />
            <KpiCard label="Total a receber" value={formatCurrency(data.revenuePending)} variant="neutral" />
          </div>
        </>
      )}

      {(isProdMedica || isProdHospitalar) && (
        <>
          <div className="bi-section-label">{isProdMedica ? 'Produção médica' : 'Produção hospitalar'}</div>
          <div className="kpi-grid">
            {isProdMedica && (
              <KpiCard label="Produção médica (mês)" value={data.medicalProductionThisMonth} variant="primary" />
            )}
            {isProdHospitalar && (
              <KpiCard label="Produção hospitalar (mês)" value={data.hospitalProductionThisMonth} variant="primary" />
            )}
          </div>
        </>
      )}

      {showFinancial && (
      <>
      {(showRevenueKpis || isOverview) && (
      <>
      <div className="bi-section-label">Financeiro</div>
      <div className="kpi-grid">
        {showRevenueKpis && (
          <>
            <KpiCard label="Receita do mês" value={formatCurrency(data.revenueThisMonth)} variant="primary" />
            <KpiCard
              label="vs mês anterior"
              value={formatPercent(revenueTrend)}
              variant={revenueTrend >= 0 ? 'success' : 'warning'}
            />
          </>
        )}
        {(showRevenueKpis || showDefaultKpis) && (
          <>
            <KpiCard label="A receber" value={formatCurrency(data.revenuePending)} variant="warning" />
            <KpiCard label="Títulos a receber" value={data.financialAccountsOpen} variant="neutral" />
          </>
        )}
        {showRevenueKpis && (
          <>
            <KpiCard label="TISS pendente" value={formatCurrency(data.tissAmountPending)} variant="info" />
            <KpiCard label="Guias TISS abertas" value={data.tissGuidesPending} variant="info" />
          </>
        )}
        {showExpenseKpis && isOverview && (
          <>
            <KpiCard label="Despesas do mês" value={formatCurrency(data.expenseThisMonth)} variant="warning" />
            <KpiCard label="Vencido a receber" value={formatCurrency(data.overdueReceivable)} variant="danger" />
          </>
        )}
      </div>
      </>
      )}
      </>
      )}

      {showOperational && (
      <>
      <div className="bi-section-label">Operação</div>
      <div className="kpi-grid">
        {isOverview && (
          <>
            <KpiCard label="Pacientes" value={data.totalPatients} variant="primary" />
            <KpiCard label="Internações ativas" value={data.activeHospitalizations} variant="success" />
          </>
        )}
        {showOccupancyKpis && (
          <>
            <KpiCard
              label="Ocupação de leitos"
              value={`${data.bedOccupancyRate}%`}
              variant="info"
            />
            <KpiCard label="Leitos" value={`${data.occupiedBeds}/${data.totalBeds}`} variant="neutral" />
          </>
        )}
        {isOverview && (
          <>
            <KpiCard label="Permanência média (dias)" value={data.averageLengthOfStayDays} variant="info" />
            <KpiCard label="Giro de leitos" value={data.bedTurnoverRate} variant="info" />
          </>
        )}
        {(isOverview || isProdHospitalar) && (
          <>
            <KpiCard label="PS aguardando" value={data.emergencyWaiting} variant="danger" />
            <KpiCard label="PS em atendimento" value={data.emergencyInCare} variant="warning" />
          </>
        )}
        {(isOverview || isProdMedica) && (
          <>
            <KpiCard label="Consultas hoje" value={data.appointmentsToday} variant="primary" />
            <KpiCard label="Cirurgias hoje" value={data.surgeriesToday} variant="success" />
          </>
        )}
        {(isOverview || isProdMedica || isProdHospitalar) && (
          <>
            <KpiCard label="Lab pendente" value={data.labOrdersPending} variant="danger" />
            <KpiCard label="Imagem pendente" value={data.imagingStudiesPending} variant="warning" />
          </>
        )}
        {isOverview && (
          <>
            <KpiCard label="Estoque crítico" value={data.lowStockProducts} variant="danger" />
            <KpiCard label="Compras aguardando" value={data.purchaseOrdersPending} variant="neutral" />
          </>
        )}
      </div>
      </>
      )}

      {showRevenueCharts && (
      <div className="bi-charts-grid">
        {(isOverview || isFaturamento) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Receita — últimos 6 meses</div>
          <div className="card-panel-body">
            <BiBarChart data={data.monthlyRevenue} valueKey="amount" formatValue={formatCurrency} />
          </div>
        </div>
        )}
        {showExpenseChart && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Despesas — últimos 6 meses</div>
          <div className="card-panel-body">
            <BiBarChart data={data.monthlyExpenses} valueKey="amount" formatValue={formatCurrency} />
          </div>
        </div>
        )}
        {(isOverview || isProdMedica || isFaturamento) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Agendamentos — últimos 6 meses</div>
          <div className="card-panel-body">
            <BiBarChart data={data.monthlyAppointments} valueKey="count" />
          </div>
        </div>
        )}
        {(isOverview || isPermanencia || isGiro || isProdHospitalar) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Internações — últimos 6 meses</div>
          <div className="card-panel-body">
            <BiBarChart data={data.monthlyHospitalizations} valueKey="count" />
          </div>
        </div>
        )}
      </div>
      )}

      {((showRevenueKpis && isFaturamento) || showProduction || showOccupancy) && (
      <div className="grid-3" style={{ marginTop: 24 }}>
        {(isOverview || isFaturamento) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Receita por categoria (mês)</div>
          <div className="card-panel-body">
            <BiCategoryList items={data.revenueByCategory} />
          </div>
        </div>
        )}
        {(isOverview || isProdMedica) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Especialidades — volume no mês</div>
          <div className="card-panel-body">
            <BiProgressList
              items={data.topSpecialties.map((s) => ({
                label: s.specialtyName,
                count: s.appointmentsThisMonth,
              }))}
            />
          </div>
        </div>
        )}
        {showOccupancy && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Ocupação por ala</div>
          <div className="card-panel-body">
            <ul className="bi-progress-list">
              {data.wardOccupancy.map((w) => (
                <li key={w.wardName}>
                  <div className="bi-progress-head">
                    <span>{w.wardName}</span>
                    <strong>{w.occupancyRate}% ({w.occupiedBeds}/{w.totalBeds})</strong>
                  </div>
                  <div className="bi-progress-track">
                    <div
                      className={`bi-progress-fill ${w.occupancyRate >= 90 ? 'bi-progress-danger' : w.occupancyRate >= 75 ? 'bi-progress-warning' : ''}`}
                      style={{ width: `${w.occupancyRate}%` }}
                    />
                  </div>
                </li>
              ))}
              {data.wardOccupancy.length === 0 && <li className="bi-empty">Nenhuma ala cadastrada</li>}
            </ul>
          </div>
        </div>
        )}
      </div>
      )}

      {(showTissPanels || showDefaultKpis || showErPanel) && (
      <div className="grid-3" style={{ marginTop: 24 }}>
        {(isOverview || isFaturamento) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Guias TISS por status</div>
          <div className="card-panel-body">
            <BiProgressList items={data.tissGuidesByStatus} showAmount />
          </div>
        </div>
        )}
        {(isOverview || isInadimplencia) && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Contas financeiras</div>
          <div className="card-panel-body">
            <BiProgressList items={data.financialAccountsByStatus} showAmount />
          </div>
        </div>
        )}
        {showErPanel && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Fila PS por urgência</div>
          <div className="card-panel-body">
            <BiProgressList items={data.emergencyByUrgency} />
          </div>
        </div>
        )}
      </div>
      )}

      {(isOverview || isProdMedica || isProdHospitalar) && (
      <div className="grid-3" style={{ marginTop: 24 }}>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Laboratório por status</div>
          <div className="card-panel-body">
            <BiProgressList items={data.labOrdersByStatus} />
          </div>
        </div>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Imagem por status</div>
          <div className="card-panel-body">
            <BiProgressList items={data.imagingByStatus} />
          </div>
        </div>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Estoque crítico — top itens</div>
          <div className="card-panel-body">
            <ul className="bi-list">
              {data.lowStockItems.map((item) => (
                <li key={item.sku}>
                  <span>{item.productName}</span>
                  <strong className="text-danger">
                    {item.onHand}/{item.minimum} {item.unit}
                  </strong>
                </li>
              ))}
              {data.lowStockItems.length === 0 && <li className="bi-empty">Nenhum item crítico</li>}
            </ul>
          </div>
        </div>
      </div>
      )}
    </>
  );
}
