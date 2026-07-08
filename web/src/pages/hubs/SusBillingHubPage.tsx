import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  isHospitalizationActive,
  type BillingDashboardDto,
  type HospitalizationDto,
  type SiaDocumentPreviewDto,
  type SihAihPreviewDto,
  type SigtapProcedureDto,
} from '../../api/client';
import { DashboardAlertsPanel } from '../../components/dashboard/DashboardAlertsPanel';
import { FilterBar } from '../../components/FilterBar';
import { KpiCard } from '../../components/KpiCard';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { susBillingTabs } from '../../navigation/moduleSections';
import { useModuleSection } from '../../navigation/useModuleSection';
import { formatBrDate } from '../../utils/dateUtils';

const SECTION_META: Record<string, { title: string; hint: string }> = {
  '': { title: 'Dashboard de Faturamento', hint: 'Visão integrada: convênios TISS, SUS, contas e glosas (TELA-139).' },
  'sus/aih': { title: 'AIH — Autorização de Internação Hospitalar', hint: 'Internações ativas elegíveis para faturamento SUS (AIH).' },
  'sus/bpa': { title: 'BPA — Boletim de Produção Ambulatorial', hint: 'Produção ambulatorial consolidada para exportação ao SIA/SUS.' },
  'sus/apac': { title: 'APAC — Alta Complexidade', hint: 'Procedimentos de alta complexidade (oncologia, diálise, etc.).' },
  'sus/producao-ambulatorial': { title: 'Produção Ambulatorial', hint: 'Consolidação de consultas e procedimentos ambulatoriais.' },
  'sus/exportacoes': { title: 'Exportações SUS', hint: 'Arquivos de exportação compatíveis com layout DATASUS / SIA-SUS / SIH-SUS.' },
  'auditoria/pre-faturamento': { title: 'Pré-Faturamento', hint: 'Conferência de contas antes do envio ao gestor.' },
  'auditoria/medica': { title: 'Auditoria Médica', hint: 'Revisão clínica de procedimentos e permanência.' },
  'auditoria/enfermagem': { title: 'Auditoria de Enfermagem', hint: 'Conferência de diárias, taxas e materiais.' },
};

