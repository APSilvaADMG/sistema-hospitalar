import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  api,
  type HospitalizationHubDashboardDto,
  type HospitalizationHubListItemDto,
  type PatientDto,
  type ProfessionalDto,
  type WardDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import {
  HospitalizationDataTable,
  type HospitalizationRowAction,
} from '../components/hospitalization/HospitalizationDataTable';
import {
  HospitalizationFiltersPanel,
  type HospitalizationFiltersState,
} from '../components/hospitalization/HospitalizationFiltersPanel';
import { ModuleNav } from '../components/ModuleNav';
import {
  getHospitalizationGroupBySlug,
  HOSPITALIZATION_FUNCTIONAL_GROUPS,
} from '../data/hospitalizationFunctionalGroups';
import { hospitalizationTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { HospitalizationPage } from './HospitalizationPage';
import { printHospitalizationSummary } from '../utils/printTemplates';

const PAGE_SIZE = 25;
const WORKFLOW_SECTIONS = ['admissao', 'leitos', 'transferencias', 'altas', 'obitos'];

function defaultDateRange() {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 30);
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  };
}

function defaultFilters(): HospitalizationFiltersState {
  const range = defaultDateRange();
  return {
    dateFrom: range.from,
    dateTo: range.to,
    patientId: '',
    wardId: '',
    professionalId: '',
    modality: '',
    status: '',
    search: '',
  };
}

