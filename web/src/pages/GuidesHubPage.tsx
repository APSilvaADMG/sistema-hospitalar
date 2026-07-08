import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  api,
  type GuideHistoryEntryDto,
  type GuideHubListItemDto,
  type GuidesHubDashboardDto,
  type HealthInsuranceDto,
  type PatientDto,
  type ProfessionalDto,
  type SpecialtyDto,
  type ServiceUnitDto,
  type SusGuideDto,
  type TissGuideDto,
  type TissGuideTypeCatalogDto,
  susGuideTypeLabels,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { GuideDataTable, type GuideRowAction } from '../components/guides/GuideDataTable';
import { GuideFiltersPanel, type GuideFiltersState } from '../components/guides/GuideFiltersPanel';
import { TissGuideEditModal } from '../components/guides/TissGuideEditModal';
import { SusGuideEditModal } from '../components/guides/SusGuideEditModal';
import { SusAuthorizeModal } from '../components/guides/SusAuthorizeModal';
import { ServiceUnitsModal } from '../components/guides/ServiceUnitsModal';
import { ModuleNav } from '../components/ModuleNav';
import {
  GUIDE_FUNCTIONAL_GROUPS,
  getGuideGroupBySlug,
} from '../data/guideFunctionalGroups';
import { guidesTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrDateTime } from '../utils/dateUtils';
import { exportTissGuidePdf, printTissGuide } from '../utils/printTissGuide';

const PAGE_SIZE = 25;

function defaultDateRange() {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 30);
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  };
}

function defaultFilters(): GuideFiltersState {
  const range = defaultDateRange();
  return {
    dateFrom: range.from,
    dateTo: range.to,
    patientId: '',
    healthInsuranceId: '',
    professionalId: '',
    specialtyId: '',
    procedureSearch: '',
    guideNumber: '',
    status: '',
    serviceUnitId: '',
  };
}