function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function SusBillingHubPage() {
  const { section } = useModuleSection('/faturamento');
  const activeSection = section ?? '';
  const meta = SECTION_META[activeSection] ?? SECTION_META[''];

  const [dashboard, setDashboard] = useState<BillingDashboardDto | null>(null);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [sigtap, setSigtap] = useState<SigtapProcedureDto[]>([]);
  const [sigtapSearch, setSigtapSearch] = useState('');
  const [competence, setCompetence] = useState(() => {
    const d = new Date();
    return `${d.getFullYear()}${String(d.getMonth() + 1).padStart(2, '0')}`;
  });
  const [siaPreview, setSiaPreview] = useState<SiaDocumentPreviewDto | null>(null);
  const [aihPreview, setAihPreview] = useState<SihAihPreviewDto | null>(null);
  const [loadingPreview, setLoadingPreview] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    api.getHospitalizations().then(setHospitalizations).catch(console.error);
    api.getSigtapProcedures(undefined, 1, 200).then((r) => setSigtap(r.items)).catch(console.error);
    api.getBillingDashboard().then(setDashboard).catch(console.error);
  }, []);

  const activeHosp = hospitalizations.filter((h) => isHospitalizationActive(h.status));
  async function generateSia(type: 'Bpa' | 'Apac') {
    setError('');
    setSuccess('');
    setLoadingPreview(true);
    try {
      const preview = await api.previewSiaDocument(type, competence);
      setSiaPreview(preview);
      setSuccess(`Prévia ${type} gerada para competência ${competence} — ${preview.recordCount} registro(s).`);
      api.getBillingDashboard().then(setDashboard).catch(console.error);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar prévia SIA.');
    } finally {
      setLoadingPreview(false);
    }
  }

  async function downloadExport(kind: 'Bpa' | 'Apac' | 'Aih' | 'Ciha') {
    setError('');
    setSuccess('');
    setLoadingPreview(true);
    try {
      if (kind === 'Bpa') await api.exportSiaDocument('Bpa', competence);
      else if (kind === 'Apac') await api.exportSiaDocument('Apac', competence);
      else if (kind === 'Aih') await api.exportSihAihBatch(competence);
      else await api.exportCihaDocument(competence);
      setSuccess(`Arquivo DATASUS (${kind}) baixado para competência ${competence}.`);
      api.getBillingDashboard().then(setDashboard).catch(console.error);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao exportar arquivo DATASUS.');
    } finally {
      setLoadingPreview(false);
    }
  }

  async function closeBillingAccount(hospitalizationId: string) {
    setError('');
    setSuccess('');
    try {
      const updated = await api.closeHospitalizationBillingAccount(hospitalizationId);
      setHospitalizations((prev) => prev.map((h) => (h.id === updated.id ? updated : h)));
      setSuccess('Conta hospitalar fechada — pronta para faturamento (RN-028).');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao fechar conta.');
    }
  }

  async function generateAih(hospitalizationId: string) {
    setError('');
    setLoadingPreview(true);
    try {
      const preview = await api.previewSihAih(hospitalizationId);
      setAihPreview(preview);
      setSuccess(`AIH gerada: ${preview.aihNumber}`);
      api.getBillingDashboard().then(setDashboard).catch(console.error);
      api.getHospitalizations().then(setHospitalizations).catch(console.error);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar AIH.');
    } finally {
      setLoadingPreview(false);
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Volume 4 · Faturamento Hospitalar"
        title={meta.title}
        subtitle={meta.hint}
      >
        <Link to="/faturamento-tiss" className="btn btn-secondary btn-sm">TISS Convênios</Link>
        <Link to="/financeiro" className="btn btn-sm">Financeiro</Link>
      </PageHeader>

      <ModuleNav basePath="/faturamento" tabs={susBillingTabs} contextId="hospitalBilling" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {activeSection === '' && dashboard && (
        <>
          <DashboardAlertsPanel alerts={dashboard.alerts.map((a) => ({
            code: a.code,
            severity: a.severity,
            title: a.title,
            message: a.message,
            linkPath: a.linkPath,
          }))} />

          <div className="kpi-grid" style={{ marginTop: 16 }}>
            <KpiCard label="Contas abertas" value={dashboard.openAccountsCount} variant="warning" />
            <KpiCard label="Valor em aberto" value={formatCurrency(dashboard.openAccountsAmount)} variant="danger" />
            <KpiCard label="Recebido no mês" value={formatCurrency(dashboard.receivedThisMonth)} variant="success" />
            <KpiCard label="Faturado TISS" value={formatCurrency(dashboard.totalBilled)} variant="primary" />
            <KpiCard label="Glosas em aberto" value={formatCurrency(dashboard.totalGlosaOpen)} variant="danger" />
            <KpiCard label="Taxa de glosa" value={`${dashboard.glosaRatePercent}%`} variant={dashboard.glosaRatePercent >= 5 ? 'danger' : 'neutral'} />
          </div>

          <div className="kpi-grid kpi-grid-6" style={{ marginTop: 12 }}>
            <KpiCard label="Guias rascunho" value={dashboard.tissGuidesDraft} variant="info" />
            <KpiCard label="Guias enviadas" value={dashboard.tissGuidesSent} variant="warning" />
            <KpiCard label="Guias pagas" value={dashboard.tissGuidesPaid} variant="success" />
            <KpiCard label="Guias glosadas" value={dashboard.tissGuidesGlosa} variant="danger" />
            <KpiCard label="Internações SUS" value={dashboard.activeSusHospitalizations} variant="primary" />
            <KpiCard label="Exportações SUS (mês)" value={dashboard.susExportsThisMonth} variant="neutral" />
          </div>

          <div className="grid-2" style={{ marginTop: 20 }}>
            <div className="card-panel appt-panel">
              <div className="card-panel-header">Produção por convênio (TISS)</div>
              <div className="card-panel-body">
                <ul className="bi-list">
                  <li><span>Recebido TISS</span><strong>{formatCurrency(dashboard.totalPaid)}</strong></li>
                  <li><span>A receber (financeiro)</span><strong>{formatCurrency(dashboard.receivableOpen)}</strong></li>
                  <li><span>Guias +30 dias</span><strong>{dashboard.guidesPendingOver30Days}</strong></li>
                  <li><span>AIH prontas</span><strong>{dashboard.aihReadyCount}</strong></li>
                </ul>
              </div>
              <div className="card-panel-footer" style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                <Link to="/faturamento-tiss/glosas" className="btn btn-secondary btn-sm">Central de Glosas</Link>
                <Link to="/faturamento-tiss/lotes" className="btn btn-secondary btn-sm">Lotes TISS</Link>
                <Link to="/faturamento/sus/aih" className="btn btn-secondary btn-sm">AIH SUS</Link>
              </div>
            </div>

            <div className="card-panel appt-panel">
              <div className="card-panel-header">Acesso rápido — Faturamento</div>
              <div className="card-panel-body action-grid">
                <Link to="/faturamento-tiss/autorizacoes" className="action-tile">Autorizações</Link>
                <Link to="/faturamento/sus/bpa" className="action-tile">BPA</Link>
                <Link to="/faturamento/sus/apac" className="action-tile">APAC</Link>
                <Link to="/faturamento/auditoria/pre-faturamento" className="action-tile">Pré-Faturamento</Link>
                <Link to="/integracoes-gov" className="action-tile">Integrações Gov</Link>
                <Link to="/relatorios" className="action-tile">Relatórios</Link>
              </div>
            </div>
          </div>
        </>
      )}

      {activeSection.startsWith('sus/aih') && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Internações para AIH — {activeHosp.length} ativa(s)</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Paciente</th><th>Leito</th><th>Admissão</th><th>Diagnóstico</th><th>Conta</th><th>AIH</th><th>Ações</th></tr>
              </thead>
              <tbody>
                {activeHosp.map((h) => (
                  <tr key={h.id}>
                    <td>{h.patientName}</td>
                    <td>{h.bedNumber ?? '—'}</td>
                    <td>{formatBrDate(h.admittedAt)}</td>
                    <td>{h.diagnosis ?? h.susData?.primaryCid10Code ?? '—'}</td>
                    <td>
                      {h.billingAccountClosedAt
                        ? <span className="badge badge-success">Fechada</span>
                        : <span className="badge badge-warning">Aberta</span>}
                    </td>
                    <td className="mono">{h.susData?.aihNumber ?? '—'}</td>
                    <td>
                      <div className="table-actions">
                        {!h.billingAccountClosedAt && (
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() => closeBillingAccount(h.id)}
                          >
                            Fechar conta
                          </button>
                        )}
                        <button
                          type="button"
                          className="btn btn-sm"
                          disabled={loadingPreview || !h.billingAccountClosedAt}
                          title={!h.billingAccountClosedAt ? 'Feche a conta antes de gerar AIH (RN-028)' : undefined}
                          onClick={() => generateAih(h.id)}
                        >
                          Gerar AIH
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {activeHosp.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma internação ativa.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          {aihPreview && (
            <div className="card-panel-footer">
              <strong>Última AIH:</strong> {aihPreview.aihNumber} — {aihPreview.payloadSummary}
            </div>
          )}
        </div>
      )}

      {(activeSection.startsWith('sus/bpa') || activeSection.startsWith('sus/apac') || activeSection.startsWith('sus/producao')) && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>{activeSection.includes('apac') ? 'APAC — Alta complexidade' : 'BPA — Produção ambulatorial'}</h3>
          <p className="form-hint">Gere a prévia de exportação SIA-SUS para a competência selecionada.</p>
          <FilterBar>
            <div className="filter-field w-md">
              <label htmlFor="competence">Competência (AAAAMM)</label>
              <input id="competence" value={competence} onChange={(e) => setCompetence(e.target.value)} placeholder="202606" />
            </div>
          </FilterBar>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 12 }}>
            <button type="button" className="btn" disabled={loadingPreview} onClick={() => generateSia('Bpa')}>
              Gerar prévia BPA
            </button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => generateSia('Apac')}>
              Gerar prévia APAC
            </button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => downloadExport('Bpa')}>
              Baixar BPA (.txt)
            </button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => downloadExport('Apac')}>
              Baixar APAC (.txt)
            </button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => downloadExport('Ciha')}>
              Baixar CIHA (.txt)
            </button>
            <Link to="/agendamentos/consultas" className="btn btn-secondary">Ver consultas</Link>
            <Link to="/relatorios?module=Regulatory&q=APAC" className="btn btn-secondary">Relatório APAC</Link>
          </div>
          {siaPreview && (
            <div className="alert alert-info" style={{ marginTop: 16 }}>
              <strong>{siaPreview.documentType}</strong> — {siaPreview.payloadSummary}
              <br />
              Registros: {siaPreview.recordCount} · Valor estimado: {formatCurrency(siaPreview.estimatedValue)}
              {siaPreview.lines && siaPreview.lines.length > 0 ? (
                <>
                  <br />
                  <span className="form-hint">Primeiros procedimentos: {siaPreview.lines.slice(0, 3).map((l) => l.procedureLabel).join(' · ')}</span>
                </>
              ) : null}
            </div>
          )}
        </div>
      )}

      {activeSection.startsWith('sus/export') && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Exportação de arquivos SUS (DATASUS)</h3>
          <p className="form-hint">
            Gere e baixe arquivos .txt compatíveis com layout DATASUS (SIA-SUS / SIH-SUS / CIHA).
            As exportações são registradas na trilha de integração.
          </p>
          <FilterBar>
            <div className="filter-field w-md">
              <label htmlFor="exportCompetence">Competência (AAAAMM)</label>
              <input id="exportCompetence" value={competence} onChange={(e) => setCompetence(e.target.value)} placeholder="202606" />
            </div>
          </FilterBar>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 12 }}>
            <button type="button" className="btn" disabled={loadingPreview} onClick={() => downloadExport('Bpa')}>Baixar BPA</button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => downloadExport('Apac')}>Baixar APAC</button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => downloadExport('Aih')}>Baixar lote AIH</button>
            <button type="button" className="btn btn-secondary" disabled={loadingPreview} onClick={() => downloadExport('Ciha')}>Baixar CIHA</button>
            <Link to="/faturamento/sus/aih" className="btn btn-secondary">AIH (por internação)</Link>
            <Link to="/integracoes-gov" className="btn btn-secondary">Painel Gov</Link>
            <Link to="/relatorios?module=Regulatory" className="btn btn-secondary">Relatórios regulatórios</Link>
          </div>
          {dashboard && (
            <p className="dashboard-meta" style={{ marginTop: 12 }}>
              Exportações SUS neste mês: {dashboard.susExportsThisMonth}
            </p>
          )}
        </div>
      )}

      {activeSection.startsWith('auditoria') && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">{meta.title}</div>
          <div className="card-panel-body">
            <p>Checklist de auditoria antes do fechamento da conta hospitalar:</p>
            <ul>
              <li>Conferir guias TISS vinculadas em <Link to="/faturamento-tiss">Faturamento Convênios</Link></li>
              <li>Validar CID, procedimentos SIGTAP/TUSS e permanência em <Link to="/internacao">Internação</Link></li>
              <li>Revisar glosas e recursos em <Link to="/faturamento-tiss/glosas">Glosas TISS</Link></li>
              <li>Conferir contas em <Link to="/financeiro">Financeiro</Link></li>
            </ul>
            {dashboard && (
              <p><strong>{dashboard.openAccountsCount}</strong> conta(s) em aberto totalizando {formatCurrency(dashboard.openAccountsAmount)}.</p>
            )}
          </div>
        </div>
      )}

      {!activeSection.startsWith('auditoria') && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Tabela SIGTAP (referência) — {sigtap.length} procedimento(s)</div>
          <FilterBar>
            <div className="filter-field grow">
              <label>Buscar procedimento</label>
              <input value={sigtapSearch} onChange={(e) => setSigtapSearch(e.target.value)} placeholder="Código ou descrição..." />
            </div>
          </FilterBar>
          <div className="card-panel-body" style={{ padding: 0, maxHeight: 280, overflow: 'auto' }}>
            <table className="data-table">
              <thead><tr><th>Código</th><th>Descrição</th><th>Grupo</th></tr></thead>
              <tbody>
                {sigtap
                  .filter((s) => !sigtapSearch || s.code.includes(sigtapSearch) || s.description.toLowerCase().includes(sigtapSearch.toLowerCase()))
                  .slice(0, 50)
                  .map((s) => (
                    <tr key={s.id}><td className="mono">{s.code}</td><td>{s.description}</td><td>{s.groupName ?? '—'}</td></tr>
                  ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
}