function HospitalizationHubOverview() {
  const { hasPermission } = useAuth();
  const navigate = useNavigate();
  const canManage = hasPermission('hospitalization.manage');
  const { section } = useModuleSection('/internacao');

  const sectionGroup = getHospitalizationGroupBySlug(section || undefined);
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(
    () => sectionGroup?.id ?? null,
  );
  const [filters, setFilters] = useState<HospitalizationFiltersState>(defaultFilters);
  const [page, setPage] = useState(1);
  const [dashboard, setDashboard] = useState<HospitalizationHubDashboardDto | null>(null);
  const [listResult, setListResult] = useState<{ total: number; items: HospitalizationHubListItemDto[] }>({
    total: 0,
    items: [],
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [wards, setWards] = useState<WardDto[]>([]);

  const activeGroup = useMemo(
    () => HOSPITALIZATION_FUNCTIONAL_GROUPS.find((g) => g.id === selectedGroupId) ?? null,
    [selectedGroupId],
  );

  useEffect(() => {
    const group = getHospitalizationGroupBySlug(section || undefined);
    if (group) setSelectedGroupId(group.id);
  }, [section]);

  const loadLookups = useCallback(async () => {
    const [patientList, profList, wardList] = await Promise.all([
      api.getPatients(undefined, 1),
      api.getProfessionals(),
      api.getWards(),
    ]);
    setPatients(patientList.items);
    setProfessionals(profList);
    setWards(wardList);
  }, []);

  const loadData = useCallback(async () => {
    if (!canManage) return;
    setLoading(true);
    setError('');
    try {
      const [dash, list] = await Promise.all([
        api.getHospitalizationHubDashboard(filters.dateFrom, filters.dateTo),
        api.getHospitalizationHubList({
          dateFrom: filters.dateFrom,
          dateTo: filters.dateTo,
          patientId: filters.patientId || undefined,
          wardId: filters.wardId || undefined,
          professionalId: filters.professionalId || undefined,
          modality: filters.modality ? Number(filters.modality) : undefined,
          status: filters.status ? Number(filters.status) : undefined,
          search: filters.search.trim() || undefined,
          groupId: selectedGroupId ?? undefined,
          skip: (page - 1) * PAGE_SIZE,
          take: PAGE_SIZE,
        }),
      ]);
      setDashboard(dash);
      setListResult(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar internações.');
    } finally {
      setLoading(false);
    }
  }, [canManage, filters, page, selectedGroupId]);

  useEffect(() => {
    loadLookups().catch(console.error);
  }, [loadLookups]);

  useEffect(() => {
    loadData().catch(console.error);
  }, [loadData]);

  function handleSearch() {
    setPage(1);
    loadData().catch(console.error);
  }

  function handleClear() {
    setFilters(defaultFilters());
    setPage(1);
  }

  function handleRowAction(action: HospitalizationRowAction, item: HospitalizationHubListItemDto) {
    if (action === 'pep') {
      navigate(`/pacientes/${item.patientId}/prontuario`);
      return;
    }
    if (action === 'admit') {
      navigate('/internacao/admissao', { state: { requestId: item.id } });
    }
  }

  function handlePrintSummary() {
    if (!dashboard) return;
    printHospitalizationSummary(dashboard, {
      dateFrom: filters.dateFrom,
      dateTo: filters.dateTo,
      items: listResult.items,
    });
  }

  if (!canManage) {
    return (
      <div className="page-content padded">
        <p>Sem permissão para acessar Internação (<code>hospitalization.manage</code>).</p>
      </div>
    );
  }

  return (
    <div className="page-content guides-bayanno-page">
      <ModuleNav basePath="/internacao" tabs={hospitalizationTabs} contextId="hospitalization" />

      <div className="box">
        <div className="box-content padded">
          <div className="guides-toolbar">
            <h2 style={{ margin: 0, flex: 1 }}>Gestão de Internação Hospitalar</h2>
            <Link to="/internacao/admissao" className="btn btn-primary">Admissão</Link>
            <Link to="/internacao/leitos" className="btn btn-secondary">Leitos</Link>
            <button type="button" className="btn btn-secondary" onClick={handlePrintSummary} disabled={!dashboard}>
              Imprimir resumo
            </button>
            <Link to="/guias/internacao" className="btn btn-secondary">Guias</Link>
            <Link to="/faturamento/sus/aih" className="btn btn-secondary">AIH SUS</Link>
          </div>

          {error && <div className="alert alert-danger" role="alert">{error}</div>}

          {dashboard && (
            <div className="guides-kpi-row">
              <div className="guides-kpi-card"><strong>{dashboard.activeCount}</strong><span>Internações ativas</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.availableBeds}</strong><span>Leitos disponíveis</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.occupiedBeds}</strong><span>Leitos ocupados</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.pendingRequests}</strong><span>Solicitações pendentes</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.dischargedInPeriod}</strong><span>Altas no período</span></div>
              <div className="guides-kpi-card">
                <strong>{dashboard.avgLengthOfStayDays != null ? `${dashboard.avgLengthOfStayDays}d` : '—'}</strong>
                <span>Permanência média</span>
              </div>
            </div>
          )}

          <div className="guides-bayanno-layout">
            <nav className="guides-module-nav" aria-label="Grupos de internação">
              <div className="guides-module-nav-head">Grupos funcionais</div>
              <button
                type="button"
                className={`guides-module-btn${selectedGroupId === null ? ' active' : ''}`}
                onClick={() => { setSelectedGroupId(null); setPage(1); }}
              >
                Visão consolidada
                <span className="guides-module-btn-desc">Internações e solicitações</span>
              </button>
              {HOSPITALIZATION_FUNCTIONAL_GROUPS.map((group) => (
                <button
                  key={group.id}
                  type="button"
                  className={`guides-module-btn${selectedGroupId === group.id ? ' active' : ''}`}
                  onClick={() => { setSelectedGroupId(group.id); setPage(1); }}
                >
                  {group.label}
                  <span className="guides-module-btn-desc">{group.description}</span>
                </button>
              ))}
            </nav>

            <div className="guides-main-panel">
              {activeGroup?.quickLinks && activeGroup.quickLinks.length > 0 && (
                <div className="guides-quick-links">
                  {activeGroup.quickLinks.map((link) => (
                    <Link key={link.path + link.label} to={link.path} className="guides-quick-link" title={link.description}>
                      {link.label}
                    </Link>
                  ))}
                </div>
              )}

              <HospitalizationFiltersPanel
                filters={filters}
                patients={patients}
                professionals={professionals}
                wards={wards}
                loading={loading}
                onChange={(patch) => setFilters((prev) => ({ ...prev, ...patch }))}
                onSearch={handleSearch}
                onClear={handleClear}
              />

              <HospitalizationDataTable
                items={listResult.items}
                total={listResult.total}
                loading={loading}
                canManage={canManage}
                onAction={handleRowAction}
                page={page}
                pageSize={PAGE_SIZE}
                onPageChange={setPage}
              />

              {dashboard && (
                <div className="guides-production-grid">
                  <SliceCard title="Por ala" slices={dashboard.byWard} />
                  <SliceCard title="Por modalidade" slices={dashboard.byModality} />
                  <SliceCard title="Por médico" slices={dashboard.byProfessional} />
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function SliceCard({ title, slices }: { title: string; slices: { label: string; count: number }[] }) {
  return (
    <div className="guides-production-card">
      <h4>{title}</h4>
      <ul>
        {slices.length === 0 && <li>Sem dados no período</li>}
        {slices.map((slice) => (
          <li key={slice.label}>
            <span>{slice.label}</span>
            <span>{slice.count}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function HospitalizationHubPage() {
  const { section } = useModuleSection('/internacao');
  if (WORKFLOW_SECTIONS.includes(section)) {
    return <HospitalizationPage />;
  }
  return <HospitalizationHubOverview />;
}
