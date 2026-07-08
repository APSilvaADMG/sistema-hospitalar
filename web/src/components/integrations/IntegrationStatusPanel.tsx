import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  type IntegrationReadinessDto,
  type IntegrationTestResultDto,
} from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';

function modeClass(demoMode: boolean) {
  return demoMode ? 'warn' : 'ok';
}

function ConfigVarList({ vars }: { vars: IntegrationReadinessDto['whatsApp']['configVars'] }) {
  if (!vars?.length) return null;
  return (
    <table className="data-table integration-config-table" style={{ marginTop: 12 }}>
      <thead>
        <tr>
          <th>Variável / caminho</th>
          <th>Descrição</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {vars.map((v) => (
          <tr key={v.envKey}>
            <td>
              <code>{v.envKey}</code>
              <div className="form-hint mono">{v.appsettingsPath}</div>
            </td>
            <td>{v.description}</td>
            <td>
              <span className={`connect-status-pill${v.isConfigured ? ' ok' : v.requiredForProduction ? ' warn' : ' neutral'}`}>
                {v.isConfigured ? 'Configurado' : v.requiredForProduction ? 'Pendente (produção)' : 'Opcional'}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export function IntegrationStatusPanel() {
  const [data, setData] = useState<IntegrationReadinessDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [testFeedback, setTestFeedback] = useState<IntegrationTestResultDto | null>(null);
  const [testing, setTesting] = useState<string | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError('');
    api
      .getIntegrationReadiness()
      .then(setData)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar status'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function runTest(kind: 'whatsapp' | 'pix' | 'tiss', operatorId?: string) {
    setTesting(operatorId ? `tiss-${operatorId}` : kind);
    setTestFeedback(null);
    try {
      const result =
        kind === 'whatsapp'
          ? await api.testWhatsAppIntegration()
          : kind === 'pix'
            ? await api.testPixIntegration()
            : await api.testTissIntegration(operatorId);
      setTestFeedback(result);
      load();
    } catch (err) {
      setTestFeedback({
        integration: kind,
        success: false,
        message: err instanceof Error ? err.message : 'Falha no teste',
        details: [],
        testedAt: new Date().toISOString(),
      });
    } finally {
      setTesting(null);
    }
  }

  if (loading && !data) {
    return <div className="card">Carregando status das integrações…</div>;
  }

  if (error && !data) {
    return (
      <div className="alert alert-error">
        {error}
        <button type="button" className="btn btn-secondary" style={{ marginLeft: 12 }} onClick={load}>
          Tentar novamente
        </button>
      </div>
    );
  }

  if (!data) return null;

  const { whatsApp, pix, tiss } = data;

  return (
    <div className="integration-readiness">
      <p className="form-hint" style={{ marginTop: 0 }}>
        Configure em <code>appsettings.json</code>, variáveis de ambiente no <code>docker-compose.yml</code> ou secrets do servidor.
        <strong> Modo demonstração</strong> não exige chaves reais — use só para demo local.
      </p>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">Checklist — ambiente de homologação / produção</div>
        <div className="card-panel-body">
          <ol className="form-hint" style={{ margin: 0, paddingLeft: 18, lineHeight: 1.6 }}>
            <li>
              Desligar dados fictícios: <code>Hospital__EnableDemoSeeds=false</code>
              {' '}(já padrão em <code>appsettings.Production.json</code> e ConfigMap k8s).
            </li>
            <li>
              WhatsApp: <code>Connect__WhatsApp__UseMockProvider=false</code> + token Meta, Phone Number ID,
              verify token e URL pública do webhook <code>/api/whatsapp/webhook</code>.
            </li>
            <li>
              PIX: <code>Connect__Collection__UseMockPixProvider=false</code> + chave PIX, beneficiário, cidade
              e secret do webhook PSP em <code>POST /api/pix/webhook</code>.
            </li>
            <li>
              TISS: por operadora em Faturamento TISS → Integrações — URL WS, credenciais e
              <code>UseMockIntegration=false</code>.
            </li>
            <li>
              Deploy: <code>.\scripts\deploy-k8s.ps1</code> + Secret a partir de{' '}
              <code>k8s/secret.example.yaml</code> (nunca commitar senhas reais).
            </li>
          </ol>
        </div>
      </div>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">WhatsApp Connect</div>
        <div className="card-panel-body">
          <div className="connect-status-grid">
            <span className={`connect-status-pill ${modeClass(whatsApp.demoMode)}`}>{whatsApp.modeLabel}</span>
            <span className={`connect-status-pill ${whatsApp.ready ? 'ok' : 'warn'}`}>
              {whatsApp.ready ? 'Pronto' : 'Configuração incompleta'}
            </span>
            <span className={`connect-status-pill ${whatsApp.liveMode ? 'ok' : 'neutral'}`}>
              {whatsApp.liveMode ? 'Produção' : `Provedor: ${whatsApp.providerName}`}
            </span>
          </div>
          <p className="form-hint">
            Webhook: <code>{whatsApp.webhookPath}</code>
            {whatsApp.publicWebhookUrl ? (
              <> · URL pública: <code>{whatsApp.publicWebhookUrl}</code></>
            ) : (
              <> · Defina <code>Connect__WhatsApp__PublicWebhookUrl</code> para produção</>
            )}
          </p>
          {whatsApp.issues.length > 0 && (
            <ul className="connect-health-issues form-hint">
              {whatsApp.issues.map((i) => (
                <li key={i}>{i}</li>
              ))}
            </ul>
          )}
          <ConfigVarList vars={whatsApp.configVars} />
          <div className="form-actions" style={{ marginTop: 12 }}>
            <button type="button" className="btn btn-secondary btn-sm" disabled={!!testing} onClick={() => void runTest('whatsapp')}>
              {testing === 'whatsapp' ? 'Testando…' : 'Testar conexão WhatsApp'}
            </button>
            <Link to="/connect/whatsapp" className="btn btn-secondary btn-sm">Abrir Connect</Link>
          </div>
        </div>
      </div>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">PIX — Cobrança automática</div>
        <div className="card-panel-body">
          <div className="connect-status-grid">
            <span className={`connect-status-pill ${modeClass(pix.demoMode)}`}>{pix.modeLabel}</span>
            <span className={`connect-status-pill ${pix.ready ? 'ok' : 'warn'}`}>
              {pix.ready ? 'Pronto' : 'Configuração incompleta'}
            </span>
            <span className="connect-status-pill neutral">
              Webhook: <code>POST {pix.webhookPath}</code>
            </span>
          </div>
          {pix.issues.length > 0 && (
            <ul className="connect-health-issues form-hint">
              {pix.issues.map((i) => (
                <li key={i}>{i}</li>
              ))}
            </ul>
          )}
          <ConfigVarList vars={pix.configVars} />
          <div className="form-actions" style={{ marginTop: 12 }}>
            <button type="button" className="btn btn-secondary btn-sm" disabled={!!testing} onClick={() => void runTest('pix')}>
              {testing === 'pix' ? 'Testando…' : 'Testar configuração PIX'}
            </button>
            <Link to="/financeiro" className="btn btn-secondary btn-sm">Financeiro</Link>
          </div>
        </div>
      </div>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">TISS — Faturamento convênio</div>
        <div className="card-panel-body">
          <div className="connect-status-grid">
            <span className={`connect-status-pill ${modeClass(tiss.demoOperators === tiss.totalOperators && tiss.totalOperators > 0)}`}>
              {tiss.modeLabel}
            </span>
            <span className={`connect-status-pill ${tiss.ready ? 'ok' : 'warn'}`}>
              {tiss.configuredOperators}/{tiss.totalOperators} operadoras configuradas
            </span>
            <span className="connect-status-pill neutral">
              Demonstração: {tiss.demoOperators} · Produção: {tiss.liveOperators}
            </span>
          </div>
          {tiss.issues.length > 0 && (
            <ul className="connect-health-issues form-hint">
              {tiss.issues.map((i) => (
                <li key={i}>{i}</li>
              ))}
            </ul>
          )}
          {tiss.operators.length > 0 && (
            <table className="data-table" style={{ marginTop: 12 }}>
              <thead>
                <tr>
                  <th>Operadora</th>
                  <th>Modo</th>
                  <th>Endpoint</th>
                  <th>Versão TISS</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {tiss.operators.map((op) => (
                  <tr key={op.id}>
                    <td>{op.name}</td>
                    <td>
                      <span className={`connect-status-pill ${op.demoMode ? 'warn' : 'ok'}`}>
                        {op.demoMode ? 'Modo demonstração' : 'Produção'}
                      </span>
                    </td>
                    <td>{op.webServiceConfigured ? 'Configurado' : 'Pendente'}</td>
                    <td>{op.tissVersion ?? '—'}</td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        disabled={!!testing}
                        onClick={() => void runTest('tiss', op.id)}
                      >
                        {testing === `tiss-${op.id}` ? '…' : 'Testar'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <ConfigVarList vars={tiss.configVars} />
          <div className="form-actions" style={{ marginTop: 12 }}>
            <button type="button" className="btn btn-secondary btn-sm" disabled={!!testing} onClick={() => void runTest('tiss')}>
              {testing === 'tiss' ? 'Testando…' : 'Testar TISS (visão geral)'}
            </button>
            <Link to="/faturamento-tiss" className="btn btn-secondary btn-sm">Faturamento TISS</Link>
            <Link to="/integracoes" className="btn btn-secondary btn-sm">HL7 / FHIR</Link>
          </div>
        </div>
      </div>

      {testFeedback && (
        <div className={`alert ${testFeedback.success ? 'alert-success' : 'alert-error'}`}>
          <strong>{testFeedback.integration}</strong>: {testFeedback.message}
          {testFeedback.details?.length > 0 && (
            <ul className="integration-test-details">
              {testFeedback.details.map((d) => (
                <li key={d}>{d}</li>
              ))}
            </ul>
          )}
          <div className="form-hint">Testado em {formatBrDateTime(testFeedback.testedAt)}</div>
        </div>
      )}

      <div className="form-actions">
        <button type="button" className="btn btn-secondary" onClick={load} disabled={loading}>
          Atualizar status
        </button>
      </div>
    </div>
  );
}
