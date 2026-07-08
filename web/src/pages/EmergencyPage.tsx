import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  emergencyStatusLabels,
  triageUrgencyLabels,
  type EmergencyVisitDto,
  type PatientDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { emergencyTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrTime } from '../utils/dateUtils';
import { printEmergencyQueueSummary } from '../utils/printTemplates';
import { useAuth } from '../auth/AuthContext';
import { PatientWorkspaceShell } from '../components/patient-workspace/PatientWorkspaceShell';
import { usePatientWorkspace } from '../hooks/usePatientWorkspace';
import { moduleOperationalViews } from '../navigation/patientWorkspaceConfig';
import { Link, useLocation } from 'react-router-dom';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';
import { isFeegowEsperaRoute } from '../utils/feegowRoutes';
import { FeegowWaitingRoomPage } from './FeegowWaitingRoomPage';

const urgencyClass: Record<string, string> = {
  Emergency: 'urgency-emergency',
  High: 'urgency-high',
  Medium: 'urgency-medium',
  Low: 'urgency-low',
  NonUrgent: 'urgency-nonurgent',
};

const urgencyOrder: Record<string, number> = {
  Emergency: 0,
  High: 1,
  Medium: 2,
  Low: 3,
  NonUrgent: 4,
};

function initials(name: string) {
  return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('');
}

type EmergencyPatientFlowProps = {
  patientId: string;
  patientView: string;
  visits: EmergencyVisitDto[];
  onUpdateStatus: (id: string, status: string) => void;
};

function EmergencyPatientFlow({ patientId, patientView, visits, onUpdateStatus }: EmergencyPatientFlowProps) {
  const visit = visits.find((v) => v.patientId === patientId);
  const viewLabels: Record<string, string> = {
    triagem: 'Triagem e classificação de risco',
    atendimento: 'Atendimento médico',
    evolucoes: 'Evoluções',
    prescricoes: 'Prescrições',
    alta: 'Alta / encaminhamento',
  };

  if (!visit) {
    return (
      <div className="card-panel">
        <p className="form-hint">Paciente sem registro ativo na fila de emergência.</p>
        <Link className="btn btn-secondary btn-sm" to="/emergencia">Ver fila geral</Link>
      </div>
    );
  }

  return (
    <article className={`appt-card emergency-card ${urgencyClass[visit.urgency] ?? ''}`}>
      <div className="card-panel-header">{viewLabels[patientView] ?? 'Fluxo de emergência'}</div>
      <div className="card-panel-body">
        <div className="appt-card-meta" style={{ marginBottom: 12 }}>
          <span className={`badge ${urgencyClass[visit.urgency] ?? ''}`}>{triageUrgencyLabels[visit.urgency]}</span>
          <span className="appt-meta-dot">•</span>
          <span>{emergencyStatusLabels[visit.status]}</span>
          <span className="appt-meta-dot">•</span>
          <span>Chegada {formatBrTime(visit.arrivedAt)}</span>
        </div>
        <p><strong>Queixa:</strong> {visit.chiefComplaint}</p>
        {visit.notes && <p className="form-hint">{visit.notes}</p>}
        <div className="appt-card-actions" style={{ marginTop: 16 }}>
          {visit.status === 'Waiting' && patientView === 'triagem' && (
            <button className="btn btn-sm" type="button" onClick={() => onUpdateStatus(visit.id, 'InCare')}>
              Classificar e iniciar atendimento
            </button>
          )}
          {visit.status === 'InCare' && (
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              {(patientView === 'atendimento' || patientView === 'alta') && (
                <button className="btn btn-sm" type="button" onClick={() => onUpdateStatus(visit.id, 'Discharged')}>
                  Registrar alta
                </button>
              )}
              {patientView === 'alta' && (
                <button className="btn btn-secondary btn-sm" type="button" onClick={() => onUpdateStatus(visit.id, 'Referred')}>
                  Encaminhar
                </button>
              )}
              <Link className="btn btn-secondary btn-sm" to={`/pep/evolucao-medica?paciente=${patientId}`}>
                Abrir PEP
              </Link>
            </div>
          )}
        </div>
      </div>
    </article>
  );
}

