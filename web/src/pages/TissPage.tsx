import { Fragment, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import {
  api,
  glosaContestationLabels,
  tissGuideStatusLabels,
  tissGuideTypeLabels,
  type CreateTissGuideRequest,
  type TissGuideClinicalRequest,
  type HealthInsuranceDto,
  type PatientDto,
  type RegisterGlosaRequest,
  type TissGlosaDto,
  type TissGuideDto,
  type TissGuideItemRequest,
  type TissGuideTypeCatalogDto,
  type TussSearchResultDto,
  type UpdateGlosaRequest,
  type UpdateTissGuideItemRequest,
  type UpdateTissGuideRequest,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';
import { TissConvenioPanels } from './TissConvenioPanels';
import { TissExtendedPanels } from './TissExtendedPanels';
import { TissGlosaFechamentoPanels } from './TissGlosaFechamentoPanels';
import { TissGuidesReferencePanel } from './TissGuidesReferencePanel';
import { getGuideCatalogEntry } from '../data/tissGuideCatalog';
import { TissGuideClinicalFields } from '../components/TissGuideClinicalFields';
import { ModuleNav } from '../components/ModuleNav';
import { tissTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { resolveTissInsertGuideType } from '../components/feegow/faturamento/feegowFaturamentoNav';

type TissTab = 'guides' | 'glosas' | 'recursos' | 'fechamento' | 'reference' | 'eligibility' | 'authorizations' | 'batches' | 'demonstrativos' | 'tuss' | 'sigtap' | 'integrations' | 'annexes' | 'reconciliation' | 'dashboard';

const defaultItem = (): TissGuideItemRequest => ({
  tussCode: '',
  description: '',
  quantity: 1,
  unitPrice: 0,
});

const defaultGlosa = (): RegisterGlosaRequest => ({
  reason: '',
  glosaAmount: 0,
});

type GuideForm = {
  patientId: string;
  healthInsuranceId: string;
  guideType: number;
  appointmentId: string;
  hospitalizationId: string;
  surgeryId: string;
  notes: string;
  clinical: TissGuideClinicalRequest;
  items: UpdateTissGuideItemRequest[];
};

function emptyClinical(): TissGuideClinicalRequest {
  return { serviceCharacter: 1, accidentIndicator: 0 };
}

function guideToForm(g: TissGuideDto): GuideForm {
  return {
    patientId: g.patientId,
    healthInsuranceId: g.healthInsuranceId,
    guideType: g.guideType,
    appointmentId: g.appointmentId ?? '',
    hospitalizationId: g.hospitalizationId ?? '',
    surgeryId: g.clinical.surgeryId ?? '',
    notes: g.notes ?? '',
    clinical: { ...g.clinical },
    items: g.items.map((i) => ({
      id: i.id,
      tussCode: i.tussCode,
      description: i.description,
      quantity: i.quantity,
      unitPrice: i.unitPrice,
      priceTableSource: i.priceTableSource,
      cid10Code: i.cid10Code,
      relatedTussCode: i.relatedTussCode,
    })),
  };
}

function emptyGuideForm(): GuideForm {
  return {
    patientId: '',
    healthInsuranceId: '',
    guideType: 1,
    appointmentId: '',
    hospitalizationId: '',
    surgeryId: '',
    notes: '',
    clinical: emptyClinical(),
    items: [{ ...defaultItem() }],
  };
}

function statusBadgeClass(status: number) {
  return `badge tiss-status-${status}`;
}

function formatMoney(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

const TISS_SECTION_TAB: Partial<Record<string, TissTab>> = {
  '': 'guides',
  fechamento: 'fechamento',
  autorizacoes: 'authorizations',
  lotes: 'batches',
  glosas: 'glosas',
  'recursos-glosa': 'recursos',
};

export function TissPage() {
  const { hasPermission } = useAuth();
  const { section } = useModuleSection('/faturamento-tiss');
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const canAccess = hasPermission('billing.read', 'billing.write');
  const canManage = hasPermission('billing.write');

  const [guides, setGuides] = useState<TissGuideDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [patientFilter, setPatientFilter] = useState('');
  const [search, setSearch] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const [showGuideModal, setShowGuideModal] = useState(false);
  const [editingGuide, setEditingGuide] = useState<TissGuideDto | null>(null);
  const [form, setForm] = useState<GuideForm>(emptyGuideForm());

  const [glosaForm, setGlosaForm] = useState<RegisterGlosaRequest>(defaultGlosa());
  const [editingGlosa, setEditingGlosa] = useState<TissGlosaDto | null>(null);

  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [activeTab, setActiveTab] = useState<TissTab>(TISS_SECTION_TAB[section] ?? 'guides');

  useEffect(() => {
    const tab = TISS_SECTION_TAB[section];
    if (tab) setActiveTab(tab);
  }, [section]);

  useEffect(() => {
    if (section === 'guias-funi' || section.startsWith('guias-funi/')) {
      navigate('/faturamento-tiss', { replace: true });
    }
  }, [section, navigate]);

  useEffect(() => {
    if (!canManage) return;

    const insertType = resolveTissInsertGuideType(section);
    if (insertType) {
      setEditingGuide(null);
      setForm({ ...emptyGuideForm(), guideType: insertType });
      setShowGuideModal(true);
      setActiveTab('guides');
      navigate('/faturamento-tiss', { replace: true });
      return;
    }

    const novo = searchParams.get('novo');
    const tipo = searchParams.get('tipo');
    if (novo === '1' && tipo) {
      const guideType = Number(tipo);
      if ([1, 2, 5].includes(guideType)) {
        setEditingGuide(null);
        setForm({ ...emptyGuideForm(), guideType });
        setShowGuideModal(true);
        setActiveTab('guides');
        const next = new URLSearchParams(searchParams);
        next.delete('novo');
        next.delete('tipo');
        setSearchParams(next, { replace: true });
      }
    }
  }, [section, searchParams, setSearchParams, navigate, canManage]);
  const [tussResults, setTussResults] = useState<Record<number, TussSearchResultDto[]>>({});
  const [contestNotes, setContestNotes] = useState('');
  const [contestingGlosaId, setContestingGlosaId] = useState<string | null>(null);
  const [guideCatalog, setGuideCatalog] = useState<TissGuideTypeCatalogDto[]>([]);
  const [prefillCard, setPrefillCard] = useState('');
  const [prefillPlan, setPrefillPlan] = useState('');
  const [prefillAuth, setPrefillAuth] = useState('');

  const selectedGuideTypeInfo = useMemo(
    () => getGuideCatalogEntry(form.guideType, guideCatalog),
    [form.guideType, guideCatalog],
  );

  async function load() {
    const [guideList, patientList, insList] = await Promise.all([
      api.getTissGuides(
        statusFilter ? Number(statusFilter) : undefined,
        patientFilter || undefined,
        search || undefined,
      ),
      api.getPatients(undefined, 1),
      api.getHealthInsurances(),
    ]);
    setGuides(guideList);
    setPatients(patientList.items);
    setInsurances(insList.filter((i) => i.name !== 'Particular' && i.name !== 'SUS'));
  }

  useEffect(() => {
    load().catch(console.error);
    api.getTissGuideTypes().then(setGuideCatalog).catch(console.error);
  }, []);

  useEffect(() => {
    if (!showGuideModal || editingGuide || !form.patientId) return;
    api.getTissGuidePrefill({
      patientId: form.patientId,
      guideType: form.guideType,
      healthInsuranceId: form.healthInsuranceId || undefined,
    }).then((prefill) => {
      setPrefillCard(prefill.beneficiaryCardNumber ?? '');
      setPrefillPlan(prefill.beneficiaryPlanName ?? '');
      setPrefillAuth(prefill.authorizationPassword ?? '');
      setForm((f) => ({
        ...f,
        healthInsuranceId: prefill.healthInsuranceId ?? f.healthInsuranceId,
        appointmentId: prefill.appointmentId ?? f.appointmentId,
        hospitalizationId: prefill.hospitalizationId ?? f.hospitalizationId,
        surgeryId: prefill.surgeryId ?? f.surgeryId,
        clinical: {
          ...f.clinical,
          cid10Code: prefill.cid10Code ?? f.clinical.cid10Code,
          requestingProfessionalId: prefill.requestingProfessionalId,
          requestingProfessionalName: prefill.requestingProfessionalName,
          requestingProfessionalCrm: prefill.requestingProfessionalCrm,
          executingProfessionalId: prefill.executingProfessionalId,
          executingProfessionalName: prefill.executingProfessionalName,
          admissionDate: prefill.admissionDate,
          dischargeDate: prefill.dischargeDate,
          requestedBedType: prefill.requestedBedType,
          surgeryId: prefill.surgeryId,
        },
        items: prefill.suggestedItems.length > 0
          ? prefill.suggestedItems.map((i) => ({ ...i }))
          : f.items,
      }));
    }).catch(console.error);
  }, [showGuideModal, editingGuide, form.patientId, form.guideType, form.healthInsuranceId]);

  const stats = useMemo(() => ({
    total: guides.length,
    drafts: guides.filter((g) => g.status === 1).length,
    sent: guides.filter((g) => g.status === 2).length,
    paid: guides.filter((g) => g.status === 3).length,
    glosa: guides.filter((g) => g.status === 4).length,
    glosaAmount: guides.flatMap((g) => g.glosas).filter((g) => !g.isResolved).reduce((s, g) => s + g.glosaAmount, 0),
    totalBilled: guides.reduce((s, g) => s + g.totalAmount, 0),
  }), [guides]);

  const formTotal = useMemo(
    () => form.items.reduce((s, i) => s + i.quantity * i.unitPrice, 0),
    [form.items],
  );

  function openCreate(guideType = 1) {
    setEditingGuide(null);
    setForm({ ...emptyGuideForm(), guideType });
    setShowGuideModal(true);
  }

  function openEdit(guide: TissGuideDto) {
    setEditingGuide(guide);
    setForm(guideToForm(guide));
    setShowGuideModal(true);
  }

  async function handleSearch(event: FormEvent) {
    event.preventDefault();
    await load();
  }

  function updateItem(index: number, patch: Partial<UpdateTissGuideItemRequest>) {
    setForm((f) => ({
      ...f,
      items: f.items.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    }));
  }

  async function searchTussForItem(index: number, query: string) {
    if (query.trim().length < 2) {
      setTussResults((prev) => ({ ...prev, [index]: [] }));
      return;
    }
    const results = await api.searchTuss(query);
    setTussResults((prev) => ({ ...prev, [index]: results }));
  }

  async function loadSuggestedItems() {
    if (!form.patientId) {
      setError('Selecione o paciente antes de importar procedimentos.');
      return;
    }
    try {
      const items = await api.getSuggestedTissItems({
        patientId: form.patientId,
        hospitalizationId: form.hospitalizationId || undefined,
        appointmentId: form.appointmentId || undefined,
        guideType: form.guideType,
        surgeryId: form.surgeryId || form.clinical.surgeryId || undefined,
      });
      if (items.length === 0) {
        setSuccess('Nenhum procedimento pendente encontrado para importar.');
        return;
      }
      setForm((f) => ({
        ...f,
        items: items.map((i) => ({ ...i })),
      }));
      setSuccess(`${items.length} procedimento(s) importado(s) do prontuário.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao importar procedimentos.');
    }
  }

  function addItem() {
    setForm((f) => ({ ...f, items: [...f.items, { ...defaultItem() }] }));
  }

  function removeItem(index: number) {
    setForm((f) => ({ ...f, items: f.items.filter((_, i) => i !== index) }));
  }

  async function handleSaveGuide(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      if (editingGuide) {
        const payload: UpdateTissGuideRequest = {
          healthInsuranceId: form.healthInsuranceId,
          appointmentId: form.appointmentId || undefined,
          hospitalizationId: form.hospitalizationId || undefined,
          guideType: form.guideType,
          notes: form.notes || undefined,
          clinical: {
            ...form.clinical,
            surgeryId: form.surgeryId || form.clinical.surgeryId,
          },
          items: form.items,
        };
        await api.updateTissGuide(editingGuide.id, payload);
        setSuccess('Guia atualizada.');
      } else {
        const payload: CreateTissGuideRequest = {
          patientId: form.patientId,
          healthInsuranceId: form.healthInsuranceId,
          appointmentId: form.appointmentId || undefined,
          hospitalizationId: form.hospitalizationId || undefined,
          guideType: form.guideType,
          notes: form.notes || undefined,
          clinical: {
            ...form.clinical,
            surgeryId: form.surgeryId || form.clinical.surgeryId,
          },
          items: form.items,
        };
        await api.createTissGuide(payload);
        setSuccess('Guia criada em rascunho.');
      }
      setShowGuideModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar guia.');
    }
  }

  async function runAction(
    action: () => Promise<unknown>,
    message: string,
  ) {
    setError('');
    setSuccess('');
    try {
      await action();
      setSuccess(message);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na operação.');
    }
  }

  async function handleGlosaSubmit(guide: TissGuideDto, event: FormEvent) {
    event.preventDefault();
    if (editingGlosa) {
      const payload: UpdateGlosaRequest = {
        tissGuideItemId: glosaForm.tissGuideItemId,
        reason: glosaForm.reason,
        glosaAmount: glosaForm.glosaAmount,
      };
      await runAction(() => api.updateTissGlosa(editingGlosa.id, payload), 'Glosa atualizada.');
    } else {
      await runAction(
        () => api.registerTissGlosa(guide.id, glosaForm),
        'Glosa registrada.',
      );
    }
    setGlosaForm(defaultGlosa());
    setEditingGlosa(null);
  }

  function startEditGlosa(glosa: TissGlosaDto) {
    setEditingGlosa(glosa);
    setGlosaForm({
      tissGuideItemId: glosa.tissGuideItemId,
      reason: glosa.reason,
      glosaAmount: glosa.glosaAmount,
    });
  }

  if (!canAccess) {
    return <div className="card">Acesso restrito à equipe de faturamento.</div>;
  }

  const isDraftForm = !editingGuide || editingGuide.status === 1;

  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title="Faturamento TISS / Convênios"
        subtitle="Guias ANS, elegibilidade, autorizações, lotes XML e glosas."
      >
        {activeTab === 'guides' && canManage && (
          <button className="btn" type="button" onClick={() => openCreate()}>+ Nova guia</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/faturamento-tiss" tabs={tissTabs} contextId="insurance" />

      <div className="tab-bar" style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 20 }}>
        {([
          ['guides', 'Guias TISS'],
          ['reference', 'Tipos ANS (19)'],
          ['eligibility', 'Elegibilidade'],
          ['authorizations', 'Autorizações'],
          ['batches', 'Lotes XML'],
          ['demonstrativos', 'Demonstrativos'],
          ['tuss', 'TUSS'],
          ['sigtap', 'SIGTAP/SUS'],
          ['annexes', 'Anexos'],
          ['integrations', 'Integrações'],
          ['reconciliation', 'Conciliação'],
          ['dashboard', 'Indicadores'],
        ] as const).map(([id, label]) => (
          <button
            key={id}
            type="button"
            className={`btn ${activeTab === id ? '' : 'btn-secondary'}`}
            onClick={() => {
              setActiveTab(id);
              if (id === 'authorizations') navigate('/faturamento-tiss/autorizacoes');
              else if (id === 'batches') navigate('/faturamento-tiss/lotes');
              else if (id === 'guides') navigate('/faturamento-tiss');
            }}
          >
            {label}
          </button>
        ))}
      </div>

      {(error || success) && activeTab !== 'guides' && (
        <div style={{ marginBottom: 16 }}>
          {error && <div className="alert alert-error">{error}</div>}
          {success && <div className="alert alert-success">{success}</div>}
        </div>
      )}

      {activeTab === 'reference' && (
        <TissGuidesReferencePanel
          catalog={guideCatalog}
          onCreateGuide={(code) => {
            setActiveTab('guides');
            openCreate(code);
          }}
        />
      )}

      {['glosas', 'recursos', 'fechamento'].includes(activeTab) && (
        <TissGlosaFechamentoPanels
          tab={activeTab as 'glosas' | 'recursos' | 'fechamento'}
          guides={guides}
          canManage={canManage}
          onReload={load}
          onMessage={(err, ok) => { setError(err); setSuccess(ok); }}
        />
      )}

      {activeTab !== 'guides' && activeTab !== 'reference' && !['glosas', 'recursos', 'fechamento'].includes(activeTab) && ['eligibility', 'authorizations', 'batches', 'dashboard'].includes(activeTab) && (
        <TissConvenioPanels
          tab={activeTab as 'eligibility' | 'authorizations' | 'batches' | 'dashboard'}
          patients={patients}
          insurances={insurances}
          onMessage={(err, ok) => { setError(err); setSuccess(ok); }}
        />
      )}

      {activeTab !== 'guides' && activeTab !== 'reference' && !['glosas', 'recursos', 'fechamento'].includes(activeTab) && !['eligibility', 'authorizations', 'batches', 'dashboard'].includes(activeTab) && (
        <TissExtendedPanels
          tab={activeTab as 'demonstrativos' | 'tuss' | 'sigtap' | 'integrations' | 'annexes' | 'reconciliation'}
          insurances={insurances}
          guides={guides}
          onMessage={(err, ok) => { setError(err); setSuccess(ok); }}
        />
      )}

      {activeTab === 'guides' && (
      <>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Guias" value={stats.total} variant="primary" />
        <KpiCard label="Rascunhos" value={stats.drafts} variant="info" />
        <KpiCard label="Enviadas" value={stats.sent} variant="warning" />
        <KpiCard label="Pagas" value={stats.paid} variant="success" />
        <KpiCard label="Em glosa" value={stats.glosa} variant="warning" />
        <KpiCard label="Valor glosado aberto" value={formatMoney(stats.glosaAmount)} variant="info" />
      </div>

      <div className="card-panel appt-panel">
        <FilterBar onSubmit={handleSearch} actions={<button className="btn" type="submit">Filtrar</button>}>
          <div className="filter-field w-sm">
            <label htmlFor="tissStatus">Status</label>
            <select id="tissStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(tissGuideStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-xl">
            <label htmlFor="tissPatient">Paciente</label>
            <select id="tissPatient" value={patientFilter} onChange={(e) => setPatientFilter(e.target.value)}>
              <option value="">Todos</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="tissSearch">Buscar</label>
            <input
              id="tissSearch"
              placeholder="Nº guia, paciente ou convênio..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>

        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table tiss-master-table">
            <thead>
              <tr>
                <th />
                <th>Guia</th>
                <th>Paciente</th>
                <th>Convênio</th>
                <th>Tipo</th>
                <th>Status</th>
                <th>Total</th>
                <th>Criada em</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {guides.map((g) => (
                <Fragment key={g.id}>
                  <tr>
                    <td>
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => setExpandedId(expandedId === g.id ? null : g.id)}
                      >
                        {expandedId === g.id ? '−' : '+'}
                      </button>
                    </td>
                    <td><strong>{g.guideNumber}</strong></td>
                    <td>
                      <Link to={`/pacientes/${g.patientId}/prontuario`}>{g.patientName}</Link>
                    </td>
                    <td>{g.healthInsuranceName}</td>
                    <td>{tissGuideTypeLabels[g.guideType]}</td>
                    <td>
                      <span className={statusBadgeClass(g.status)}>{tissGuideStatusLabels[g.status]}</span>
                      {g.status === 1 && (
                        <div className="form-hint" style={{ marginTop: 4 }}>
                          {g.accountClosedAt ? 'Conta fechada' : 'Conta aberta'}
                        </div>
                      )}
                    </td>
                    <td>{formatMoney(g.totalAmount)}</td>
                    <td>{formatBrDateTime(g.createdAt)}</td>
                    <td>
                      <div className="table-actions">
                        {g.status === 1 && canManage && (
                          <>
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => openEdit(g)}>Editar</button>
                            {!g.accountClosedAt && (
                              <button type="button" className="btn btn-secondary btn-sm" onClick={() => runAction(() => api.closeTissGuideAccount(g.id), 'Conta fechada (RN-028).')}>Fechar conta</button>
                            )}
                            <button
                              type="button"
                              className="btn btn-sm"
                              disabled={!g.accountClosedAt}
                              title={!g.accountClosedAt ? 'Feche a conta antes de enviar (RN-028)' : undefined}
                              onClick={() => runAction(() => api.sendTissGuide(g.id), 'Guia enviada.')}
                            >
                              Enviar
                            </button>
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => runAction(() => api.deleteTissGuide(g.id), 'Guia excluída.')}>Excluir</button>
                          </>
                        )}
                        {(g.status === 2 || g.status === 4) && (
                          <button type="button" className="btn btn-sm" onClick={() => runAction(() => api.markTissGuidePaid(g.id), 'Guia marcada como paga.')}>Marcar paga</button>
                        )}
                        {g.status !== 3 && g.status !== 5 && (
                          <button type="button" className="btn btn-secondary btn-sm" onClick={() => runAction(() => api.cancelTissGuide(g.id), 'Guia cancelada.')}>Cancelar</button>
                        )}
                      </div>
                    </td>
                  </tr>
                  {expandedId === g.id && (
                    <tr className="tiss-detail-row">
                      <td colSpan={9}>
                        <div className="tiss-detail-panel">
                          {g.notes && <p><strong>Observações:</strong> {g.notes}</p>}
                          {g.sentAt && <p><strong>Enviada em:</strong> {formatBrDateTime(g.sentAt)}</p>}
                          {(g.beneficiaryCardNumber || g.authorizationPassword) && (
                            <p>
                              <strong>Beneficiário:</strong> {g.beneficiaryCardNumber ?? '—'}
                              {g.beneficiaryPlanName ? ` · ${g.beneficiaryPlanName}` : ''}
                              {g.authorizationPassword ? ` · Senha: ${g.authorizationPassword}` : ''}
                            </p>
                          )}

                          <h4>Procedimentos TUSS</h4>
                          <table className="data-table tiss-items-table">
                            <thead>
                              <tr><th>TUSS</th><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th><th>Auditado</th></tr>
                            </thead>
                            <tbody>
                              {g.items.map((i) => (
                                <tr key={i.id}>
                                  <td>{i.tussCode}</td>
                                  <td>{i.description}</td>
                                  <td>{i.quantity}</td>
                                  <td>{formatMoney(i.unitPrice)}</td>
                                  <td>{formatMoney(i.total)}</td>
                                  <td>{i.isAudited ? 'Sim' : 'Não'}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>

                          <h4>Glosas</h4>
                          {g.glosas.length > 0 ? (
                            <table className="data-table tiss-items-table">
                              <thead>
                                <tr><th>Item</th><th>Motivo</th><th>Valor</th><th>Status</th><th>Ações</th></tr>
                              </thead>
                              <tbody>
                                {g.glosas.map((gl) => (
                                  <tr key={gl.id}>
                                    <td>{gl.itemDescription ?? 'Guia inteira'}</td>
                                    <td>{gl.reason}{gl.ansGlosaCode ? ` (${gl.ansGlosaCode})` : ''}</td>
                                    <td>{formatMoney(gl.glosaAmount)}</td>
                                    <td>{gl.isResolved ? 'Resolvida' : glosaContestationLabels[gl.contestationStatus] ?? 'Aberta'}</td>
                                    <td>
                                      <div className="table-actions">
                                        {!gl.isResolved && (
                                          <>
                                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => startEditGlosa(gl)}>Editar</button>
                                            <button type="button" className="btn btn-sm" onClick={() => runAction(() => api.resolveTissGlosa(gl.id), 'Glosa resolvida.')}>Resolver</button>
                                            {gl.contestationStatus === 0 && (
                                              <button type="button" className="btn btn-secondary btn-sm" onClick={() => setContestingGlosaId(gl.id)}>Recurso</button>
                                            )}
                                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => runAction(() => api.deleteTissGlosa(gl.id), 'Glosa excluída.')}>Excluir</button>
                                          </>
                                        )}
                                      </div>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          ) : (
                            <p className="form-hint">Nenhuma glosa registrada.</p>
                          )}

                          {g.status !== 1 && g.status !== 3 && g.status !== 5 && (
                            <form className="tiss-glosa-form" onSubmit={(e) => handleGlosaSubmit(g, e)}>
                              <strong>{editingGlosa ? 'Editar glosa' : 'Nova glosa'}</strong>
                              <div className="form-grid" style={{ marginTop: 8 }}>
                                <div className="form-field">
                                  <label>Item (opcional)</label>
                                  <select
                                    value={glosaForm.tissGuideItemId ?? ''}
                                    onChange={(e) => setGlosaForm({ ...glosaForm, tissGuideItemId: e.target.value || undefined })}
                                  >
                                    <option value="">Guia inteira</option>
                                    {g.items.map((i) => (
                                      <option key={i.id} value={i.id}>{i.tussCode} — {i.description}</option>
                                    ))}
                                  </select>
                                </div>
                                <div className="form-field">
                                  <label>Motivo</label>
                                  <input
                                    required
                                    value={glosaForm.reason}
                                    onChange={(e) => setGlosaForm({ ...glosaForm, reason: e.target.value })}
                                  />
                                </div>
                                <div className="form-field">
                                  <label>Valor glosado</label>
                                  <input
                                    type="number"
                                    step="0.01"
                                    min={0.01}
                                    required
                                    value={glosaForm.glosaAmount || ''}
                                    onChange={(e) => setGlosaForm({ ...glosaForm, glosaAmount: Number(e.target.value) })}
                                  />
                                </div>
                                <div className="form-field align-end">
                                  <button type="submit" className="btn btn-secondary">
                                    {editingGlosa ? 'Salvar glosa' : 'Registrar glosa'}
                                  </button>
                                  {editingGlosa && (
                                    <button
                                      type="button"
                                      className="btn btn-secondary btn-sm"
                                      style={{ marginLeft: 8 }}
                                      onClick={() => { setEditingGlosa(null); setGlosaForm(defaultGlosa()); }}
                                    >
                                      Cancelar
                                    </button>
                                  )}
                                </div>
                              </div>
                            </form>
                          )}
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
              {guides.length === 0 && (
                <tr>
                  <td colSpan={9} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma guia TISS encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={showGuideModal}
        onClose={() => setShowGuideModal(false)}
        title={editingGuide ? `Editar ${editingGuide.guideNumber}` : 'Nova guia TISS'}
        subtitle={isDraftForm ? 'Rascunho — todos os campos podem ser alterados.' : 'Somente leitura.'}
        width="lg"
      >
        <form className="form-grid" onSubmit={handleSaveGuide}>
          {!editingGuide && (
            <div className="form-field">
              <label>Paciente *</label>
              <select required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
                <option value="">Selecione</option>
                {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
          )}
          {editingGuide && (
            <div className="form-field">
              <label>Paciente</label>
              <input disabled value={editingGuide.patientName} />
            </div>
          )}
          <div className="form-field">
            <label>Convênio *</label>
            <select
              required
              disabled={!isDraftForm}
              value={form.healthInsuranceId}
              onChange={(e) => setForm({ ...form, healthInsuranceId: e.target.value })}
            >
              <option value="">Selecione</option>
              {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Tipo de guia</label>
            <select
              disabled={!isDraftForm}
              value={form.guideType}
              onChange={(e) => setForm({ ...form, guideType: Number(e.target.value) })}
            >
              {Object.entries(tissGuideTypeLabels)
                .filter(([code]) => {
                  const n = Number(code);
                  return n <= 7 || n === 12 || guideCatalog.some((g) => g.code === n && g.isCreatable);
                })
                .map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
            {selectedGuideTypeInfo && (
              <p className="form-hint tiss-guide-type-hint">
                <strong>{selectedGuideTypeInfo.name}</strong> — {selectedGuideTypeInfo.whenToUse}
              </p>
            )}
          </div>
          <div className="form-field">
            <label>ID agendamento (opcional)</label>
            <input
              disabled={!isDraftForm}
              value={form.appointmentId}
              onChange={(e) => setForm({ ...form, appointmentId: e.target.value })}
              placeholder="UUID do agendamento"
            />
          </div>
          <div className="form-field">
            <label>ID internação (opcional)</label>
            <input
              disabled={!isDraftForm}
              value={form.hospitalizationId}
              onChange={(e) => setForm({ ...form, hospitalizationId: e.target.value })}
              placeholder="UUID da internação"
            />
          </div>
          <div className="form-field full">
            <label>Observações</label>
            <textarea
              rows={2}
              disabled={!isDraftForm}
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
            />
          </div>

          <div className="form-field full">
            <TissGuideClinicalFields
              guideType={form.guideType}
              clinical={form.clinical}
              disabled={!isDraftForm}
              beneficiaryCard={prefillCard || editingGuide?.beneficiaryCardNumber}
              beneficiaryPlan={prefillPlan || editingGuide?.beneficiaryPlanName}
              authorizationPassword={prefillAuth || editingGuide?.authorizationPassword}
              onChange={(clinical) => setForm((f) => ({ ...f, clinical }))}
            />
          </div>

          <div className="form-field full">
            <div className="form-section-title">
              Procedimentos TUSS
              <span style={{ float: 'right', fontWeight: 600 }}>Total: {formatMoney(formTotal)}</span>
            </div>
            <table className="data-table tiss-editable-table">
              <thead>
                <tr><th>TUSS</th><th>Descrição</th><th>Qtd</th><th>Valor unit.</th><th>Total</th>{isDraftForm && <th />}</tr>
              </thead>
              <tbody>
                {form.items.map((item, idx) => (
                  <tr key={item.id ?? `new-${idx}`}>
                    <td>
                      <input
                        required
                        disabled={!isDraftForm}
                        value={item.tussCode}
                        onChange={(e) => {
                          updateItem(idx, { tussCode: e.target.value });
                          searchTussForItem(idx, e.target.value).catch(console.error);
                        }}
                      />
                      {(tussResults[idx]?.length ?? 0) > 0 && isDraftForm && (
                        <div className="card" style={{ marginTop: 4, padding: 4, maxHeight: 100, overflow: 'auto' }}>
                          {tussResults[idx]?.map((t) => (
                            <button
                              key={`${t.tussCode}-${t.description}`}
                              type="button"
                              className="btn btn-secondary btn-sm"
                              style={{ display: 'block', width: '100%', marginBottom: 2, textAlign: 'left', fontSize: 11 }}
                              onClick={() => {
                                updateItem(idx, { tussCode: t.tussCode, description: t.description, unitPrice: t.suggestedPrice ?? item.unitPrice });
                                setTussResults((prev) => ({ ...prev, [idx]: [] }));
                              }}
                            >
                              {t.tussCode} — {t.description} ({t.source})
                            </button>
                          ))}
                        </div>
                      )}
                    </td>
                    <td>
                      <input
                        required
                        disabled={!isDraftForm}
                        value={item.description}
                        onChange={(e) => updateItem(idx, { description: e.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min={1}
                        required
                        disabled={!isDraftForm}
                        value={item.quantity}
                        onChange={(e) => updateItem(idx, { quantity: Number(e.target.value) })}
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        step="0.01"
                        min={0}
                        required
                        disabled={!isDraftForm}
                        value={item.unitPrice}
                        onChange={(e) => updateItem(idx, { unitPrice: Number(e.target.value) })}
                      />
                    </td>
                    <td>{formatMoney(item.quantity * item.unitPrice)}</td>
                    {isDraftForm && (
                      <td>
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => removeItem(idx)} disabled={form.items.length <= 1}>
                          Remover
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
            {isDraftForm && (
              <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
                <button type="button" className="btn btn-secondary btn-sm" onClick={addItem}>
                  + Procedimento
                </button>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => loadSuggestedItems().catch(console.error)}>
                  Importar do prontuário
                </button>
              </div>
            )}
          </div>

          {isDraftForm && (
            <div className="form-field full modal-actions">
              <button type="button" className="btn btn-secondary" onClick={() => setShowGuideModal(false)}>Cancelar</button>
              <button type="submit" className="btn">{editingGuide ? 'Salvar alterações' : 'Criar guia'}</button>
            </div>
          )}
        </form>
      </Modal>
      </>
      )}

      <Modal
        open={!!contestingGlosaId}
        title="Recurso de glosa"
        onClose={() => { setContestingGlosaId(null); setContestNotes(''); }}
      >
        <form className="form-grid" onSubmit={(e) => {
          e.preventDefault();
          if (!contestingGlosaId) return;
          runAction(
            () => api.contestTissGlosa(contestingGlosaId, { contestationNotes: contestNotes }),
            'Recurso de glosa registrado.',
          ).then(() => { setContestingGlosaId(null); setContestNotes(''); });
        }}>
          <div className="form-field full">
            <label>Justificativa do recurso</label>
            <textarea required rows={4} value={contestNotes} onChange={(e) => setContestNotes(e.target.value)} />
          </div>
          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setContestingGlosaId(null)}>Cancelar</button>
            <button type="submit" className="btn">Enviar recurso</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
