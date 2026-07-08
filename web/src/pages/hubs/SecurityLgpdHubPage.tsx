import { type FormEvent, useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
  api,
  type AuditLogDto,
  type ComplianceDashboardDto,
  type ConsentTermDto,
  type DataSubjectRequestDto,
  type LoginAttemptDto,
  type PatientConsentDto,
  type PatientDto,
  type PrivacyIncidentDto,
  type UserSessionDto,
} from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { KpiCard } from '../../components/KpiCard';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { PatientConsentsPanel } from '../../components/PatientConsentsPanel';
import { ConsentDocumentModal } from '../../components/ConsentDocumentModal';
import { securityLgpdTabs } from '../../navigation/moduleSections';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useModuleSection } from '../../navigation/useModuleSection';
import { formatBrDateTime } from '../../utils/dateUtils';

export function SecurityLgpdHubPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/seguranca-lgpd');
  const active = section || '';

  const [dashboard, setDashboard] = useState<ComplianceDashboardDto | null>(null);
  const [auditLogs, setAuditLogs] = useState<AuditLogDto[]>([]);
  const [loginAttempts, setLoginAttempts] = useState<LoginAttemptDto[]>([]);
  const [sessions, setSessions] = useState<UserSessionDto[]>([]);
  const [consents, setConsents] = useState<PatientConsentDto[]>([]);
  const [terms, setTerms] = useState<ConsentTermDto[]>([]);
  const [subjectRequests, setSubjectRequests] = useState<DataSubjectRequestDto[]>([]);
  const [incidents, setIncidents] = useState<PrivacyIncidentDto[]>([]);
  const [mfaSetup, setMfaSetup] = useState<{ secret: string; qrCodeUri: string } | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [success, setSuccess] = useState('');
  const [incidentForm, setIncidentForm] = useState({
    title: '', incidentType: 'Acesso indevido', severity: 'Medium', description: '',
  });
  const [consentPatientId, setConsentPatientId] = useState('');
  const [consentPatients, setConsentPatients] = useState<PatientDto[]>([]);
  const [viewConsentId, setViewConsentId] = useState<string | null>(null);

  const canAudit = hasPermission('audit.read');
  const canSecurity = hasPermission('security.manage');
  const canLgpd = hasPermission('lgpd.manage', 'lgpd.consent.manage', 'lgpd.subject_requests', 'incidents.manage');

  function load() {
    if (canSecurity || canLgpd) {
      api.getComplianceDashboard().then(setDashboard).catch(console.error);
    }
    if (canAudit) {
      api.getAuditLogs(100).then(setAuditLogs).catch(console.error);
    }
    if (canSecurity) {
      api.getLoginAttempts(100).then(setLoginAttempts).catch(console.error);
      api.getUserSessions(true).then(setSessions).catch(console.error);
    }
    if (hasPermission('lgpd.consent.manage')) {
      api.getConsentTerms().then(setTerms).catch(console.error);
      api.getPatientConsents().then(setConsents).catch(console.error);
      api.getPatients('', 1).then((r) => setConsentPatients(Array.isArray(r.items) ? r.items : [])).catch(console.error);
    }
    if (hasPermission('lgpd.subject_requests')) {
      api.getDataSubjectRequests().then(setSubjectRequests).catch(console.error);
    }
    if (hasPermission('incidents.manage', 'audit.read', 'security.manage', 'lgpd.manage')) {
      api.getPrivacyIncidents().then(setIncidents).catch(console.error);
    }
  }

  useEffect(() => { load(); }, [canAudit, canSecurity, canLgpd]);

  async function handleRevokeSession(id: string) {
    await api.revokeUserSession(id);
    setSuccess('Sessão revogada.');
    load();
  }

  async function handleMfaSetup() {
    const setup = await api.setupMfa();
    setMfaSetup({ secret: setup.manualEntryKey, qrCodeUri: setup.qrCodeUri });
  }

  async function handleMfaEnable(e: FormEvent) {
    e.preventDefault();
    await api.enableMfa(mfaCode);
    setSuccess('MFA ativado com sucesso.');
    setMfaSetup(null);
    setMfaCode('');
  }

  async function handleCreateIncident(e: FormEvent) {
    e.preventDefault();
    await api.createPrivacyIncident(incidentForm);
    setIncidentForm({ title: '', incidentType: 'Acesso indevido', severity: 'Medium', description: '' });
    setSuccess('Incidente registrado.');
    load();
  }

  async function handleExportRequest(id: string) {
    await api.exportDataSubjectRequest(id);
    setSuccess('Pacote LGPD exportado (JSON).');
  }

  if (!canAudit && !canSecurity && !canLgpd) {
    return <div className="card">Acesso restrito — módulo Segurança e LGPD.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Governança"
        title="Segurança e LGPD"
        subtitle={breadcrumb?.title ?? 'RBAC, auditoria imutável, consentimentos e direitos do titular.'}
      />

      <ModuleNav basePath="/seguranca-lgpd" tabs={securityLgpdTabs} contextId="securityLgpd" />

      {success && (
        <div className="alert alert-success" style={{ marginBottom: 16 }}>
          {success}
          <button type="button" className="btn-link" style={{ marginLeft: 12 }} onClick={() => setSuccess('')}>Fechar</button>
        </div>
      )}

      {active === '' && dashboard && (
        <div className="kpi-grid">
          <KpiCard label="Consentimentos ativos" value={dashboard.activeConsents} variant="success" />
          <KpiCard label="Consentimentos revogados" value={dashboard.revokedConsents} />
          <KpiCard label="Solicitações titular" value={dashboard.openSubjectRequests} variant="warning" />
          <KpiCard label="Incidentes abertos" value={dashboard.openPrivacyIncidents} variant="danger" />
          <KpiCard label="Logins falhos (24h)" value={dashboard.failedLogins24h} variant="warning" />
          <KpiCard label="Sessões ativas" value={dashboard.activeSessions} variant="info" />
          <KpiCard label="Usuários com MFA" value={dashboard.usersWithMfa} variant="primary" />
        </div>
      )}

      {active === 'auditoria' && canAudit && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Auditoria imutável — {auditLogs.length}</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Data</th><th>Usuário</th><th>Categoria</th><th>Ação</th><th>Entidade</th><th>Sensível</th><th>IP</th></tr>
              </thead>
              <tbody>
                {auditLogs.map((l) => (
                  <tr key={l.id}>
                    <td>{formatBrDateTime(l.createdAt)}</td>
                    <td>{l.userEmail}</td>
                    <td>{l.actionCategory ?? '—'}</td>
                    <td>{l.action}</td>
                    <td>{l.entityType}</td>
                    <td>{l.isSensitive ? 'Sim' : 'Não'}</td>
                    <td>{l.ipAddress ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'logins' && canSecurity && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Tentativas de login — {loginAttempts.length}</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Data</th><th>E-mail</th><th>Resultado</th><th>Motivo</th><th>IP</th></tr>
              </thead>
              <tbody>
                {loginAttempts.map((l) => (
                  <tr key={l.id}>
                    <td>{formatBrDateTime(l.createdAt)}</td>
                    <td>{l.email}</td>
                    <td>{l.success ? 'Sucesso' : 'Falha'}</td>
                    <td>{l.failureReason ?? '—'}</td>
                    <td>{l.ipAddress ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'sessoes' && canSecurity && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Sessões ativas — {sessions.length}</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Usuário</th><th>Início</th><th>Expira</th><th>IP</th><th>Ações</th></tr>
              </thead>
              <tbody>
                {sessions.map((s) => (
                  <tr key={s.id}>
                    <td>{s.userFullName}<br /><small>{s.userEmail}</small></td>
                    <td>{formatBrDateTime(s.createdAt)}</td>
                    <td>{formatBrDateTime(s.expiresAt)}</td>
                    <td>{s.ipAddress ?? '—'}</td>
                    <td>
                      {s.isActive && (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleRevokeSession(s.id)}>
                          Revogar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'consentimentos' && hasPermission('lgpd.consent.manage') && (
        <>
          <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
            <div className="card-panel-header">Coletar consentimento (recepção / titular)</div>
            <div className="card-panel-body">
              <p className="form-hint" style={{ marginTop: 0 }}>
                Fluxo obrigatório: leitura integral do termo → ciência → assinatura digital. Só então o consentimento é registrado.
              </p>
              <PatientConsentsPanel
                patientId={consentPatientId || undefined}
                patients={consentPatients}
                onPatientChange={setConsentPatientId}
                onSuccess={(msg) => { setSuccess(msg); load(); }}
                onError={() => setSuccess('')}
              />
            </div>
          </div>

          <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
            <div className="card-panel-header">Termos vigentes — {terms.filter((t) => t.isCurrent).length}</div>
            <div className="card-panel-body">
              {terms.filter((t) => t.isCurrent).map((t) => (
                <div key={t.id} className="card" style={{ marginBottom: 16, padding: 16 }}>
                  <strong>{t.title}</strong> — v{t.version}
                  <p style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{t.content}</p>
                  <small>Finalidades: {t.purposes.join(', ')}</small>
                </div>
              ))}
            </div>
          </div>

          <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
            <div className="card-panel-header">Registros — {consents.length}</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead>
                  <tr><th>Paciente</th><th>Termo</th><th>Assinante</th><th>Leitura</th><th>Ciência</th><th>Assinatura</th><th>Registrado</th><th>IP</th><th /></tr>
                </thead>
                <tbody>
                  {consents.map((c) => (
                    <tr key={c.id}>
                      <td>{c.patientName}</td>
                      <td>{c.termTitle} v{c.termVersion}</td>
                      <td>{c.signerName ?? '—'}</td>
                      <td>{c.readAt ? formatBrDateTime(c.readAt) : '—'}</td>
                      <td>{c.acknowledgedAt ? formatBrDateTime(c.acknowledgedAt) : '—'}</td>
                      <td>{c.hasSignature ? 'Sim' : 'Não'}</td>
                      <td>{formatBrDateTime(c.grantedAt)}</td>
                      <td>{c.ipAddress ?? '—'}</td>
                      <td>
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => setViewConsentId(c.id)}>
                          Ver documento
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}

      <ConsentDocumentModal
        consentId={viewConsentId}
        onClose={() => setViewConsentId(null)}
      />

      {active === 'titular' && hasPermission('lgpd.subject_requests') && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Solicitações do titular — {subjectRequests.length}</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr><th>Paciente</th><th>Tipo</th><th>Status</th><th>Solicitado</th><th>Responsável</th><th /></tr>
              </thead>
              <tbody>
                {subjectRequests.map((r) => (
                  <tr key={r.id}>
                    <td>{r.patientName}</td>
                    <td>{r.requestType}</td>
                    <td>{r.status}</td>
                    <td>{formatBrDateTime(r.requestedAt)}</td>
                    <td>{r.handledByName ?? '—'}</td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => { handleExportRequest(r.id).catch(console.error); }}
                      >
                        Exportar JSON
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'incidentes' && (
        <>
          {hasPermission('incidents.manage') ? (
            <form className="form-grid card" style={{ marginTop: 24 }} onSubmit={handleCreateIncident}>
              <div className="form-field"><label>Título</label><input required value={incidentForm.title} onChange={(e) => setIncidentForm({ ...incidentForm, title: e.target.value })} /></div>
              <div className="form-field"><label>Tipo</label><input required value={incidentForm.incidentType} onChange={(e) => setIncidentForm({ ...incidentForm, incidentType: e.target.value })} /></div>
              <div className="form-field"><label>Severidade</label>
                <select value={incidentForm.severity} onChange={(e) => setIncidentForm({ ...incidentForm, severity: e.target.value })}>
                  <option value="Low">Baixa</option><option value="Medium">Média</option><option value="High">Alta</option><option value="Critical">Crítica</option>
                </select>
              </div>
              <div className="form-field full"><label>Descrição</label><textarea required rows={3} value={incidentForm.description} onChange={(e) => setIncidentForm({ ...incidentForm, description: e.target.value })} /></div>
              <div className="form-actions">
                <button className="btn" type="submit">Registrar incidente</button>
              </div>
            </form>
          ) : (
            <div className="card" style={{ marginTop: 24 }}>
              <p className="form-hint" style={{ margin: 0 }}>
                Visualização de incidentes de privacidade (LGPD). Para registrar novos incidentes, é necessária a permissão de gestão.
              </p>
            </div>
          )}
          <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
            <div className="card-panel-header">Incidentes de privacidade — {incidents.length}</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead><tr><th>Título</th><th>Tipo</th><th>Severidade</th><th>Status</th><th>Detectado</th></tr></thead>
                <tbody>
                  {incidents.map((i) => (
                    <tr key={i.id}>
                      <td><strong>{i.title}</strong><br /><small>{i.description}</small></td>
                      <td>{i.incidentType}</td>
                      <td>{i.severity}</td>
                      <td>{i.status}</td>
                      <td>{formatBrDateTime(i.detectedAt)}</td>
                    </tr>
                  ))}
                  {incidents.length === 0 && (
                    <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum incidente registrado</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}

      {active === 'mfa' && (
        <div className="card" style={{ marginTop: 24, maxWidth: 520 }}>
          <h3>Autenticação multifator (MFA)</h3>
          <p>Configure o Google Authenticator ou app compatível com TOTP.</p>
          {!mfaSetup ? (
            <button type="button" className="btn" onClick={() => { handleMfaSetup().catch(console.error); }}>Gerar chave MFA</button>
          ) : (
            <form onSubmit={handleMfaEnable}>
              <p><strong>URI:</strong> <code style={{ wordBreak: 'break-all' }}>{mfaSetup.qrCodeUri}</code></p>
              <p><strong>Chave manual:</strong> <code>{mfaSetup.secret}</code></p>
              <div className="form-field">
                <label htmlFor="mfaCode">Código de verificação</label>
                <input id="mfaCode" value={mfaCode} onChange={(e) => setMfaCode(e.target.value)} required />
              </div>
              <button className="btn" type="submit">Ativar MFA</button>
            </form>
          )}
        </div>
      )}
    </>
  );
}
