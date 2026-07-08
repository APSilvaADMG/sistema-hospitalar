import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { api, type IntegrationMessageDto, type PatientDto } from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { integrationTabs } from '../navigation/moduleSections';
import { resolvePageTitle } from '../navigation/sectionBreadcrumb';
import { useModuleSection } from '../navigation/useModuleSection';
import { useLocation, Link } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';
import { IntegrationStatusPanel } from '../components/integrations/IntegrationStatusPanel';

const typeLabels: Record<string, string> = {
  Hl7Inbound: 'HL7 entrada',
  Hl7Outbound: 'HL7 saída',
  FhirExport: 'FHIR export',
  FhirImport: 'FHIR import',
};

const statusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Processed: 'Processado',
  Failed: 'Falha',
};

const demoHl7 = `MSH|^~\\&|LIS|HOSP|HIS|HOSP|20240608120000||ORU^R01|MSG002|P|2.5
PID|1||67890^^^HOSP||Oliveira^Carlos||19750320|M
OBR|1||HEM001|40304361^Hemograma`;

const demoFhir = `{
  "resourceType": "Patient",
  "identifier": [{ "system": "urn:cpf", "value": "12345678901" }],
  "name": [{ "use": "official", "text": "Maria Importada FHIR" }],
  "birthDate": "1990-06-15",
  "gender": "female"
}`;

const SECTION_LINKS: Record<string, { title: string; path: string; desc: string }> = {
  tiss: { title: 'Faturamento TISS', path: '/faturamento-tiss', desc: 'Guias, lotes e glosas' },
  ans: { title: 'ANS / Convênios', path: '/convenios', desc: 'Operadoras e elegibilidade' },
  cnes: { title: 'CNES', path: '/integracoes-gov/cnes', desc: 'Cadastro nacional de estabelecimentos' },
  cadsus: { title: 'CADSUS / CNS', path: '/integracoes-gov/cns', desc: 'Cartão nacional de saúde' },
  sisreg: { title: 'SISREG', path: '/regulacao/sisreg', desc: 'Regulação de vagas' },
  esus: { title: 'e-SUS APS', path: '/integracoes-gov/esus', desc: 'Atenção primária' },
  hl7: { title: 'HL7', path: '/integracoes/hl7', desc: 'Mensageria clínica v2.x' },
  fhir: { title: 'FHIR R4', path: '/integracoes/fhir', desc: 'Interoperabilidade Patient/Bundle' },
  pacs: { title: 'PACS / Imagem', path: '/imagem/pacs', desc: 'Arquivamento de imagens DICOM' },
  laboratorio: { title: 'Laboratório', path: '/laboratorio/integracoes', desc: 'LIS e resultados' },
};

function safeMessages(list: IntegrationMessageDto[] | null | undefined) {
  return Array.isArray(list) ? list : [];
}

