import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  api,
  demonstrativoStatusLabels,
  operatorTransactionTypeLabels,
  tissAnnexTypeLabels,
  tussTableTypeLabel,
  type CreateTissGuideAnnexRequest,
  type FetchOperatorDemonstrativoRequest,
  type HealthInsuranceDto,
  type HealthInsuranceIntegrationDto,
  type ImportDemonstrativoRequest,
  type OperatorTransactionLogDto,
  type SigtapCatalogSummaryDto,
  type SigtapProcedureDto,
  type TissDemonstrativoDto,
  type TissGuideAnnexDto,
  type TissGuideDto,
  type TissReconciliationSummaryDto,
  type TussCatalogDto,
  type BillingCatalogSummaryDto,
  type OperatorProfileDto,
  type UpdateHealthInsuranceIntegrationRequest,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { formatBrDateTime } from '../utils/dateUtils';
import { formatTussDescription } from '../utils/formatTussDescription';

function money(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

type ExtendedTab = 'demonstrativos' | 'tuss' | 'sigtap' | 'integrations' | 'annexes' | 'reconciliation';

type Props = {
  tab: ExtendedTab;
  insurances: HealthInsuranceDto[];
  guides: TissGuideDto[];
  onMessage: (error: string, success: string) => void;
};

function tussDescriptionLabel(item: TussCatalogDto): string {
  const description = item.description?.trim();
  if (!description || description === item.code)
    return '—';
  return formatTussDescription(description);
}

export function TissExtendedPanels({ tab, insurances, guides, onMessage }: Props) {
  const { hasPermission } = useAuth();
  const canManageTuss = hasPermission('billing.write');
  const [demonstrativos, setDemonstrativos] = useState<TissDemonstrativoDto[]>([]);
  const [tussItems, setTussItems] = useState<TussCatalogDto[]>([]);
  const [tussPage, setTussPage] = useState(1);
  const [tussTotalCount, setTussTotalCount] = useState(0);
  const [tussTotalPages, setTussTotalPages] = useState(0);
  const [tussLoading, setTussLoading] = useState(false);
  const [tussLoadError, setTussLoadError] = useState('');
  const [sigtapItems, setSigtapItems] = useState<SigtapProcedureDto[]>([]);
  const [sigtapPage, setSigtapPage] = useState(1);
  const [sigtapTotalCount, setSigtapTotalCount] = useState(0);
  const [sigtapTotalPages, setSigtapTotalPages] = useState(0);
  const [sigtapLoading, setSigtapLoading] = useState(false);
  const [sigtapLoadError, setSigtapLoadError] = useState('');
  const [sigtapSummary, setSigtapSummary] = useState<SigtapCatalogSummaryDto | null>(null);
  const [integrations, setIntegrations] = useState<HealthInsuranceIntegrationDto[]>([]);
  const [operatorProfiles, setOperatorProfiles] = useState<OperatorProfileDto[]>([]);
  const [logs, setLogs] = useState<OperatorTransactionLogDto[]>([]);
  const [reconciliation, setReconciliation] = useState<TissReconciliationSummaryDto | null>(null);
  const [annexes, setAnnexes] = useState<TissGuideAnnexDto[]>([]);

  const [tussSearch, setTussSearch] = useState('');
  const [tussCsvImport, setTussCsvImport] = useState('');
  const [tussImporting, setTussImporting] = useState(false);
  const [tussImportStatus, setTussImportStatus] = useState('');
  const [tussImportFeedback, setTussImportFeedback] = useState<{ kind: 'success' | 'error'; message: string } | null>(null);
  const [sigtapImporting, setSigtapImporting] = useState(false);
  const [sigtapSyncing, setSigtapSyncing] = useState(false);
  const [sigtapImportStatus, setSigtapImportStatus] = useState('');
  const [sigtapImportFeedback, setSigtapImportFeedback] = useState<{ kind: 'success' | 'error'; message: string } | null>(null);
  const [catalogSummary, setCatalogSummary] = useState<BillingCatalogSummaryDto | null>(null);
  const [sigtapSearch, setSigtapSearch] = useState('');
  const [demoImport, setDemoImport] = useState<ImportDemonstrativoRequest>({
    healthInsuranceId: '', competence: new Date().toISOString().slice(0, 7), csvContent: '',
  });
  const [fetchDemo, setFetchDemo] = useState<FetchOperatorDemonstrativoRequest>({ healthInsuranceId: '' });
  const [annexForm, setAnnexForm] = useState<CreateTissGuideAnnexRequest>({
    tissGuideId: '', annexType: 3, opmeItems: [{ tussCode: '', description: '', quantity: 1, unitPrice: 0 }],
  });
  const [showAnnexModal, setShowAnnexModal] = useState(false);
  const [editingIntegration, setEditingIntegration] = useState<HealthInsuranceIntegrationDto | null>(null);
  const [integrationForm, setIntegrationForm] = useState<UpdateHealthInsuranceIntegrationRequest>({
    useMockIntegration: true,
  });

  const tussPageSize = 50;
  const sigtapPageSize = 50;

  const loadTussCatalog = useCallback(async (page: number) => {
    setTussLoading(true);
    setTussLoadError('');
    try {
      const [pageResult, summary] = await Promise.all([
        api.getTussCatalog(tussSearch || undefined, undefined, page, tussPageSize),
        api.getBillingCatalogSummary(),
      ]);
      setTussItems(pageResult.items);
      setTussPage(pageResult.page);
      setTussTotalCount(pageResult.totalCount);
      setTussTotalPages(pageResult.totalPages);
      setCatalogSummary(summary);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Erro ao carregar catálogo TUSS.';
      setTussLoadError(message);
      setTussItems([]);
      onMessage(message, '');
    } finally {
      setTussLoading(false);
    }
  }, [onMessage, tussPageSize, tussSearch]);

  const loadSigtapCatalog = useCallback(async (page: number) => {
    setSigtapLoading(true);
    setSigtapLoadError('');
    try {
      const [pageResult, summary] = await Promise.all([
        api.getSigtapProcedures(sigtapSearch || undefined, page, sigtapPageSize),
        api.getSigtapSummary(),
      ]);
      setSigtapItems(pageResult.items);
      setSigtapPage(pageResult.page);
      setSigtapTotalCount(pageResult.totalCount);
      setSigtapTotalPages(pageResult.totalPages);
      setSigtapSummary(summary);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Erro ao carregar catálogo SIGTAP.';
      setSigtapLoadError(message);
      setSigtapItems([]);
      onMessage(message, '');
    } finally {
      setSigtapLoading(false);
    }
  }, [onMessage, sigtapPageSize, sigtapSearch]);

  async function load() {
    onMessage('', '');
    try {
      if (tab === 'demonstrativos') setDemonstrativos(await api.getTissDemonstrativos());
      if (tab === 'sigtap') await loadSigtapCatalog(1);
      if (tab === 'integrations') {
        const [integrationList, profileList, logList] = await Promise.all([
          api.getInsuranceIntegrations(),
          api.getOperatorProfiles(),
          api.getOperatorTransactionLogs(30),
        ]);
        setIntegrations(integrationList);
        setOperatorProfiles(profileList);
        setLogs(logList);
      }
      if (tab === 'reconciliation') setReconciliation(await api.getTissReconciliation());
      if (tab === 'annexes' && annexForm.tissGuideId) {
        setAnnexes(await api.getTissGuideAnnexes(annexForm.tissGuideId));
      }
    } catch (err) {
      onMessage(err instanceof Error ? err.message : 'Erro ao carregar.', '');
    }
  }

  useEffect(() => { load().catch(console.error); }, [tab]);

  useEffect(() => {
    if (tab !== 'tuss') return;
    setTussPage(1);
    void loadTussCatalog(1);
  }, [tab, tussSearch, loadTussCatalog]);

  useEffect(() => {
    if (tab !== 'sigtap') return;
    setSigtapPage(1);
    void loadSigtapCatalog(1);
  }, [tab, sigtapSearch, loadSigtapCatalog]);

  if (tab === 'reconciliation' && reconciliation) {
    return (
      <div className="kpi-grid">
        <KpiCard label="Guias com conta a receber" value={reconciliation.guidesWithReceivable} variant="primary" />
        <KpiCard label="Quitadas no financeiro" value={reconciliation.guidesPaidInFinance} variant="success" />
        <KpiCard label="A receber em aberto" value={money(reconciliation.totalReceivableOpen)} variant="warning" />
        <KpiCard label="Recebido (financeiro)" value={money(reconciliation.totalReceivablePaid)} variant="info" />
      </div>
    );
  }

  if (tab === 'tuss') {
    return (
      <div className="card-panel appt-panel">
        {catalogSummary && (
          <div className="kpi-grid" style={{ marginBottom: 16 }}>
            <KpiCard label="TUSS" value={catalogSummary.tussCount} variant="primary" />
            <KpiCard label="CBHPM" value={catalogSummary.cbhpmCount} variant="info" />
            <KpiCard label="Brasíndice" value={catalogSummary.brasindiceCount} variant="success" />
            <KpiCard label="SIMPRO" value={catalogSummary.simproCount} variant="warning" />
            <KpiCard label="CID-10" value={catalogSummary.cid10Count} variant="neutral" />
          </div>
        )}
        <FilterBar>
          <div className="filter-field grow">
            <label>Buscar TUSS (tabela master ANS)</label>
            <input value={tussSearch} onChange={(e) => setTussSearch(e.target.value)} placeholder="Código ou descrição..." />
          </div>
        </FilterBar>
        {tussTotalCount > 0 ? (
          <p className="form-hint" style={{ marginBottom: 12 }}>
            Exibindo {((tussPage - 1) * tussPageSize) + 1}–{Math.min(tussPage * tussPageSize, tussTotalCount)} de {tussTotalCount.toLocaleString('pt-BR')} itens TUSS
            {tussSearch.trim() ? ` · filtro: “${tussSearch.trim()}”` : ''}
          </p>
        ) : null}
        {canManageTuss ? (
        <div className="card" style={{ marginBottom: 16 }}>
          <h3 style={{ marginTop: 0, fontSize: '0.95rem' }}>Importar tabela TUSS (ANS)</h3>
          <p className="form-hint">
            Importe planilhas .xlsx oficiais (TUSS 202601, incluindo OPME parte 1 e 2) ou cole CSV. Pacote local: Diversos/TISS/202601.
            Se a coluna Descrição repetir o código, reimporte o pacote para corrigir os rótulos do catálogo.
          </p>
          {tussImportStatus && (
            <div className="alert alert-info" style={{ marginBottom: 12 }}>
              {tussImportStatus}
            </div>
          )}
          {tussImportFeedback && (
            <div className={`alert ${tussImportFeedback.kind === 'success' ? 'alert-success' : 'alert-error'}`} style={{ marginBottom: 12 }}>
              {tussImportFeedback.message}
            </div>
          )}
          <div style={{ display: 'flex', gap: 8, marginBottom: 12, flexWrap: 'wrap', alignItems: 'center' }}>
            <label className={`btn btn-secondary${tussImporting ? ' disabled' : ''}`} style={{ cursor: tussImporting ? 'wait' : 'pointer', opacity: tussImporting ? 0.7 : 1 }}>
              {tussImporting ? 'Importando…' : 'Enviar arquivo .xlsx'}
              <input
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                style={{ display: 'none' }}
                disabled={tussImporting}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (!file) return;
                  setTussImporting(true);
                  setTussImportFeedback(null);
                  setTussImportStatus(`Enviando ${file.name}… A importação pode levar vários minutos.`);
                  onMessage('', '');
                  api.importTussXlsx(file).then((r) => {
                    const message = r.message || `Importação concluída: ${r.imported} termo(s) de ${r.totalInFile}.`;
                    onMessage('', message);
                    setTussImportFeedback({ kind: 'success', message });
                    setTussImportStatus('');
                    void loadTussCatalog(1);
                  }).catch((err) => {
                    const message = err instanceof Error ? err.message : 'Erro na importação XLSX';
                    onMessage(message, '');
                    setTussImportFeedback({ kind: 'error', message });
                    setTussImportStatus('');
                  }).finally(() => setTussImporting(false));
                  e.target.value = '';
                }}
              />
            </label>
            <button
              className="btn"
              type="button"
              disabled={tussImporting}
              onClick={() => {
                setTussImporting(true);
                setTussImportFeedback(null);
                setTussImportStatus('Importando pacote TUSS 202601 (procedimentos, medicamentos, diárias, OPME…). Pode levar 5–15 minutos — não feche a página.');
                onMessage('', '');
                api.importBundledTuss202601().then((r) => {
                  const message = r.message || `Importação concluída: ${r.imported} termo(s) de ${r.totalInFile}.`;
                  onMessage('', message);
                  setTussImportFeedback({ kind: 'success', message });
                  setTussImportStatus('');
                  void loadTussCatalog(1);
                }).catch((err) => {
                  const message = err instanceof Error ? err.message : 'Erro ao importar pacote 202601';
                  onMessage(message, '');
                  setTussImportFeedback({ kind: 'error', message });
                  setTussImportStatus('');
                }).finally(() => setTussImporting(false));
              }}
            >
              {tussImporting ? 'Importando TUSS 202601…' : 'Importar TUSS 202601 (pasta local)'}
            </button>
          </div>
          <textarea
            rows={5}
            value={tussCsvImport}
            onChange={(e) => setTussCsvImport(e.target.value)}
            placeholder="codigo;descricao;tipo;unidade;valor_referencia"
            style={{ width: '100%', fontFamily: 'monospace', fontSize: 12 }}
          />
          <div style={{ display: 'flex', gap: 8, marginTop: 8, flexWrap: 'wrap' }}>
            <button
              className="btn"
              type="button"
              onClick={() => {
                setTussImportFeedback(null);
                api.importTussCsv(tussCsvImport).then((r) => {
                  const message = r.message || `Importação concluída: ${r.imported} termo(s) de ${r.totalInFile}.`;
                  onMessage('', message);
                  setTussImportFeedback({ kind: 'success', message });
                  void loadTussCatalog(1);
                }).catch((err) => {
                  const message = err instanceof Error ? err.message : 'Erro na importação';
                  onMessage(message, '');
                  setTussImportFeedback({ kind: 'error', message });
                });
              }}
            >
              Importar CSV
            </button>
            <button
              className="btn btn-secondary"
              type="button"
              onClick={() => {
                api.seedExpandedTussCatalog().then((r) => {
                  onMessage('', r.message);
                  void loadTussCatalog(1);
                }).catch((err) => onMessage(err instanceof Error ? err.message : 'Erro', ''));
              }}
            >
              Carregar catálogo hospitalar (~60 itens)
            </button>
            <button
              className="btn btn-secondary"
              type="button"
              onClick={() => {
                api.getTussSampleCsv().then((r) => setTussCsvImport(r.csv)).catch(console.error);
              }}
            >
              Ver modelo CSV
            </button>
          </div>
        </div>
        ) : null}
        {tussLoadError ? <div className="alert alert-error" style={{ marginBottom: 12 }}>{tussLoadError}</div> : null}
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table tuss-catalog-table">
            <colgroup>
              <col className="tuss-col-code" />
              <col className="tuss-col-desc" />
              <col className="tuss-col-tabela" />
              <col className="tuss-col-unidade" />
              <col className="tuss-col-ref" />
            </colgroup>
            <thead>
              <tr>
                <th className="tuss-col-code">Código</th>
                <th className="tuss-col-desc">Descrição</th>
                <th className="tuss-col-tabela">Tabela</th>
                <th className="tuss-col-unidade">Unidade</th>
                <th className="tuss-col-ref">Ref. R$</th>
              </tr>
            </thead>
            <tbody>
              {tussLoading && tussItems.length === 0 ? (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Carregando catálogo TUSS…</td></tr>
              ) : null}
              {!tussLoading && tussItems.length === 0 ? (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                  {tussLoadError ? 'Não foi possível carregar os itens TUSS.' : 'Nenhum item encontrado para o filtro informado.'}
                </td></tr>
              ) : null}
              {tussItems.map((t) => (
                <tr key={t.id}>
                  <td className="tuss-col-code"><strong>{t.code}</strong></td>
                  <td className="tuss-col-desc" title={tussDescriptionLabel(t)}>{tussDescriptionLabel(t)}</td>
                  <td className="tuss-col-tabela">{tussTableTypeLabel(t.tableType)}</td>
                  <td className="tuss-col-unidade">{t.unit ?? '—'}</td>
                  <td className="tuss-col-ref">{t.referencePrice != null ? money(t.referencePrice) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {tussTotalPages > 1 ? (
          <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 12, flexWrap: 'wrap' }}>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              disabled={tussLoading || tussPage <= 1}
              onClick={() => void loadTussCatalog(tussPage - 1)}
            >
              Anterior
            </button>
            <span className="form-hint" style={{ alignSelf: 'center' }}>
              Página {tussPage} de {tussTotalPages}
            </span>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              disabled={tussLoading || tussPage >= tussTotalPages}
              onClick={() => void loadTussCatalog(tussPage + 1)}
            >
              Próxima
            </button>
          </div>
        ) : null}
      </div>
    );
  }

  if (tab === 'sigtap') {
    return (
      <div className="card-panel appt-panel">
        {sigtapSummary && (
          <div className="kpi-grid" style={{ marginBottom: 16 }}>
            <KpiCard label="SIGTAP ativos" value={sigtapSummary.totalCount} variant="primary" />
            <KpiCard label="Última competência" value={sigtapSummary.lastCompetence ?? '—'} variant="info" />
            <KpiCard label="Última importação" value={sigtapSummary.lastImportAt ? formatBrDateTime(sigtapSummary.lastImportAt) : '—'} variant="neutral" />
          </div>
        )}
        <FilterBar>
          <div className="filter-field grow">
            <label>Buscar procedimento SIGTAP (SUS)</label>
            <input value={sigtapSearch} onChange={(e) => setSigtapSearch(e.target.value)} placeholder="Código ou descrição..." />
          </div>
        </FilterBar>
        {canManageTuss ? (
          <div className="card" style={{ marginBottom: 16 }}>
            <h3 style={{ marginTop: 0, fontSize: '0.95rem' }}>Importar tabela SIGTAP (DATASUS)</h3>
            <p className="form-hint">
              Baixe automaticamente a competência mais recente no DATASUS ou envie manualmente o .zip/.txt da tabela unificada.
            </p>
            {sigtapImportStatus && (
              <div className="alert alert-info" style={{ marginBottom: 12 }}>
                {sigtapImportStatus}
              </div>
            )}
            {sigtapImportFeedback && (
              <div className={`alert ${sigtapImportFeedback.kind === 'success' ? 'alert-success' : 'alert-error'}`} style={{ marginBottom: 12 }}>
                {sigtapImportFeedback.message}
              </div>
            )}
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
              <button
                type="button"
                className={`btn btn-primary${sigtapSyncing || sigtapImporting ? ' disabled' : ''}`}
                disabled={sigtapSyncing || sigtapImporting}
                style={{ cursor: sigtapSyncing ? 'wait' : 'pointer', opacity: sigtapSyncing ? 0.7 : 1 }}
                onClick={() => {
                  setSigtapSyncing(true);
                  setSigtapImportFeedback(null);
                  setSigtapImportStatus('Consultando feed oficial DATASUS e baixando tabela unificada…');
                  onMessage('', '');
                  api.syncSigtapOfficial().then((r) => {
                    const detail = r.inserted + r.updated > 0
                      ? `${r.inserted} novo(s), ${r.updated} atualizado(s), ${r.skipped} sem alteração`
                      : `${r.skipped} procedimento(s) já estavam atualizados`;
                    const message = r.message || `SIGTAP ${r.competence} sincronizado (${detail}).`;
                    onMessage('', message);
                    setSigtapImportFeedback({ kind: r.success ? 'success' : 'error', message });
                    setSigtapImportStatus('');
                    setSigtapSearch('');
                    void loadSigtapCatalog(1);
                  }).catch((err) => {
                    const message = err instanceof Error ? err.message : 'Erro na sincronização SIGTAP';
                    onMessage(message, '');
                    setSigtapImportFeedback({ kind: 'error', message });
                    setSigtapImportStatus('');
                  }).finally(() => setSigtapSyncing(false));
                }}
              >
                {sigtapSyncing ? 'Atualizando…' : 'Atualizar tabela SIGTAP (oficial)'}
              </button>
            <label className={`btn btn-secondary${sigtapImporting || sigtapSyncing ? ' disabled' : ''}`} style={{ cursor: sigtapImporting ? 'wait' : 'pointer', opacity: sigtapImporting ? 0.7 : 1 }}>
              {sigtapImporting ? 'Importando…' : 'Enviar SIGTAP (.zip/.txt)'}
              <input
                type="file"
                accept=".zip,.txt,application/zip,text/plain"
                style={{ display: 'none' }}
                disabled={sigtapImporting || sigtapSyncing}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (!file) return;
                  setSigtapImporting(true);
                  setSigtapImportFeedback(null);
                  setSigtapImportStatus(`Enviando ${file.name}...`);
                  onMessage('', '');
                  api.importSigtapZip(file).then((r) => {
                    const message = r.message || `Importação concluída: ${r.imported} procedimento(s) na competência ${r.competence}.`;
                    const ok = r.imported > 0 || r.totalInFile > 0;
                    onMessage(ok ? '' : message, ok ? message : '');
                    setSigtapImportFeedback({ kind: ok ? 'success' : 'error', message });
                    setSigtapImportStatus('');
                    if (ok) {
                      setSigtapSearch('');
                      void loadSigtapCatalog(1);
                    }
                  }).catch((err) => {
                    const message = err instanceof Error ? err.message : 'Erro na importação SIGTAP';
                    onMessage(message, '');
                    setSigtapImportFeedback({ kind: 'error', message });
                    setSigtapImportStatus('');
                  }).finally(() => setSigtapImporting(false));
                  e.target.value = '';
                }}
              />
            </label>
            </div>
          </div>
        ) : null}
        {sigtapTotalCount > 0 ? (
          <p className="form-hint" style={{ marginBottom: 12 }}>
            Exibindo {((sigtapPage - 1) * sigtapPageSize) + 1}–{Math.min(sigtapPage * sigtapPageSize, sigtapTotalCount)} de {sigtapTotalCount.toLocaleString('pt-BR')} itens SIGTAP
            {sigtapSearch.trim() ? ` · filtro: "${sigtapSearch.trim()}"` : ''}
          </p>
        ) : null}
        {sigtapLoadError ? <div className="alert alert-error" style={{ marginBottom: 12 }}>{sigtapLoadError}</div> : null}
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Código</th><th>Competência</th><th>Descrição</th><th>Grupo</th><th>Complexidade</th><th>Hosp.</th><th>Prof.</th></tr></thead>
            <tbody>
              {sigtapLoading && sigtapItems.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Carregando catálogo SIGTAP…</td></tr>
              ) : null}
              {!sigtapLoading && sigtapItems.length === 0 ? (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                  {sigtapLoadError ? 'Não foi possível carregar os itens SIGTAP.' : 'Nenhum item encontrado para o filtro informado.'}
                </td></tr>
              ) : null}
              {sigtapItems.map((s) => (
                <tr key={s.id}>
                  <td><strong>{s.code}</strong></td>
                  <td>{s.competence}</td>
                  <td>{s.description}</td>
                  <td>{s.groupName ?? '—'}</td>
                  <td>{s.complexity ?? '—'}</td>
                  <td>{s.hospitalAmount != null ? money(s.hospitalAmount) : '—'}</td>
                  <td>{s.professionalAmount != null ? money(s.professionalAmount) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {sigtapTotalPages > 1 ? (
          <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 12, flexWrap: 'wrap' }}>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              disabled={sigtapLoading || sigtapPage <= 1}
              onClick={() => void loadSigtapCatalog(sigtapPage - 1)}
            >
              Anterior
            </button>
            <span className="form-hint" style={{ alignSelf: 'center' }}>
              Página {sigtapPage} de {sigtapTotalPages}
            </span>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              disabled={sigtapLoading || sigtapPage >= sigtapTotalPages}
              onClick={() => void loadSigtapCatalog(sigtapPage + 1)}
            >
              Próxima
            </button>
          </div>
        ) : null}
      </div>
    );
  }

  if (tab === 'demonstrativos') {
    return (
      <>
        <div className="grid-2">
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Importar CSV demonstrativo</div>
            <div className="card-panel-body">
              <form className="form-grid" onSubmit={async (e) => {
                e.preventDefault();
                try {
                  await api.importTissDemonstrativo(demoImport);
                  setDemonstrativos(await api.getTissDemonstrativos());
                  onMessage('', 'Demonstrativo importado.');
                } catch (err) {
                  onMessage(err instanceof Error ? err.message : 'Erro.', '');
                }
              }}>
                <div className="form-field">
                  <label>Operadora</label>
                  <select required value={demoImport.healthInsuranceId} onChange={(e) => setDemoImport({ ...demoImport, healthInsuranceId: e.target.value })}>
                    <option value="">Selecione</option>
                    {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
                  </select>
                </div>
                <div className="form-field full">
                  <label>CSV (guia;tuss;faturado;pago;glosa;motivo;codigo)</label>
                  <textarea rows={4} required value={demoImport.csvContent} onChange={(e) => setDemoImport({ ...demoImport, csvContent: e.target.value })} />
                </div>
                <button className="btn" type="submit">Importar</button>
              </form>
            </div>
          </div>
          <div className="card-panel appt-panel">
            <div className="card-panel-header">Buscar na operadora (mock/WS)</div>
            <div className="card-panel-body">
              <form className="form-grid" onSubmit={async (e) => {
                e.preventDefault();
                try {
                  await api.fetchOperatorDemonstrativo(fetchDemo);
                  setDemonstrativos(await api.getTissDemonstrativos());
                  onMessage('', 'Demonstrativo obtido e processado.');
                } catch (err) {
                  onMessage(err instanceof Error ? err.message : 'Erro.', '');
                }
              }}>
                <div className="form-field">
                  <label>Operadora</label>
                  <select required value={fetchDemo.healthInsuranceId} onChange={(e) => setFetchDemo({ ...fetchDemo, healthInsuranceId: e.target.value })}>
                    <option value="">Selecione</option>
                    {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
                  </select>
                </div>
                <button className="btn" type="submit">Buscar demonstrativo</button>
              </form>
            </div>
          </div>
        </div>
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Demonstrativos</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Nº</th><th>Operadora</th><th>Competência</th><th>Faturado</th><th>Pago</th><th>Glosa</th><th>Status</th><th>Ações</th></tr></thead>
              <tbody>
                {demonstrativos.map((d) => (
                  <tr key={d.id}>
                    <td>{d.demonstrativoNumber}</td>
                    <td>{d.healthInsuranceName}</td>
                    <td>{d.competence}</td>
                    <td>{money(d.totalBilled)}</td>
                    <td>{money(d.totalPaid)}</td>
                    <td>{money(d.totalGlosa)}</td>
                    <td>{demonstrativoStatusLabels[d.status]}</td>
                    <td>
                      {d.status === 1 && (
                        <button type="button" className="btn btn-sm" onClick={async () => {
                          await api.processTissDemonstrativo(d.id);
                          setDemonstrativos(await api.getTissDemonstrativos());
                          onMessage('', 'Demonstrativo processado — glosas e financeiro atualizados.');
                        }}>Processar</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </>
    );
  }

  function findOperatorProfile(integration: HealthInsuranceIntegrationDto) {
    const code = integration.operatorCode?.toUpperCase();
    if (code) return operatorProfiles.find((p) => p.operatorCode.toUpperCase() === code);
    const name = integration.name.toLowerCase();
    return operatorProfiles.find((p) =>
      p.names.some((n) => name.includes(n.toLowerCase()) || n.toLowerCase().includes(name)));
  }

  if (tab === 'integrations') {
    return (
      <>
        {operatorProfiles.length > 0 && (
          <div className="card" style={{ marginBottom: 16 }}>
            <h3 style={{ marginTop: 0, fontSize: '0.95rem' }}>Operadoras prioritárias (perfil TISS)</h3>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
              {operatorProfiles.map((p) => (
                <span key={p.operatorCode} className="badge" title={p.businessRules}>
                  {p.operatorCode} · {p.authorizationDeadlineDays}d
                  {p.requiresOnlineAuthorization ? ' · online' : ''}
                </span>
              ))}
            </div>
            <p className="form-hint" style={{ marginBottom: 0, marginTop: 8 }}>
              Bradesco, Amil, SulAmérica, Hapvida, Unimed e demais perfis com mock realista até configurar webservice real.
            </p>
          </div>
        )}
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Configuração por operadora</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Operadora</th><th>TISS</th><th>Webservice</th><th>Modo</th><th>Ações</th></tr></thead>
              <tbody>
                {integrations.map((i) => {
                  const profile = findOperatorProfile(i);
                  return (
                  <tr key={i.id}>
                    <td>
                      <strong>{i.name}</strong>
                      {profile && (
                        <span className="badge" style={{ marginLeft: 8 }} title={profile.businessRules}>
                          {profile.operatorCode}
                        </span>
                      )}
                    </td>
                    <td>{i.tissVersion ?? '—'}</td>
                    <td className="mono">{i.webServiceUrl ?? '—'}</td>
                    <td>{i.useMockIntegration ? 'Mock' : 'HTTP'}</td>
                    <td>
                      <button type="button" className="btn btn-secondary btn-sm" onClick={() => {
                        setEditingIntegration(i);
                        setIntegrationForm({
                          tissVersion: i.tissVersion, operatorCode: i.operatorCode, portalUrl: i.portalUrl,
                          webServiceUrl: i.webServiceUrl, integrationUser: i.integrationUser,
                          useMockIntegration: i.useMockIntegration,
                        });
                      }}>Configurar</button>
                    </td>
                  </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Log de transações operadora</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Data</th><th>Operadora</th><th>Tipo</th><th>Status</th><th>Ref.</th><th>ms</th></tr></thead>
              <tbody>
                {logs.map((l) => (
                  <tr key={l.id}>
                    <td>{formatBrDateTime(l.createdAt)}</td>
                    <td>{l.healthInsuranceName}</td>
                    <td>{operatorTransactionTypeLabels[l.transactionType]}</td>
                    <td>{l.status === 1 ? 'OK' : 'Erro'}</td>
                    <td>{l.referenceId ?? '—'}</td>
                    <td>{l.durationMs ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
        <Modal open={!!editingIntegration} title={`Integração — ${editingIntegration?.name}`} onClose={() => setEditingIntegration(null)}>
          <form className="form-grid" onSubmit={async (e) => {
            e.preventDefault();
            if (!editingIntegration) return;
            await api.updateInsuranceIntegration(editingIntegration.id, integrationForm);
            setIntegrations(await api.getInsuranceIntegrations());
            setEditingIntegration(null);
            onMessage('', 'Integração atualizada.');
          }}>
            <div className="form-field"><label>Versão TISS</label><input value={integrationForm.tissVersion ?? ''} onChange={(e) => setIntegrationForm({ ...integrationForm, tissVersion: e.target.value })} /></div>
            <div className="form-field"><label>URL Webservice</label><input value={integrationForm.webServiceUrl ?? ''} onChange={(e) => setIntegrationForm({ ...integrationForm, webServiceUrl: e.target.value })} placeholder="https://api.operadora.com.br/tiss" /></div>
            <div className="form-field"><label>Usuário integração</label><input value={integrationForm.integrationUser ?? ''} onChange={(e) => setIntegrationForm({ ...integrationForm, integrationUser: e.target.value })} /></div>
            <div className="form-field"><label><input type="checkbox" checked={integrationForm.useMockIntegration} onChange={(e) => setIntegrationForm({ ...integrationForm, useMockIntegration: e.target.checked })} /> Usar mock (desmarque para HTTP real)</label></div>
            <div className="form-actions"><button className="btn" type="submit">Salvar</button></div>
          </form>
        </Modal>
      </>
    );
  }

  if (tab === 'annexes') {
    return (
      <>
        <FilterBar actions={
          <button className="btn" type="button" onClick={() => setShowAnnexModal(true)}>+ Anexo TISS</button>
        }>
          <div className="filter-field w-2xl">
            <label>Guia</label>
            <select value={annexForm.tissGuideId} onChange={async (e) => {
              const id = e.target.value;
              setAnnexForm({ ...annexForm, tissGuideId: id });
              if (id) setAnnexes(await api.getTissGuideAnnexes(id));
            }}>
              <option value="">Selecione uma guia</option>
              {guides.map((g) => <option key={g.id} value={g.id}>{g.guideNumber} — {g.patientName}</option>)}
            </select>
          </div>
        </FilterBar>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Anexos (quimio, radio, OPME)</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Tipo</th><th>CID-10</th><th>Indicação</th><th>Itens OPME</th></tr></thead>
              <tbody>
                {annexes.map((a) => (
                  <tr key={a.id}>
                    <td>{tissAnnexTypeLabels[a.annexType]}</td>
                    <td>{a.cid10Code ?? '—'}</td>
                    <td>{a.clinicalIndication ?? '—'}</td>
                    <td>{a.opmeItems.length}</td>
                  </tr>
                ))}
                {annexes.length === 0 && (
                  <tr><td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhum anexo</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
        <Modal open={showAnnexModal} title="Novo anexo TISS" onClose={() => setShowAnnexModal(false)}>
          <form className="form-grid" onSubmit={async (e: FormEvent) => {
            e.preventDefault();
            await api.createTissGuideAnnex(annexForm);
            setShowAnnexModal(false);
            if (annexForm.tissGuideId) setAnnexes(await api.getTissGuideAnnexes(annexForm.tissGuideId));
            onMessage('', 'Anexo registrado.');
          }}>
            <div className="form-field">
              <label>Guia</label>
              <select required value={annexForm.tissGuideId} onChange={(e) => setAnnexForm({ ...annexForm, tissGuideId: e.target.value })}>
                <option value="">Selecione</option>
                {guides.map((g) => <option key={g.id} value={g.id}>{g.guideNumber}</option>)}
              </select>
            </div>
            <div className="form-field">
              <label>Tipo anexo</label>
              <select value={annexForm.annexType} onChange={(e) => setAnnexForm({ ...annexForm, annexType: Number(e.target.value) })}>
                {Object.entries(tissAnnexTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div className="form-field"><label>CID-10</label><input value={annexForm.cid10Code ?? ''} onChange={(e) => setAnnexForm({ ...annexForm, cid10Code: e.target.value })} /></div>
            <div className="form-field"><label>Indicação clínica</label><input value={annexForm.clinicalIndication ?? ''} onChange={(e) => setAnnexForm({ ...annexForm, clinicalIndication: e.target.value })} /></div>
            <div className="form-actions"><button className="btn" type="submit">Salvar anexo</button></div>
          </form>
        </Modal>
      </>
    );
  }

  return null;
}
