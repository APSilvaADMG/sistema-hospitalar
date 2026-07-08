import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  api,
  type PatientDto,
  type ProfessionalDto,
  type HealthInsuranceDto,
  type SpecialtyDto,
  type ReportCatalogItemDto,
  type ReportCatalogSummaryDto,
  type ReportResultDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ModuleNav } from '../components/ModuleNav';
import { ReportDataTable } from '../components/reports/ReportDataTable';
import { ReportFiltersPanel, type ReportFiltersState } from '../components/reports/ReportFiltersPanel';
import { ExternalSourcesPanel } from '../components/reports/ExternalSourcesPanel';
import {
  REPORT_FUNCTIONAL_GROUPS,
  filterCatalogByFunctionalGroup,
  getFunctionalGroupBySlug,
} from '../data/reportFunctionalGroups';
import { resolveReportPrintLayout } from '../data/reportPrintLayouts';
import { reportsTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { REPORT_SECTION_FILTERS } from './reports/reportSectionFilters';
import { saveReportSource } from '../utils/clinicalDocumentWorkflow';
import { printAnalyticsReport } from '../utils/printAnalyticsReport';
import { exportReportCsv, exportReportExcel, exportReportPdf } from '../utils/reportExport';

type ViewMode = 'catalog' | 'result';

function defaultDateRange() {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 30);
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  };
}

function defaultFilters(): ReportFiltersState {
  const range = defaultDateRange();
  return {
    search: '',
    dateFrom: range.from,
    dateTo: range.to,
    patientId: '',
    professionalId: '',
    specialtyId: '',
    healthInsuranceId: '',
    essentialOnly: false,
    implementedOnly: true,
  };
}

