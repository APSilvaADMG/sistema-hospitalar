import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  api,
  bedStatusLabels,
  bedStatusValue,
  cleaningStatusLabels,
  cleaningTypeLabels,
  cleaningTriggerLabels,
  transportLocationLabels,
  transportRequestStatusLabels,
  type BedDto,
  type CleaningChecklistItemDto,
  type CleaningRequestDto,
  type HotelariaNocDto,
  type TransportRequestDto,
} from '../../api/client';
import { FilterBar } from '../../components/FilterBar';
import { KpiCard } from '../../components/KpiCard';
import { Modal } from '../../components/Modal';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { hotelariaTabs } from '../../navigation/moduleSections';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useModuleSection } from '../../navigation/useModuleSection';
import { formatBrDateTime } from '../../utils/dateUtils';
import { useAuth } from '../../auth/AuthContext';
import { OperationsOfflineBar } from '../../components/OperationsOfflineBar';
import {
  completeCleaningAction,
  startCleaningAction,
  updateCleaningChecklistAction,
} from '../../offline/operationsActions';
import { isBrowserOnline, listCachedCleanings, listCachedTransports } from '../../offline/operationsOfflineDb';
import { useOperationsOffline } from '../../offline/useOperationsOffline';
import { RecentHospitalEventsPanel } from '../../components/RecentHospitalEventsPanel';

const emptyCleaningForm = { bedId: '', cleaningType: 'Terminal', notes: '' };

function buildNocFromCache(
  cleanings: CleaningRequestDto[],
  transports: TransportRequestDto[],
): HotelariaNocDto {
  const pendingCleaningQueue = cleanings.filter((c) => c.status === 'Requested' || c.status === 'InProgress');
  const activeTransportQueue = transports.filter((t) =>
    ['Queued', 'Accepted', 'InTransit'].includes(t.status),
  );

  return {
    totalBeds: 0,
    availableBeds: 0,
    occupiedBeds: 0,
    cleaningBeds: cleanings.filter((c) => c.status === 'InProgress').length,
    maintenanceBeds: 0,
    pendingAdmissions: 0,
    occupancyRate: 0,
    pendingCleanings: pendingCleaningQueue.length,
    activeTransports: activeTransportQueue.length,
    pendingCleaningQueue,
    activeTransportQueue,
    bedMap: [],
  };
}

function locationLabel(type: string, detail?: string) {
  const base = transportLocationLabels[type] ?? type;
  return detail ? `${base} — ${detail}` : base;
}