function money(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function GuidesHubPage() {
  const { hasPermission } = useAuth();
  const navigate = useNavigate();
  const canRead = hasPermission('billing.read');
  const canWrite = hasPermission('billing.write');
  const { section } = useModuleSection('/guias');

  const sectionGroup = getGuideGroupBySlug(section || undefined);
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(
    () => sectionGroup?.id ?? null,
  );
  const [filters, setFilters] = useState<GuideFiltersState>(defaultFilters);
  const [page, setPage] = useState(1);
  const [dashboard, setDashboard] = useState<GuidesHubDashboardDto | null>(null);
  const [listResult, setListResult] = useState<{ total: number; items: GuideHubListItemDto[] }>({
    total: 0,
    items: [],
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [serviceUnits, setServiceUnits] = useState<ServiceUnitDto[]>([]);
  const [tissGuideCatalog, setTissGuideCatalog] = useState<TissGuideTypeCatalogDto[]>([]);

  const [tissCreateGuideType, setTissCreateGuideType] = useState<number>(1);
  const [susCreateGuideType, setSusCreateGuideType] = useState<number>(1);

  const [viewGuide, setViewGuide] = useState<TissGuideDto | null>(null);
  const [historyGuide, setHistoryGuide] = useState<GuideHubListItemDto | null>(null);
  const [historyEntries, setHistoryEntries] = useState<GuideHistoryEntryDto[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const [tissEditOpen, setTissEditOpen] = useState(false);
  const [tissEditGuide, setTissEditGuide] = useState<TissGuideDto | null>(null);
  const [susEditOpen, setSusEditOpen] = useState(false);
  const [susEditGuide, setSusEditGuide] = useState<SusGuideDto | null>(null);

  const [serviceUnitsModalOpen, setServiceUnitsModalOpen] = useState(false);

  const [susAuthorizeGuide, setSusAuthorizeGuide] = useState<GuideHubListItemDto | null>(null);
  const [susAuthorizeSaving, setSusAuthorizeSaving] = useState(false);

  const activeGroup = useMemo(
    () => GUIDE_FUNCTIONAL_GROUPS.find((g) => g.id === selectedGroupId) ?? null,
    [selectedGroupId],
  );

  useEffect(() => {
    const group = getGuideGroupBySlug(section || undefined);
    if (group) setSelectedGroupId(group.id);
  }, [section]);

  const loadLookups = useCallback(async () => {
    const [patientList, profList, specList, insList, units, guideTypes] = await Promise.all([
      api.getPatients(undefined, 1),
      api.getProfessionals(),
      api.getSpecialties(),
      api.getHealthInsurances(),
      api.getServiceUnits(),
      api.getTissGuideTypes(),
    ]);
    setPatients(patientList.items);
    setProfessionals(profList);
    setSpecialties(specList);
    setInsurances(insList);
    setServiceUnits(units);
    setTissGuideCatalog(guideTypes);
  }, []);

  const loadData = useCallback(async () => {
    if (!canRead) return;
    setLoading(true);
    setError('');
    try {
      const [dash, list] = await Promise.all([
        api.getGuidesHubDashboard(filters.dateFrom, filters.dateTo),
        api.getGuidesHubList({
          dateFrom: filters.dateFrom,
          dateTo: filters.dateTo,
          patientId: filters.patientId || undefined,
          healthInsuranceId: filters.healthInsuranceId || undefined,
          professionalId: filters.professionalId || undefined,
          specialtyId: filters.specialtyId || undefined,
          procedureSearch: filters.procedureSearch.trim() || undefined,
          guideNumber: filters.guideNumber.trim() || undefined,
          status: filters.status || undefined,
          serviceUnitId: filters.serviceUnitId || undefined,
          groupId: selectedGroupId ?? undefined,
          skip: (page - 1) * PAGE_SIZE,
          take: PAGE_SIZE,
        }),
      ]);
      setDashboard(dash);
      setListResult(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar guias.');
    } finally {
      setLoading(false);
    }
  }, [canRead, filters, page, selectedGroupId]);

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

  async function handleRowAction(action: GuideRowAction, item: GuideHubListItemDto) {
    setError('');
    setSuccess('');
    try {
      if (action === 'view') {
        if (item.source === 'sus') {
          const guide = await api.getSusGuide(item.id);
          setViewGuide(null);
          setSusEditGuide(guide);
          setSusEditOpen(true);
        } else {
          const guide = await api.getTissGuide(item.id);
          setViewGuide(guide);
        }
        return;
      }
      if (action === 'edit') {
        if (item.source === 'sus') {
          const guide = await api.getSusGuide(item.id);
          setViewGuide(null);
          setSusEditGuide(guide);
          setSusEditOpen(true);
        } else {
          const guide = await api.getTissGuide(item.id);
          setTissEditGuide(guide);
          setTissEditOpen(true);
        }
        return;
      }
      if (action === 'cancel') {
        if (!window.confirm(`Cancelar a guia ${item.guideNumber}?`)) return;
        if (item.source === 'sus') await api.cancelSusGuide(item.id);
        else await api.cancelTissGuide(item.id);
        setSuccess('Guia cancelada.');
        await loadData();
        return;
      }
      if (action === 'print' || action === 'pdf') {
        if (item.source !== 'tiss') return;
        const guide = await api.getTissGuide(item.id);
        if (action === 'print') printTissGuide(guide);
        else exportTissGuidePdf(guide);
        return;
      }
      if (action === 'duplicate') {
        if (item.source === 'sus') {
          const dup = await api.duplicateSusGuide(item.id);
          setSuccess(`Guia duplicada: ${dup.guideNumber}`);
        } else {
          const dup = await api.duplicateGuide(item.id);
          setSuccess(`Guia duplicada: ${dup.guideNumber}`);
        }
        await loadData();
        return;
      }
      if (action === 'authorize') {
        if (item.source === 'sus') {
          setSusAuthorizeGuide(item);
          return;
        }
        navigate('/faturamento-tiss/autorizacoes', { state: { guideId: item.id } });
        return;
      }
      if (action === 'history') {
        setHistoryGuide(item);
        setHistoryLoading(true);
        const entries = await api.getGuideHistory(item.id, item.source);
        setHistoryEntries(entries);
        setHistoryLoading(false);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível concluir a ação.');
      setHistoryLoading(false);
    }
  }

  function handleNewGuide() {
    if (activeGroup?.id === 'sus') {
      openSusCreate(susCreateGuideType);
      return;
    }

    const initialType = activeGroup?.guideTypes?.[0] ?? 1;
    setTissCreateGuideType(initialType);
    setTissEditGuide(null);
    setTissEditOpen(true);
  }

  function openSusCreate(guideType: number) {
    setViewGuide(null);
    setSusEditGuide(null);
    setSusCreateGuideType(guideType);
    setSusEditOpen(true);
  }

  async function handleSusAuthorize(authorizationNumber?: string) {
    if (!susAuthorizeGuide) return;
    setSusAuthorizeSaving(true);
    setError('');
    try {
      await api.authorizeSusGuide(susAuthorizeGuide.id, authorizationNumber);
      setSuccess('Guia SUS autorizada.');
      setSusAuthorizeGuide(null);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível autorizar a guia.');
    } finally {
      setSusAuthorizeSaving(false);
    }
  }

  if (!canRead) {
    return (
      <div className="page-content padded">
        <p>Sem permissão para acessar o módulo de Guias (<code>billing.read</code>).</p>
      </div>
    );
  }

  return (
    <div className="page-content guides-bayanno-page">
      <ModuleNav basePath="/guias" tabs={guidesTabs} contextId="insurance" />

      <div className="box">
        <div className="box-content padded">
          <div className="guides-toolbar">
            <h2 style={{ margin: 0, flex: 1 }}>Gestão de Guias Hospitalares</h2>
            {canWrite && (
              <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                {activeGroup?.id === 'sus' && (
                  <select
                    className="form-control"
                    value={susCreateGuideType}
                    onChange={(e) => setSusCreateGuideType(Number(e.target.value))}
                    aria-label="Tipo da guia SUS"
                  >
                    {Object.entries(susGuideTypeLabels).map(([v, l]) => (
                      <option key={v} value={v}>
                        {l}
                      </option>
                    ))}
                  </select>
                )}
                <button type="button" className="btn btn-primary" onClick={handleNewGuide}>
                  {activeGroup?.id === 'sus' ? 'Nova guia SUS' : 'Nova guia'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setServiceUnitsModalOpen(true)}
                  title="Cadastrar unidades de atendimento"
                >
                  Unidades
                </button>
              </div>
            )}
            <Link to="/faturamento-tiss" className="btn btn-secondary">TISS Convênios</Link>
            <Link to="/faturamento-tiss/guias-funi" className="btn btn-secondary">Formulários FUNI</Link>
          </div>

          {error && <div className="alert alert-danger" role="alert">{error}</div>}
          {success && <div className="alert alert-success" role="status">{success}</div>}

          {dashboard && (
            <div className="guides-kpi-row">
              <div className="guides-kpi-card"><strong>{dashboard.issuedCount}</strong><span>Emitidas no período</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.authorizedCount}</strong><span>Autorizadas</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.pendingCount}</strong><span>Pendentes</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.billedCount}</strong><span>Faturadas</span></div>
              <div className="guides-kpi-card"><strong>{dashboard.glosaCount}</strong><span>Glosadas</span></div>
              <div className="guides-kpi-card">
                <strong>{dashboard.avgAuthorizationHours != null ? `${dashboard.avgAuthorizationHours}h` : '—'}</strong>
                <span>Tempo médio autorização</span>
              </div>
            </div>
          )}

          <div className="guides-bayanno-layout">
            <nav className="guides-module-nav" aria-label="Grupos de guias">
              <div className="guides-module-nav-head">Grupos funcionais</div>
              <button
                type="button"
                className={`guides-module-btn${selectedGroupId === null ? ' active' : ''}`}
                onClick={() => { setSelectedGroupId(null); setPage(1); }}
              >
                Todas as guias
                <span className="guides-module-btn-desc">Visão consolidada TISS</span>
              </button>
              {GUIDE_FUNCTIONAL_GROUPS.map((group) => (
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
                    link.susGuideType && canWrite ? (
                      <button
                        key={link.path + link.label}
                        type="button"
                        className="guides-quick-link"
                        title={link.description}
                        onClick={() => openSusCreate(link.susGuideType!)}
                      >
                        {link.label}
                      </button>
                    ) : (
                      <Link key={link.path + link.label} to={link.path} className="guides-quick-link" title={link.description}>
                        {link.label}
                      </Link>
                    )
                  ))}
                </div>
              )}

              <GuideFiltersPanel
                filters={filters}
                patients={patients}
                professionals={professionals}
                specialties={specialties}
                insurances={insurances}
                serviceUnits={serviceUnits}
                loading={loading}
                onChange={(patch) => setFilters((prev) => ({ ...prev, ...patch }))}
                onSearch={handleSearch}
                onClear={handleClear}
              />

              <GuideDataTable
                items={listResult.items}
                total={listResult.total}
                loading={loading}
                canWrite={canWrite}
                onAction={handleRowAction}
                page={page}
                pageSize={PAGE_SIZE}
                onPageChange={setPage}
              />

              {dashboard && (
                <div className="guides-production-grid">
                  <ProductionCard title="Produção por convênio" slices={dashboard.byInsurance} />
                  <ProductionCard title="Produção por médico" slices={dashboard.byProfessional} />
                  <ProductionCard title="Produção por especialidade" slices={dashboard.bySpecialty} />
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {viewGuide && (
        <div className="guides-modal-backdrop" role="presentation" onClick={() => setViewGuide(null)}>
          <div className="guides-modal" role="dialog" aria-modal onClick={(e) => e.stopPropagation()}>
            <div className="guides-modal-header">
              <h3 style={{ margin: 0 }}>{viewGuide.guideNumber}</h3>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => setViewGuide(null)}>Fechar</button>
            </div>
            <div className="guides-modal-body">
              <p><strong>Paciente:</strong> {viewGuide.patientName}</p>
              <p><strong>Convênio:</strong> {viewGuide.healthInsuranceName}</p>
              <p><strong>Médico:</strong> {viewGuide.clinical.requestingProfessionalName ?? '—'}</p>
              <p><strong>CID:</strong> {viewGuide.clinical.cid10Code ?? '—'}</p>
              <p><strong>Total:</strong> {money(viewGuide.totalAmount)}</p>
              <ul>
                {viewGuide.items.map((item) => (
                  <li key={item.id}>{item.tussCode} — {item.description} ({item.quantity} × {money(item.unitPrice)})</li>
                ))}
              </ul>
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => printTissGuide(viewGuide)}>Imprimir</button>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => exportTissGuidePdf(viewGuide)}>Exportar PDF</button>
                <Link to="/faturamento-tiss" className="btn btn-primary btn-sm">Abrir no TISS</Link>
              </div>
            </div>
          </div>
        </div>
      )}

      <TissGuideEditModal
        open={tissEditOpen}
        editingGuide={tissEditGuide}
        initialGuideType={tissCreateGuideType}
        patients={patients}
        insurances={insurances}
        serviceUnits={serviceUnits}
        guideCatalog={tissGuideCatalog}
        onClose={() => {
          setTissEditOpen(false);
          setTissEditGuide(null);
        }}
        onSaved={(message) => {
          setSuccess(message);
          setError('');
          void loadData().catch(console.error);
        }}
        onError={(message) => {
          setError(message);
        }}
      />

      <SusGuideEditModal
        open={susEditOpen}
        editingGuide={susEditGuide}
        initialGuideType={susCreateGuideType}
        patients={patients}
        professionals={professionals}
        serviceUnits={serviceUnits}
        onClose={() => {
          setSusEditOpen(false);
          setSusEditGuide(null);
        }}
        onSaved={(message) => {
          setSuccess(message);
          setError('');
          void loadData().catch(console.error);
        }}
        onError={(message) => {
          setError(message);
        }}
      />

      <ServiceUnitsModal
        open={serviceUnitsModalOpen}
        onClose={() => setServiceUnitsModalOpen(false)}
        onChanged={() => loadLookups().catch(console.error)}
        onError={(message) => setError(message)}
        onSuccess={(message) => setSuccess(message)}
      />

      <SusAuthorizeModal
        open={!!susAuthorizeGuide}
        guide={susAuthorizeGuide}
        saving={susAuthorizeSaving}
        onClose={() => setSusAuthorizeGuide(null)}
        onConfirm={(authorizationNumber) => {
          handleSusAuthorize(authorizationNumber).catch(console.error);
        }}
      />

      {historyGuide && (
        <div className="guides-modal-backdrop" role="presentation" onClick={() => setHistoryGuide(null)}>
          <div className="guides-modal" role="dialog" aria-modal onClick={(e) => e.stopPropagation()}>
            <div className="guides-modal-header">
              <h3 style={{ margin: 0 }}>Histórico — {historyGuide.guideNumber}</h3>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => setHistoryGuide(null)}>Fechar</button>
            </div>
            <div className="guides-modal-body">
              {historyLoading && <p>Carregando histórico…</p>}
              {!historyLoading && historyEntries.length === 0 && <p>Nenhum registro encontrado.</p>}
              <ul className="guides-history-list">
                {historyEntries.map((entry, idx) => (
                  <li key={`${entry.occurredAt}-${idx}`}>
                    <time>{formatBrDateTime(entry.occurredAt)}</time>
                    <strong>{entry.action}</strong>
                    <div>{entry.details}</div>
                    {entry.userEmail && <div style={{ fontSize: 11, color: '#888' }}>{entry.userEmail} · {entry.source}</div>}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function ProductionCard({
  title,
  slices,
}: {
  title: string;
  slices: { label: string; count: number; amount: number }[];
}) {
  return (
    <div className="guides-production-card">
      <h4>{title}</h4>
      <ul>
        {slices.length === 0 && <li>Sem dados no período</li>}
        {slices.map((slice) => (
          <li key={slice.label}>
            <span>{slice.label}</span>
            <span>{slice.count} · {money(slice.amount)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