export function ReportsPage() {
  const { hasPermission } = useAuth();
  const canRead = hasPermission('reports.read');
  const [searchParams] = useSearchParams();
  const { section } = useModuleSection('/relatorios');

  const sectionGroup = getFunctionalGroupBySlug(section || undefined);
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(
    () => sectionGroup?.id ?? null,
  );
  const [view, setView] = useState<ViewMode>('catalog');
  const [summary, setSummary] = useState<ReportCatalogSummaryDto | null>(null);
  const [catalog, setCatalog] = useState<ReportCatalogItemDto[]>([]);
  const [filters, setFilters] = useState<ReportFiltersState>(() => {
    const base = defaultFilters();
    const q = searchParams.get('q');
    if (q) base.search = q;
    const impl = searchParams.get('implementedOnly');
    if (impl === '0') base.implementedOnly = false;
    return base;
  });
  const [selectedReport, setSelectedReport] = useState<ReportCatalogItemDto | null>(null);
  const [result, setResult] = useState<ReportResultDto | null>(null);
  const [loadingCatalog, setLoadingCatalog] = useState(true);
  const [running, setRunning] = useState(false);
  const [savingReport, setSavingReport] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [saveSuccess, setSaveSuccess] = useState('');
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);

  useEffect(() => {
    const sectionFilter = section ? REPORT_SECTION_FILTERS[section] : undefined;
    const group = getFunctionalGroupBySlug(section || undefined);
    if (group) setSelectedGroupId(group.id);
    else if (sectionFilter?.groupId) setSelectedGroupId(sectionFilter.groupId);

    if (sectionFilter?.search) {
      setFilters((prev) => ({ ...prev, search: sectionFilter.search ?? prev.search }));
    }
  }, [section]);

  const loadCatalog = useCallback(async () => {
    setLoadingCatalog(true);
    setError('');
    try {
      const [sum, items] = await Promise.all([
        api.getReportsSummary(),
        api.getReportsCatalog({
          essentialOnly: filters.essentialOnly,
          implementedOnly: filters.implementedOnly,
          search: filters.search.trim() || undefined,
        }),
      ]);
      setSummary(sum);
      setCatalog(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar relatórios.');
    } finally {
      setLoadingCatalog(false);
    }
  }, [filters.essentialOnly, filters.implementedOnly, filters.search]);

  useEffect(() => {
    if (canRead) loadCatalog().catch(console.error);
  }, [loadCatalog, canRead]);

  useEffect(() => {
    if (!canRead) return;
    Promise.all([
      api.getPatients(undefined, 1),
      api.getProfessionals(),
      api.getSpecialties(),
      api.getHealthInsurances(),
    ])
      .then(([p, prof, spec, ins]) => {
        setPatients(p.items);
        setProfessionals(prof);
        setSpecialties(spec);
        setInsurances(ins);
      })
      .catch(console.error);
  }, [canRead]);

  const displayedCatalog = useMemo(
    () => filterCatalogByFunctionalGroup(catalog, selectedGroupId),
    [catalog, selectedGroupId],
  );

  const reportNamesByCode = useMemo(
    () => Object.fromEntries(catalog.map((item) => [item.code, item.name])),
    [catalog],
  );

  const activeGroup = REPORT_FUNCTIONAL_GROUPS.find((g) => g.id === selectedGroupId);

  function patchFilters(patch: Partial<ReportFiltersState>) {
    setFilters((prev) => ({ ...prev, ...patch }));
  }

  function clearFilters() {
    setFilters(defaultFilters());
    setSelectedGroupId(sectionGroup?.id ?? null);
    setError('');
    setSaveSuccess('');
  }

  async function runReport(report: ReportCatalogItemDto) {
    setSelectedReport(report);
    setRunning(true);
    setError('');
    setSaveSuccess('');
    try {
      const data = await api.runReport(report.code, {
        dateFrom: `${filters.dateFrom}T00:00:00Z`,
        dateTo: `${filters.dateTo}T23:59:59Z`,
        patientId: filters.patientId || undefined,
        professionalId: filters.professionalId || undefined,
        specialtyId: filters.specialtyId || undefined,
        healthInsuranceId: filters.healthInsuranceId || undefined,
        year: Number(filters.dateFrom.slice(0, 4)),
        month: Number(filters.dateFrom.slice(5, 7)),
        department: section || undefined,
      });
      setResult(data);
      setView('result');
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar relatório.');
      setResult(null);
    } finally {
      setRunning(false);
    }
  }

  async function saveReportFilters(report: ReportCatalogItemDto) {
    if (!filters.patientId) {
      setError('Selecione um paciente para salvar os filtros.');
      return;
    }
    setSavingReport(report.code);
    setError('');
    setSaveSuccess('');
    try {
      await saveReportSource(
        filters.patientId,
        report.code,
        report.name,
        { label: `${report.name} — filtros salvos` },
        {
          reportCode: report.code,
          reportName: report.name,
          filters: {
            patientId: filters.patientId,
            dateFrom: `${filters.dateFrom}T00:00:00Z`,
            dateTo: `${filters.dateTo}T23:59:59Z`,
            professionalId: filters.professionalId || undefined,
            specialtyId: filters.specialtyId || undefined,
            healthInsuranceId: filters.healthInsuranceId || undefined,
          },
        },
      );
      setSaveSuccess(`Filtros de "${report.name}" salvos.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar filtros.');
    } finally {
      setSavingReport(null);
    }
  }

  function printCurrentResult() {
    if (!result || !selectedReport) return;
    printAnalyticsReport(result, {
      dateFrom: filters.dateFrom,
      dateTo: filters.dateTo,
      patientName: patients.find((p) => p.id === filters.patientId)?.fullName,
      reportName: selectedReport.name,
      moduleLabel: activeGroup?.label ?? selectedReport.moduleLabel,
    });
  }

  const printLayout = result ? resolveReportPrintLayout(result.code) : null;

  if (!canRead) {
    return (
      <div className="reports-feegow-page">
        <div className="reports-empty-state">
          <p>Você não tem permissão para acessar relatórios (`reports.read`).</p>
          <Link to="/">Voltar ao início</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="reports-feegow-page">
      <ModuleNav basePath="/relatorios" tabs={reportsTabs} />

      <div className="box-header feegow-inner-box-header">
        <ul className="nav nav-tabs nav-tabs-left">
          <li className={view === 'catalog' ? 'active' : undefined}>
            <button type="button" className="reports-tab-btn" onClick={() => setView('catalog')}>
              <i className="icon-align-justify" aria-hidden /> Lista de relatórios
            </button>
          </li>
          {result && selectedReport ? (
            <li className={view === 'result' ? 'active' : undefined}>
              <button type="button" className="reports-tab-btn" onClick={() => setView('result')}>
                <i className="icon-bar-chart" aria-hidden /> Resultado
              </button>
            </li>
          ) : null}
        </ul>
      </div>

      <div className="box-content padded">
        {view === 'catalog' ? (
          <div className="reports-bayanno-layout">
            <nav className="reports-module-nav" aria-label="Grupos funcionais">
              <div className="reports-module-nav-head">Grupo</div>
              <button
                type="button"
                className={`reports-module-btn${selectedGroupId === null ? ' active' : ''}`}
                onClick={() => setSelectedGroupId(null)}
              >
                <span className="reports-module-label">Todos</span>
                <span className="reports-module-meta">
                  {summary?.implementedReports ?? '—'} disponíveis
                </span>
              </button>
              {REPORT_FUNCTIONAL_GROUPS.map((group) => {
                const count = filterCatalogByFunctionalGroup(catalog, group.id).length;
                return (
                  <button
                    key={group.id}
                    type="button"
                    className={`reports-module-btn${selectedGroupId === group.id ? ' active' : ''}`}
                    onClick={() => setSelectedGroupId(group.id)}
                  >
                    <span className="reports-module-label">{group.label}</span>
                    <span className="reports-module-meta">{count} relatório(s)</span>
                  </button>
                );
              })}
            </nav>

            <div className="reports-main-panel">
              {activeGroup ? (
                <div className="reports-group-intro">
                  <h3>{activeGroup.label}</h3>
                  <p>{activeGroup.description}</p>
                </div>
              ) : null}

              <ExternalSourcesPanel reportNames={reportNamesByCode} />

              <ReportFiltersPanel
                filters={filters}
                patients={patients}
                professionals={professionals}
                specialties={specialties}
                insurances={insurances}
                loading={loadingCatalog}
                onChange={patchFilters}
                onSearch={() => loadCatalog().catch(console.error)}
                onClear={clearFilters}
              />

              {error ? <p className="reports-message is-error">{error}</p> : null}
              {saveSuccess ? <p className="reports-message is-success">{saveSuccess}</p> : null}

              <div className="reports-summary-bar">
                {loadingCatalog
                  ? 'Carregando catálogo…'
                  : `Exibindo ${displayedCatalog.length} relatório(s)${activeGroup ? ` · ${activeGroup.label}` : ''}`}
              </div>

              <div className="table-responsive-wrap">
                <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">
                  <thead>
                    <tr>
                      <th style={{ width: 48 }}><div>#</div></th>
                      <th><div>Relatório</div></th>
                      <th className="reports-col-situacao"><div>Situação</div></th>
                      <th className="reports-col-options"><div>Opções</div></th>
                    </tr>
                  </thead>
                  <tbody>
                    {loadingCatalog ? (
                      <tr>
                        <td colSpan={4} className="dataTables_empty center">Carregando…</td>
                      </tr>
                    ) : displayedCatalog.length === 0 ? (
                      <tr>
                        <td colSpan={4} className="dataTables_empty center">
                          Nenhum relatório encontrado. Ajuste os filtros ou selecione outro grupo.
                        </td>
                      </tr>
                    ) : (
                      displayedCatalog.map((item, index) => (
                        <tr key={item.code} className={index % 2 === 1 ? 'even' : undefined}>
                          <td>{index + 1}</td>
                          <td>
                            <div className="reports-row-name">{item.name}</div>
                            <div className="reports-row-desc">{item.description}</div>
                            <span className="reports-row-module">{item.moduleLabel}</span>
                          </td>
                          <td className="reports-col-situacao">
                            {item.isImplemented ? (
                              <span className="bayanno-status-badge is-ready">Disponível</span>
                            ) : (
                              <span className="bayanno-status-badge is-pending">Fase {item.phase}</span>
                            )}
                            {item.isEssential ? (
                              <span className="bayanno-status-badge is-form" style={{ marginLeft: 4 }}>MVP</span>
                            ) : null}
                          </td>
                          <td className="center reports-col-options">
                            <div className="reports-row-actions">
                              <button
                                type="button"
                                className="btn btn-green reports-action-btn"
                                disabled={!item.isImplemented || running}
                                onClick={() => runReport(item)}
                              >
                                <i className="icon-play" aria-hidden />
                                {running && selectedReport?.code === item.code ? 'Gerando…' : 'Gerar'}
                              </button>
                              {item.isImplemented ? (
                                <button
                                  type="button"
                                  className="btn btn-blue reports-action-btn"
                                  disabled={savingReport === item.code}
                                  onClick={() => saveReportFilters(item)}
                                >
                                  Salvar filtros
                                </button>
                              ) : null}
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        ) : result && selectedReport ? (
          <div className="reports-result-screen">
            <div className="reports-result-toolbar">
              <div>
                <h4>{result.title}</h4>
                {result.subtitle ? <p>{result.subtitle}</p> : null}
                <p className="reports-result-period">
                  Período: {filters.dateFrom.split('-').reverse().join('/')}
                  {' — '}
                  {filters.dateTo.split('-').reverse().join('/')}
                  {' · '}
                  {result.rows.length} registro(s)
                  {printLayout ? (
                    <>
                      {' · '}
                      <span title={printLayout.sourceUrl || printLayout.sourceRepo}>
                        Layout A4: {printLayout.sourceLabel}
                      </span>
                    </>
                  ) : null}
                </p>
              </div>
              <div className="reports-result-actions">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => {
                    setView('catalog');
                    setResult(null);
                    setSelectedReport(null);
                  }}
                >
                  <i className="icon-arrow-left" aria-hidden /> Voltar
                </button>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => exportReportCsv(result)}>
                  <i className="icon-download-alt" aria-hidden /> Exportar CSV
                </button>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => exportReportExcel(result)}>
                  <i className="icon-download-alt" aria-hidden /> Exportar Excel
                </button>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => exportReportPdf(printCurrentResult)}
                >
                  <i className="icon-file" aria-hidden /> Exportar PDF
                </button>
                <button type="button" className="btn btn-secondary btn-sm" onClick={printCurrentResult}>
                  <i className="icon-print" aria-hidden /> Imprimir
                </button>
              </div>
            </div>

            {result.kpis.length > 0 ? (
              <table className="bayanno-stats-table reports-kpi-table">
                <thead>
                  <tr>
                    {result.kpis.map((kpi) => (
                      <th key={kpi.label}>{kpi.label}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    {result.kpis.map((kpi) => (
                      <td key={kpi.label}>{kpi.value}</td>
                    ))}
                  </tr>
                </tbody>
              </table>
            ) : null}

            <ReportDataTable columns={result.columns} rows={result.rows} />
          </div>
        ) : (
          <div className="reports-empty-state">Selecione um relatório na lista para gerar.</div>
        )}
      </div>
    </div>
  );
}
