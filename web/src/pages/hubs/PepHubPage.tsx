import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api,
  formatEntryTypeLabel,
  type AuditLogDto,
  type MedicalRecordEntryDto,
  type PatientDetailDto,
  type PatientDto,
  type PendingSignatureEntryDto,
  type ProfessionalDto,
  type SpecialtyClinicalCatalogDto,
} from '../../api/client';
import { ClinicalEntryForm, type ClinicalEntryPayload } from '../../components/ClinicalEntryForm';
import { DigitalSignaturePad } from '../../components/DigitalSignaturePad';
import { PatientWorkspaceShell } from '../../components/patient-workspace/PatientWorkspaceShell';
import { moduleOperationalViews } from '../../navigation/patientWorkspaceConfig';
import { usePatientWorkspace } from '../../hooks/usePatientWorkspace';
import { Modal } from '../../components/Modal';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { pepTabs, PEP_ENTRY_TYPE } from '../../navigation/moduleSections';
import { useModuleSection } from '../../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../../utils/dateUtils';
import { useAuth } from '../../auth/AuthContext';
import { signMedicalEntry } from '../../offline/pepActions';
import { AdministrationRoutesCatalogPage } from '../AdministrationRoutesCatalogPage';

const SECTION_HINTS: Record<string, string> = {
  anamnese: 'Anamnese estruturada — queixa, história, hábitos, exame físico e hipótese.',
  'evolucao-medica': 'Registre evoluções médicas no prontuário do paciente.',
  'evolucao-enfermagem': 'Evolução de enfermagem e cuidados assistenciais.',
  'evolucao-multidisciplinar': 'Anotações de fisioterapia, nutrição, psicologia e demais áreas.',
  prescricao: 'Prescrições eletrônicas vinculadas ao PEP.',
  'solicitacao-exames': 'Solicitações de laboratório e diagnóstico por imagem no PEP.',
  'vias-administracao': 'Catálogo MADRE de vias de administração (oral, IV, IM, etc.).',
  diagnosticos: 'Diagnósticos CID-10 registrados no prontuário.',
  procedimentos: 'Procedimentos realizados e solicitados.',
  'sinais-vitais': 'Registro de sinais vitais e monitorização.',
  escalas: 'Escalas clínicas (Glasgow, Braden, dor, etc.).',
  anexos: 'Documentos e anexos clínicos.',
  assinaturas: 'Entradas pendentes de assinatura digital.',
};

function resolveSigningProfessionalId(
  userProfessionalId: string | undefined,
  selectedProfessionalId: string,
): string | undefined {
  return userProfessionalId || selectedProfessionalId || undefined;
}

