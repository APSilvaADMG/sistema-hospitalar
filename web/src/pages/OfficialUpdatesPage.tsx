import { useCallback, useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
  api,
  type IntegrationLogDto,
  type OfficialSourceStatusDto,
  type OfficialUpdatesDashboardDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { configTabs } from '../navigation/moduleSections';
import { resolvePageTitle } from '../navigation/sectionBreadcrumb';

const statusLabels: Record<string, string> = {
  NeverChecked: 'Nunca verificado',
  UpToDate: 'Atualizado',
  UpdateAvailable: 'Atualização disponível',
  ManualDownloadRequired: 'Download manual necessário',
  CheckFailed: 'Falha na verificação',
  Importing: 'Importando…',
};

const logStatusLabels: Record<string, string> = {
  Info: 'Info',
  Success: 'Sucesso',
  Warning: 'Atenção',
  Failed: 'Falha',
};

function formatDateTime(value?: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleString('pt-BR');
}

export function OfficialUpdatesPage() {
  const { pathname } = useLocation();
  const { hasPermission } = useAuth();
  const canManage = hasPermission('integrations.manage', 'users.manage', 'billing.write');

  const [dashboard, setDashboard] = useState<OfficialUpdatesDashboardDto | null>(null);
  const [logs, setLogs] = useState<IntegrationLogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const load = useCallback(async () => {
    setError('');
    try {
      const [dash, logList] = await Promise.all([
        api.getOfficialUpdatesDashboard(),
        api.getOfficialUpdateLogs(50),
      ]);
      setDashboard(dash);
      setLogs(logList);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar atualizações oficiais');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleCheckAll() {
    setBusy('all');
    setMessage('');
    setError('');
    try {
      const dash = await api.checkAllOfficialUpdates();
      setDashboard(dash);
      const logList = await api.getOfficialUpdateLogs(50);
      setLogs(logList);
      setMessage('Verificação concluída em todas as fontes.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao verificar atualizações');
    } finally {
      setBusy(null);
    }
  }

  async function handleUpdate(source: OfficialSourceStatusDto) {
    setBusy(source.sourceType);
    setMessage('');
    setError('');
    try {
      const result = await api.updateOfficialSource(source.sourceType);
      await load();
      setMessage(result.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : `Erro ao atualizar ${source.displayName}`);
    } finally {
      setBusy(null);
    }
  }

  if (!canManage) {
    return (
      <>
        <PageHeader
          eyebrow="Configurações · Integrações"
          title="Atualizações Oficiais"
          subtitle="Central de atualizações governamentais — ANS, TUSS, TISS, SIGTAP e tabelas SUS."
        />
        <ModuleNav basePath="/configuracoes" tabs={configTabs} />
        <div className="card" style={{ marginTop: 16, padding: 24 }}>
          Acesso restrito. Solicite a permissão de integrações ou perfil administrador.
        </div>
      </>
    );
  }

  const sources = dashboard?.sources ?? [];
  const updatesAvailable = sources.filter((s) => s.status === 'UpdateAvailable').length;
  const manualRequired = sources.filter((s) => s.status === 'ManualDownloadRequired').length;

  return (
    <>
      <PageHeader
        eyebrow="Configurações · Integrações"
        title={resolvePageTitle(pathname) || 'Atualizações Oficiais'}
        subtitle="Verificação periódica de catálogos ANS, layouts TISS, SIGTAP e tabelas SUS — com histórico de importação."
      />

      <ModuleNav basePath="/configuracoes" tabs={configTabs} />

      {error && <div className="alert alert-error" style={{ marginTop: 12 }}>{error}</div>}
      {message && <div className="alert alert-info" style={{ marginTop: 12 }}>{message}</div>}

      <div className="kpi-grid" style={{ marginTop: 16, marginBottom: 20 }}>
        <KpiCard
          label="Última verificação"
          value={formatDateTime(dashboard?.lastCheckAt)}
          variant="primary"
        />
        <KpiCard label="Fontes monitoradas" value={sources.length} />
        <KpiCard label="Atualizações disponíveis" value={updatesAvailable} variant="warning" />
        <KpiCard label="Download manual" value={manualRequired} />
      </div>

      <div style={{ display: 'flex', gap: 12, marginBottom: 20, flexWrap: 'wrap' }}>
        <button
          type="button"
          className="btn btn-primary"
          disabled={loading || busy !== null}
          onClick={handleCheckAll}
        >
          {busy === 'all' ? 'Verificando…' : 'Atualizar Agora (verificar todas)'}
        </button>
      </div>

      {loading ? (
        <div className="card" style={{ padding: 24 }}>Carregando…</div>
      ) : (
        <div className="kpi-grid" style={{ marginBottom: 24 }}>
          {sources.map((source) => (
            <SourceCard
              key={source.sourceType}
              source={source}
              busy={busy === source.sourceType}
              onUpdate={() => handleUpdate(source)}
            />
          ))}
        </div>
      )}

      <div className="card-panel">
        <div className="card-panel-header">Log de integração</div>
        <div className="table-responsive">
          <table className="data-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Fonte</th>
                <th>Ação</th>
                <th>Status</th>
                <th>Mensagem</th>
                <th>Origem</th>
              </tr>
            </thead>
            <tbody>
              {logs.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', color: '#666' }}>
                    Nenhum registro ainda.
                  </td>
                </tr>
              )}
              {logs.map((log) => (
                <tr key={log.id}>
                  <td>{formatDateTime(log.createdAt)}</td>
                  <td>{log.sourceType}</td>
                  <td>{log.action}</td>
                  <td>{logStatusLabels[log.status] ?? log.status}</td>
                  <td>{log.message}</td>
                  <td>{log.triggeredBy ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

function SourceCard({
  source,
  busy,
  onUpdate,
}: {
  source: OfficialSourceStatusDto;
  busy: boolean;
  onUpdate: () => void;
}) {
  const statusLabel = statusLabels[source.status] ?? source.status;
  const hasUpdate = source.status === 'UpdateAvailable' || source.availableVersion;

  return (
    <div className="card" style={{ padding: 16, minHeight: 220 }}>
      <div style={{ fontWeight: 600, fontSize: 16, marginBottom: 8 }}>{source.displayName}</div>
      <div style={{ fontSize: 13, color: '#555', marginBottom: 4 }}>
        Versão instalada: <strong>{source.currentVersion}</strong>
      </div>
      {hasUpdate && source.availableVersion && (
        <div style={{ fontSize: 13, color: '#c62828', marginBottom: 4 }}>
          Nova versão: <strong>{source.availableVersion}</strong>
        </div>
      )}
      <div style={{ fontSize: 13, marginBottom: 4 }}>
        Status: <span className="badge">{statusLabel}</span>
      </div>
      {source.installedRecordCount != null && (
        <div style={{ fontSize: 12, color: '#666' }}>
          Registros: {source.installedRecordCount.toLocaleString('pt-BR')}
        </div>
      )}
      <div style={{ fontSize: 12, color: '#666', marginTop: 4 }}>
        Verificado: {formatDateTime(source.lastCheckedAt)}
      </div>
      {source.notes && (
        <div style={{ fontSize: 12, color: '#666', marginTop: 8 }}>{source.notes}</div>
      )}
      {source.sourceUrl && (
        <div style={{ marginTop: 8 }}>
          <a href={source.sourceUrl} target="_blank" rel="noreferrer" className="btn btn-link btn-sm">
            Portal oficial
          </a>
        </div>
      )}
      <div style={{ marginTop: 12 }}>
        <button
          type="button"
          className="btn btn-default btn-sm"
          disabled={busy}
          title={
            source.canAutoImport
              ? 'Executar importação automática'
              : 'Tentar atualização (pode exigir download manual no portal oficial)'
          }
          onClick={onUpdate}
        >
          {busy ? 'Processando…' : 'Atualizar'}
        </button>
      </div>
    </div>
  );
}
