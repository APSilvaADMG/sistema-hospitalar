import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  icuAlertLabels,
  type IcuDashboardDto,
  type PatientDto,
  type RecordVitalSignsRequest,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { icuTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { PatientWorkspaceShell } from '../components/patient-workspace/PatientWorkspaceShell';

const alertClass: Record<string, string> = {
  Critical: 'urgency-emergency',
  Warning: 'urgency-medium',
  Normal: 'urgency-low',
};

const defaultVitals: Omit<RecordVitalSignsRequest, 'hospitalizationId'> = {
  heartRate: 80, systolicBp: 120, diastolicBp: 80, spO2: 98, temperature: 36.5, respiratoryRate: 16,
};

function initials(name: string) {
  return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('');
}

export function IcuPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/uti');
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [data, setData] = useState<IcuDashboardDto | null>(null);
  const [selectedPatient, setSelectedPatient] = useState<{ id: string; name: string } | null>(null);
  const [vitals, setVitals] = useState(defaultVitals);
  const [alertFilter, setAlertFilter] = useState('');
  const [search, setSearch] = useState('');
  const [success, setSuccess] = useState('');
  const [patients, setPatients] = useState<PatientDto[]>([]);

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
  }, []);

  async function load() {
    setData(await api.getIcuDashboard());
  }

  const filtered = useMemo(() => {
    if (!data) return [];
    return data.patients
      .filter((p) => !alertFilter || p.alertLevel === alertFilter)
      .filter((p) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          p.patientName.toLowerCase().includes(term)
          || p.wardName.toLowerCase().includes(term)
          || p.bedNumber.toLowerCase().includes(term)
        );
      });
  }, [data, alertFilter, search]);

  function openVitalsModal(hospitalizationId: string, patientName: string) {
    setSelectedPatient({ id: hospitalizationId, name: patientName });
    setVitals(defaultVitals);
  }

  async function handleRecord(e: FormEvent) {
    e.preventDefault();
    if (!selectedPatient) return;
    await api.recordVitalSigns({ hospitalizationId: selectedPatient.id, ...vitals });
    setSuccess(`Sinais vitais registrados para ${selectedPatient.name}.`);
    setSelectedPatient(null);
    await load();
  }

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito à equipe clínica.</div>;
  }

  if (!data) return <div className="card">Carregando UTI...</div>;

  return (
    <>
      <PageHeader
        eyebrow="Internação"
        title={activeSection ? breadcrumb.title : 'UTI — Monitoramento'}
        subtitle="Sinais vitais, alertas clínicos e acompanhamento em tempo real."
      />

      <ModuleNav basePath="/uti" tabs={icuTabs} contextId="hospitalization" />

      {success && <div className="alert alert-success">{success}</div>}

      <PatientWorkspaceShell moduleId="icu" patients={patients} hidePickerWhenSelected>

      <div className="kpi-grid">
        <KpiCard label="Leitos UTI" value={`${data.occupiedBeds}/${data.totalIcuBeds}`} variant="primary" />
        <KpiCard label="Alertas críticos" value={data.criticalAlerts} variant="danger" />
        <KpiCard label="Pacientes monitorados" value={data.patients.length} variant="info" />
        <KpiCard
          label="Em atenção"
          value={data.patients.filter((p) => p.alertLevel === 'Warning').length}
          variant="warning"
        />
      </div>

      {activeSection === 'indicadores' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Indicadores assistenciais UTI</h3>
          <ul className="bi-progress-list">
            <li>Taxa de ocupação: {data.occupiedBeds}/{data.totalIcuBeds} leitos</li>
            <li>Alertas críticos: {data.criticalAlerts}</li>
            <li>Pacientes monitorados: {data.patients.length}</li>
          </ul>
        </div>
      )}

      {activeSection === 'escalas' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Escalas clínicas (Glasgow, SOFA, APACHE)</h3>
          <p>Registre escalas no PEP do paciente selecionado na aba Evoluções.</p>
        </div>
      )}

      {(activeSection === '' || activeSection === 'evolucoes') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Pacientes internados — {filtered.length} na UTI</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="icuAlert">Nível de alerta</label>
            <select id="icuAlert" value={alertFilter} onChange={(e) => setAlertFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(icuAlertLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="icuSearch">Buscar</label>
            <input
              id="icuSearch"
              placeholder="Paciente, ala ou leito..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body appt-panel-body">
          {filtered.length === 0 ? (
            <div className="appt-empty">
              <div className="appt-empty-icon">💓</div>
              <h3>Nenhum paciente</h3>
              <p>Nenhum paciente corresponde aos filtros selecionados.</p>
            </div>
          ) : (
            <div className="emergency-queue">
              {filtered.map((p) => (
                <article key={p.hospitalizationId} className={`appt-card emergency-card ${alertClass[p.alertLevel] ?? ''}`}>
                  <div className="appt-card-time">
                    <span>{p.bedNumber}</span>
                    <span className="appt-card-duration">leito</span>
                  </div>
                  <div className="appt-card-main">
                    <div className="appt-card-patient">
                      <div className="appt-avatar">{initials(p.patientName)}</div>
                      <div>
                        <strong>{p.patientName}</strong>
                        <span className="appt-card-reason">{p.wardName}</span>
                      </div>
                    </div>
                    <div className="appt-card-meta">
                      <span className={`badge ${alertClass[p.alertLevel] ?? ''}`}>{icuAlertLabels[p.alertLevel]}</span>
                      {p.latestVitals && (
                        <>
                          <span className="appt-meta-dot">•</span>
                          <span>
                            FC {p.latestVitals.heartRate} · PA {p.latestVitals.systolicBp}/{p.latestVitals.diastolicBp} ·
                            SpO₂ {p.latestVitals.spO2}% · {p.latestVitals.temperature}°C
                          </span>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="appt-card-actions">
                    <a className="btn btn-secondary btn-sm" href={`/uti?paciente=${p.patientId}&visao=monitorizacao`}>
                      Ver paciente
                    </a>
                    <button
                      className="btn btn-sm"
                      type="button"
                      onClick={() => openVitalsModal(p.hospitalizationId, p.patientName)}
                    >
                      Registrar sinais
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>
      )}

      </PatientWorkspaceShell>

      <Modal
        open={!!selectedPatient}
        onClose={() => setSelectedPatient(null)}
        title="Registrar sinais vitais"
        subtitle={selectedPatient ? `Paciente: ${selectedPatient.name}` : undefined}
        width="lg"
      >
        <form className="form-grid" onSubmit={handleRecord}>
          <div className="form-field">
            <label htmlFor="heartRate">FC (bpm)</label>
            <input id="heartRate" type="number" value={vitals.heartRate} onChange={(e) => setVitals({ ...vitals, heartRate: +e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="systolicBp">PAS (mmHg)</label>
            <input id="systolicBp" type="number" value={vitals.systolicBp} onChange={(e) => setVitals({ ...vitals, systolicBp: +e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="diastolicBp">PAD (mmHg)</label>
            <input id="diastolicBp" type="number" value={vitals.diastolicBp} onChange={(e) => setVitals({ ...vitals, diastolicBp: +e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="spO2">SpO₂ (%)</label>
            <input id="spO2" type="number" value={vitals.spO2} onChange={(e) => setVitals({ ...vitals, spO2: +e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="temperature">Temperatura (°C)</label>
            <input id="temperature" type="number" step="0.1" value={vitals.temperature} onChange={(e) => setVitals({ ...vitals, temperature: +e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="respiratoryRate">FR (irpm)</label>
            <input id="respiratoryRate" type="number" value={vitals.respiratoryRate} onChange={(e) => setVitals({ ...vitals, respiratoryRate: +e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setSelectedPatient(null)}>Cancelar</button>
            <button className="btn" type="submit">Salvar sinais vitais</button>
          </div>
        </form>
      </Modal>
    </>
  );
}
