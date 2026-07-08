import { useCallback, useEffect, useState } from 'react';
import {
  api,
  type MimicEtlStatusDto,
  type MimicResearchStatusDto,
  type MimicVitalSignDto,
} from '../api/client';
import { PageHeader } from '../components/PageHeader';

const DOC_PATH = 'docs/mimic-iii-integration.md';
const IS_DEV = import.meta.env.DEV;

function formatDate(value: string | null | undefined) {
  if (!value) return '—';
  return new Date(value).toLocaleString('pt-BR');
}

export function MimicResearchPage() {
  const [status, setStatus] = useState<MimicResearchStatusDto | null>(null);
  const [etlStatus, setEtlStatus] = useState<MimicEtlStatusDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [importMessage, setImportMessage] = useState<string | null>(null);
  const [importing, setImporting] = useState(false);
  const [subjectId, setSubjectId] = useState('3');
  const [vitals, setVitals] = useState<MimicVitalSignDto[]>([]);
  const [vitalsLoading, setVitalsLoading] = useState(false);
  const [vitalsError, setVitalsError] = useState<string | null>(null);

  const refreshStatus = useCallback(async () => {
    try {
      const data = await api.getMimicResearchStatus();
      setStatus(data);
      setEtlStatus(data.etl);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Falha ao carregar status');
    }
  }, []);

  const refreshEtl = useCallback(async () => {
    try {
      const data = await api.getMimicEtlStatus();
      setEtlStatus(data);
    } catch {
      // ETL endpoint may be disabled; status.etl is fallback
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        await refreshStatus();
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [refreshStatus]);

  useEffect(() => {
    if (!etlStatus?.importInProgress) return undefined;
    const timer = window.setInterval(() => {
      void refreshEtl();
    }, 3000);
    return () => window.clearInterval(timer);
  }, [etlStatus?.importInProgress, refreshEtl]);

  const handleTriggerImport = async () => {
    setImporting(true);
    setImportMessage(null);
    try {
      const result = await api.triggerMimicSubsetImport();
      setImportMessage(result.message);
      await refreshStatus();
      await refreshEtl();
    } catch (e) {
      setImportMessage(e instanceof Error ? e.message : 'Falha ao disparar importação');
    } finally {
      setImporting(false);
    }
  };

  const handleQueryVitals = async () => {
    const id = parseInt(subjectId, 10);
    if (!Number.isFinite(id) || id <= 0) {
      setVitalsError('Informe um SUBJECT_ID MIMIC válido (inteiro positivo).');
      return;
    }
    setVitalsLoading(true);
    setVitalsError(null);
    try {
      const result = await api.getMimicVitals(id, 25);
      setVitals(result.records);
      if (result.count === 0) {
        setVitalsError('Nenhum registro em mimic_staging.vital_sign_snapshot para este subject.');
      }
    } catch (e) {
      setVitals([]);
      setVitalsError(e instanceof Error ? e.message : 'Falha ao consultar sinais vitais');
    } finally {
      setVitalsLoading(false);
    }
  };

  return (
    <>
      <PageHeader
        eyebrow="Pesquisa clínica"
        title="MIMIC-III (sandbox)"
        subtitle="Ambiente isolado para dados desidentificados de pesquisa — nunca misturar com pacientes reais."
      />

      <div className="card" style={{ marginBottom: '1rem', borderLeft: '4px solid #c62828' }}>
        <strong>Aviso legal</strong>
        <p style={{ margin: '0.5rem 0 0' }}>
          MIMIC-III é um dataset público credenciado (PhysioNet) de um hospital dos EUA. Não representa
          pacientes deste sistema, não substitui PHI real e exige CITI + DUA. Proibido reidentificação e
          uso em produção sem isolamento.
        </p>
      </div>

      {loading && <p className="muted">Carregando status…</p>}
      {error && <p className="error-text">{error}</p>}

      {status && (
        <>
          <div className="kpi-grid" style={{ marginBottom: '1.5rem' }}>
            <div className="card kpi-card">
              <span className="kpi-label">Módulo habilitado</span>
              <span className="kpi-value">{status.enabled ? 'Sim' : 'Não'}</span>
            </div>
            <div className="card kpi-card">
              <span className="kpi-label">Banco sandbox</span>
              <span className="kpi-value">{status.databaseConfigured ? 'Configurado' : 'Não configurado'}</span>
            </div>
            <div className="card kpi-card">
              <span className="kpi-label">Conexão</span>
              <span className="kpi-value">{status.databaseReachable ? 'OK' : 'Indisponível'}</span>
            </div>
            {etlStatus && (
              <div className="card kpi-card">
                <span className="kpi-label">Snapshots ETL</span>
                <span className="kpi-value">{etlStatus.snapshotRows.toLocaleString('pt-BR')}</span>
              </div>
            )}
          </div>

          <div className="card" style={{ marginBottom: '1rem' }}>
            <h3>ETL — sinais vitais (CHARTEVENTS)</h3>
            <p className="muted">
              Dados em <code>mimic_staging.vital_sign_snapshot</code> no banco <code>mimic_iii</code>.
              Não grava em <code>sistema_hospitalar</code> nem em <code>VitalSignRecord</code> de produção.
            </p>

            {etlStatus ? (
              <ul style={{ margin: '0.5rem 0 1rem', paddingLeft: '1.25rem' }}>
                <li>Schema staging: {etlStatus.stagingSchemaReady ? 'pronto' : 'não aplicado'}</li>
                <li>Linhas raw filtradas: {etlStatus.rawVitalRows.toLocaleString('pt-BR')}</li>
                <li>Última execução: {etlStatus.lastRunStatus ?? '—'} (id {etlStatus.lastRunId ?? '—'})</li>
                <li>Início: {formatDate(etlStatus.lastRunStartedAt)}</li>
                <li>Fim: {formatDate(etlStatus.lastRunCompletedAt)}</li>
                {etlStatus.importInProgress && (
                  <li>
                    <strong>Em andamento</strong> — fase {etlStatus.currentPhase ?? '…'},{' '}
                    {etlStatus.currentRowsProcessed?.toLocaleString('pt-BR') ?? 0} linhas
                  </li>
                )}
                {etlStatus.lastRunError && (
                  <li className="error-text">Erro: {etlStatus.lastRunError}</li>
                )}
              </ul>
            ) : (
              <p className="muted">ETL ainda não consultado ou banco indisponível.</p>
            )}

            {IS_DEV && status.enabled && (
              <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'center' }}>
                <button
                  type="button"
                  className="btn btn-primary"
                  disabled={importing || etlStatus?.importInProgress}
                  onClick={() => void handleTriggerImport()}
                >
                  {importing || etlStatus?.importInProgress ? 'Importando…' : 'Disparar import subset (dev)'}
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => void refreshEtl()}>
                  Atualizar status ETL
                </button>
              </div>
            )}
            {importMessage && <p className="muted" style={{ marginTop: '0.75rem' }}>{importMessage}</p>}
            {!IS_DEV && (
              <p className="muted" style={{ marginTop: '0.75rem' }}>
                Import via UI disponível apenas em build de desenvolvimento. Use o script PowerShell em produção de dados.
              </p>
            )}
          </div>

          <div className="card" style={{ marginBottom: '1rem' }}>
            <h3>Consultar sinais vitais (API)</h3>
            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', alignItems: 'center', marginBottom: '0.75rem' }}>
              <label>
                SUBJECT_ID{' '}
                <input
                  type="number"
                  min={1}
                  value={subjectId}
                  onChange={(e) => setSubjectId(e.target.value)}
                  style={{ width: '6rem', marginLeft: '0.25rem' }}
                />
              </label>
              <button
                type="button"
                className="btn btn-secondary"
                disabled={vitalsLoading || !status.enabled}
                onClick={() => void handleQueryVitals()}
              >
                {vitalsLoading ? 'Consultando…' : 'Buscar amostra'}
              </button>
            </div>
            {vitalsError && <p className="error-text">{vitalsError}</p>}
            {vitals.length > 0 && (
              <div style={{ overflowX: 'auto' }}>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Data/hora</th>
                      <th>ICU stay</th>
                      <th>FC</th>
                      <th>PAS</th>
                      <th>PAD</th>
                      <th>SpO2</th>
                      <th>FR</th>
                      <th>Temp °C</th>
                    </tr>
                  </thead>
                  <tbody>
                    {vitals.map((v) => (
                      <tr key={v.id}>
                        <td>{formatDate(v.recordedAt)}</td>
                        <td>{v.icuStayId ?? '—'}</td>
                        <td>{v.heartRate ?? '—'}</td>
                        <td>{v.systolicBp ?? '—'}</td>
                        <td>{v.diastolicBp ?? '—'}</td>
                        <td>{v.spO2 ?? '—'}</td>
                        <td>{v.respiratoryRate ?? '—'}</td>
                        <td>{v.temperatureC ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card" style={{ marginBottom: '1rem' }}>
            <h3>Pré-requisitos PhysioNet</h3>
            <ol>
              <li>Conta em physionet.org</li>
              <li>Treinamento CITI — Data or Specimens Only Research</li>
              <li>Assinatura do Data Use Agreement (DUA) 1.5.0</li>
              <li>Download manual v1.4 (não incluído no repositório)</li>
            </ol>
            <p>
              <a href="https://physionet.org/content/mimiciii/1.4/" target="_blank" rel="noreferrer">
                MIMIC-III v1.4 no PhysioNet
              </a>
            </p>
          </div>

          <div className="card" style={{ marginBottom: '1rem' }}>
            <h3>Configuração local</h3>
            <p>Documentação: <code>{DOC_PATH}</code></p>
            <p>Validação + ETL PowerShell:</p>
            <pre className="code-block" style={{ overflow: 'auto' }}>
{`powershell -ExecutionPolicy Bypass -File scripts/mimic/import-mimic-subset.ps1 \\
  -MimicCsvPath "CAMINHO_DO_SEU_DOWNLOAD" -SubsetOnly -RunEtl -MaxSubjects 50`}
            </pre>
            <p>
              Em desenvolvimento, defina <code>MimicResearch:Enabled</code>,{' '}
              <code>ConnectionString</code> (banco <strong>mimic_iii</strong>) e{' '}
              <code>CsvPath</code> para import via API.
            </p>
            <p className="muted">{status.displayLabel}</p>
            <p className="muted">{status.warning}</p>
          </div>

          <div className="card">
            <h3>Consultas SQL de exemplo (schema nativo MIMIC)</h3>
            <p className="muted">Somente referência — executar no banco sandbox, não na API de produção.</p>
            {status.sampleQueries.map((q) => (
              <details key={q.id} style={{ marginBottom: '0.75rem' }}>
                <summary>
                  <strong>{q.title}</strong> — {q.description}
                </summary>
                <pre className="code-block" style={{ overflow: 'auto', marginTop: '0.5rem' }}>{q.sql}</pre>
              </details>
            ))}
          </div>
        </>
      )}
    </>
  );
}
