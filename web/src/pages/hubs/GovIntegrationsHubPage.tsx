import { type FormEvent, useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  api,
  type CnesEstablishmentDto,
  type CnsLookupResultDto,
  type GovIntegrationProfileDto,
  type HospitalizationDto,
  type PatientDto,
  type RndsPatientSummaryDto,
  type SiaDocumentPreviewDto,
  type SihAihPreviewDto,
} from '../../api/client';
import { KpiCard } from '../../components/KpiCard';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { govIntegrationTabs } from '../../navigation/moduleSections';
import { resolvePageTitle } from '../../navigation/sectionBreadcrumb';
import { useModuleSection } from '../../navigation/useModuleSection';

const priorityLabels: Record<string, string> = {
  Priority1: 'Prioridade 1',
  Priority2: 'Prioridade 2',
  Priority3: 'Prioridade 3',
};

const credentialLabels: Record<string, string> = {
  NotConfigured: 'Não configurado',
  MockActive: 'Mock ativo',
  PendingCredential: 'Credenciamento pendente',
  ProductionReady: 'Produção',
};

export function GovIntegrationsHubPage() {
  const { pathname } = useLocation();
  const { section } = useModuleSection('/integracoes-gov');
  const active = (section || '').split('/')[0];

  const [profiles, setProfiles] = useState<GovIntegrationProfileDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [cnsInput, setCnsInput] = useState('898001234567890');
  const [cnsResult, setCnsResult] = useState<CnsLookupResultDto | null>(null);
  const [cnesCode, setCnesCode] = useState('1234567');
  const [cnesResult, setCnesResult] = useState<CnesEstablishmentDto | null>(null);
  const [selectedPatientId, setSelectedPatientId] = useState('');
  const [selectedHospId, setSelectedHospId] = useState('');
  const [sihPreview, setSihPreview] = useState<SihAihPreviewDto | null>(null);
  const [siaPreview, setSiaPreview] = useState<SiaDocumentPreviewDto | null>(null);
  const [rndsSummary, setRndsSummary] = useState<RndsPatientSummaryDto | null>(null);
  const [msg, setMsg] = useState('');

  useEffect(() => {
    api.getGovIntegrationProfiles()
      .then((list) => setProfiles(Array.isArray(list) ? list : []))
      .catch(console.error);
    api.getPatients('', 1)
      .then((p) => setPatients(Array.isArray(p?.items) ? p.items : []))
      .catch(console.error);
    api.getHospitalizations()
      .then((list) => setHospitalizations(Array.isArray(list) ? list : []))
      .catch(console.error);
  }, []);

  const profileList = Array.isArray(profiles) ? profiles : [];
  const p1 = profileList.filter((p) => p.priority === 'Priority1').length;
  const mockCount = profileList.filter((p) => p.mockEnabled).length;

  async function handleCnsLookup(e: FormEvent) {
    e.preventDefault();
    const result = await api.lookupCns(cnsInput);
    setCnsResult(result);
    setMsg(result.message ?? '');
  }

  async function handleApplyCns(e: FormEvent) {
    e.preventDefault();
    if (!selectedPatientId || !cnsResult?.cns) return;
    const result = await api.applyCnsToPatient(selectedPatientId, cnsResult.cns);
    setMsg(result.message);
  }

  async function handleCnesLookup(e: FormEvent) {
    e.preventDefault();
    setCnesResult(await api.lookupCnes(cnesCode));
  }

  async function handleSihPreview(e: FormEvent) {
    e.preventDefault();
    if (!selectedHospId) return;
    setSihPreview(await api.previewSihAih(selectedHospId));
  }

  async function handleSiaPreview(type: 'Bpa' | 'Apac') {
    setSiaPreview(await api.previewSiaDocument(type));
  }

  async function handleRnds() {
    if (!selectedPatientId) return;
    setRndsSummary(await api.queryRndsPatient(selectedPatientId));
  }

  return (
    <>
      <PageHeader
        eyebrow="Integrações Governamentais"
        title={resolvePageTitle(pathname)}
        subtitle="Ecossistema SUS — CNS, CNES, SIH-SUS, SIA-SUS, RNDS, Hórus e FHIR. APIs mock até credenciamento oficial."
      />

      <ModuleNav basePath="/integracoes-gov" tabs={govIntegrationTabs} contextId="regulatory" />

      {msg && <div className="alert alert-info" style={{ marginTop: 12 }}>{msg}</div>}

      {active === '' && (
        <>
          <div className="kpi-grid" style={{ marginTop: 16 }}>
            <KpiCard label="Integrações cadastradas" value={profiles.length} variant="primary" />
            <KpiCard label="Prioridade 1 (SUS core)" value={p1} variant="success" />
            <KpiCard label="Modo mock" value={mockCount} />
            <KpiCard label="Padrão técnico" value="FHIR R4" variant="info" />
          </div>
          <div className="card-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Ecossistema governamental</div>
            <table className="data-table">
              <thead><tr><th>Sistema</th><th>Prioridade</th><th>Status</th><th>Descrição</th></tr></thead>
              <tbody>
                {profileList.map((p) => (
                  <tr key={p.system}>
                    <td><strong>{p.name}</strong></td>
                    <td>{priorityLabels[p.priority] ?? p.priority}</td>
                    <td>{credentialLabels[p.credentialStatus] ?? p.credentialStatus}</td>
                    <td>{p.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {active === 'cns' && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel">
            <div className="card-panel-header">Consulta CNS (CADSUS)</div>
            <form onSubmit={handleCnsLookup} className="form-grid form-panel">
              <div className="form-field full">
                <label>Número do CNS</label>
                <input value={cnsInput} onChange={(e) => setCnsInput(e.target.value)} placeholder="15 dígitos" />
              </div>
              <div className="form-actions">
                <button type="submit" className="btn">Buscar</button>
              </div>
            </form>
            {cnsResult?.found && (
              <div style={{ padding: '0 16px 16px', fontSize: 14 }}>
                <p><strong>{cnsResult.fullName}</strong></p>
                <p>Nascimento: {cnsResult.birthDate} · Mãe: {cnsResult.motherName}</p>
                <p>{cnsResult.addressCity}/{cnsResult.addressState}</p>
                <form onSubmit={handleApplyCns} className="form-grid" style={{ marginTop: 12 }}>
                  <div className="form-field full">
                    <label>Vincular ao paciente</label>
                    <select value={selectedPatientId} onChange={(e) => setSelectedPatientId(e.target.value)} required>
                      <option value="">Selecione</option>
                      {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
                    </select>
                  </div>
                  <div className="form-actions">
                    <button type="submit" className="btn btn-secondary">Aplicar ao cadastro</button>
                  </div>
                </form>
              </div>
            )}
          </div>
          <div className="card-panel" style={{ padding: 16, color: 'var(--muted)' }}>
            <p>Evita cadastros duplicados e vincula procedimentos SUS ao paciente correto.</p>
            <p style={{ marginTop: 8 }}>Produção requer credenciamento DATASUS/CADSUS.</p>
          </div>
        </div>
      )}

      {active === 'cnes' && (
        <div className="card-panel hub-form-split">
          <div className="card-panel-header">Consulta CNES</div>
          <form onSubmit={handleCnesLookup} className="form-grid form-panel">
            <div className="form-field">
              <label>Código CNES</label>
              <input value={cnesCode} onChange={(e) => setCnesCode(e.target.value)} />
            </div>
            <div className="form-field align-end">
              <button type="submit" className="btn">Consultar estabelecimento</button>
            </div>
          </form>
          {cnesResult && (
            <>
              <div style={{ padding: '0 16px' }}><strong>{cnesResult.name}</strong> — {cnesResult.city}/{cnesResult.state}</div>
              <table className="data-table">
                <thead><tr><th>Profissional</th><th>CBO</th><th>Especialidade</th></tr></thead>
                <tbody>
                  {(cnesResult.professionals ?? []).map((pr) => (
                    <tr key={pr.name}><td>{pr.name}</td><td>{pr.cboCode}</td><td>{pr.specialty}</td></tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </div>
      )}

      {active === 'sih' && (
        <div className="card-panel hub-form-split">
          <div className="card-panel-header">SIH-SUS — prévia AIH</div>
          <form onSubmit={handleSihPreview} className="form-grid form-panel">
            <div className="form-field full">
              <label>Internação ativa</label>
              <select value={selectedHospId} onChange={(e) => setSelectedHospId(e.target.value)} required>
                <option value="">Selecione</option>
                {hospitalizations.map((h) => <option key={h.id} value={h.id}>{h.patientName} — {h.wardName}</option>)}
              </select>
            </div>
            <div className="form-actions">
              <button type="submit" className="btn">Gerar prévia AIH</button>
            </div>
          </form>
          {sihPreview && (
            <div style={{ padding: 16, fontSize: 14 }}>
              <p>AIH: <code>{sihPreview.aihNumber}</code> · Competência {sihPreview.competence} · CNES {sihPreview.cnesCode ?? '—'}</p>
              <p>CID: {sihPreview.primaryCid10Code ?? '—'}{sihPreview.secondaryCid10Code ? ` / ${sihPreview.secondaryCid10Code}` : ''}</p>
              <p>Procedimento SIGTAP: {sihPreview.primaryProcedureCode ?? '—'}{sihPreview.secondaryProcedureCode ? ` / ${sihPreview.secondaryProcedureCode}` : ''}</p>
              <p>Caráter: {sihPreview.character ?? '—'} · Modalidade: {sihPreview.modality ?? '—'}</p>
              <p>{sihPreview.payloadSummary}</p>
            </div>
          )}
        </div>
      )}

      {active === 'sia' && (
        <div className="card-panel" style={{ marginTop: 16, padding: 16 }}>
          <div className="card-panel-header">SIA-SUS — produção ambulatorial e alta complexidade</div>
          <div style={{ display: 'flex', gap: 12, marginTop: 12, flexWrap: 'wrap' }}>
            <button type="button" className="btn" onClick={() => handleSiaPreview('Bpa')}>Prévia BPA</button>
            <button type="button" className="btn btn-secondary" onClick={() => handleSiaPreview('Apac')}>Prévia APAC</button>
            <button type="button" className="btn btn-secondary" onClick={() => api.exportSiaDocument('Bpa').then(() => setMsg('Arquivo BPA baixado.')).catch((e) => setMsg(e.message))}>
              Baixar BPA (.txt)
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => api.exportSiaDocument('Apac').then(() => setMsg('Arquivo APAC baixado.')).catch((e) => setMsg(e.message))}>
              Baixar APAC (.txt)
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => api.exportCihaDocument().then(() => setMsg('Arquivo CIHA baixado.')).catch((e) => setMsg(e.message))}>
              Baixar CIHA (.txt)
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => api.exportSihAihBatch().then(() => setMsg('Lote AIH baixado.')).catch((e) => setMsg(e.message))}>
              Baixar lote AIH (.txt)
            </button>
          </div>
          {siaPreview && (
            <p style={{ marginTop: 16 }}>
              {siaPreview.payloadSummary} — {siaPreview.recordCount} registro(s) · R$ {siaPreview.estimatedValue.toFixed(2)}
            </p>
          )}
        </div>
      )}

      {active === 'rnds' && (
        <div className="card-panel hub-form-split">
          <div className="card-panel-header">RNDS — histórico do paciente (FHIR)</div>
          <div className="form-grid form-panel">
            <div className="form-field">
              <label>Paciente</label>
              <select value={selectedPatientId} onChange={(e) => setSelectedPatientId(e.target.value)}>
                <option value="">Selecione</option>
                {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
            <div className="form-field align-end">
              <button type="button" className="btn" onClick={handleRnds}>Consultar RNDS</button>
            </div>
          </div>
          {rndsSummary && (
            <table className="data-table">
              <thead><tr><th>Categoria</th><th>Registro</th><th>Fonte</th></tr></thead>
              <tbody>
                {(rndsSummary.items ?? []).map((item, i) => (
                  <tr key={i}><td>{item.category}</td><td>{item.title}</td><td>{item.source}</td></tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {(active === 'tiss' || active === 'tuss') && (
        <div className="card-panel" style={{ marginTop: 16, padding: 24 }}>
          <p>Módulo <strong>{active.toUpperCase()}</strong> já implementado.</p>
          <Link to={active === 'tiss' ? '/faturamento-tiss' : '/integracoes/tiss'} className="btn" style={{ marginTop: 12 }}>
            Abrir {active === 'tiss' ? 'Faturamento TISS' : 'catálogo TUSS'}
          </Link>
        </div>
      )}

      {['horus', 'esus', 'conecte', 'fhir'].includes(active) && (
        <div className="card-panel" style={{ marginTop: 16, padding: 24, color: 'var(--muted)' }}>
          <p><strong>{resolvePageTitle(pathname)}</strong> — estrutura reservada. Credenciamento e certificado ICP-Brasil necessários para produção.</p>
          {active === 'fhir' && <Link to="/integracoes/fhir" className="btn" style={{ marginTop: 12 }}>Painel FHIR existente</Link>}
        </div>
      )}
    </>
  );
}
