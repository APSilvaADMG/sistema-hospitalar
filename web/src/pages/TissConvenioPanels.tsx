import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  authorizationStatusLabels,
  authorizationTypeLabels,
  eligibilityStatusLabels,
  tissBatchStatusLabels,
  type CreateAuthorizationRequest,
  type CreateTissBatchRequest,
  type EligibilityCheckDto,
  type HealthInsuranceDto,
  type InsuranceAuthorizationDto,
  type PatientDto,
  type TissBatchDetailDto,
  type TissBatchDto,
  type TissConvenioDashboardDto,
  type TissXmlValidationResultDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { formatBrDateTime } from '../utils/dateUtils';

function money(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

type Props = {
  tab: 'eligibility' | 'authorizations' | 'batches' | 'dashboard';
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  onMessage: (error: string, success: string) => void;
};

export function TissConvenioPanels({ tab, patients, insurances, onMessage }: Props) {
  const [eligibility, setEligibility] = useState<EligibilityCheckDto[]>([]);
  const [authorizations, setAuthorizations] = useState<InsuranceAuthorizationDto[]>([]);
  const [batches, setBatches] = useState<TissBatchDto[]>([]);
  const [dashboard, setDashboard] = useState<TissConvenioDashboardDto | null>(null);
  const [batchDetail, setBatchDetail] = useState<TissBatchDetailDto | null>(null);

  const [eligForm, setEligForm] = useState({ patientId: '', healthInsuranceId: '', cardNumber: '' });
  const [authForm, setAuthForm] = useState<CreateAuthorizationRequest>({
    patientId: '', healthInsuranceId: '', authorizationType: 2, authorizationNumber: '',
  });
  const [batchForm, setBatchForm] = useState<CreateTissBatchRequest>({
    healthInsuranceId: '', competence: new Date().toISOString().slice(0, 7),
  });
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [showBatchXml, setShowBatchXml] = useState(false);
  const [batchValidation, setBatchValidation] = useState<TissXmlValidationResultDto | null>(null);
  const [validatingBatch, setValidatingBatch] = useState(false);

  async function loadTab() {
    onMessage('', '');
    try {
      if (tab === 'eligibility') setEligibility(await api.getEligibilityHistory());
      if (tab === 'authorizations') setAuthorizations(await api.getInsuranceAuthorizations());
      if (tab === 'batches') setBatches(await api.getTissBatches());
      if (tab === 'dashboard') setDashboard(await api.getTissConvenioDashboard());
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Erro ao carregar.', '');
    }
  }

  useEffect(() => { loadTab().catch(console.error); }, [tab]);

  async function handleEligibility(event: FormEvent) {
    event.preventDefault();
    onMessage('', '');
    try {
      const result = await api.checkEligibility(eligForm);
      setEligibility((prev) => [result, ...prev]);
      onMessage('', result.responseMessage ?? 'Consulta realizada.');
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Erro na elegibilidade.', '');
    }
  }

  async function handleCreateAuth(event: FormEvent) {
    event.preventDefault();
    if (!authForm.authorizationNumber?.trim()) {
      onMessage('Informe a senha manualmente ou use "Solicitar online na operadora".', '');
      return;
    }
    onMessage('', '');
    try {
      await api.createInsuranceAuthorization(authForm);
      setShowAuthModal(false);
      setAuthorizations(await api.getInsuranceAuthorizations());
      onMessage('', 'Autorização registrada.');
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Erro ao salvar autorização.', '');
    }
  }

  async function handleRequestOnlineAuth() {
    if (!authForm.patientId || !authForm.healthInsuranceId) {
      onMessage('Selecione paciente e convênio.', '');
      return;
    }
    onMessage('', '');
    try {
      const result = await api.requestOnlineAuthorization({
        patientId: authForm.patientId,
        healthInsuranceId: authForm.healthInsuranceId,
        authorizationType: authForm.authorizationType,
        procedureSummary: authForm.procedureSummary,
        tissGuideId: authForm.tissGuideId,
        notes: authForm.notes,
        validFrom: authForm.validFrom,
        validUntil: authForm.validUntil,
      });
      setAuthForm((f) => ({ ...f, authorizationNumber: result.authorizationNumber }));
      setShowAuthModal(false);
      setAuthorizations(await api.getInsuranceAuthorizations());
      onMessage('', `Autorização online aprovada. Senha: ${result.authorizationNumber}`);
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Falha na autorização online.', '');
    }
  }

  async function handleCreateBatch(event: FormEvent) {
    event.preventDefault();
    onMessage('', '');
    try {
      const batch = await api.createTissBatch(batchForm);
      setBatches(await api.getTissBatches());
      setBatchDetail(batch);
      setShowBatchXml(true);
      onMessage('', `Lote ${batch.batchNumber} gerado com XML TISS.`);
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Erro ao gerar lote.', '');
    }
  }

  if (tab === 'dashboard' && dashboard) {
    return (
      <>
        <div className="kpi-grid">
          <KpiCard label="Faturado (guias)" value={money(dashboard.totalBilled)} variant="primary" />
          <KpiCard label="Recebido (pagas)" value={money(dashboard.totalPaid)} variant="success" />
          <KpiCard label="Glosas em aberto" value={money(dashboard.totalGlosaOpen)} variant="warning" />
          <KpiCard label="Índice de glosa" value={`${dashboard.glosaRatePercent}%`} variant="info" />
          <KpiCard label="Enviadas +30 dias" value={dashboard.guidesSentOver30Days} variant="warning" />
          <KpiCard label="Enviadas +60 dias" value={dashboard.guidesSentOver60Days} variant="warning" />
        </div>
        <div className="grid-2" style={{ marginTop: 24 }}>
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Faturamento por operadora</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead><tr><th>Operadora</th><th>Guias</th><th>Valor</th></tr></thead>
                <tbody>
                  {dashboard.byOperator.map((o) => (
                    <tr key={o.operatorName}><td>{o.operatorName}</td><td>{o.count}</td><td>{money(o.amount)}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Glosas por operadora</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead><tr><th>Operadora</th><th>Guias</th><th>Valor glosado</th></tr></thead>
                <tbody>
                  {dashboard.glosaByOperator.length === 0 && (
                    <tr><td colSpan={3} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Sem glosas abertas</td></tr>
                  )}
                  {dashboard.glosaByOperator.map((o) => (
                    <tr key={o.operatorName}><td>{o.operatorName}</td><td>{o.count}</td><td>{money(o.amount)}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </>
    );
  }

  if (tab === 'eligibility') {
    return (
      <>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Consulta de elegibilidade (TISS)</div>
          <div className="card-panel-body">
            <form className="form-grid" onSubmit={handleEligibility}>
              <div className="form-field">
                <label>Paciente</label>
                <select required value={eligForm.patientId} onChange={(e) => setEligForm({ ...eligForm, patientId: e.target.value })}>
                  <option value="">Selecione</option>
                  {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Convênio</label>
                <select required value={eligForm.healthInsuranceId} onChange={(e) => setEligForm({ ...eligForm, healthInsuranceId: e.target.value })}>
                  <option value="">Selecione</option>
                  {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Carteirinha (opcional)</label>
                <input value={eligForm.cardNumber} onChange={(e) => setEligForm({ ...eligForm, cardNumber: e.target.value })} placeholder="Usa cadastro do paciente se vazio" />
              </div>
              <div className="form-actions">
                <button className="btn" type="submit">Consultar elegibilidade</button>
              </div>
            </form>
          </div>
        </div>
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Histórico de consultas</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Data</th><th>Paciente</th><th>Operadora</th><th>Carteirinha</th><th>Status</th><th>Retorno</th></tr></thead>
              <tbody>
                {eligibility.map((e) => (
                  <tr key={e.id}>
                    <td>{formatBrDateTime(e.createdAt)}</td>
                    <td>{e.patientName}</td>
                    <td>{e.healthInsuranceName}</td>
                    <td>{e.cardNumber}</td>
                    <td>{eligibilityStatusLabels[e.status]}</td>
                    <td>{e.responseMessage}</td>
                  </tr>
                ))}
                {eligibility.length === 0 && (
                  <tr><td colSpan={6} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma consulta registrada</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </>
    );
  }

  if (tab === 'authorizations') {
    return (
      <>
        <FilterBar actions={<button className="btn" type="button" onClick={() => setShowAuthModal(true)}>+ Nova autorização</button>}>
          <span />
        </FilterBar>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Autorizações / senhas convênio</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Data</th><th>Paciente</th><th>Operadora</th><th>Tipo</th><th>Senha</th><th>Validade</th><th>Status</th></tr></thead>
              <tbody>
                {authorizations.map((a) => (
                  <tr key={a.id}>
                    <td>{formatBrDateTime(a.createdAt)}</td>
                    <td>{a.patientName}</td>
                    <td>{a.healthInsuranceName}</td>
                    <td>{authorizationTypeLabels[a.authorizationType]}</td>
                    <td><strong>{a.authorizationNumber}</strong></td>
                    <td>{a.validUntil ? formatBrDateTime(a.validUntil) : '—'}</td>
                    <td>{authorizationStatusLabels[a.status]}</td>
                  </tr>
                ))}
                {authorizations.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma autorização</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
        <Modal open={showAuthModal} title="Registrar autorização" onClose={() => setShowAuthModal(false)}>
          <form className="form-grid" onSubmit={handleCreateAuth}>
            <div className="form-field">
              <label>Paciente</label>
              <select required value={authForm.patientId} onChange={(e) => setAuthForm({ ...authForm, patientId: e.target.value })}>
                <option value="">Selecione</option>
                {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
            <div className="form-field">
              <label>Convênio</label>
              <select required value={authForm.healthInsuranceId} onChange={(e) => setAuthForm({ ...authForm, healthInsuranceId: e.target.value })}>
                <option value="">Selecione</option>
                {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
              </select>
            </div>
            <div className="form-field">
              <label>Tipo</label>
              <select value={authForm.authorizationType} onChange={(e) => setAuthForm({ ...authForm, authorizationType: Number(e.target.value) })}>
                {Object.entries(authorizationTypeLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Nº autorização / senha</label>
              <input value={authForm.authorizationNumber} onChange={(e) => setAuthForm({ ...authForm, authorizationNumber: e.target.value })} placeholder="Manual ou via botão online" />
            </div>
            <div className="form-field">
              <label>Procedimento / resumo</label>
              <input value={authForm.procedureSummary ?? ''} onChange={(e) => setAuthForm({ ...authForm, procedureSummary: e.target.value })} />
            </div>
            <p className="form-hint">Operadoras prioritárias: Bradesco, Amil, SulAmérica, Hapvida, Unimed, Porto, Notre Dame, Golden Cross.</p>
            <div className="form-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setShowAuthModal(false)}>Cancelar</button>
              <button className="btn btn-secondary" type="button" onClick={() => handleRequestOnlineAuth().catch(console.error)}>
                Solicitar online na operadora
              </button>
              <button className="btn" type="submit">Salvar senha manual</button>
            </div>
          </form>
        </Modal>
      </>
    );
  }

  if (tab === 'batches') {
    return (
      <>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Gerar lote XML TISS</div>
          <div className="card-panel-body">
            <form className="form-grid" onSubmit={handleCreateBatch}>
              <div className="form-field">
                <label>Operadora</label>
                <select required value={batchForm.healthInsuranceId} onChange={(e) => setBatchForm({ ...batchForm, healthInsuranceId: e.target.value })}>
                  <option value="">Selecione</option>
                  {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Competência (AAAA-MM)</label>
                <input required pattern="\d{4}-\d{2}" value={batchForm.competence} onChange={(e) => setBatchForm({ ...batchForm, competence: e.target.value })} />
              </div>
              <div className="form-actions">
                <button className="btn" type="submit">Gerar lote XML</button>
              </div>
              <p className="form-hint">Inclui automaticamente guias com status &quot;Enviada&quot; ainda não vinculadas a lote.</p>
            </form>
          </div>
        </div>
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Lotes gerados</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Lote</th><th>Operadora</th><th>Competência</th><th>Guias</th><th>Total</th><th>Status</th><th>Ações</th></tr></thead>
              <tbody>
                {batches.map((b) => (
                  <tr key={b.id}>
                    <td><strong>{b.batchNumber}</strong></td>
                    <td>{b.healthInsuranceName}</td>
                    <td>{b.competence}</td>
                    <td>{b.guideCount}</td>
                    <td>{money(b.totalAmount)}</td>
                    <td>{tissBatchStatusLabels[b.status]}</td>
                    <td>
                      <div className="table-actions">
                        <button type="button" className="btn btn-secondary btn-sm" onClick={async () => {
                          const detail = await api.getTissBatch(b.id);
                          setBatchDetail(detail);
                          setBatchValidation(null);
                          setShowBatchXml(true);
                        }}>Ver XML</button>
                        <button type="button" className="btn btn-secondary btn-sm" onClick={async () => {
                          try {
                            await api.downloadTissBatchXml(b.id, b.batchNumber);
                          } catch (err) {
                            onMessage(err instanceof Error ? err.message : 'Erro ao baixar XML.', '');
                          }
                        }}>Download</button>
                        {b.status === 2 && (
                          <button type="button" className="btn btn-sm" onClick={async () => {
                            await api.sendTissBatch(b.id);
                            await loadTab();
                            onMessage('', 'Lote marcado como enviado à operadora.');
                          }}>Enviar</button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {batches.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhum lote</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
        <Modal open={showBatchXml} title={batchDetail ? `XML — ${batchDetail.batchNumber}` : 'XML TISS'} onClose={() => setShowBatchXml(false)} width="lg">
          {batchDetail && (
            <div className="form-actions" style={{ marginBottom: 12 }}>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                disabled={validatingBatch}
                onClick={async () => {
                  setValidatingBatch(true);
                  onMessage('', '');
                  try {
                    const result = await api.validateTissBatch(batchDetail.id);
                    setBatchValidation(result);
                    onMessage(
                      result.isValid ? '' : result.errors.join(' · '),
                      result.isValid ? 'XML válido (hash e estrutura verificados).' : '',
                    );
                  } catch (err) {
                    onMessage(err instanceof Error ? err.message : 'Erro na validação.', '');
                  } finally {
                    setValidatingBatch(false);
                  }
                }}
              >
                {validatingBatch ? 'Validando...' : 'Validar XML (ANS)'}
              </button>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => api.downloadTissBatchXml(batchDetail.id, batchDetail.batchNumber).catch((err) => {
                  onMessage(err instanceof Error ? err.message : 'Erro ao baixar.', '');
                })}
              >
                Baixar .xml
              </button>
            </div>
          )}
          {batchValidation && (
            <div className={`alert ${batchValidation.isValid ? 'alert-success' : 'alert-error'}`} style={{ marginBottom: 12 }}>
              <strong>Versão TISS:</strong> {batchValidation.tissVersion ?? '—'}
              {' · '}
              <strong>Hash:</strong> {batchValidation.hashValid ? 'OK' : 'inválido'}
              {batchValidation.schemaMessage && (
                <>
                  <br />
                  <span className="form-hint">{batchValidation.schemaMessage}</span>
                </>
              )}
              {batchValidation.errors.length > 0 && (
                <ul style={{ margin: '8px 0 0', paddingLeft: 18 }}>
                  {batchValidation.errors.map((e) => <li key={e}>{e}</li>)}
                </ul>
              )}
            </div>
          )}
          {batchDetail?.xmlContent ? (
            <pre className="mono" style={{ maxHeight: 480, overflow: 'auto', fontSize: 12, whiteSpace: 'pre-wrap' }}>{batchDetail.xmlContent}</pre>
          ) : (
            <p className="form-hint">XML não disponível.</p>
          )}
        </Modal>
      </>
    );
  }

  return null;
}
