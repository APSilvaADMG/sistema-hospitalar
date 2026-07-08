import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  api,
  type ConnectCommSummaryDto,
  type CreateMailRequest,
  type MailDetailDto,
  type MailListItemDto,
  type UserListDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { BulletinBoard } from '../components/connect/BulletinBoard';
import { ChatPanel } from '../components/connect/ChatPanel';
import { ComposeModal } from '../components/connect/ComposeModal';
import { MailList } from '../components/connect/MailList';
import { NotificationsPanel } from '../components/connect/NotificationsPanel';
import { TicketsPanel } from '../components/connect/TicketsPanel';
import { TasksPanel } from '../components/connect/TasksPanel';
import { ApprovalsPanel } from '../components/connect/ApprovalsPanel';
import { CalendarPanel } from '../components/connect/CalendarPanel';
import { ConnectAiAssistantPanel } from '../components/connect/ConnectAiAssistantPanel';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { connectTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import { subscribeConnectCommRefresh, subscribeConnectMailRefresh } from '../offline/connectRealtimeSync';
import { formatBrDateTime } from '../utils/dateUtils';
import { useLocation } from 'react-router-dom';
import '../components/connect/connectComm.css';

const folderBySection: Record<string, 'Inbox' | 'Sent' | 'Drafts'> = {
  '': 'Inbox',
  enviadas: 'Sent',
  rascunhos: 'Drafts',
};

export function ConnectHubPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section, goToSection } = useModuleSection('/connect');
  const activeSection = section || '';

  const canRead = hasPermission('connect.read');
  const canWrite = hasPermission('connect.write');
  const canApprove = hasPermission('connect.approve');

  const [summary, setSummary] = useState<ConnectCommSummaryDto | null>(null);
  const [mailItems, setMailItems] = useState<MailListItemDto[]>([]);
  const [selectedMail, setSelectedMail] = useState<MailDetailDto | null>(null);
  const [users, setUsers] = useState<UserListDto[]>([]);
  const [composeOpen, setComposeOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [loadingMail, setLoadingMail] = useState(false);

  const isMailSection = ['', 'enviadas', 'rascunhos'].includes(activeSection);
  const mailFolder = folderBySection[activeSection] ?? 'Inbox';

  const refreshSummary = useCallback(async () => {
    try {
      setSummary(await api.getConnectCommSummary());
    } catch {
      /* ignore */
    }
  }, []);

  const loadMail = useCallback(async () => {
    if (!isMailSection) return;
    setLoadingMail(true);
    setError('');
    try {
      const items = await api.getConnectMail(mailFolder, search || undefined);
      setMailItems(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar mensagens');
    } finally {
      setLoadingMail(false);
    }
  }, [isMailSection, mailFolder, search]);

  const openMail = useCallback(async (id: string) => {
    try {
      const detail = await api.getConnectMailDetail(id);
      setSelectedMail(detail);
      if (!detail.isRead) {
        await api.markConnectMailRead(id);
        await loadMail();
        await refreshSummary();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao abrir mensagem');
    }
  }, [loadMail, refreshSummary]);

  useEffect(() => {
    if (!canRead) return;
    refreshSummary().catch(console.error);
    api.getUsers().then(setUsers).catch(console.error);
    return subscribeConnectCommRefresh(() => refreshSummary().catch(console.error));
  }, [canRead, refreshSummary]);

  useEffect(() => {
    if (!canRead) return;
    loadMail().catch(console.error);
    setSelectedMail(null);
  }, [canRead, loadMail, activeSection]);

  useEffect(() => {
    if (!canRead || !isMailSection) return;
    return subscribeConnectMailRefresh(() => {
      loadMail().catch(console.error);
      refreshSummary().catch(console.error);
    });
  }, [canRead, isMailSection, loadMail, refreshSummary]);

  async function handleCompose(payload: CreateMailRequest) {
    await api.createConnectMail(payload);
    await loadMail();
    await refreshSummary();
    if (!payload.sendNow) goToSection('rascunhos');
  }

  const sectionTitle = useMemo(() => {
    const tab = connectTabs.find((t) => t.slug === activeSection);
    return tab?.label ?? 'APSMed Connect';
  }, [activeSection]);

  if (!canRead) {
    return (
      <div className="page">
        <PageHeader title="APSMed Connect" subtitle="Comunicação corporativa" />
        <p className="text-muted">Sem permissão para acessar o módulo de comunicação.</p>
      </div>
    );
  }

  return (
    <div className="page connect-hub">
      <PageHeader
        title="APSMed Connect"
        subtitle="Comunicação corporativa interna"
        eyebrow={breadcrumb.parents.length ? breadcrumb.parents.join(' › ') : 'Comunicação'}
      />
      <ModuleNav tabs={connectTabs} basePath="/connect" />

      {summary ? (
        <div className="connect-summary">
          <div className="connect-summary-card">
            <span className="text-muted">E-mail não lido</span>
            <strong>{summary.unreadMailCount}</strong>
          </div>
          <div className="connect-summary-card">
            <span className="text-muted">Chat</span>
            <strong>{summary.unreadChatCount}</strong>
          </div>
          <div className="connect-summary-card">
            <span className="text-muted">Notificações</span>
            <strong>{summary.unreadNotificationCount}</strong>
          </div>
          <div className="connect-summary-card">
            <span className="text-muted">Mural novo</span>
            <strong>{summary.unviewedBulletinCount}</strong>
          </div>
        </div>
      ) : null}

      <h2 style={{ margin: 0, fontSize: '1.1rem' }}>{sectionTitle}</h2>
      {error ? <p className="text-danger">{error}</p> : null}

      {isMailSection ? (
        <>
          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', alignItems: 'center' }}>
            {canWrite ? (
              <button type="button" className="btn btn-primary btn-sm" onClick={() => setComposeOpen(true)}>
                Nova mensagem
              </button>
            ) : null}
            <input
              placeholder="Buscar assunto ou conteúdo…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ maxWidth: 280 }}
            />
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => loadMail().catch(console.error)}>
              Atualizar
            </button>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: selectedMail ? '1fr 1fr' : '1fr', gap: '1rem' }}>
            <MailList
              items={mailItems}
              selectedId={selectedMail?.id}
              onSelect={(id) => openMail(id).catch(console.error)}
              loading={loadingMail}
            />
            {selectedMail ? (
              <div className="connect-panel">
                <h3 style={{ marginTop: 0 }}>{selectedMail.subject}</h3>
                <div className="text-muted" style={{ fontSize: '0.85rem', marginBottom: '0.75rem' }}>
                  De {selectedMail.senderName} · {formatBrDateTime(selectedMail.createdAt)} ·{' '}
                  {selectedMail.priority}
                </div>
                <div style={{ whiteSpace: 'pre-wrap' }}>{selectedMail.content}</div>
                {selectedMail.attachments.length > 0 ? (
                  <div style={{ marginTop: '1rem' }}>
                    <strong>Anexos</strong>
                    <ul>
                      {selectedMail.attachments.map((a) => (
                        <li key={a.id}>
                          <button
                            type="button"
                            className="btn btn-link btn-sm"
                            style={{ padding: 0 }}
                            onClick={() =>
                              api
                                .downloadConnectMailAttachment(selectedMail.id, a.id, a.fileName)
                                .catch(console.error)
                            }
                          >
                            {a.fileName}
                          </button>
                          {' '}({a.mimeType}, {(a.sizeBytes / 1024).toFixed(0)} KB)
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : null}
                {canWrite && selectedMail.folder === 'Inbox' ? (
                  <div style={{ marginTop: '1rem', display: 'flex', gap: '0.5rem' }}>
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => api.archiveConnectMail(selectedMail.id).then(() => loadMail())}
                    >
                      Arquivar
                    </button>
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => api.trashConnectMail(selectedMail.id).then(() => {
                        setSelectedMail(null);
                        loadMail();
                      })}
                    >
                      Excluir
                    </button>
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>
          <ComposeModal
            open={composeOpen}
            users={users}
            onClose={() => setComposeOpen(false)}
            onSubmit={handleCompose}
          />
        </>
      ) : null}

      {activeSection === 'chat' ? <ChatPanel /> : null}
      {activeSection === 'notificacoes' ? <NotificationsPanel /> : null}
      {activeSection === 'mural' ? <BulletinBoard /> : null}
      {activeSection === 'chamados' ? <TicketsPanel canWrite={canWrite} /> : null}
      {activeSection === 'tarefas' ? <TasksPanel canWrite={canWrite} /> : null}
      {activeSection === 'aprovacoes' ? <ApprovalsPanel canWrite={canWrite} canApprove={canApprove} /> : null}
      {activeSection === 'agenda' ? <CalendarPanel canWrite={canWrite} /> : null}
      {activeSection === 'assistente' ? <ConnectAiAssistantPanel /> : null}
    </div>
  );
}
