import { type FormEvent, useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  api,
  type AccessCredentialDto,
  type PatientDto,
  type ProductDto,
} from '../../api/client';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { automacaoTabs } from '../../navigation/moduleSections';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useModuleSection } from '../../navigation/useModuleSection';
import { printPatientLabel } from '../../utils/printTemplates';
import { formatBrDateTime } from '../../utils/dateUtils';

type FlowRule = {
  id: string;
  name: string;
  trigger: string;
  action: string;
  enabled: boolean;
  lastRun?: string;
};

const DEFAULT_FLOWS: FlowRule[] = [
  { id: '1', name: 'Check-in → Triagem PS', trigger: 'Agendamento confirmado', action: 'Abrir fila de classificação de risco', enabled: true },
  { id: '2', name: 'Alta internação → Faturamento', trigger: 'Alta hospitalar', action: 'Gerar conta e pré-faturamento SUS', enabled: true },
  { id: '3', name: 'Estoque baixo → Compras', trigger: 'Produto abaixo do mínimo', action: 'Sugerir requisição de compra', enabled: false },
  { id: '4', name: 'Laudo imagem → PEP', trigger: 'Laudo liberado', action: 'Anexar ao prontuário do paciente', enabled: true },
];

function barcodePattern(value: string) {
  const clean = value.replace(/\D/g, '').slice(0, 20) || value.slice(0, 12);
  return clean.padEnd(12, '0').split('').map((c) => (/\d/.test(c) ? '█'.repeat(Number(c) % 3 + 1) : '▌')).join(' ');
}