export function IntegrationsPage() {
  const { pathname } = useLocation();
  const { section } = useModuleSection('/integracoes');
  const activeSection = (section || '').split('/')[0];

  const { hasPermission } = useAuth();
  const [messages, setMessages] = useState<IntegrationMessageDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [exportPatientId, setExportPatientId] = useState('');
  const [fhirExport, setFhirExport] = useState('');
  const [fhirImport, setFhirImport] = useState(demoFhir);
  const [hl7Message, setHl7Message] = useState(demoHl7);
  const [feedback, setFeedback] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  useEffect(() => {
    refresh();
    api.getPatients('', 1)
      .then((p) => setPatients(Array.isArray(p?.items) ? p.items : []))
      .catch(console.error);
  }, []);

  function refresh() {
    setLoading(true);
    api.getIntegrationMessages()
      .then((list) => setMessages(safeMessages(list)))
      .catch(console.error)
      .finally(() => setLoading(false));
  }

  const messageList = safeMessages(messages);

  const stats = useMemo(() => ({
    total: messageList.length,
    processed: messageList.filter((m) => m.status === 'Processed').length,
    failed: messageList.filter((m) => m.status === 'Failed').length,
    pending: messageList.filter((m) => m.status === 'Pending').length,
  }), [messageList]);

  const filtered = useMemo(() => {
    return messageList.filter((m) => !statusFilter || m.status === statusFilter);
  }, [messageList, statusFilter]);

  const showInteropForms =
    (!activeSection || activeSection === 'hl7' || activeSection === 'fhir') && activeSection !== 'status';
  const showMessageLog = showInteropForms || activeSection === 'tiss';
  const sectionLink = SECTION_LINKS[activeSection];

  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {
    return <div className="card">Acesso restrito à equipe clínica e administrativa.</div>;
  }

  async function handleHl7(e: FormEvent) {
    e.preventDefault();
    try {
      const result = await api.processHl7Inbound({ rawMessage: hl7Message, source: 'Web-UI' });
      setFeedback(result.summary ?? 'HL7 processado');
      refresh();
    } catch (err) {
      setFeedback(err instanceof Error ? err.message : 'Erro HL7');
    }
  }

  async function handleFhirExport(e: FormEvent) {
    e.preventDefault();
    if (!exportPatientId) return;
    try {
      const result = await api.exportFhirPatient(exportPatientId);
      setFhirExport(result.json);
      setFeedback(`FHIR exportado — ${result.resourceType}/${result.id}`);
      refresh();
    } catch (err) {
      setFeedback(err instanceof Error ? err.message : 'Erro FHIR export');
    }
  }

  async function handleFhirImport(e: FormEvent) {
    e.preventDefault();
    try {
      const result = await api.importFhirPatient(fhirImport);
      setFeedback(result.summary ?? 'FHIR importado');
      refresh();
      api.getPatients('', 1)
        .then((p) => setPatients(Array.isArray(p?.items) ? p.items : []))
        .catch(console.error);
    } catch (err) {
      setFeedback(err instanceof Error ? err.message : 'Erro FHIR import');
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Administrativo · Interoperabilidade"
        title={resolvePageTitle(pathname)}
        subtitle="HL7, FHIR e conectores com TISS, ANS, CADSUS, PACS e laboratório."
      />

      <ModuleNav basePath="/integracoes" tabs={integrationTabs} contextId="regulatory" />

      {feedback && <div className="alert alert-success">{feedback}</div>}

      {activeSection === 'status' && <IntegrationStatusPanel />}

      <div className="kpi-grid">
        <KpiCard label="Mensagens totais" value={stats.total} variant="primary" />
        <KpiCard label="Processadas" value={stats.processed} variant="success" />
        <KpiCard label="Pendentes" value={stats.pending} variant="warning" />
        <KpiCard label="Falhas" value={stats.failed} variant="danger" />
      </div>

      {sectionLink && activeSection && activeSection !== 'hl7' && activeSection !== 'fhir' && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="card-panel-header">{sectionLink.title}</div>
          <div className="card-panel-body">
            <p className="form-hint" style={{ marginTop: 0 }}>{sectionLink.desc}</p>
            <div className="form-actions" style={{ marginTop: 12 }}>
              <Link to={sectionLink.path} className="btn">Abrir módulo</Link>
              <Link to="/integracoes-gov" className="btn btn-secondary">Integrações governamentais</Link>
            </div>
          </div>
        </div>
      )}

      {!activeSection && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="card-panel-header">Conectores disponíveis</div>
          <div className="card-panel-body">
            <div className="action-grid">
              {Object.entries(SECTION_LINKS).map(([slug, link]) => (
                <Link key={slug} to={slug === 'hl7' || slug === 'fhir' ? `/integracoes/${slug}` : link.path} className="action-tile">
                  <span>{link.title}</span>
                </Link>
              ))}
            </div>
          </div>
        </div>
      )}

      {showInteropForms && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel appt-panel" id="hl7">
            <div className="card-panel-header">HL7 — Entrada</div>
            <div className="card-panel-body">
              <form onSubmit={handleHl7} className="form-grid">
                <div className="form-field full">
                  <label>Mensagem HL7 v2.x</label>
                  <textarea rows={6} value={hl7Message} onChange={(e) => setHl7Message(e.target.value)} />
                </div>
                <div className="form-actions">
                  <button type="submit" className="btn btn-primary">Processar HL7</button>
                </div>
              </form>
            </div>
          </div>

          <div className="card-panel appt-panel" id="fhir">
            <div className="card-panel-header">FHIR R4 — Patient</div>
            <div className="card-panel-body">
              <form onSubmit={handleFhirExport} className="form-grid">
                <div className="form-field full">
                  <label>Exportar paciente</label>
                  <select value={exportPatientId} onChange={(e) => setExportPatientId(e.target.value)} required>
                    <option value="">Selecione...</option>
                    {patients.map((p) => (
                      <option key={p.id} value={p.id}>{p.fullName}</option>
                    ))}
                  </select>
                </div>
                <div className="form-actions">
                  <button type="submit" className="btn btn-secondary">Exportar FHIR</button>
                </div>
              </form>
              {fhirExport && (
                <pre className="code-block" style={{ marginTop: 12 }}>{fhirExport}</pre>
              )}
              <form onSubmit={handleFhirImport} className="form-grid" style={{ marginTop: 16 }}>
                <div className="form-field full">
                  <label>Importar JSON FHIR</label>
                  <textarea rows={6} value={fhirImport} onChange={(e) => setFhirImport(e.target.value)} />
                </div>
                <div className="form-actions">
                  <button type="submit" className="btn btn-primary">Importar FHIR</button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {showMessageLog && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="card-panel-header">Log de mensagens — {filtered.length} mensagem(ns)</div>
          <FilterBar>
            <div className="filter-field w-lg">
              <label htmlFor="intStatus">Status</label>
              <select id="intStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">Todos</option>
                {Object.entries(statusLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
          </FilterBar>
          <div className="card-panel-body" style={{ padding: 0 }}>
            {loading ? (
              <p className="form-hint" style={{ padding: 16 }}>Carregando mensagens...</p>
            ) : (
              <table className="data-table">
                <thead>
                  <tr><th>Data</th><th>Tipo</th><th>Status</th><th>Origem</th><th>Paciente</th><th>Preview</th></tr>
                </thead>
                <tbody>
                  {filtered.map((m) => (
                    <tr key={m.id}>
                      <td>{formatBrDateTime(m.createdAt)}</td>
                      <td>{typeLabels[m.type] ?? m.type}</td>
                      <td><span className="badge">{statusLabels[m.status] ?? m.status}</span></td>
                      <td>{m.source}</td>
                      <td>{m.patientName ?? '—'}</td>
                      <td className="mono">{m.payloadPreview}</td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr>
                      <td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                        Nenhuma mensagem registrada. Processe HL7 ou FHIR acima para gerar entradas no log.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}
    </>
  );
}
