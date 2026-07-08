import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api, imagingModalityLabels, imagingStatusLabels,
  type HealthInsuranceDto, type ImagingStudyDto, type PatientDto, type ProfessionalDto,
  type SpecialtyClinicalCatalogDto,
} from '../api/client';
import { ClinicalGuideCaptureModal } from '../components/funi/ClinicalGuideCaptureModal';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { imagingTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { Link, useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';

const IMAGING_SECTION_MODALITY: Record<string, number> = {
  'raio-x': 1, tomografia: 2, ressonancia: 3, ultrassom: 4, mamografia: 5,
};
import { SpecialtyCatalogPanel } from '../components/SpecialtyCatalogPanel';
import { useAuth } from '../auth/AuthContext';
import { printImagingReport } from '../utils/printTemplates';

export function ImagingPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/imagem');
  const activeSection = section || '';

  const { user, hasPermission } = useAuth();
  const canManage = hasPermission('pep.read', 'pep.write');
  const [catalog, setCatalog] = useState<SpecialtyClinicalCatalogDto | null>(null);
  const [studies, setStudies] = useState<ImagingStudyDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [form, setForm] = useState({
    patientId: '',
    requestingProfessionalId: user?.professionalId ?? '',
    modality: 1,
    studyDescription: '',
    scheduledAt: '',
    selectedProcedureId: '',
  });
  const [reports, setReports] = useState<Record<string, string>>({});
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [modalityFilter, setModalityFilter] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [clinicalStudy, setClinicalStudy] = useState<ImagingStudyDto | null>(null);

  async function loadCatalog(professionalId: string) {
    if (!professionalId) {
      setCatalog(null);
      return;
    }
    setCatalog(await api.getClinicalCatalogByProfessional(professionalId));
  }

  async function load() {
    const [studyList, patientList, profList, insuranceList] = await Promise.all([
      api.getImagingStudies(), api.getPatients(undefined, 1), api.getProfessionals(), api.getHealthInsurances(),
    ]);
    setStudies(studyList);
    setPatients(patientList.items);
    setProfessionals(profList);
    setInsurances(Array.isArray(insuranceList) ? insuranceList : []);
    if (form.requestingProfessionalId) {
      await loadCatalog(form.requestingProfessionalId);
    }
  }

  useEffect(() => { load().catch(console.error); }, []);

  async function handleProfessionalChange(professionalId: string) {
    setForm((f) => ({
      ...f,
      requestingProfessionalId: professionalId,
      selectedProcedureId: '',
      studyDescription: '',
    }));
    await loadCatalog(professionalId);
  }

  function handleProcedureSelect(id: string, label: string) {
    const proc = catalog?.imagingProcedures.find((p) => p.id === id);
    setForm((f) => ({
      ...f,
      selectedProcedureId: id,
      studyDescription: label,
      modality: proc?.modality ?? f.modality,
    }));
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createImagingStudy({
        patientId: form.patientId,
        requestingProfessionalId: form.requestingProfessionalId,
        modality: form.modality,
        studyDescription: form.studyDescription,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
      });
      setSuccess('Exame de imagem agendado.');
      setForm((f) => ({
        ...f,
        patientId: '',
        studyDescription: '',
        scheduledAt: '',
        selectedProcedureId: '',
      }));
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao agendar.');
    }
  }

  async function handleReport(id: string) {
    const content = reports[id];
    if (!content?.trim()) return;
    setError('');
    setSuccess('');
    try {
      await api.registerImagingReport(id, { reportContent: content, reportingProfessionalId: user?.professionalId });
      setSuccess('Laudo registrado.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar laudo.');
    }
  }

  const stats = useMemo(() => ({
    total: studies.length,
    scheduled: studies.filter((s) => s.status === 1).length,
    inProgress: studies.filter((s) => s.status === 2).length,
    completed: studies.filter((s) => s.status === 3).length,
    pendingReport: studies.filter((s) => s.status === 3 && !s.reportContent).length,
  }), [studies]);

  const filteredStudies = useMemo(() => {
    const sectionModality = IMAGING_SECTION_MODALITY[activeSection];
    return studies
      .filter((s) => {
        if (sectionModality) return s.modality === sectionModality;
        if (activeSection === '') return s.status === 3 || !s.reportContent;
        return true;
      })
      .filter((s) => !statusFilter || s.status === Number(statusFilter))
      .filter((s) => !modalityFilter || s.modality === Number(modalityFilter))
      .filter((s) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          s.patientName.toLowerCase().includes(term)
          || s.studyDescription.toLowerCase().includes(term)
          || s.requestingProfessionalName.toLowerCase().includes(term)
        );
      });
  }, [studies, statusFilter, modalityFilter, search, activeSection]);

  return (
    <>
      <PageHeader eyebrow="Diagnóstico" title={activeSection ? breadcrumb.title : 'Diagnóstico por Imagem'} subtitle="Procedimentos de imagem filtrados pela especialidade do médico.">
        {canManage && activeSection !== 'pacs' && activeSection !== '' && (
          <button className="btn" type="button" onClick={() => setShowModal(true)}>
            + Agendar exame
          </button>
        )}
      </PageHeader>

      <ModuleNav basePath="/imagem" tabs={imagingTabs} contextId="imaging" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {activeSection === 'pacs' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>PACS — Picture Archiving and Communication System</h3>
          <p>Visualização DICOM, worklist e laudos estruturados integrados ao RIS.</p>
          <Link to="/integracoes/pacs" className="btn btn-secondary" style={{ marginTop: 12 }}>Integração PACS</Link>
        </div>
      )}

      {activeSection !== 'pacs' && (
      <>
      <div className="kpi-grid">
        <KpiCard label="Total de estudos" value={stats.total} variant="primary" />
        <KpiCard label="Agendados" value={stats.scheduled} variant="info" />
        <KpiCard label="Em andamento" value={stats.inProgress} variant="warning" />
        <KpiCard label="Concluídos" value={stats.completed} variant="success" />
        <KpiCard label="Laudo pendente" value={stats.pendingReport} variant="danger" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Estudos — {filteredStudies.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-xs">
            <label htmlFor="imgModality">Modalidade</label>
            <select id="imgModality" value={modalityFilter} onChange={(e) => setModalityFilter(e.target.value)}>
              <option value="">Todas</option>
              {Object.entries(imagingModalityLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-md">
            <label htmlFor="imgStatus">Status</label>
            <select id="imgStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(imagingStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="imgSearch">Buscar</label>
            <input
              id="imgSearch"
              placeholder="Paciente, exame ou solicitante..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Exame</th>
                <th>Modalidade</th>
                <th>Agendamento</th>
                <th>Status</th>
                <th>Laudo</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filteredStudies.map((s) => (
                <tr key={s.id}>
                  <td><strong>{s.patientName}</strong></td>
                  <td>{s.studyDescription}</td>
                  <td>{imagingModalityLabels[s.modality]}</td>
                  <td>{formatBrDateTime(s.scheduledAt)}</td>
                  <td><span className="badge">{imagingStatusLabels[s.status]}</span></td>
                  <td>
                    {s.reportContent ? (
                      <span className="pep-entry-preview">{s.reportContent.slice(0, 120)}{s.reportContent.length > 120 ? '…' : ''}</span>
                    ) : canManage ? (
                      <div className="result-inline">
                        <input
                          placeholder="Laudo"
                          value={reports[s.id] ?? ''}
                          onChange={(e) => setReports({ ...reports, [s.id]: e.target.value })}
                        />
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleReport(s.id)}>
                          Salvar
                        </button>
                      </div>
                    ) : '—'}
                  </td>
                  <td>
                    <div className="table-actions">
                      {s.reportContent ? (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => printImagingReport(s)}>
                          Imprimir laudo
                        </button>
                      ) : null}
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => setClinicalStudy(s)}
                      >
                        Dados TISS
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {filteredStudies.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhum estudo encontrado.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      </>
      )}

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Agendar exame"
        subtitle="Procedimentos filtrados pela especialidade do médico solicitante."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleCreate}>
          <div className="form-field">
            <label htmlFor="imgPatient">Paciente</label>
            <select id="imgPatient" required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
              <option value="">Selecione</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="imgProfessional">Solicitante</label>
            <select
              id="imgProfessional"
              required
              value={form.requestingProfessionalId}
              onChange={(e) => handleProfessionalChange(e.target.value)}
            >
              <option value="">Selecione</option>
              {professionals.map((p) => (
                <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="imgModalityForm">Modalidade</label>
            <select id="imgModalityForm" value={form.modality} onChange={(e) => setForm({ ...form, modality: Number(e.target.value) })}>
              {Object.entries(imagingModalityLabels).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="imgDescription">Descrição</label>
            <input id="imgDescription" required value={form.studyDescription} onChange={(e) => setForm({ ...form, studyDescription: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="imgScheduled">Data/hora</label>
            <input id="imgScheduled" type="datetime-local" required value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} />
          </div>
          <div className="form-field full">
            <SpecialtyCatalogPanel
              specialtyName={catalog?.specialtyName}
              labExams={[]}
              imagingProcedures={catalog?.imagingProcedures ?? []}
              medications={[]}
              selectedImagingId={form.selectedProcedureId}
              onImagingSelect={handleProcedureSelect}
              showLabs={false}
              showMeds={false}
            />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Agendar exame</button>
          </div>
        </form>
      </Modal>

      {clinicalStudy && (
        <ClinicalGuideCaptureModal
          open
          onClose={() => setClinicalStudy(null)}
          guideType={2}
          patients={patients}
          insurances={insurances}
          patientId={clinicalStudy.patientId}
          clinicalContext={{
            imagingStudyId: clinicalStudy.id,
            label: `Imagem — ${clinicalStudy.studyDescription}`,
          }}
        />
      )}
    </>
  );
}
