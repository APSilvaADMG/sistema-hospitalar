import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  api,
  transportAssetStatusLabels,
  transportAssetTypeLabels,
  transportLocationLabels,
  transportPriorityLabels,
  transportRequestStatusLabels,
  type CreateTransportAssetRequest,
  type CreateTransportRequestRequest,
  type TransportAssetDto,
  type TransportDashboardDto,
  type TransportMetricsDto,
  type TransportPorterDto,
  type TransportRequestDto,
} from '../../api/client';
import { FilterBar } from '../../components/FilterBar';
import { KpiCard } from '../../components/KpiCard';
import { Modal } from '../../components/Modal';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { transportTabs } from '../../navigation/moduleSections';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useModuleSection } from '../../navigation/useModuleSection';
import { formatBrDateTime } from '../../utils/dateUtils';
import { useAuth } from '../../auth/AuthContext';
import { OperationsOfflineBar } from '../../components/OperationsOfflineBar';
import {
  acceptTransportAction,
  advanceTransportAction,
  cancelTransportAction,
} from '../../offline/operationsActions';
import { isBrowserOnline, listCachedTransports } from '../../offline/operationsOfflineDb';
import {
  getCachedPorters,
  getCachedTransportAssets,
} from '../../offline/operationsSyncEngine';
import { useOperationsOffline } from '../../offline/useOperationsOffline';

const emptyRequestForm = {
  patientName: '',
  originType: 'Hospitalization',
  originDetail: '',
  destinationType: 'ImagingTomography',
  destinationDetail: '',
  priority: 'Normal',
  notes: '',
};

const emptyAssetForm = {
  code: '',
  assetTag: '',
  assetType: 'Stretcher',
  sector: '',
  trackingCode: '',
  notes: '',
};

function locationLabel(type: string, detail?: string) {
  const base = transportLocationLabels[type] ?? type;
  return detail ? `${base} — ${detail}` : base;
}

function buildDashboardFromCache(requests: TransportRequestDto[]): TransportDashboardDto {
  const activeStatuses = ['Queued', 'Accepted', 'InTransit'];
  const liveQueue = requests.filter((r) => activeStatuses.includes(r.status));
  const recentCompleted = requests
    .filter((r) => r.status === 'Completed')
    .sort((a, b) => {
      const aTime = new Date(a.completedAt ?? a.requestedAt).getTime();
      const bTime = new Date(b.completedAt ?? b.requestedAt).getTime();
      return bTime - aTime;
    })
    .slice(0, 10);

  return {
    totalAssets: 0,
    availableAssets: 0,
    activeRequests: liveQueue.length,
    queuedRequests: requests.filter((r) => r.status === 'Queued').length,
    inTransitRequests: requests.filter((r) => r.status === 'InTransit').length,
    liveQueue,
    recentCompleted,
  };
}

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Queued': return 'badge-warning';
    case 'Accepted': return 'badge-info';
    case 'InTransit': return 'badge-primary';
    case 'Completed': return 'badge-success';
    case 'Cancelled': return 'badge-muted';
    default: return '';
  }
}

