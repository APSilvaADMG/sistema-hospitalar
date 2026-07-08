import { useEffect, useState, type FormEvent } from 'react';
import { api, type PatientDto } from '../../api/client';
import { PatientWorkspaceShell } from '../../components/patient-workspace/PatientWorkspaceShell';
import { moduleOperationalViews } from '../../navigation/patientWorkspaceConfig';
import { usePatientWorkspace } from '../../hooks/usePatientWorkspace';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { nursingTabs } from '../../navigation/moduleSections';
import { useModuleSection } from '../../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { BedsideCarePanel, PatientIdentityTools } from '../../components/nursing/BedsideCarePanel';

const SECTION_FORMS: Record<string, { title: string; fields: string[] }> = {
  'sae/diagnosticos': { title: 'Diagnósticos de Enfermagem (NANDA)', fields: ['Diagnóstico', 'Fatores relacionados', 'Evidências'] },
  'sae/planejamento': { title: 'Planejamento Assistencial', fields: ['Objetivos', 'Intervenções NIC', 'Prazo'] },
  'sae/evolucao': { title: 'Evolução de Enfermagem', fields: ['Evolução', 'Intervenções realizadas'] },
  medicamentos: { title: 'Administração de Medicamentos', fields: ['Medicamento', 'Dose', 'Via', 'Horário'] },
  'sinais-vitais': { title: 'Sinais Vitais', fields: ['PA', 'FC', 'FR', 'Temp', 'SpO2'] },
  curativos: { title: 'Curativos', fields: ['Local', 'Tipo de curativo', 'Aspecto da ferida'] },
  checklists: { title: 'Checklist Assistencial', fields: ['Item verificado', 'Status', 'Responsável'] },
  escalas: { title: 'Escalas Assistenciais', fields: ['Escala', 'Pontuação', 'Observações'] },
};

export function NursingHubPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/enfermagem');
  const activeSection = section || 'sae/evolucao';
  const formDef = SECTION_FORMS[activeSection] ?? SECTION_FORMS['sae/evolucao'];

  const [patients, setPatients] = useState<PatientDto[]>([]);
  const { patientId, setPatientId } = usePatientWorkspace('nursing');
  const [values, setValues] = useState<Record<string, string>>({});
  const [records, setRecords] = useState<{ id: string; text: string; at: string }[]>([]);
  const [success, setSuccess] = useState('');

  useEffect(() => {
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!patientId) return;
    const text = formDef.fields.map((f) => `${f}: ${values[f] ?? ''}`).join('\n');
    await api.addMedicalRecordEntry(patientId, {
      entryType: 2,
      content: `[Enfermagem — ${formDef.title}]\n${text}`,
      cid10Code: '',
    });
    setRecords((prev) => [{ id: crypto.randomUUID(), text, at: new Date().toISOString() }, ...prev]);
    setValues({});
    setSuccess('Registro de enfermagem salvo no PEP.');
  }

  return (
    <>
      <PageHeader
        eyebrow="Enfermagem"
        title={breadcrumb.title}
        subtitle="Assistência de enfermagem integrada ao prontuário eletrônico."
      />

      <ModuleNav basePath="/enfermagem" tabs={nursingTabs} contextId="nursing" />
      {success && <div className="alert alert-success">{success}</div>}

      {activeSection === 'leito' ? (
        <BedsideCarePanel />
      ) : (
      <PatientWorkspaceShell
        moduleId="nursing"
        patients={patients}
        patientId={patientId}
        onPatientIdChange={setPatientId}
        hidePickerWhenSelected
        operationalViews={moduleOperationalViews.nursing}
        operationalContent={patientId ? (
          <>
            <form className="card form-grid" onSubmit={handleSubmit}>
              <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>{formDef.title}</h3>
              {formDef.fields.map((field) => (
                <div key={field} className={`form-field${field.length > 20 ? ' full' : ''}`}>
                  <label>{field}</label>
                  {field.toLowerCase().includes('evolu') || field.toLowerCase().includes('observ') ? (
                    <textarea rows={3} value={values[field] ?? ''} onChange={(e) => setValues({ ...values, [field]: e.target.value })} />
                  ) : (
                    <input value={values[field] ?? ''} onChange={(e) => setValues({ ...values, [field]: e.target.value })} />
                  )}
                </div>
              ))}
              <div className="form-actions">
                <button className="btn" type="submit">Salvar no PEP</button>
              </div>
            </form>
            {records.length > 0 && (
              <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
                <div className="card-panel-header">Registros recentes (sessão)</div>
                <ul className="bi-progress-list">
                  {records.map((r) => (
                    <li key={r.id}><pre style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{r.text}</pre></li>
                  ))}
                </ul>
              </div>
            )}
            <PatientIdentityTools patientId={patientId} />
          </>
        ) : undefined}
      >
        {!patientId && (
          <div className="patient-workspace-empty">
            Selecione um paciente para ver dados clínicos em abas e registrar assistência de enfermagem.
          </div>
        )}
      </PatientWorkspaceShell>
      )}
    </>
  );
}
