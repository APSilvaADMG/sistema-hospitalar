import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  securityIncidentStatusLabels,
  securityIncidentTypeLabels,
  visitorLogStatusLabels,
  type HospitalizationDto,
  type PatientDto,
  type SecurityDashboardDto,
  type SecurityIncidentDto,
  type VisitorLogDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { securityPortariaTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import { PersonAvatar } from '../components/PersonAvatar';
import { PhotoCapture } from '../components/PhotoCapture';
import { useAuth } from '../auth/AuthContext';
import { printVisitorBadge } from '../utils/printTemplates';
import { formatBrDateTime, formatBrTime } from '../utils/dateUtils';
import { buildVisitorDestination, patientVisitorLabel } from '../utils/visitorDestination';
import { useLocation } from 'react-router-dom';

const VISITOR_PHOTO_REQUIRED_KEY = 'hms.visitorPhotoRequired';

type ViewMode = 'cards' | 'list';

function visitorStatusVariant(status: string): 'success' | 'neutral' | 'warning' {
  if (status === 'Inside') return 'success';
  if (status === 'Exited') return 'neutral';
  return 'warning';
}

export function SecurityPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/seguranca');
  const activeSection = section || '';

  const [dashboard, setDashboard] = useState<SecurityDashboardDto | null>(null);
  const [visitors, setVisitors] = useState<VisitorLogDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [activeHospitalizations, setActiveHospitalizations] = useState<HospitalizationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [autoPrint, setAutoPrint] = useState(true);
  const [view, setView] = useState<ViewMode>('cards');
  const [search, setSearch] = useState('');
  const [success, setSuccess] = useState('');
  const [error, setError] = useState('');

  const [incidentForm, setIncidentForm] = useState({
    type: 'VisitorIssue',
    location: '',
    description: '',
    reportedBy: '',
  });
  const [visitorForm, setVisitorForm] = useState({
    visitorName: '',
    documentNumber: '',
    patientId: '',
    destination: '',
    badgeNumber: '',
    photoData: null as string | null,
  });
  const [serverPhotoRequired, setServerPhotoRequired] = useState(false);
  const [visitorPhotoRequired, setVisitorPhotoRequired] = useState(() => {
    const stored = localStorage.getItem(VISITOR_PHOTO_REQUIRED_KEY);
    return stored === 'true';
  });
  const photoRequired = visitorPhotoRequired || serverPhotoRequired;
  const [visitorFormError, setVisitorFormError] = useState('');
  const [showVisitorModal, setShowVisitorModal] = useState(false);
  const [showIncidentModal, setShowIncidentModal] = useState(false);
  const [incidents, setIncidents] = useState<SecurityIncidentDto[]>([]);
  const [incidentSearch, setIncidentSearch] = useState('');
  const [loadingIncidents, setLoadingIncidents] = useState(false);

  useEffect(() => {
    load();
    Promise.all([
      api.getPatients('', 1),
      api.getHospitalizations(),
      api.getSecuritySettings(),
    ]).then(([patientPage, hospitalizations, settings]) => {
      setPatients(patientPage.items);
      setActiveHospitalizations(hospitalizations);
      setServerPhotoRequired(settings.visitorPhotoRequired);
      const stored = localStorage.getItem(VISITOR_PHOTO_REQUIRED_KEY);
      if (stored === null) {
        setVisitorPhotoRequired(settings.visitorPhotoRequired);
      }
    }).catch(console.error);
  }, []);

  useEffect(() => {
    if (activeSection !== 'incidentes') return;
    setLoadingIncidents(true);
    api
      .getSecurityIncidents()
      .then((list) => setIncidents(list.filter((i) => i.status !== 'Resolved')))
      .catch(console.error)
      .finally(() => setLoadingIncidents(false));
  }, [activeSection]);

  const visitorStats = useMemo(() => ({
    inside: visitors.filter((v) => v.status === 'Inside').length,
    withPhoto: visitors.filter((v) => v.hasPhoto || v.photoData).length,
    withPatient: visitors.filter((v) => v.patientName).length,
    openIncidents: dashboard?.openIncidents ?? 0,
  }), [visitors, dashboard]);

  const filteredVisitors = useMemo(() => {
    const term = search.trim().toLowerCase();
    return visitors
      .filter((v) => {
        if (!term) return true;
        return (
          v.visitorName.toLowerCase().includes(term)
          || (v.destination?.toLowerCase().includes(term) ?? false)
          || (v.badgeNumber?.toLowerCase().includes(term) ?? false)
          || (v.patientName?.toLowerCase().includes(term) ?? false)
          || (v.documentNumber?.toLowerCase().includes(term) ?? false)
        );
      })
      .sort((a, b) => new Date(b.enteredAt).getTime() - new Date(a.enteredAt).getTime());
  }, [visitors, search]);

  const filteredIncidents = useMemo(() => {
    const term = incidentSearch.trim().toLowerCase();
    if (!term) return incidents;
    return incidents.filter((i) =>
      i.location.toLowerCase().includes(term)
      || i.description.toLowerCase().includes(term)
      || (securityIncidentTypeLabels[i.type] ?? i.type).toLowerCase().includes(term),
    );
  }, [incidents, incidentSearch]);

  function handlePatientChange(patientId: string) {
    setVisitorForm((prev) => ({
      ...prev,
      patientId,
      destination: buildVisitorDestination(patientId, activeHospitalizations, patients),
    }));
  }

  function openVisitorModal() {
    setVisitorForm({
      visitorName: '',
      documentNumber: '',
      patientId: '',
      destination: '',
      badgeNumber: '',
      photoData: null,
    });
    setVisitorFormError('');
    api.getHospitalizations().then(setActiveHospitalizations).catch(console.error);
    setShowVisitorModal(true);
  }

  function handleVisitorPhotoRequiredChange(required: boolean) {
    setVisitorPhotoRequired(required);
    localStorage.setItem(VISITOR_PHOTO_REQUIRED_KEY, String(required));
  }

  async function load() {
    setLoading(true);
    try {
      const [d, v] = await Promise.all([api.getSecurityDashboard(), api.getVisitorLogs(true)]);
      setDashboard(d);
      setVisitors(v);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar portaria.');
    } finally {
      setLoading(false);
    }
  }

  async function handleIncident(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await api.createSecurityIncident({
        type: incidentForm.type,
        location: incidentForm.location,
        description: incidentForm.description,
        reportedBy: incidentForm.reportedBy || undefined,
      });
      setIncidentForm({ type: 'VisitorIssue', location: '', description: '', reportedBy: '' });
      setShowIncidentModal(false);
      setSuccess('Incidente registrado com sucesso.');
      await load();
      if (activeSection === 'incidentes') {
        const list = await api.getSecurityIncidents();
        setIncidents(list.filter((i) => i.status !== 'Resolved'));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar incidente.');
    }
  }

  async function handleVisitor(e: FormEvent) {
    e.preventDefault();
    setVisitorFormError('');
    if (photoRequired && !visitorForm.photoData) {
      setVisitorFormError('A foto do visitante é obrigatória.');
      return;
    }
    try {
      const created = await api.registerVisitor({
        visitorName: visitorForm.visitorName,
        documentNumber: visitorForm.documentNumber || undefined,
        patientId: visitorForm.patientId || undefined,
        destination: visitorForm.destination || undefined,
        badgeNumber: visitorForm.badgeNumber || undefined,
        photoData: visitorForm.photoData || undefined,
      });
      setShowVisitorModal(false);
      setSuccess(`Entrada registrada — ${created.visitorName}${created.badgeNumber ? ` · Crachá ${created.badgeNumber}` : ''}.`);
      await load();
      if (autoPrint) printVisitorBadge(created, true);
    } catch (err) {
      setVisitorFormError(err instanceof Error ? err.message : 'Não foi possível registrar o visitante.');
    }
  }

  async function resolveIncident(id: string) {
    await api.resolveSecurityIncident(id, 'Incidente tratado pela equipe de segurança.');
    setSuccess('Incidente resolvido.');
    await load();
    const list = await api.getSecurityIncidents();
    setIncidents(list.filter((i) => i.status !== 'Resolved'));
  }

  async function registerExit(id: string, name: string) {
    await api.registerVisitorExit(id);
    setSuccess(`Saída registrada — ${name}.`);
    await load();
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito à recepção.</div>;
  }

  const pageTitle = activeSection === 'incidentes'
    ? 'Incidentes de segurança'
    : activeSection
      ? breadcrumb.title
      : 'Controle de visitantes';

  return (
    <>
      <PageHeader
        eyebrow="1 · Entrada e Recepção · Portaria"
        title={pageTitle}
        subtitle="Registro de visitantes, emissão de crachás e ocorrências patrimoniais em tempo real."
      >
        <Link to="/acesso-fisico" className="btn btn-secondary btn-sm">Acesso físico</Link>
        <button className="btn btn-secondary" type="button" onClick={() => setShowIncidentModal(true)}>
          + Incidente
        </button>
        <button className="btn" type="button" onClick={openVisitorModal}>
          + Registrar visitante
        </button>
      </PageHeader>

      <ModuleNav basePath="/seguranca" tabs={securityPortariaTabs} contextId="reception" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="appt-kpi-grid">
        <KpiCard label="Visitantes no hospital" value={visitorStats.inside} variant="primary" />
        <KpiCard label="Com foto no crachá" value={visitorStats.withPhoto} variant="info" />
        <KpiCard label="Visitando paciente" value={visitorStats.withPatient} variant="success" />
        <KpiCard label="Incidentes abertos" value={visitorStats.openIncidents} variant="danger" />
      </div>

      {activeSection !== 'incidentes' && (
        <div className="card-panel appt-panel">
          <div className="appt-panel-toolbar">
            <div>
              <strong style={{ fontSize: '0.95rem' }}>Visitantes ativos</strong>
              <p className="form-hint" style={{ margin: '4px 0 0' }}>
                {filteredVisitors.length} de {visitors.length} exibidos
              </p>
            </div>
            <div className="view-tabs">
              <button
                type="button"
                className={`view-tab${view === 'cards' ? ' active' : ''}`}
                onClick={() => setView('cards')}
              >
                Cartões
              </button>
              <button
                type="button"
                className={`view-tab${view === 'list' ? ' active' : ''}`}
                onClick={() => setView('list')}
              >
                Lista
              </button>
            </div>
          </div>

          <FilterBar>
            <div className="filter-field grow-lg">
              <label htmlFor="visitorSearch">Buscar visitante</label>
              <input
                id="visitorSearch"
                type="search"
                placeholder="Nome, destino, crachá ou paciente..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="filter-bar-prefs">
              <label className="print-option">
                <input type="checkbox" checked={autoPrint} onChange={(e) => setAutoPrint(e.target.checked)} />
                Imprimir crachá na entrada
              </label>
              <label className="print-option">
                <input
                  type="checkbox"
                  checked={visitorPhotoRequired}
                  onChange={(e) => handleVisitorPhotoRequiredChange(e.target.checked)}
                />
                Exigir foto
                {serverPhotoRequired && <span className="form-hint"> (servidor)</span>}
              </label>
            </div>
          </FilterBar>

          <div className="card-panel-body appt-panel-body">
            {loading ? (
              <p className="form-hint">Carregando visitantes...</p>
            ) : filteredVisitors.length === 0 ? (
              <div className="appt-empty">
                <div className="appt-empty-icon">🪪</div>
                <h3>Nenhum visitante no hospital</h3>
                <p>Registre a entrada para emitir crachá com foto e destino automático.</p>
                <button className="btn" type="button" onClick={openVisitorModal}>
                  Registrar visitante
                </button>
              </div>
            ) : view === 'cards' ? (
              <div className="visitor-card-grid">
                {filteredVisitors.map((v) => (
                  <article key={v.id} className={`visitor-card visitor-card-${v.status}`}>
                    <div className="visitor-card-photo">
                      <PersonAvatar name={v.visitorName} photoData={v.photoData} size={56} />
                      <span className={`badge badge-${visitorStatusVariant(v.status)}`}>
                        {visitorLogStatusLabels[v.status] ?? v.status}
                      </span>
                    </div>
                    <div className="visitor-card-body">
                      <h4>{v.visitorName}</h4>
                      {v.patientName && (
                        <p className="visitor-card-patient">
                          Visitando <strong>{v.patientName}</strong>
                        </p>
                      )}
                      <div className="visitor-card-meta">
                        <span>{v.destination ?? 'Destino não informado'}</span>
                        {v.badgeNumber && (
                          <>
                            <span className="appt-meta-dot">·</span>
                            <span>Crachá {v.badgeNumber}</span>
                          </>
                        )}
                      </div>
                      <div className="visitor-card-time">
                        Entrada {formatBrDateTime(v.enteredAt)}
                        <span className="visitor-card-time-sub"> às {formatBrTime(v.enteredAt)}</span>
                      </div>
                    </div>
                    <div className="visitor-card-actions">
                      <button className="btn btn-sm" type="button" onClick={() => printVisitorBadge(v)}>
                        Imprimir
                      </button>
                      {v.status === 'Inside' && (
                        <button
                          className="btn btn-secondary btn-sm"
                          type="button"
                          onClick={() => registerExit(v.id, v.visitorName)}
                        >
                          Registrar saída
                        </button>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Visitante</th>
                    <th>Destino</th>
                    <th>Crachá</th>
                    <th>Entrada</th>
                    <th>Status</th>
                    <th>Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredVisitors.map((v) => (
                    <tr key={v.id}>
                      <td>
                        <div className="table-cell-with-avatar">
                          <PersonAvatar name={v.visitorName} photoData={v.photoData} size={36} />
                          <div>
                            <strong>{v.visitorName}</strong>
                            {v.patientName && <div className="table-sub">Paciente: {v.patientName}</div>}
                          </div>
                        </div>
                      </td>
                      <td>{v.destination ?? '—'}</td>
                      <td><code>{v.badgeNumber ?? '—'}</code></td>
                      <td>{formatBrDateTime(v.enteredAt)}</td>
                      <td>
                        <span className={`badge badge-${visitorStatusVariant(v.status)}`}>
                          {visitorLogStatusLabels[v.status] ?? v.status}
                        </span>
                      </td>
                      <td>
                        <div className="table-actions">
                          <button className="btn btn-sm" type="button" onClick={() => printVisitorBadge(v)}>
                            Crachá
                          </button>
                          {v.status === 'Inside' && (
                            <button
                              className="btn btn-secondary btn-sm"
                              type="button"
                              onClick={() => registerExit(v.id, v.visitorName)}
                            >
                              Saída
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {activeSection === 'incidentes' && (
        <div className="card-panel appt-panel">
          <div className="appt-panel-toolbar">
            <div>
              <strong style={{ fontSize: '0.95rem' }}>Incidentes em aberto</strong>
              <p className="form-hint" style={{ margin: '4px 0 0' }}>
                {filteredIncidents.length} ocorrência(s) aguardando tratamento
              </p>
            </div>
            <button className="btn btn-sm" type="button" onClick={() => setShowIncidentModal(true)}>
              + Novo incidente
            </button>
          </div>

          <FilterBar>
            <div className="filter-field grow-lg">
              <label htmlFor="incidentSearch">Buscar incidente</label>
              <input
                id="incidentSearch"
                type="search"
                placeholder="Local, tipo ou descrição..."
                value={incidentSearch}
                onChange={(e) => setIncidentSearch(e.target.value)}
              />
            </div>
          </FilterBar>

          <div className="card-panel-body appt-panel-body">
            {loadingIncidents ? (
              <p className="form-hint">Carregando incidentes...</p>
            ) : filteredIncidents.length === 0 ? (
              <div className="appt-empty">
                <div className="appt-empty-icon">✓</div>
                <h3>Nenhum incidente aberto</h3>
                <p>A portaria está operando sem ocorrências pendentes.</p>
              </div>
            ) : (
              <div className="visitor-card-grid">
                {filteredIncidents.map((i) => (
                  <article key={i.id} className="visitor-card visitor-card-incident">
                    <div className="visitor-card-body">
                      <div className="visitor-card-meta" style={{ marginBottom: 8 }}>
                        <span className="badge badge-warning">
                          {securityIncidentTypeLabels[i.type] ?? i.type}
                        </span>
                        <span className={`badge badge-${i.status === 'Open' ? 'danger' : 'neutral'}`}>
                          {securityIncidentStatusLabels[i.status] ?? i.status}
                        </span>
                      </div>
                      <h4>{i.location}</h4>
                      <p className="visitor-card-desc">{i.description}</p>
                      <div className="visitor-card-time">
                        {formatBrDateTime(i.createdAt)}
                        {i.reportedBy && <span className="visitor-card-time-sub"> · {i.reportedBy}</span>}
                      </div>
                    </div>
                    <div className="visitor-card-actions">
                      {i.status !== 'Resolved' && (
                        <button
                          className="btn btn-secondary btn-sm"
                          type="button"
                          onClick={() => resolveIncident(i.id)}
                        >
                          Resolver
                        </button>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      <Modal open={showVisitorModal} onClose={() => setShowVisitorModal(false)} title="Registrar visitante" width="lg">
        <form onSubmit={handleVisitor} className="form-grid visitor-modal-form">
          <div className="form-field">
            <label htmlFor="visName">Nome completo *</label>
            <input
              id="visName"
              value={visitorForm.visitorName}
              onChange={(e) => setVisitorForm({ ...visitorForm, visitorName: e.target.value })}
              placeholder="Nome como no documento"
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="visDoc">Documento (RG / CNH)</label>
            <input
              id="visDoc"
              value={visitorForm.documentNumber}
              onChange={(e) => setVisitorForm({ ...visitorForm, documentNumber: e.target.value })}
              placeholder="Opcional"
            />
          </div>
          <div className="form-field">
            <label htmlFor="visPatient">Paciente visitado</label>
            <select id="visPatient" value={visitorForm.patientId} onChange={(e) => handlePatientChange(e.target.value)}>
              <option value="">Visitante avulso / serviço</option>
              {patients.map((p) => (
                <option key={p.id} value={p.id}>
                  {patientVisitorLabel(p, activeHospitalizations)}
                </option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="visDest">Destino / setor</label>
            <input
              id="visDest"
              placeholder="Ex.: UTI — Ala B, leito 204"
              value={visitorForm.destination}
              onChange={(e) => setVisitorForm({ ...visitorForm, destination: e.target.value })}
            />
            {visitorForm.patientId && visitorForm.destination && (
              <p className="form-hint">Sugerido pela internação do paciente — ajuste se necessário.</p>
            )}
          </div>
          <div className="form-field">
            <label htmlFor="visBadge">Número do crachá</label>
            <input
              id="visBadge"
              placeholder="Gerado automaticamente se vazio"
              value={visitorForm.badgeNumber}
              onChange={(e) => setVisitorForm({ ...visitorForm, badgeNumber: e.target.value })}
            />
          </div>
          <div className="form-field full visitor-modal-photo">
            <label>Foto para o crachá{photoRequired ? ' *' : ''}</label>
            <PhotoCapture
              name={visitorForm.visitorName || 'Visitante'}
              value={visitorForm.photoData}
              onChange={(photoData) => setVisitorForm({ ...visitorForm, photoData })}
            />
          </div>
          {visitorFormError && (
            <div className="form-field full">
              <p className="form-error">{visitorFormError}</p>
            </div>
          )}
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowVisitorModal(false)}>
              Cancelar
            </button>
            <button className="btn" type="submit">Confirmar entrada</button>
          </div>
        </form>
      </Modal>

      <Modal open={showIncidentModal} onClose={() => setShowIncidentModal(false)} title="Registrar incidente" width="lg">
        <form onSubmit={handleIncident} className="form-grid">
          <div className="form-field">
            <label htmlFor="incType">Tipo</label>
            <select id="incType" value={incidentForm.type} onChange={(e) => setIncidentForm({ ...incidentForm, type: e.target.value })}>
              {Object.entries(securityIncidentTypeLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="incReporter">Reportado por</label>
            <input
              id="incReporter"
              value={incidentForm.reportedBy}
              onChange={(e) => setIncidentForm({ ...incidentForm, reportedBy: e.target.value })}
              placeholder="Nome do responsável"
            />
          </div>
          <div className="form-field full">
            <label htmlFor="incLocation">Local *</label>
            <input
              id="incLocation"
              placeholder="Ex.: UTI, recepção, estacionamento"
              value={incidentForm.location}
              onChange={(e) => setIncidentForm({ ...incidentForm, location: e.target.value })}
              required
            />
          </div>
          <div className="form-field full">
            <label htmlFor="incDesc">Descrição *</label>
            <textarea
              id="incDesc"
              rows={4}
              placeholder="Descreva o ocorrido com o máximo de detalhes"
              value={incidentForm.description}
              onChange={(e) => setIncidentForm({ ...incidentForm, description: e.target.value })}
              required
            />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowIncidentModal(false)}>
              Cancelar
            </button>
            <button className="btn" type="submit">Registrar incidente</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