export function AutomacaoHubPage() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/automacao');
  const active = section || 'codigo-barras';

  useEffect(() => {
    if (pathname === '/automacao' || pathname === '/automacao/') {
      navigate('/automacao/codigo-barras', { replace: true });
    }
  }, [pathname, navigate]);

  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [apiCredentials, setApiCredentials] = useState<AccessCredentialDto[]>([]);
  const [localRfid, setLocalRfid] = useState<AccessCredentialDto[]>([]);
  const [patientSearch, setPatientSearch] = useState('');
  const [selectedPatientId, setSelectedPatientId] = useState('');
  const [productId, setProductId] = useState('');
  const [rfidForm, setRfidForm] = useState({ holderName: '', token: '', zoneName: 'Ala A' });
  const [flows, setFlows] = useState<FlowRule[]>(DEFAULT_FLOWS);
  const [flowForm, setFlowForm] = useState({ name: '', trigger: '', action: '' });
  const [msg, setMsg] = useState('');

  useEffect(() => {
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
    api.getProducts().then(setProducts).catch(console.error);
    api.getAccessCredentials().then(setApiCredentials).catch(console.error);
  }, []);

  const rfidCredentials = [...apiCredentials.filter((c) => c.credentialType === 'Rfid'), ...localRfid];
  const selectedPatient = patients.find((p) => p.id === selectedPatientId);

  async function handlePrintLabel() {
    if (!selectedPatientId) return;
    try {
      const detail = await api.getPatient(selectedPatientId);
      printPatientLabel(detail);
      setMsg(`Etiqueta gerada para ${detail.fullName}.`);
    } catch (err) {
      setMsg(err instanceof Error ? err.message : 'Erro ao gerar etiqueta.');
    }
  }

  function handleRfidRegister(e: FormEvent) {
    e.preventDefault();
    if (!rfidForm.holderName.trim()) return;
    const token = rfidForm.token.trim() || `RFID-${Date.now().toString(36).toUpperCase()}`;
    setLocalRfid((prev) => [{
      id: crypto.randomUUID(),
      personType: 'Employee',
      holderName: rfidForm.holderName,
      credentialType: 'Rfid',
      status: 'Active',
      token,
      zoneName: rfidForm.zoneName,
      validUntil: new Date(Date.now() + 365 * 86400000).toISOString(),
    }, ...prev]);
    setRfidForm({ holderName: '', token: '', zoneName: 'Ala A' });
    setMsg('Credencial RFID registrada (sessão local). Use Acesso Físico para emissão integrada.');
  }

  function handleFlowAdd(e: FormEvent) {
    e.preventDefault();
    if (!flowForm.name.trim()) return;
    setFlows((prev) => [{
      id: crypto.randomUUID(),
      name: flowForm.name,
      trigger: flowForm.trigger,
      action: flowForm.action,
      enabled: true,
      lastRun: undefined,
    }, ...prev]);
    setFlowForm({ name: '', trigger: '', action: '' });
    setMsg('Fluxo automático cadastrado.');
  }

  function toggleFlow(id: string) {
    setFlows((prev) => prev.map((f) => f.id === id ? { ...f, enabled: !f.enabled } : f));
  }

  const filteredPatients = patients.filter((p) => {
    if (!patientSearch.trim()) return true;
    const t = patientSearch.toLowerCase();
    return p.fullName.toLowerCase().includes(t) || p.cpf.includes(t);
  });

  return (
    <>
      <PageHeader
        eyebrow="Automação e IA"
        title={breadcrumb.title}
        subtitle="Identificação por código de barras, RFID hospitalar e fluxos automáticos entre módulos."
      >
        <Link to="/ia" className="btn btn-secondary">IA Assistencial</Link>
      </PageHeader>

      <ModuleNav basePath="/automacao" tabs={automacaoTabs} />

      {msg && <div className="alert alert-info" style={{ marginTop: 12 }}>{msg}</div>}

      {active === 'codigo-barras' && (
        <div style={{ marginTop: 16, display: 'grid', gap: 16 }}>
          <div className="card form-grid">
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Etiquetas de paciente</h3>
            <div className="form-field"><label>Buscar paciente</label>
              <input value={patientSearch} onChange={(e) => setPatientSearch(e.target.value)} placeholder="Nome ou CPF..." />
            </div>
            <div className="form-field"><label>Paciente</label>
              <select value={selectedPatientId} onChange={(e) => setSelectedPatientId(e.target.value)}>
                <option value="">Selecione</option>
                {filteredPatients.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName} — {p.cpf}</option>
                ))}
              </select>
            </div>
            {selectedPatient && (
              <div className="form-field full" style={{ fontFamily: 'monospace', textAlign: 'center', padding: 16, background: 'var(--surface-2)', borderRadius: 8 }}>
                <div style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 8 }}>Pré-visualização código de barras</div>
                <div style={{ letterSpacing: 2 }}>{barcodePattern(selectedPatient.cpf || selectedPatient.id)}</div>
                <div style={{ marginTop: 8, fontSize: 13 }}>{selectedPatient.cpf}</div>
              </div>
            )}
            <div className="form-actions">
              <button className="btn" type="button" onClick={handlePrintLabel} disabled={!selectedPatientId}>Imprimir etiqueta</button>
              <Link to="/pacientes" className="btn btn-secondary">Cadastro de pacientes</Link>
            </div>
          </div>

          <div className="card-panel appt-panel">
            <div className="card-panel-header">Produtos / insumos (GS1-128 simulado)</div>
            <div className="card-panel-body">
              <div className="form-field" style={{ maxWidth: 400 }}>
                <label>Produto</label>
                <select value={productId} onChange={(e) => setProductId(e.target.value)}>
                  <option value="">Selecione</option>
                  {products.map((p) => (
                    <option key={p.id} value={p.id}>{p.name} — SKU {p.sku}</option>
                  ))}
                </select>
              </div>
              {productId && (
                <div style={{ marginTop: 12, fontFamily: 'monospace', textAlign: 'center', padding: 12, background: 'var(--surface-2)', borderRadius: 8 }}>
                  {barcodePattern(products.find((p) => p.id === productId)?.sku ?? '')}
                </div>
              )}
              <Link to="/farmacia" className="btn btn-secondary btn-sm" style={{ marginTop: 12 }}>Farmácia</Link>
            </div>
          </div>
        </div>
      )}

      {active === 'rfid' && (
        <div style={{ marginTop: 16, display: 'grid', gap: 16 }}>
          <form className="card form-grid" onSubmit={handleRfidRegister}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Registrar credencial RFID</h3>
            <div className="form-field"><label>Portador</label>
              <input value={rfidForm.holderName} onChange={(e) => setRfidForm({ ...rfidForm, holderName: e.target.value })} required />
            </div>
            <div className="form-field"><label>Token / UID do cartão</label>
              <input value={rfidForm.token} onChange={(e) => setRfidForm({ ...rfidForm, token: e.target.value })} placeholder="Auto se vazio" />
            </div>
            <div className="form-field"><label>Zona permitida</label>
              <input value={rfidForm.zoneName} onChange={(e) => setRfidForm({ ...rfidForm, zoneName: e.target.value })} />
            </div>
            <div className="form-actions">
              <button className="btn" type="submit">Registrar</button>
              <Link to="/acesso-fisico/credenciais" className="btn btn-secondary">Credenciais integradas</Link>
            </div>
          </form>

          <div className="card-panel appt-panel">
            <div className="card-panel-header">Credenciais RFID — {rfidCredentials.length}</div>
            <table className="data-table">
              <thead><tr><th>Portador</th><th>Token</th><th>Zona</th><th>Status</th><th>Validade</th></tr></thead>
              <tbody>
                {rfidCredentials.map((c) => (
                  <tr key={c.id}>
                    <td>{c.holderName}</td>
                    <td><code>{c.token}</code></td>
                    <td>{c.zoneName ?? '—'}</td>
                    <td>{c.status}</td>
                    <td>{c.validUntil ? formatBrDateTime(c.validUntil) : '—'}</td>
                  </tr>
                ))}
                {rfidCredentials.length === 0 && (
                  <tr><td colSpan={5} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Nenhuma credencial RFID.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'fluxos' && (
        <div style={{ marginTop: 16, display: 'grid', gap: 16 }}>
          <form className="card form-grid" onSubmit={handleFlowAdd}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Novo fluxo automático</h3>
            <div className="form-field"><label>Nome</label>
              <input value={flowForm.name} onChange={(e) => setFlowForm({ ...flowForm, name: e.target.value })} required />
            </div>
            <div className="form-field"><label>Gatilho</label>
              <input value={flowForm.trigger} onChange={(e) => setFlowForm({ ...flowForm, trigger: e.target.value })} placeholder="Ex.: Alta hospitalar" />
            </div>
            <div className="form-field full"><label>Ação</label>
              <input value={flowForm.action} onChange={(e) => setFlowForm({ ...flowForm, action: e.target.value })} placeholder="Ex.: Notificar faturamento" />
            </div>
            <div className="form-actions">
              <button className="btn" type="submit">Adicionar fluxo</button>
            </div>
          </form>

          <div className="card-panel appt-panel">
            <div className="card-panel-header">Fluxos configurados</div>
            <table className="data-table">
              <thead><tr><th>Nome</th><th>Gatilho</th><th>Ação</th><th>Ativo</th><th /></tr></thead>
              <tbody>
                {flows.map((f) => (
                  <tr key={f.id}>
                    <td><strong>{f.name}</strong></td>
                    <td>{f.trigger}</td>
                    <td>{f.action}</td>
                    <td>{f.enabled ? 'Sim' : 'Não'}</td>
                    <td>
                      <button type="button" className="btn btn-secondary btn-sm" onClick={() => toggleFlow(f.id)}>
                        {f.enabled ? 'Desativar' : 'Ativar'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
}
