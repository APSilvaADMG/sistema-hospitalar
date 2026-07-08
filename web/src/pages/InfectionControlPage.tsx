import { type FormEvent, useEffect, useState } from 'react';
import {
  api,
  infectionSurveillanceStatusLabels,
  infectionTypeLabels,
  isolationPrecautionTypeLabels,
  type InfectionControlDashboardDto,
  type InfectionSurveillanceDto,
  type IsolationPrecautionDto,
  type PatientDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { ccihTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { PatientWorkspaceShell } from '../components/patient-workspace/PatientWorkspaceShell';

export function InfectionControlPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/ccih');
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [dashboard, setDashboard] = useState<InfectionControlDashboardDto | null>(null);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [surveillanceForm, setSurveillanceForm] = useState({
    patientId: '', location: '', infectionType: 'Respiratory',
    organism: '', site: '', reportedBy: '', notes: '',
  });
  const [isolationForm, setIsolationForm] = useState({
    patientId: '', precautionType: 'Contact',
    startDate: new Date().toISOString().slice(0, 10), reason: '',
  });
  const [showSurveillanceModal, setShowSurveillanceModal] = useState(false);
  const [showIsolationModal, setShowIsolationModal] = useState(false);

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
  }, []);

  async function load() {
    setDashboard(await api.getInfectionControlDashboard());
  }

  async function handleSurveillance(e: FormEvent) {
    e.preventDefault();
    await api.createInfectionSurveillance({
      ...surveillanceForm,
      patientId: surveillanceForm.patientId || undefined,
    });
    setSurveillanceForm({ patientId: '', location: '', infectionType: 'Respiratory', organism: '', site: '', reportedBy: '', notes: '' });
    setShowSurveillanceModal(false);
    await load();
  }

  async function handleIsolation(e: FormEvent) {
    e.preventDefault();
    await api.createIsolationPrecaution(isolationForm);
    setIsolationForm({ patientId: '', precautionType: 'Contact', startDate: new Date().toISOString().slice(0, 10), reason: '' });
    setShowIsolationModal(false);
    await load();
  }

  async function resolveCase(id: string) {
    await api.resolveInfectionSurveillance(id, 'Caso resolvido pela equipe CCIH.');
    await load();
  }

  async function liftIsolation(id: string) {
    await api.liftIsolationPrecaution(id);
    await load();
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Apoio clínico"
        title={activeSection ? breadcrumb.title : 'CCIH — Controle de Infecção'}
        subtitle="Vigilância epidemiológica e precauções de isolamento."
      >
        {(activeSection === '' || activeSection === 'vigilancia') && (
          <button className="btn btn-secondary" type="button" onClick={() => setShowSurveillanceModal(true)}>+ Caso de vigilância</button>
        )}
        {activeSection === '' && (
          <button className="btn" type="button" onClick={() => setShowIsolationModal(true)}>+ Isolamento</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/ccih" tabs={ccihTabs} contextId="infectionControl" />

      <PatientWorkspaceShell moduleId="ccih" patients={patients} hidePickerWhenSelected>

      {dashboard && (
        <div className="kpi-grid">
          <KpiCard label="Isolamentos ativos" value={dashboard.activeIsolations} variant="warning" />
          <KpiCard label="Casos em vigilância" value={dashboard.openSurveillanceCases} variant="danger" />
        </div>
      )}

      {activeSection === 'indicadores' && dashboard && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Indicadores CCIH</h3>
          <p>Isolamentos ativos: {dashboard.activeIsolations} · Casos abertos: {dashboard.openSurveillanceCases}</p>
          <Link to="/bi" className="btn btn-secondary btn-sm">Painel BI</Link>
        </div>
      )}

      {activeSection === 'notificacoes' && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Notificações compulsórias (sinan)</div>
          <p style={{ padding: 16, color: 'var(--muted)' }}>Registre notificações epidemiológicas vinculadas aos casos de vigilância.</p>
        </div>
      )}

      {(activeSection === '' || activeSection === 'vigilancia') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Casos em vigilância — {dashboard?.recentCases.length ?? 0} caso(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>Tipo</th><th>Organismo</th><th>Status</th><th>Ações</th></tr></thead>
            <tbody>
              {(dashboard?.recentCases ?? []).map((c: InfectionSurveillanceDto) => (
                <tr key={c.id}>
                  <td>{c.patientName ?? '—'}</td>
                  <td>{infectionTypeLabels[c.infectionType]}</td>
                  <td>{c.organism}</td>
                  <td><span className="badge">{infectionSurveillanceStatusLabels[c.status] ?? c.status}</span></td>
                  <td>
                    {c.status !== 'Resolved' && (
                      <button className="btn btn-secondary btn-sm" type="button" onClick={() => resolveCase(c.id)}>Resolver</button>
                    )}
                  </td>
                </tr>
              ))}
              {(dashboard?.recentCases.length ?? 0) === 0 && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum caso</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {(activeSection === '' || activeSection === 'vigilancia') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Isolamentos ativos — {dashboard?.activePrecautions.length ?? 0} paciente(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>Tipo</th><th>Motivo</th><th>Ações</th></tr></thead>
            <tbody>
              {(dashboard?.activePrecautions ?? []).map((p: IsolationPrecautionDto) => (
                <tr key={p.id}>
                  <td>{p.patientName}</td>
                  <td>{isolationPrecautionTypeLabels[p.precautionType]}</td>
                  <td>{p.reason}</td>
                  <td>
                    <button className="btn btn-secondary btn-sm" type="button" onClick={() => liftIsolation(p.id)}>Suspender</button>
                  </td>
                </tr>
              ))}
              {(dashboard?.activePrecautions.length ?? 0) === 0 && (
                <tr><td colSpan={4} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum isolamento</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      </PatientWorkspaceShell>

      <Modal open={showSurveillanceModal} onClose={() => setShowSurveillanceModal(false)} title="Novo caso de vigilância" width="md">
        <form onSubmit={handleSurveillance} className="form-grid">
          <div className="form-field">
            <label htmlFor="survPatient">Paciente</label>
            <select id="survPatient" value={surveillanceForm.patientId} onChange={(e) => setSurveillanceForm({ ...surveillanceForm, patientId: e.target.value })}>
              <option value="">Opcional</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="survLocation">Local *</label>
            <input id="survLocation" value={surveillanceForm.location} onChange={(e) => setSurveillanceForm({ ...surveillanceForm, location: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="survType">Tipo de infecção</label>
            <select id="survType" value={surveillanceForm.infectionType} onChange={(e) => setSurveillanceForm({ ...surveillanceForm, infectionType: e.target.value })}>
              {Object.entries(infectionTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="survOrganism">Microorganismo *</label>
            <input id="survOrganism" value={surveillanceForm.organism} onChange={(e) => setSurveillanceForm({ ...surveillanceForm, organism: e.target.value })} required />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowSurveillanceModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Registrar caso</button>
          </div>
        </form>
      </Modal>

      <Modal open={showIsolationModal} onClose={() => setShowIsolationModal(false)} title="Nova precaução de isolamento" width="md">
        <form onSubmit={handleIsolation} className="form-grid">
          <div className="form-field">
            <label htmlFor="isoPatient">Paciente *</label>
            <select id="isoPatient" value={isolationForm.patientId} onChange={(e) => setIsolationForm({ ...isolationForm, patientId: e.target.value })} required>
              <option value="">Selecione...</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="isoType">Tipo de precaução</label>
            <select id="isoType" value={isolationForm.precautionType} onChange={(e) => setIsolationForm({ ...isolationForm, precautionType: e.target.value })}>
              {Object.entries(isolationPrecautionTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="isoReason">Motivo *</label>
            <input id="isoReason" value={isolationForm.reason} onChange={(e) => setIsolationForm({ ...isolationForm, reason: e.target.value })} required />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowIsolationModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Registrar isolamento</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