export function PepHubPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/pep');
  const { user, hasPermission } = useAuth();
  const canRead = hasPermission('pep.read');
  const canWrite = hasPermission('pep.write');

  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const { patientId, setPatientId } = usePatientWorkspace('pep');
  const [patient, setPatient] = useState<PatientDetailDto | null>(null);
  const [catalog, setCatalog] = useState<SpecialtyClinicalCatalogDto | null>(null);
  const [entries, setEntries] = useState<MedicalRecordEntryDto[]>([]);
  const [showModal, setShowModal] = useState(false);
  const [signOnSave, setSignOnSave] = useState(true);
  const [signatureImage, setSignatureImage] = useState<string | null>(null);
  const [signingProfessionalId, setSigningProfessionalId] = useState('');
  const [showSignModal, setShowSignModal] = useState(false);
  const [signingEntryId, setSigningEntryId] = useState<string | null>(null);
  const [signPassword, setSignPassword] = useState('');
  const [pendingGlobal, setPendingGlobal] = useState<PendingSignatureEntryDto[]>([]);
  const [signatureAudit, setSignatureAudit] = useState<AuditLogDto[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const activeSection = section || 'evolucao-medica';
  const entryType = PEP_ENTRY_TYPE[activeSection];

  useEffect(() => {
    Promise.all([
      api.getPatients('', 1).then((r) => r.items),
      api.getProfessionals(),
    ])
      .then(([patientList, profList]) => {
        setPatients(patientList);
        setProfessionals(profList);
        if (profList.length > 0) {
          setSigningProfessionalId((prev) => prev || user?.professionalId || profList[0].id);
        }
      })
      .catch(console.error);
  }, [user?.professionalId]);

  useEffect(() => {
    if (!patientId) {
      setPatient(null);
      setEntries([]);
      return;
    }
    Promise.all([
      api.getPatient(patientId),
      api.getMedicalRecord(patientId),
      api.getClinicalCatalog().catch(() => null),
    ])
      .then(([p, record, cat]) => {
        setPatient(p);
        setEntries(record.entries);
        setCatalog(cat);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar PEP.'));
  }, [patientId]);

  useEffect(() => {
    if (activeSection !== 'assinaturas') return;
    Promise.all([
      api.getPendingSignatures(50).catch(() => []),
      api.getSignatureAudit(30).catch(() => []),
    ]).then(([pending, audit]) => {
      setPendingGlobal(pending);
      setSignatureAudit(audit);
    });
  }, [activeSection, entries, success]);

  const filteredEntries = useMemo(() => {
    if (activeSection === 'assinaturas') {
      return entries.filter((e) => !e.isSigned);
    }
    if (activeSection === 'diagnosticos') {
      return entries.filter((e) => e.cid10Code);
    }
    if (activeSection === 'anexos') {
      return entries.filter((e) => e.content.includes('[anexo]') || e.content.length > 500);
    }
    if (entryType) {
      return entries.filter((e) => e.entryType === entryType);
    }
    return entries;
  }, [entries, activeSection, entryType]);

  async function refreshEntries() {
    if (!patientId) return;
    const record = await api.getMedicalRecord(patientId);
    setEntries(record.entries);
  }

  async function handleSubmit(payload: ClinicalEntryPayload) {
    if (!patientId) return;
    setError('');
    setSuccess('');

    const professionalId = resolveSigningProfessionalId(user?.professionalId, signingProfessionalId);

    if (signOnSave && !signatureImage) {
      setError('Desenhe sua assinatura digital para concluir o registro.');
      return;
    }

    if (signOnSave && !professionalId) {
      setError('Selecione o profissional responsável pela assinatura.');
      return;
    }

    if (signOnSave && !signPassword) {
      setError('Informe sua senha para assinar o registro.');
      return;
    }

    try {
      await api.addMedicalRecordEntry(patientId, {
        ...payload,
        professionalId,
        signatureImage: signOnSave ? signatureImage ?? undefined : undefined,
        password: signOnSave ? signPassword : undefined,
        signatureType: 1,
      });
      await refreshEntries();
      setShowModal(false);
      setSignatureImage(null);
      setSignPassword('');
      setSuccess(signOnSave ? 'Registro salvo e assinado digitalmente.' : 'Registro salvo no prontuário.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.');
    }
  }

  function openSignModal(entryId: string) {
    setSigningEntryId(entryId);
    setSignatureImage(null);
    setSignPassword('');
    setShowSignModal(true);
  }

  async function handleSignExistingEntry(e: FormEvent) {
    e.preventDefault();
    if (!patientId || !signingEntryId) return;

    const professionalId = resolveSigningProfessionalId(user?.professionalId, signingProfessionalId);
    if (!professionalId) {
      setError('Selecione o profissional responsável pela assinatura.');
      return;
    }
    if (!signatureImage) {
      setError('Desenhe sua assinatura digital.');
      return;
    }
    if (!signPassword) {
      setError('Informe sua senha para confirmar a assinatura.');
      return;
    }

    setError('');
    try {
      await signMedicalEntry(patientId, signingEntryId, professionalId, signatureImage, signPassword);
      await refreshEntries();
      setShowSignModal(false);
      setSigningEntryId(null);
      setSignatureImage(null);
      setSignPassword('');
      setSuccess('Registro assinado digitalmente.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao assinar.');
    }
  }

  function renderSectionBody() {
    if (activeSection === 'vias-administracao') {
      return <AdministrationRoutesCatalogPage embedded />;
    }

    if (activeSection === 'assinaturas') {
      return (
        <>
          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Fila global — pendentes de assinatura</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Paciente</th>
                    <th>Prontuário</th>
                    <th>Tipo</th>
                    <th>Resumo</th>
                    <th>Data</th>
                    <th>Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {pendingGlobal.map((p) => (
                    <tr key={p.entryId}>
                      <td>{p.patientName}</td>
                      <td>{p.recordNumber ?? '—'}</td>
                      <td>{formatEntryTypeLabel(p.entryType)}</td>
                      <td>{p.contentPreview}</td>
                      <td>{formatBrDateTime(p.createdAt)}</td>
                      <td>
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          onClick={() => setPatientId(p.patientId)}
                        >
                          Abrir e assinar
                        </button>
                      </td>
                    </tr>
                  ))}
                  {pendingGlobal.length === 0 && (
                    <tr>
                      <td colSpan={6} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                        Nenhum registro pendente em todo o hospital.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {signatureAudit.length > 0 && (
            <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
              <div className="card-panel-header">Trilha de auditoria — assinaturas recentes</div>
              <ul className="bi-progress-list" style={{ padding: '12px 16px' }}>
                {signatureAudit.slice(0, 10).map((log) => (
                  <li key={log.id}>
                    <strong>{log.userEmail}</strong> — {log.action} — {formatBrDateTime(log.createdAt)}
                    <div style={{ color: 'var(--muted)', fontSize: 13 }}>{log.details}</div>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {patientId ? renderPatientSignatureTable() : (
            <div className="card module-placeholder" style={{ marginTop: 16 }}>
              <p>Selecione um paciente para ver registros pendentes no prontuário dele.</p>
            </div>
          )}
        </>
      );
    }

    if (!patientId) {
      return (
        <div className="card module-placeholder" style={{ marginTop: 16 }}>
          <p>Selecione um paciente para acessar o módulo <strong>{breadcrumb.title}</strong>.</p>
        </div>
      );
    }

    if (['sinais-vitais', 'escalas'].includes(activeSection)) {
      return (
        <VitalsScalesForm
          section={activeSection}
          patientId={patientId}
          onSaved={refreshEntries}
        />
      );
    }

    return renderPatientSignatureTable();
  }

  function renderPatientSignatureTable() {
    const showSignActions = activeSection === 'assinaturas' && canWrite;

    return (
      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Registros — {breadcrumb.title}</span>
          {canWrite && entryType && activeSection !== 'assinaturas' && (
            <button type="button" className="btn btn-sm" onClick={() => setShowModal(true)}>Novo registro</button>
          )}
        </div>
        {showSignActions && filteredEntries.length > 0 && (
          <p style={{ padding: '12px 16px 0', margin: 0, color: 'var(--muted)', fontSize: 14 }}>
            {filteredEntries.length} registro(s) aguardando assinatura digital.
          </p>
        )}
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Tipo</th>
                <th>Profissional</th>
                <th>CID</th>
                <th>Resumo</th>
                <th>Assinado</th>
                {showSignActions && <th>Ações</th>}
              </tr>
            </thead>
            <tbody>
              {filteredEntries.map((e) => (
                <tr key={e.id}>
                  <td>{formatBrDateTime(e.createdAt)}</td>
                  <td>{formatEntryTypeLabel(e.entryType)}</td>
                  <td>{e.professionalName ?? '—'}</td>
                  <td>{e.cid10Code ?? '—'}</td>
                  <td>{e.content.slice(0, 80)}{e.content.length > 80 ? '…' : ''}</td>
                  <td>{e.isSigned ? 'Sim' : 'Não'}</td>
                  {showSignActions && (
                    <td>
                      {!e.isSigned && (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => openSignModal(e.id)}>
                          Assinar
                        </button>
                      )}
                    </td>
                  )}
                </tr>
              ))}
              {filteredEntries.length === 0 && (
                <tr>
                  <td colSpan={showSignActions ? 7 : 6} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                    {activeSection === 'assinaturas' ? 'Nenhum registro pendente de assinatura.' : 'Nenhum registro nesta seção.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  const needsProfessionalPicker = !user?.professionalId && professionals.length > 0;

  if (!canRead) {
    return <div className="card">Acesso restrito ao prontuário eletrônico.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Prontuário Eletrônico"
        title={breadcrumb.title}
        subtitle={SECTION_HINTS[activeSection] ?? 'Módulo PEP integrado ao prontuário do paciente.'}
      />

      <ModuleNav basePath="/pep" tabs={pepTabs} contextId="medicalRecord" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {canWrite && needsProfessionalPicker && (
        <div className="alert alert-info" style={{ marginTop: 12 }}>
          Seu usuário não está vinculado a um profissional. Selecione quem assina nos formulários abaixo.
        </div>
      )}

      <PatientWorkspaceShell
        moduleId="pep"
        patients={patients}
        patientId={patientId}
        onPatientIdChange={setPatientId}
        hidePickerWhenSelected
        operationalViews={moduleOperationalViews.pep}
        operationalContent={
          activeSection === 'vias-administracao' || activeSection === 'assinaturas' || patientId
            ? renderSectionBody()
            : undefined
        }
      >
        {!patientId && activeSection !== 'vias-administracao' && activeSection !== 'assinaturas' && (
          <div className="patient-workspace-empty">
            Selecione um paciente para ver o prontuário em abas (resumo, evoluções, prescrições, exames).
          </div>
        )}
      </PatientWorkspaceShell>

      <Modal open={showModal} title={`Novo registro — ${breadcrumb.title}`} onClose={() => setShowModal(false)}>
        {needsProfessionalPicker && (
          <div className="form-field" style={{ marginBottom: 12 }}>
            <label htmlFor="pep-sign-prof">Profissional assinante</label>
            <select
              id="pep-sign-prof"
              value={signingProfessionalId}
              onChange={(e) => setSigningProfessionalId(e.target.value)}
            >
              {professionals.map((p) => (
                <option key={p.id} value={p.id}>{p.fullName}</option>
              ))}
            </select>
          </div>
        )}
        <ClinicalEntryForm
          catalog={catalog}
          patient={patient}
          signOnSave={signOnSave}
          signatureImage={signatureImage}
          onSignOnSaveChange={setSignOnSave}
          onSignatureImageChange={setSignatureImage}
          onCancel={() => setShowModal(false)}
          onSubmit={handleSubmit}
          initialEntryType={entryType ?? 2}
          signatureLayoutKey={showModal ? 'open' : 'closed'}
        />
        {signOnSave && (
          <div className="form-field full" style={{ marginTop: 12 }}>
            <label htmlFor="pep-sign-password">Senha (reautenticação)</label>
            <input
              id="pep-sign-password"
              type="password"
              value={signPassword}
              onChange={(e) => setSignPassword(e.target.value)}
              autoComplete="current-password"
              required={signOnSave}
            />
          </div>
        )}
      </Modal>

      <Modal
        open={showSignModal}
        onClose={() => { setShowSignModal(false); setSigningEntryId(null); setSignatureImage(null); }}
        title="Assinatura digital"
        subtitle="Confirme o registro clínico com sua assinatura."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleSignExistingEntry}>
          {needsProfessionalPicker && (
            <div className="form-field full">
              <label htmlFor="pep-sign-entry-prof">Profissional assinante</label>
              <select
                id="pep-sign-entry-prof"
                value={signingProfessionalId}
                onChange={(e) => setSigningProfessionalId(e.target.value)}
                required
              >
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            </div>
          )}
          <div className="form-field full">
            <DigitalSignaturePad
              onChange={setSignatureImage}
              layoutKey={showSignModal ? signingEntryId ?? 'sign' : 'closed'}
            />
          </div>
          <div className="form-field full">
            <label htmlFor="pep-sign-entry-password">Senha (reautenticação)</label>
            <input
              id="pep-sign-entry-password"
              type="password"
              value={signPassword}
              onChange={(e) => setSignPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowSignModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Confirmar assinatura</button>
          </div>
        </form>
      </Modal>
    </>
  );
}

function VitalsScalesForm({
  section,
  patientId,
  onSaved,
}: {
  section: string;
  patientId: string;
  onSaved: () => void;
}) {
  const [form, setForm] = useState({
    pa: '',
    fc: '',
    fr: '',
    temp: '',
    spo2: '',
    scale: '',
    score: '',
    notes: '',
  });
  const [saving, setSaving] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const content = section === 'sinais-vitais'
        ? `Sinais vitais — PA: ${form.pa} | FC: ${form.fc} | FR: ${form.fr} | Temp: ${form.temp}°C | SpO2: ${form.spo2}%${form.notes ? `\n${form.notes}` : ''}`
        : `Escala ${form.scale}: ${form.score}${form.notes ? `\n${form.notes}` : ''}`;
      await api.addMedicalRecordEntry(patientId, { entryType: 2, content, cid10Code: '' });
      onSaved();
      setForm({ pa: '', fc: '', fr: '', temp: '', spo2: '', scale: '', score: '', notes: '' });
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={handleSubmit}>
      <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>{section === 'sinais-vitais' ? 'Sinais Vitais' : 'Escalas Clínicas'}</h3>
      {section === 'sinais-vitais' ? (
        <>
          <div className="form-field"><label>PA</label><input value={form.pa} onChange={(e) => setForm({ ...form, pa: e.target.value })} placeholder="120x80" /></div>
          <div className="form-field"><label>FC</label><input value={form.fc} onChange={(e) => setForm({ ...form, fc: e.target.value })} placeholder="bpm" /></div>
          <div className="form-field"><label>FR</label><input value={form.fr} onChange={(e) => setForm({ ...form, fr: e.target.value })} placeholder="irpm" /></div>
          <div className="form-field"><label>Temperatura</label><input value={form.temp} onChange={(e) => setForm({ ...form, temp: e.target.value })} placeholder="°C" /></div>
          <div className="form-field"><label>SpO2</label><input value={form.spo2} onChange={(e) => setForm({ ...form, spo2: e.target.value })} placeholder="%" /></div>
        </>
      ) : (
        <>
          <div className="form-field"><label>Escala</label>
            <select value={form.scale} onChange={(e) => setForm({ ...form, scale: e.target.value })}>
              <option value="">Selecione...</option>
              <option value="Glasgow">Glasgow</option>
              <option value="Braden">Braden</option>
              <option value="Dor (EVA)">Dor (EVA)</option>
              <option value="Morse">Morse (queda)</option>
            </select>
          </div>
          <div className="form-field"><label>Pontuação</label><input value={form.score} onChange={(e) => setForm({ ...form, score: e.target.value })} /></div>
        </>
      )}
      <div className="form-field full">
        <label>Observações</label>
        <textarea rows={3} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
      </div>
      <div className="form-actions">
        <button className="btn" type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Registrar no PEP'}</button>
      </div>
    </form>
  );
}