export function EmergencyPage() {
  const { pathname } = useLocation();
  const { appearance } = useAppearance();
  const feegowWaitingRoom = isFeegowBrand(appearance.brand) && isFeegowEsperaRoute(pathname);

  const { hasPermission } = useAuth();
  const { section } = useModuleSection('/emergencia');
  const { patientId, patientView } = usePatientWorkspace('emergency');
  const canRegister = hasPermission('patients.create', 'reports.read');

  const [visits, setVisits] = useState<EmergencyVisitDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [form, setForm] = useState({ patientId: '', chiefComplaint: '', urgency: 'Medium', notes: '' });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [urgencyFilter, setUrgencyFilter] = useState('');
  const [search, setSearch] = useState('');

  useEffect(() => {
    load();
    api.getPatients('', 1).then((p) => setPatients(p.items)).catch(console.error);
  }, []);

  async function load() {
    setVisits(await api.getEmergencyVisits());
  }

  const filtered = useMemo(() => {
    return visits
      .filter((v) => {
        if (section === 'classificacao-risco') return v.status === 'Waiting';
        if (section === 'atendimento-medico' || section === 'evolucao' || section === 'prescricoes') return v.status === 'InCare';
        if (section === 'encaminhamentos') return v.status === 'Transferred';
        if (section === 'alta') return v.status === 'Discharged';
        return true;
      })
      .filter((v) => !statusFilter || v.status === statusFilter)
      .filter((v) => !urgencyFilter || v.urgency === urgencyFilter)
      .filter((v) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return v.patientName.toLowerCase().includes(term) || v.chiefComplaint.toLowerCase().includes(term);
      })
      .sort((a, b) => {
        const ua = urgencyOrder[a.urgency] ?? 9;
        const ub = urgencyOrder[b.urgency] ?? 9;
        if (ua !== ub) return ua - ub;
        return new Date(a.arrivedAt).getTime() - new Date(b.arrivedAt).getTime();
      });
  }, [visits, statusFilter, urgencyFilter, search, section]);

  const stats = useMemo(() => ({
    total: visits.length,
    waiting: visits.filter((v) => v.status === 'Waiting').length,
    inCare: visits.filter((v) => v.status === 'InCare').length,
    discharged: visits.filter((v) => v.status === 'Discharged').length,
    critical: visits.filter((v) => v.urgency === 'Emergency' || v.urgency === 'High').length,
  }), [visits]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createEmergencyVisit(form);
      setForm({ patientId: '', chiefComplaint: '', urgency: 'Medium', notes: '' });
      setShowModal(false);
      setSuccess('Paciente registrado na fila de emergência.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar');
    }
  }

  async function updateStatus(id: string, status: string) {
    setError('');
    try {
      await api.updateEmergencyVisitStatus(id, { status });
      setSuccess('Status atualizado.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao atualizar status');
    }
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito à equipe clínica.</div>;
  }

  if (feegowWaitingRoom) {
    return <FeegowWaitingRoomPage />;
  }

  return (
    <>
      <PageHeader
        eyebrow="Atendimento"
        title="Pronto-Socorro / Emergência"
        subtitle="Fila ordenada pelo Protocolo de Manchester — prioridade clínica, não ordem de chegada."
      >
        <button
          type="button"
          className="btn btn-secondary"
          onClick={() => printEmergencyQueueSummary(visits, stats)}
        >
          Imprimir fila PS
        </button>
        {canRegister && (
          <button className="btn" type="button" onClick={() => setShowModal(true)}>
            + Novo atendimento
          </button>
        )}
      </PageHeader>

      <ModuleNav basePath="/emergencia" tabs={emergencyTabs} contextId="emergency" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <PatientWorkspaceShell
        moduleId="emergency"
        patients={patients}
        hidePickerWhenSelected
        operationalViews={moduleOperationalViews.emergency}
        operationalContent={patientId ? (
          <EmergencyPatientFlow
            patientId={patientId}
            patientView={patientView}
            visits={visits}
            onUpdateStatus={updateStatus}
          />
        ) : undefined}
      >

      <div className="kpi-grid">
        <KpiCard label="Na fila" value={stats.waiting} variant="warning" />
        <KpiCard label="Em atendimento" value={stats.inCare} variant="info" />
        <KpiCard label="Altas" value={stats.discharged} variant="success" />
        <KpiCard label="Urgência alta / emergência" value={stats.critical} variant="danger" />
        <KpiCard label="Total do plantão" value={stats.total} variant="primary" />
      </div>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">Fila de atendimento — {filtered.length} paciente(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="emStatus">Status</label>
            <select id="emStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(emergencyStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-md">
            <label htmlFor="emUrgency">Urgência</label>
            <select id="emUrgency" value={urgencyFilter} onChange={(e) => setUrgencyFilter(e.target.value)}>
              <option value="">Todas</option>
              {Object.entries(triageUrgencyLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="emSearch">Buscar</label>
            <input
              id="emSearch"
              placeholder="Paciente ou queixa..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>

        <div className="card-panel-body appt-panel-body">
          {filtered.length === 0 ? (
            <div className="appt-empty">
              <div className="appt-empty-icon">🚨</div>
              <h3>Fila vazia</h3>
              <p>Nenhum paciente corresponde aos filtros selecionados.</p>
            </div>
          ) : (
            <div className="emergency-queue">
              {filtered.map((v) => (
                <article key={v.id} className={`appt-card emergency-card ${urgencyClass[v.urgency] ?? ''}`}>
                  <div className="appt-card-time">
                    <span>{formatBrTime(v.arrivedAt)}</span>
                    <span className="appt-card-duration">chegada</span>
                  </div>
                  <div className="appt-card-main">
                    <div className="appt-card-patient">
                      <div className="appt-avatar">{initials(v.patientName)}</div>
                      <div>
                        <strong>{v.patientName}</strong>
                        <span className="appt-card-reason">{v.chiefComplaint}</span>
                      </div>
                    </div>
                    <div className="appt-card-meta">
                      <span className={`badge ${urgencyClass[v.urgency] ?? ''}`}>{triageUrgencyLabels[v.urgency]}</span>
                      <span className="appt-meta-dot">•</span>
                      <span>{emergencyStatusLabels[v.status]}</span>
                      {v.professionalName && (
                        <>
                          <span className="appt-meta-dot">•</span>
                          <span>{v.professionalName}</span>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="appt-card-actions">
                    <Link
                      className="btn btn-secondary btn-sm"
                      to={`/emergencia?paciente=${v.patientId}&visao=resumo`}
                    >
                      Ver paciente
                    </Link>
                    {v.status === 'Waiting' && (
                      <button className="btn btn-sm" type="button" onClick={() => updateStatus(v.id, 'InCare')}>
                        Iniciar atendimento
                      </button>
                    )}
                    {v.status === 'InCare' && (
                      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
                        <button className="btn btn-sm" type="button" onClick={() => updateStatus(v.id, 'Discharged')}>Alta</button>
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => updateStatus(v.id, 'Referred')}>Encaminhar</button>
                      </div>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>

      </PatientWorkspaceShell>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Registrar na fila"
        subtitle="Registro na fila após classificação de risco (Manchester)."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleCreate}>
          <div className="form-field">
            <label htmlFor="patientId">Paciente *</label>
            <select id="patientId" value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })} required>
              <option value="">Selecione...</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="urgency">Urgência *</label>
            <select id="urgency" value={form.urgency} onChange={(e) => setForm({ ...form, urgency: e.target.value })}>
              {Object.entries(triageUrgencyLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="chiefComplaint">Queixa principal *</label>
            <textarea
              id="chiefComplaint"
              rows={3}
              value={form.chiefComplaint}
              onChange={(e) => setForm({ ...form, chiefComplaint: e.target.value })}
              required
            />
          </div>
          <div className="form-field full">
            <label htmlFor="notes">Observações</label>
            <input id="notes" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Registrar na fila</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