export function TransportHubPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/transportes');
  const active = section || '';
  const canManage = hasPermission('transport.manage');

  const [dashboard, setDashboard] = useState<TransportDashboardDto | null>(null);
  const [metrics, setMetrics] = useState<TransportMetricsDto | null>(null);
  const [assets, setAssets] = useState<TransportAssetDto[]>([]);
  const [requests, setRequests] = useState<TransportRequestDto[]>([]);
  const [porters, setPorters] = useState<TransportPorterDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [showRequestModal, setShowRequestModal] = useState(false);
  const [showAssetModal, setShowAssetModal] = useState(false);
  const [requestForm, setRequestForm] = useState(emptyRequestForm);
  const [assetForm, setAssetForm] = useState(emptyAssetForm);
  const [acceptTarget, setAcceptTarget] = useState<TransportRequestDto | null>(null);
  const [acceptForm, setAcceptForm] = useState({ employeeId: '', transportAssetId: '' });
  const [success, setSuccess] = useState('');
  const {
    online,
    pendingCount,
    syncing,
    realtimeConnected,
    syncNow,
    refreshToken,
  } = useOperationsOffline();

  async function loadFromCache() {
    const [cached, cachedPorters, cachedAssets] = await Promise.all([
      listCachedTransports(),
      getCachedPorters(),
      getCachedTransportAssets(),
    ]);
    setRequests(cached);
    setDashboard(buildDashboardFromCache(cached));
    setMetrics(null);
    setAssets(cachedAssets);
    setPorters(cachedPorters);
  }

  async function load() {
    if (isBrowserOnline()) {
      try {
        const [dash, met, ast, req, port] = await Promise.all([
          api.getTransportDashboard(),
          api.getTransportMetrics(),
          api.getTransportAssets(),
          api.getTransportRequests(),
          api.getTransportPorters(),
        ]);
        setDashboard(dash);
        setMetrics(met);
        setAssets(ast);
        setRequests(req);
        setPorters(port);
        return;
      } catch (err) {
        if (isBrowserOnline()) {
          console.error(err);
          return;
        }
      }
    }
    await loadFromCache();
  }

  useEffect(() => { load().catch(console.error); }, [refreshToken]);

  const filteredRequests = useMemo(() => {
    return requests.filter((r) => !statusFilter || r.status === statusFilter);
  }, [requests, statusFilter]);

  const liveQueue = dashboard?.liveQueue ?? [];

  async function handleCreateRequest(e: FormEvent) {
    e.preventDefault();
    const payload: CreateTransportRequestRequest = {
      patientName: requestForm.patientName,
      originType: requestForm.originType,
      originDetail: requestForm.originDetail || undefined,
      destinationType: requestForm.destinationType,
      destinationDetail: requestForm.destinationDetail || undefined,
      priority: requestForm.priority,
      notes: requestForm.notes || undefined,
    };
    await api.createTransportRequest(payload);
    setRequestForm(emptyRequestForm);
    setShowRequestModal(false);
    setSuccess('Solicitação de transporte registrada na fila.');
    load();
  }

  async function handleCreateAsset(e: FormEvent) {
    e.preventDefault();
    const payload: CreateTransportAssetRequest = {
      code: assetForm.code,
      assetTag: assetForm.assetTag,
      assetType: assetForm.assetType,
      sector: assetForm.sector,
      trackingCode: assetForm.trackingCode || undefined,
      notes: assetForm.notes || undefined,
    };
    await api.createTransportAsset(payload);
    setAssetForm(emptyAssetForm);
    setShowAssetModal(false);
    setSuccess('Equipamento cadastrado.');
    load();
  }

  async function handleAccept(e: FormEvent) {
    e.preventDefault();
    if (!acceptTarget) return;
    const result = await acceptTransportAction(acceptTarget.id, {
      employeeId: acceptForm.employeeId,
      transportAssetId: acceptForm.transportAssetId || undefined,
    });
    setAcceptTarget(null);
    setAcceptForm({ employeeId: '', transportAssetId: '' });
    setSuccess(result.queued
      ? 'Aceite salvo offline — sincronizará quando a rede voltar.'
      : 'Maqueiro atribuído à solicitação.');
    await load();
  }

  async function advance(id: string, status: string) {
    const result = await advanceTransportAction(id, status);
    if (result.queued) {
      setSuccess('Atualização salva offline — sincronizará quando a rede voltar.');
    }
    await load();
  }

  async function cancel(id: string) {
    const result = await cancelTransportAction(id);
    if (result.queued) {
      setSuccess('Cancelamento salvo offline — sincronizará quando a rede voltar.');
    }
    await load();
  }

  async function updateAssetStatus(id: string, status: string) {
    await api.updateTransportAssetStatus(id, status);
    load();
  }

  if (!hasPermission('transport.operate', 'transport.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow={breadcrumb?.parents[0] ?? 'Operacional'}
        title="Central de Transportes"
        subtitle="Maqueiros, macas, cadeiras e fila operacional em tempo real."
      >
        <button className="btn" type="button" onClick={() => setShowRequestModal(true)}>+ Nova solicitação</button>
        {canManage && (
          <button className="btn btn-secondary" type="button" onClick={() => setShowAssetModal(true)}>+ Equipamento</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/transportes" tabs={transportTabs} />

      <OperationsOfflineBar
        online={online}
        pendingCount={pendingCount}
        syncing={syncing}
        realtimeConnected={realtimeConnected}
        onSync={() => { syncNow().then(() => load()).catch(console.error); }}
      />

      {success && (
        <div className="alert alert-success" style={{ marginBottom: 16 }}>
          {success}
          <button type="button" className="btn-link" style={{ marginLeft: 12 }} onClick={() => setSuccess('')}>Fechar</button>
        </div>
      )}

      {active === '' && dashboard && (
        <>
          <div className="kpi-grid">
            <KpiCard label="Equipamentos" value={dashboard.totalAssets} variant="primary" />
            <KpiCard label="Disponíveis" value={dashboard.availableAssets} variant="success" />
            <KpiCard label="Na fila" value={dashboard.queuedRequests} variant="warning" />
            <KpiCard label="Em deslocamento" value={dashboard.inTransitRequests} variant="info" />
            <KpiCard
              label="Tempo médio aceite"
              value={dashboard.avgAcceptMinutes != null ? `${Math.round(dashboard.avgAcceptMinutes)} min` : '—'}
              variant="default"
            />
            <KpiCard
              label="Tempo médio conclusão"
              value={dashboard.avgCompleteMinutes != null ? `${Math.round(dashboard.avgCompleteMinutes)} min` : '—'}
              variant="default"
            />
          </div>

          <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
            <div className="card-panel-header">Painel em tempo real — {liveQueue.length} solicitação(ões) ativa(s)</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Paciente</th>
                    <th>Origem → Destino</th>
                    <th>Prioridade</th>
                    <th>Status</th>
                    <th>SLA</th>
                    <th>Maqueiro / Maca</th>
                    <th>Solicitado</th>
                    <th>Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {liveQueue.map((r) => (
                    <tr key={r.id}>
                      <td><strong>{r.patientName}</strong></td>
                      <td>{locationLabel(r.originType, r.originDetail)} → {locationLabel(r.destinationType, r.destinationDetail)}</td>
                      <td><span className={`badge ${r.priority === 'Urgent' ? 'badge-danger' : ''}`}>{transportPriorityLabels[r.priority]}</span></td>
                      <td><span className={`badge ${statusBadgeClass(r.status)}`}>{transportRequestStatusLabels[r.status]}</span></td>
                      <td>
                        {r.slaDeadlineAt && (
                          <span className={`badge ${r.isSlaViolated ? 'badge-danger' : 'badge-info'}`}>
                            {r.isSlaViolated ? 'SLA violado' : `Até ${formatBrDateTime(r.slaDeadlineAt)}`}
                          </span>
                        )}
                      </td>
                      <td>
                        {r.assignedEmployeeName ?? '—'}
                        {r.transportAssetCode && <><br /><small>{r.transportAssetCode}</small></>}
                      </td>
                      <td>{formatBrDateTime(r.requestedAt)}</td>
                      <td>
                        <div className="table-actions">
                          {r.status === 'Queued' && (
                            <button type="button" className="btn btn-sm" onClick={() => { setAcceptTarget(r); setAcceptForm({ employeeId: porters[0]?.id ?? '', transportAssetId: '' }); }}>
                              Aceitar
                            </button>
                          )}
                          {r.status === 'Accepted' && (
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => advance(r.id, 'InTransit')}>Iniciar deslocamento</button>
                          )}
                          {r.status === 'InTransit' && (
                            <button type="button" className="btn btn-sm" onClick={() => advance(r.id, 'Completed')}>Concluir</button>
                          )}
                          {r.status !== 'Completed' && r.status !== 'Cancelled' && (
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => cancel(r.id)}>Cancelar</button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                  {liveQueue.length === 0 && (
                    <tr><td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma solicitação ativa</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {dashboard.recentCompleted.length > 0 && (
            <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
              <div className="card-panel-header">Concluídos recentemente</div>
              <div className="card-panel-body" style={{ padding: 0 }}>
                <table className="data-table">
                  <thead>
                    <tr><th>Paciente</th><th>Trajeto</th><th>Maqueiro</th><th>Concluído</th></tr>
                  </thead>
                  <tbody>
                    {dashboard.recentCompleted.map((r) => (
                      <tr key={r.id}>
                        <td>{r.patientName}</td>
                        <td>{locationLabel(r.originType, r.originDetail)} → {locationLabel(r.destinationType, r.destinationDetail)}</td>
                        <td>{r.assignedEmployeeName ?? '—'}</td>
                        <td>{r.completedAt ? formatBrDateTime(r.completedAt) : '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}

      {active === 'fila' && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Fila de transportes — {filteredRequests.length}</div>
          <FilterBar>
            <div className="filter-field w-lg">
              <label htmlFor="trStatus">Status</label>
              <select id="trStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">Todos</option>
                {Object.entries(transportRequestStatusLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
          </FilterBar>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Paciente</th><th>Origem</th><th>Destino</th><th>Status</th><th>Prioridade</th><th>Horários</th>
                </tr>
              </thead>
              <tbody>
                {filteredRequests.map((r) => (
                  <tr key={r.id}>
                    <td><strong>{r.patientName}</strong></td>
                    <td>{locationLabel(r.originType, r.originDetail)}</td>
                    <td>{locationLabel(r.destinationType, r.destinationDetail)}</td>
                    <td><span className={`badge ${statusBadgeClass(r.status)}`}>{transportRequestStatusLabels[r.status]}</span></td>
                    <td>{transportPriorityLabels[r.priority]}</td>
                    <td style={{ fontSize: 12 }}>
                      Sol: {formatBrDateTime(r.requestedAt)}
                      {r.acceptedAt && <><br />Aceite: {formatBrDateTime(r.acceptedAt)}</>}
                      {r.completedAt && <><br />Fim: {formatBrDateTime(r.completedAt)}</>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'equipamentos' && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Macas e equipamentos — {assets.length}</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Código</th><th>Patrimônio</th><th>Tipo</th><th>Setor</th><th>Status</th><th>QR/RFID</th>
                  {canManage && <th>Ações</th>}
                </tr>
              </thead>
              <tbody>
                {assets.map((a) => (
                  <tr key={a.id}>
                    <td><strong>{a.code}</strong></td>
                    <td>{a.assetTag}</td>
                    <td>{transportAssetTypeLabels[a.assetType]}</td>
                    <td>{a.sector}</td>
                    <td><span className="badge">{transportAssetStatusLabels[a.status]}</span></td>
                    <td><code>{a.trackingCode ?? '—'}</code></td>
                    {canManage && (
                      <td>
                        <select
                          className="btn-sm"
                          value={a.status}
                          onChange={(e) => updateAssetStatus(a.id, e.target.value)}
                        >
                          {Object.entries(transportAssetStatusLabels).map(([k, v]) => (
                            <option key={k} value={k}>{v}</option>
                          ))}
                        </select>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'indicadores' && metrics && (
        <div className="form-grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 24 }}>
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Setores mais demandantes</div>
            <div className="card-panel-body">
              <ul className="simple-list">
                {metrics.sectorDemand.map((s) => (
                  <li key={s.originType}>
                    <strong>{transportLocationLabels[s.originType] ?? s.originType}</strong>
                    <span>{s.requestCount} solicitação(ões)</span>
                  </li>
                ))}
                {metrics.sectorDemand.length === 0 && <p className="bula-empty">Sem dados ainda.</p>}
              </ul>
            </div>
          </div>
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Maqueiros mais produtivos</div>
            <div className="card-panel-body">
              <ul className="simple-list">
                {metrics.porterProductivity.map((p) => (
                  <li key={p.employeeId}>
                    <strong>{p.employeeName}</strong>
                    <span>{p.completedCount} concluídos · média {p.avgCompleteMinutes != null ? `${Math.round(p.avgCompleteMinutes)} min` : '—'}</span>
                  </li>
                ))}
                {metrics.porterProductivity.length === 0 && <p className="bula-empty">Sem dados ainda.</p>}
              </ul>
            </div>
          </div>
          <div className="card-panel appt-panel" style={{ gridColumn: '1 / -1' }}>
            <div className="card-panel-header">Tempos médios</div>
            <div className="card-panel-body kpi-grid">
              <KpiCard label="Aceite" value={metrics.avgAcceptMinutes != null ? `${Math.round(metrics.avgAcceptMinutes)} min` : '—'} />
              <KpiCard label="Conclusão" value={metrics.avgCompleteMinutes != null ? `${Math.round(metrics.avgCompleteMinutes)} min` : '—'} />
              <KpiCard label="Deslocamento" value={metrics.avgTransitMinutes != null ? `${Math.round(metrics.avgTransitMinutes)} min` : '—'} />
            </div>
          </div>
        </div>
      )}

      <p style={{ marginTop: 24, color: 'var(--muted)', fontSize: 13 }}>
        Integrado com <Link to="/internacao">Internação</Link> e <Link to="/hotelaria">NOC Hotelaria</Link>.
      </p>

      <Modal open={showRequestModal} onClose={() => setShowRequestModal(false)} title="Nova solicitação de transporte" width="lg">
        <form className="form-grid" onSubmit={handleCreateRequest}>
          <div className="form-field">
            <label htmlFor="trPatient">Paciente *</label>
            <input id="trPatient" value={requestForm.patientName} onChange={(e) => setRequestForm({ ...requestForm, patientName: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="trPriority">Prioridade</label>
            <select id="trPriority" value={requestForm.priority} onChange={(e) => setRequestForm({ ...requestForm, priority: e.target.value })}>
              {Object.entries(transportPriorityLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="trOrigin">Origem</label>
            <select id="trOrigin" value={requestForm.originType} onChange={(e) => setRequestForm({ ...requestForm, originType: e.target.value })}>
              {Object.entries(transportLocationLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="trOriginDetail">Detalhe origem</label>
            <input id="trOriginDetail" placeholder="Ex: Ala B — Leito 204" value={requestForm.originDetail} onChange={(e) => setRequestForm({ ...requestForm, originDetail: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="trDest">Destino</label>
            <select id="trDest" value={requestForm.destinationType} onChange={(e) => setRequestForm({ ...requestForm, destinationType: e.target.value })}>
              {Object.entries(transportLocationLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="trDestDetail">Detalhe destino</label>
            <input id="trDestDetail" placeholder="Ex: Tomografia — Subsolo" value={requestForm.destinationDetail} onChange={(e) => setRequestForm({ ...requestForm, destinationDetail: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="trNotes">Observações</label>
            <textarea id="trNotes" rows={2} value={requestForm.notes} onChange={(e) => setRequestForm({ ...requestForm, notes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowRequestModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Enviar para fila</button>
          </div>
        </form>
      </Modal>

      <Modal open={showAssetModal} onClose={() => setShowAssetModal(false)} title="Cadastrar equipamento" width="md">
        <form className="form-grid" onSubmit={handleCreateAsset}>
          <div className="form-field">
            <label htmlFor="asCode">Código *</label>
            <input id="asCode" value={assetForm.code} onChange={(e) => setAssetForm({ ...assetForm, code: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="asTag">Patrimônio *</label>
            <input id="asTag" value={assetForm.assetTag} onChange={(e) => setAssetForm({ ...assetForm, assetTag: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="asType">Tipo</label>
            <select id="asType" value={assetForm.assetType} onChange={(e) => setAssetForm({ ...assetForm, assetType: e.target.value })}>
              {Object.entries(transportAssetTypeLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="asSector">Setor *</label>
            <input id="asSector" value={assetForm.sector} onChange={(e) => setAssetForm({ ...assetForm, sector: e.target.value })} required />
          </div>
          <div className="form-field full">
            <label htmlFor="asQr">Código QR / RFID</label>
            <input id="asQr" value={assetForm.trackingCode} onChange={(e) => setAssetForm({ ...assetForm, trackingCode: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowAssetModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Salvar</button>
          </div>
        </form>
      </Modal>

      <Modal open={!!acceptTarget} onClose={() => setAcceptTarget(null)} title="Aceitar solicitação" width="md">
        {acceptTarget && (
          <form className="form-grid" onSubmit={handleAccept}>
            <p style={{ gridColumn: '1 / -1', margin: 0 }}>
              <strong>{acceptTarget.patientName}</strong><br />
              {locationLabel(acceptTarget.originType, acceptTarget.originDetail)} → {locationLabel(acceptTarget.destinationType, acceptTarget.destinationDetail)}
            </p>
            <div className="form-field">
              <label htmlFor="accPorter">Maqueiro *</label>
              <select id="accPorter" value={acceptForm.employeeId} onChange={(e) => setAcceptForm({ ...acceptForm, employeeId: e.target.value })} required>
                <option value="">Selecione...</option>
                {porters.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label htmlFor="accAsset">Maca / equipamento</label>
              <select id="accAsset" value={acceptForm.transportAssetId} onChange={(e) => setAcceptForm({ ...acceptForm, transportAssetId: e.target.value })}>
                <option value="">Opcional</option>
                {assets.filter((a) => a.status === 'Available').map((a) => (
                  <option key={a.id} value={a.id}>{a.code} — {transportAssetTypeLabels[a.assetType]}</option>
                ))}
              </select>
            </div>
            <div className="form-field full modal-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setAcceptTarget(null)}>Cancelar</button>
              <button className="btn" type="submit">Confirmar aceite</button>
            </div>
          </form>
        )}
      </Modal>
    </>
  );
}