export function HotelariaHubPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/hotelaria');
  const active = section || '';

  const [noc, setNoc] = useState<HotelariaNocDto | null>(null);
  const [cleanings, setCleanings] = useState<CleaningRequestDto[]>([]);
  const [beds, setBeds] = useState<BedDto[]>([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [showCleaningModal, setShowCleaningModal] = useState(false);
  const [cleaningForm, setCleaningForm] = useState(emptyCleaningForm);
  const [checklistTarget, setChecklistTarget] = useState<CleaningRequestDto | null>(null);
  const [checklist, setChecklist] = useState<CleaningChecklistItemDto[]>([]);
  const [startTarget, setStartTarget] = useState<CleaningRequestDto | null>(null);
  const [startTeam, setStartTeam] = useState('Equipe Hotelaria');
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
    const [cachedCleanings, cachedTransports] = await Promise.all([
      listCachedCleanings(),
      listCachedTransports(),
    ]);
    const filtered = statusFilter
      ? cachedCleanings.filter((c) => c.status === statusFilter)
      : cachedCleanings;
    setCleanings(filtered);
    setNoc(buildNocFromCache(cachedCleanings, cachedTransports));
    setBeds([]);
  }

  async function load() {
    if (isBrowserOnline()) {
      try {
        const [n, c, b] = await Promise.all([
          api.getHotelariaNoc(),
          api.getCleaningRequests(statusFilter || undefined),
          api.getBeds({}),
        ]);
        setNoc(n);
        setCleanings(c);
        setBeds(b);
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

  useEffect(() => { load().catch(console.error); }, [statusFilter, refreshToken]);

  const availableBeds = useMemo(
    () => beds.filter((b) => bedStatusValue(b.status) === 1),
    [beds],
  );

  async function handleCreateCleaning(e: FormEvent) {
    e.preventDefault();
    await api.createCleaningRequest({
      bedId: cleaningForm.bedId,
      cleaningType: cleaningForm.cleaningType,
      notes: cleaningForm.notes || undefined,
    });
    setShowCleaningModal(false);
    setCleaningForm(emptyCleaningForm);
    setSuccess('Solicitação de higienização criada.');
    load();
  }

  async function handleStartCleaning(e: FormEvent) {
    e.preventDefault();
    if (!startTarget) return;
    const result = await startCleaningAction(startTarget.id, startTeam);
    setStartTarget(null);
    setSuccess(result.queued
      ? 'Início salvo offline — sincronizará quando a rede voltar.'
      : 'Higienização iniciada.');
    await load();
  }

  function openChecklist(c: CleaningRequestDto) {
    setChecklistTarget(c);
    setChecklist(c.checklist.map((item) => ({ ...item })));
  }

  async function saveChecklist() {
    if (!checklistTarget) return;
    const result = await updateCleaningChecklistAction(checklistTarget.id, checklist);
    setChecklistTarget(null);
    setSuccess(result.queued
      ? 'Checklist salvo offline — sincronizará quando a rede voltar.'
      : 'Checklist atualizado.');
    await load();
  }

  async function completeCleaning(id: string) {
    const result = await completeCleaningAction(id);
    setSuccess(result.queued
      ? 'Conclusão salva offline — sincronizará quando a rede voltar.'
      : 'Higienização concluída — leito liberado.');
    await load();
  }

  async function cancelCleaning(id: string) {
    await api.cancelCleaningRequest(id);
    load();
  }

  if (!hasPermission('cleaning.operate', 'cleaning.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow={breadcrumb?.parents[0] ?? 'Operacional'}
        title="Hotelaria Hospitalar"
        subtitle="NOC operacional — leitos, higienização e transportes em tempo real."
      >
        <button className="btn" type="button" onClick={() => setShowCleaningModal(true)}>+ Higienização</button>
        <Link to="/transportes" className="btn btn-secondary">Central de Transportes</Link>
      </PageHeader>

      <ModuleNav basePath="/hotelaria" tabs={hotelariaTabs} />

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

      {active === '' && noc && (
        <>
          <div className="kpi-grid">
            <KpiCard label="Taxa de ocupação" value={`${noc.occupancyRate}%`} variant="primary" />
            <KpiCard label="Leitos livres" value={noc.availableBeds} variant="success" />
            <KpiCard label="Em higienização" value={noc.cleaningBeds} variant="warning" />
            <KpiCard label="Manutenção" value={noc.maintenanceBeds} variant="default" />
            <KpiCard label="Aguardando internação" value={noc.pendingAdmissions} variant="info" />
            <KpiCard label="Transportes ativos" value={noc.activeTransports} variant="info" />
            <KpiCard
              label="Tempo médio higienização"
              value={noc.avgCleaningMinutes != null ? `${Math.round(noc.avgCleaningMinutes)} min` : '—'}
            />
            <KpiCard
              label="Tempo médio aceite maqueiro"
              value={noc.avgTransportAcceptMinutes != null ? `${Math.round(noc.avgTransportAcceptMinutes)} min` : '—'}
            />
          </div>

          <div className="form-grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 24, marginTop: 24 }}>
            <div className="card-panel appt-panel">
              <div className="card-panel-header">Higienizações pendentes — {noc.pendingCleaningQueue.length}</div>
              <div className="card-panel-body" style={{ padding: 0 }}>
                <table className="data-table">
                  <thead>
                    <tr><th>Leito</th><th>Tipo</th><th>Status</th><th>Solicitado</th></tr>
                  </thead>
                  <tbody>
                    {noc.pendingCleaningQueue.map((c) => (
                      <tr key={c.id}>
                        <td><strong>{c.wardName} / {c.bedNumber}</strong></td>
                        <td>{cleaningTypeLabels[c.cleaningType]}</td>
                        <td>{cleaningStatusLabels[c.status]}</td>
                        <td>{formatBrDateTime(c.requestedAt)}</td>
                      </tr>
                    ))}
                    {noc.pendingCleaningQueue.length === 0 && (
                      <tr><td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma pendência</td></tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="card-panel appt-panel">
              <div className="card-panel-header">Fila de maqueiros — {noc.activeTransportQueue.length}</div>
              <div className="card-panel-body" style={{ padding: 0 }}>
                <table className="data-table">
                  <thead>
                    <tr><th>Paciente</th><th>Trajeto</th><th>Status</th></tr>
                  </thead>
                  <tbody>
                    {noc.activeTransportQueue.map((r) => (
                      <tr key={r.id}>
                        <td><strong>{r.patientName}</strong></td>
                        <td style={{ fontSize: 12 }}>
                          {locationLabel(r.originType, r.originDetail)} → {locationLabel(r.destinationType, r.destinationDetail)}
                        </td>
                        <td>{transportRequestStatusLabels[r.status]}</td>
                      </tr>
                    ))}
                    {noc.activeTransportQueue.length === 0 && (
                      <tr><td colSpan={3} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhum transporte ativo</td></tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
            <div className="card-panel-header">Mapa de leitos (amostra) — {noc.bedMap.length}</div>
            <div className="card-panel-body">
              <div className="bed-grid">
                {noc.bedMap.map((b) => {
                  const statusNum = bedStatusValue(b.status as never);
                  return (
                    <div key={b.bedId} className={`bed-card bed-status-${statusNum}`}>
                      <div className="bed-card-number">{b.bedNumber}</div>
                      <div className="bed-card-ward">{b.wardName}</div>
                      <span className="badge">{bedStatusLabels[statusNum]}</span>
                      {b.occupantName && <div className="bed-card-patient">{b.occupantName}</div>}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </>
      )}

      {active === 'higienizacao' && (
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Gestão de higienização — {cleanings.length}</div>
          <FilterBar>
            <div className="filter-field w-lg">
              <label htmlFor="clStatus">Status</label>
              <select id="clStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">Todos</option>
                {Object.entries(cleaningStatusLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
          </FilterBar>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Leito</th><th>Tipo</th><th>Gatilho</th><th>Status</th><th>Equipe</th><th>Horários</th><th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {cleanings.map((c) => (
                  <tr key={c.id}>
                    <td><strong>{c.wardName} / {c.bedNumber}</strong></td>
                    <td>{cleaningTypeLabels[c.cleaningType]}</td>
                    <td>{cleaningTriggerLabels[c.triggerReason]}</td>
                    <td>{cleaningStatusLabels[c.status]}</td>
                    <td>{c.assignedTeam ?? c.assignedEmployeeName ?? '—'}</td>
                    <td style={{ fontSize: 12 }}>
                      Sol: {formatBrDateTime(c.requestedAt)}
                      {c.startedAt && <><br />Início: {formatBrDateTime(c.startedAt)}</>}
                      {c.completedAt && <><br />Fim: {formatBrDateTime(c.completedAt)}</>}
                    </td>
                    <td>
                      <div className="table-actions">
                        {c.status === 'Requested' && (
                          <button type="button" className="btn btn-sm" onClick={() => { setStartTarget(c); setStartTeam('Equipe Hotelaria'); }}>Iniciar</button>
                        )}
                        {c.status === 'InProgress' && (
                          <>
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => openChecklist(c)}>Checklist</button>
                            <button type="button" className="btn btn-sm" onClick={() => completeCleaning(c.id)}>Concluir</button>
                          </>
                        )}
                        {(c.status === 'Requested' || c.status === 'InProgress') && (
                          <button type="button" className="btn btn-secondary btn-sm" onClick={() => cancelCleaning(c.id)}>Cancelar</button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <Modal open={showCleaningModal} onClose={() => setShowCleaningModal(false)} title="Solicitar higienização" width="md">
        <form className="form-grid" onSubmit={handleCreateCleaning}>
          <div className="form-field">
            <label htmlFor="clBed">Leito *</label>
            <select id="clBed" required value={cleaningForm.bedId} onChange={(e) => setCleaningForm({ ...cleaningForm, bedId: e.target.value })}>
              <option value="">Selecione...</option>
              {availableBeds.map((b) => (
                <option key={b.id} value={b.id}>{b.wardName} / {b.bedNumber}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="clType">Tipo</label>
            <select id="clType" value={cleaningForm.cleaningType} onChange={(e) => setCleaningForm({ ...cleaningForm, cleaningType: e.target.value })}>
              {Object.entries(cleaningTypeLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="clNotes">Observações</label>
            <textarea id="clNotes" rows={2} value={cleaningForm.notes} onChange={(e) => setCleaningForm({ ...cleaningForm, notes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowCleaningModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Solicitar</button>
          </div>
        </form>
      </Modal>

      <Modal open={!!startTarget} onClose={() => setStartTarget(null)} title="Iniciar higienização" width="md">
        {startTarget && (
          <form className="form-grid" onSubmit={handleStartCleaning}>
            <p style={{ gridColumn: '1 / -1', margin: 0 }}>
              {startTarget.wardName} / {startTarget.bedNumber} — {cleaningTypeLabels[startTarget.cleaningType]}
            </p>
            <div className="form-field full">
              <label htmlFor="clTeam">Equipe</label>
              <input id="clTeam" value={startTeam} onChange={(e) => setStartTeam(e.target.value)} required />
            </div>
            <div className="form-field full modal-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setStartTarget(null)}>Cancelar</button>
              <button className="btn" type="submit">Iniciar</button>
            </div>
          </form>
        )}
      </Modal>

      <Modal open={!!checklistTarget} onClose={() => setChecklistTarget(null)} title="Checklist de higienização" width="md">
        <ul style={{ listStyle: 'none', padding: 0, margin: '0 0 16px' }}>
          {checklist.map((item, idx) => (
            <li key={item.id} style={{ marginBottom: 8 }}>
              <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input
                  type="checkbox"
                  checked={item.done}
                  onChange={(e) => {
                    const next = [...checklist];
                    next[idx] = { ...item, done: e.target.checked };
                    setChecklist(next);
                  }}
                />
                {item.label}
              </label>
            </li>
          ))}
        </ul>
        <div className="modal-actions">
          <button className="btn btn-secondary" type="button" onClick={() => setChecklistTarget(null)}>Fechar</button>
          <button className="btn" type="button" onClick={saveChecklist}>Salvar checklist</button>
        </div>
      </Modal>

      <RecentHospitalEventsPanel limit={20} title="Eventos operacionais recentes" />
    </>
  );
}
