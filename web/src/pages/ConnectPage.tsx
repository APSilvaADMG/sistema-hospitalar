import { useCallback, useEffect, useState } from 'react';
import {
  api,
  type ConnectConversationDetailDto,
  type ConnectConversationDto,
  type ConnectDashboardDto,
  type ConnectIntegrationStatusDto,
  type ConnectKnowledgeArticleDto,
  type ConnectSatisfactionStatsDto,
  type ConnectWaitlistDto,
  type ProfessionalDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { connectWhatsAppTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';
import { useLocation } from 'react-router-dom';
import { FeegowConnectInbox } from '../components/feegow/connect/FeegowConnectInbox';

export function ConnectPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section, goToSection } = useModuleSection('/connect/whatsapp');
  const activeSection = section || '';
  const [dashboard, setDashboard] = useState<ConnectDashboardDto | null>(null);
  const [integration, setIntegration] = useState<ConnectIntegrationStatusDto | null>(null);
  const [conversations, setConversations] = useState<ConnectConversationDto[]>([]);
  const [selected, setSelected] = useState<ConnectConversationDetailDto | null>(null);
  const [waitlist, setWaitlist] = useState<ConnectWaitlistDto[]>([]);
  const [knowledge, setKnowledge] = useState<ConnectKnowledgeArticleDto[]>([]);
  const [nps, setNps] = useState<ConnectSatisfactionStatsDto | null>(null);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [error, setError] = useState('');
  const [simPhone, setSimPhone] = useState('11999990001');
  const [simMessage, setSimMessage] = useState('oi');
  const [simReply, setSimReply] = useState('');
  const [simHistory, setSimHistory] = useState<{ from: 'user' | 'bot'; text: string }[]>([]);
  const [blockProfId, setBlockProfId] = useState('');
  const [blockDate, setBlockDate] = useState(new Date().toISOString().slice(0, 10));
  const [blockReason, setBlockReason] = useState('Indisponibilidade médica');

  const canAccess = hasPermission(
    'connect.read',
    'patients.read',
    'pep.read',
    'reports.read',
    'hospitalization.manage',
  );

  const load = useCallback(async () => {
    setError('');
    try {
      const [dash, convs, wait, know, sat, profs, status] = await Promise.all([
        api.getConnectDashboard(),
        api.getConnectConversations(),
        api.getConnectWaitlist(),
        api.getConnectKnowledge(),
        api.getConnectSatisfaction(),
        api.getProfessionals(),
        api.getConnectIntegrationStatus(),
      ]);
      setDashboard(dash);
      setIntegration(status);
      setConversations(convs);
      setWaitlist(wait);
      setKnowledge(know);
      setNps(sat);
      setProfessionals(profs);
      if (!blockProfId && profs[0]) setBlockProfId(profs[0].id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar WhatsApp Connect');
    }
  }, [blockProfId]);

  useEffect(() => {
    if (canAccess) void load();
  }, [canAccess, load]);

  async function openConversation(id: string) {
    try {
      const detail = await api.getConnectConversation(id);
      setSelected(detail);
      goToSection('conversas');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao abrir conversa');
    }
  }

  async function sendSimulation(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setSimHistory((h) => [...h, { from: 'user', text: simMessage }]);
    try {
      const res = await api.simulateConnectInbound({ phone: simPhone, message: simMessage });
      setSimReply(res.reply);
      setSimHistory((h) => [...h, { from: 'bot', text: res.reply }]);
      setSimMessage('');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na simulação');
    }
  }

  async function blockSchedule(e: React.FormEvent) {
    e.preventDefault();
    if (!blockProfId) return;
    try {
      const res = await api.blockConnectSchedule({
        professionalId: blockProfId,
        date: blockDate,
        reason: blockReason,
      });
      alert(`Agenda bloqueada: ${res.affectedAppointments} consulta(s), ${res.notificationsSent} notificação(ões).`);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao bloquear agenda');
    }
  }

  if (!canAccess) {
    return <div className="card">Acesso restrito à equipe de atendimento.</div>;
  }

  return (
    <div className="connect-page">
      <PageHeader
        eyebrow="Integrações"
        title={activeSection ? breadcrumb.title : 'WhatsApp Connect'}
        subtitle="Central de comunicação via WhatsApp — agendamentos, lembretes, cobrança, lista de espera, check-in e NPS."
      />

      <ModuleNav basePath="/connect/whatsapp" tabs={connectWhatsAppTabs} />

      {error && <div className="alert alert-error">{error}</div>}

      {!activeSection && dashboard && (
        <section className="connect-dashboard">
          <div className="kpi-grid">
            <KpiCard label="Conversas ativas hoje" value={dashboard.activeConversations} variant="primary" />
            <KpiCard label="Mensagens hoje" value={dashboard.messagesToday} variant="info" />
            <KpiCard label="Lembretes pendentes" value={dashboard.pendingReminders} variant="warning" />
            <KpiCard label="Contas em atraso" value={integration?.overdueAccounts ?? 0} variant="warning" />
            <KpiCard label="Lista de espera" value={dashboard.waitlistWaiting} variant="neutral" />
            <KpiCard label="Check-ins hoje" value={dashboard.checkInsToday} variant="success" />
            <KpiCard label="NPS médio (mês)" value={dashboard.averageNps.toFixed(1)} variant="primary" />
          </div>

          {integration && (
            <div className="card connect-integration-status">
              <h3>Integração WhatsApp</h3>
              <div className="connect-status-grid">
                <span className={`connect-status-pill${integration.useMockProvider ? ' warn' : ' ok'}`}>
                  {integration.useMockProvider ? 'Modo demonstração' : 'Produção'}
                </span>
                <span className={`connect-status-pill${integration.ready ? ' ok' : ' warn'}`}>
                  {integration.ready ? 'Pronto para operação' : 'Configuração incompleta'}
                </span>
                <span className={`connect-status-pill${integration.liveMode ? ' ok' : ' neutral'}`}>
                  {integration.liveMode ? 'Meta Cloud API ativa' : `Provedor: ${integration.providerName}`}
                </span>
                <span className={`connect-status-pill${integration.metaConfigured ? ' ok' : ' warn'}`}>
                  {integration.metaConfigured ? 'Credenciais Meta OK' : 'PhoneNumberId / AccessToken pendentes'}
                </span>
                <span className={`connect-status-pill${integration.verifyTokenConfigured ? ' ok' : ' warn'}`}>
                  {integration.verifyTokenConfigured ? 'Verify Token OK' : 'Verify Token pendente'}
                </span>
                <span className={`connect-status-pill${integration.webhookSecretConfigured ? ' ok' : ' neutral'}`}>
                  {integration.webhookSecretConfigured ? 'App Secret configurado' : 'App Secret ausente (obrigatório em Meta live)'}
                </span>
                <span className={`connect-status-pill${integration.collectionAgentEnabled ? ' ok' : ' neutral'}`}>
                  Agente de cobrança {integration.collectionAgentEnabled ? 'ativo' : 'desativado'}
                </span>
              </div>
              <p className="form-hint">
                Webhook: <code>GET/POST {integration.webhookPath}</code>
                {integration.publicWebhookUrl ? (
                  <> · URL pública: <code>{integration.publicWebhookUrl}</code></>
                ) : (
                  <> · Defina <code>Connect:WhatsApp:PublicWebhookUrl</code> (ex.: https://seu-dominio/api/whatsapp/webhook)</>
                )}
                {' '}· Lembretes de cobrança hoje: {integration.collectionRemindersSentToday}
                {' '}· Falhas hoje: {integration.failedMessagesToday}
              </p>
              {integration.healthIssues.length > 0 && (
                <ul className="connect-health-issues form-hint">
                  {integration.healthIssues.map((issue) => (
                    <li key={issue}>{issue}</li>
                  ))}
                </ul>
              )}
              <div className="connect-go-live-grid">
                <div className="connect-go-live-item">
                  <strong>Idioma templates</strong>
                  {integration.templateLanguageCode}
                </div>
                <div className="connect-go-live-item">
                  <strong>Confirmação</strong>
                  {integration.confirmationTemplateName}
                </div>
                <div className="connect-go-live-item">
                  <strong>Lembrete 24h/72h</strong>
                  {integration.reminderTemplateName}
                </div>
                <div className="connect-go-live-item">
                  <strong>Cobrança</strong>
                  {integration.billingTemplateName}
                </div>
              </div>
              {!integration.useMockProvider && integration.metaConfigured ? (
                <p className="form-hint" style={{ marginTop: 10 }}>
                  Produção ativa — mensagens proativas usam templates fora da janela de 24h. Opt-out LGPD: paciente envia SAIR.
                </p>
              ) : (
                <p className="form-hint" style={{ marginTop: 10 }}>
                  Para produção: defina <code>Connect__WhatsApp__UseMockProvider=false</code>, credenciais Meta, Verify Token, App Secret e URL HTTPS pública.
                </p>
              )}
            </div>
          )}

          <div className="card connect-roadmap">
            <h3>Próximos passos (prioridade)</h3>
            <ol className="connect-roadmap-list">
              <li>
                <strong>P1 — WhatsApp Business API (produção)</strong>
                <span>Definir <code>UseMockProvider=false</code>, credenciais Meta, HTTPS público e templates aprovados para lembretes fora da janela de 24h.</span>
              </li>
              <li>
                <strong>P1 — PIX automático</strong>
                <span>Cobrança dinâmica com copia e cola, webhook de confirmação e baixa automática na conta (implementado). Em produção: integrar PSP (Efi, Mercado Pago, etc.).</span>
              </li>
              <li>
                <strong>P2 — Templates e mídia</strong>
                <span>Botões interativos, listas, áudio/imagem e atualização de status de entrega/leitura (base do webhook pronta).</span>
              </li>
              <li>
                <strong>P2 — Handoff humano</strong>
                <span>Notificar recepção/financeiro quando paciente pede atendente ou envia comprovante.</span>
              </li>
              <li>
                <strong>P3 — TISS + cobrança unificada</strong>
                <span>Visão única de recebíveis do paciente e glosas de convênio; exportação XML TISS.</span>
              </li>
            </ol>
          </div>

          <div className="card connect-features">
            <h3>Fluxos disponíveis</h3>
            <ul className="connect-feature-list">
              <li>Agendamento inteligente via WhatsApp (menu + linguagem natural)</li>
              <li>Confirmação automática 72h e lembrete 24h antes</li>
              <li>Remarcação automática quando médico falta</li>
              <li>Lista de espera inteligente ao cancelar vaga</li>
              <li>Check-in digital 2h antes da consulta</li>
              <li>Pré-triagem automatizada (sintomas → ficha enfermagem)</li>
              <li>Pesquisa de satisfação pós-atendimento (NPS)</li>
              <li>Assistente FAQ (convênios, horários, jejum)</li>
              <li>Agente de cobrança — débitos, PIX automático com baixa instantânea e lembretes</li>
            </ul>
          </div>

          <form className="card form-grid" onSubmit={blockSchedule}>
            <h3 className="full">Bloquear agenda do médico (remarcação automática)</h3>
            <div className="form-field">
              <label>Médico</label>
              <select value={blockProfId} onChange={(e) => setBlockProfId(e.target.value)}>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Data</label>
              <input type="date" value={blockDate} onChange={(e) => setBlockDate(e.target.value)} />
            </div>
            <div className="form-field full">
              <label>Motivo</label>
              <input value={blockReason} onChange={(e) => setBlockReason(e.target.value)} />
            </div>
            <div className="form-field full">
              <button type="submit" className="btn btn-secondary">Notificar pacientes e liberar remarcação</button>
            </div>
          </form>
        </section>
      )}

      {activeSection === 'inbox' && (
        <FeegowConnectInbox initialFilter="human" />
      )}

      {activeSection === 'simulador' && (
        <section className="connect-simulator card">
          <h3>Simulador WhatsApp (desenvolvimento)</h3>
          <p className="form-hint">Use o telefone do paciente demo ou 11999990001. Envie &quot;oi&quot; para ver o menu.</p>
          <form className="form-grid" onSubmit={sendSimulation}>
            <div className="form-field">
              <label>Telefone</label>
              <input value={simPhone} onChange={(e) => setSimPhone(e.target.value)} />
            </div>
            <div className="form-field full">
              <label>Mensagem</label>
              <input value={simMessage} onChange={(e) => setSimMessage(e.target.value)} placeholder="Ex: quero marcar cardiologista" />
            </div>
            <div className="form-field full">
              <button type="submit" className="btn">Enviar</button>
            </div>
          </form>
          <div className="connect-chat-preview">
            {simHistory.map((m, i) => (
              <div key={i} className={`connect-chat-bubble ${m.from}`}>{m.text}</div>
            ))}
            {simReply && simHistory.length === 0 && (
              <div className="connect-chat-bubble bot">{simReply}</div>
            )}
          </div>
        </section>
      )}

      {activeSection === 'conversas' && (
        <section className="connect-split">
          <div className="card connect-conv-list">
            <h3>Conversas recentes</h3>
            {conversations.length === 0 && <p className="form-hint">Nenhuma conversa ainda. Use o simulador.</p>}
            {conversations.map((c) => (
              <button key={c.id} type="button" className="connect-conv-item" onClick={() => openConversation(c.id)}>
                <strong>{c.patientName ?? c.contactPhone}</strong>
                <span>{c.lastMessagePreview?.slice(0, 60) ?? '—'}</span>
                <small>{c.botStep} · {c.lastMessageAt ? formatBrDateTime(c.lastMessageAt) : ''}</small>
              </button>
            ))}
          </div>
          <div className="card connect-conv-detail">
            {selected ? (
              <>
                <h3>{selected.conversation.patientName ?? selected.conversation.contactPhone}</h3>
                <div className="connect-chat-preview tall">
                  {selected.messages.map((m) => (
                    <div key={m.id} className={`connect-chat-bubble ${m.direction === 'Inbound' ? 'user' : 'bot'}`}>
                      {m.body}
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <p className="form-hint">Selecione uma conversa.</p>
            )}
          </div>
        </section>
      )}

      {activeSection === 'lista-espera' && (
        <section className="card">
          <h3>Lista de espera</h3>
          <table className="data-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Especialidade</th>
                <th>Médico</th>
                <th>Status</th>
                <th>Prioridade</th>
              </tr>
            </thead>
            <tbody>
              {waitlist.map((w) => (
                <tr key={w.id}>
                  <td>{w.patientName}</td>
                  <td>{w.specialtyName}</td>
                  <td>{w.professionalName ?? 'Qualquer'}</td>
                  <td><span className="badge">{w.status}</span></td>
                  <td>{w.priority}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {activeSection === 'faq' && (
        <section className="card">
          <h3>Base de conhecimento (IA / FAQ)</h3>
          <div className="connect-knowledge-grid">
            {knowledge.map((k) => (
              <article key={k.id} className="connect-knowledge-card">
                <span className="badge">{k.category}</span>
                <h4>{k.question}</h4>
                <p>{k.answer}</p>
              </article>
            ))}
          </div>
        </section>
      )}

      {activeSection === 'nps' && nps && (
        <section className="card">
          <h3>Pesquisa de satisfação (NPS)</h3>
          <p>Média geral: <strong>{nps.averageScore.toFixed(1)}</strong> · {nps.totalResponses} respostas</p>
          <h4>Por médico</h4>
          <ul>
            {nps.byProfessional.map((x) => (
              <li key={x.name}>{x.name}: {x.averageScore.toFixed(1)} ({x.count})</li>
            ))}
          </ul>
          <h4>Por especialidade</h4>
          <ul>
            {nps.bySpecialty.map((x) => (
              <li key={x.name}>{x.name}: {x.averageScore.toFixed(1)} ({x.count})</li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
