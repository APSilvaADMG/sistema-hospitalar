import { useCallback, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type HubNotificationItemDto, type HubSummaryDto, type PendencyDto } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { formatBrDateTime } from '../../utils/dateUtils';
import {
  subscribeConnectCommRefresh,
  subscribeHubNotificationRefresh,
} from '../../offline/connectRealtimeSync';

const SOUND_PREF_KEY = 'notification_hub_sound_enabled';

function statusDot(status: HubSummaryDto['status']) {
  if (status === 'red') return '🔴';
  if (status === 'yellow') return '🟡';
  return '🟢';
}

type NotificationDropdownPanelProps = {
  summary: HubSummaryDto | null;
  pendencies: PendencyDto[];
  tab: 'notifications' | 'pendencies';
  onTabChange: (tab: 'notifications' | 'pendencies') => void;
  onMarkRead: (item: HubNotificationItemDto) => void;
  onClose: () => void;
};

export function NotificationDropdownPanel({
  summary,
  pendencies,
  tab,
  onTabChange,
  onMarkRead,
  onClose,
}: NotificationDropdownPanelProps) {
  return (
    <div className="notification-hub-dropdown" role="dialog" aria-label="Central de notificações">
      <div className="notification-hub-tabs">
        <button
          type="button"
          className={tab === 'notifications' ? 'active' : ''}
          onClick={() => onTabChange('notifications')}
        >
          Notificações
          {summary && summary.unreadNotifications > 0 ? (
            <span className="notification-hub-tab-badge">{summary.unreadNotifications}</span>
          ) : null}
        </button>
        <button
          type="button"
          className={tab === 'pendencies' ? 'active' : ''}
          onClick={() => onTabChange('pendencies')}
        >
          Pendências
          {summary && summary.pendingItemsCount > 0 ? (
            <span className="notification-hub-tab-badge">{summary.pendingItemsCount}</span>
          ) : null}
        </button>
      </div>

      {tab === 'notifications' ? (
        <div className="notification-hub-list">
          {!summary?.items.length ? (
            <p className="notification-hub-empty">Nenhuma notificação recente.</p>
          ) : (
            summary.items.map((item) => (
              <button
                key={`${item.source}-${item.id}`}
                type="button"
                className={`notification-hub-item${item.isRead ? '' : ' unread'}${item.priority === 'critical' ? ' critical' : ''}`}
                onClick={() => {
                  if (!item.isRead) onMarkRead(item);
                  onClose();
                }}
              >
                <div className="notification-hub-item-title">{item.title}</div>
                <div className="notification-hub-item-msg">{item.message}</div>
                <div className="notification-hub-item-meta">
                  {formatBrDateTime(item.createdAt)} · {item.source === 'connect' ? 'Connect' : 'Sistema'}
                </div>
                {item.linkDestino ? (
                  <Link to={item.linkDestino} className="notification-hub-item-link" onClick={onClose}>
                    Abrir
                  </Link>
                ) : null}
              </button>
            ))
          )}
          <Link to="/notificacoes" className="notification-hub-footer-link" onClick={onClose}>
            Ver todas →
          </Link>
        </div>
      ) : (
        <div className="notification-hub-list">
          {!pendencies.length ? (
            <p className="notification-hub-empty">Nenhuma pendência aberta.</p>
          ) : (
            pendencies.map((p) => (
              <Link
                key={p.id}
                to={p.linkDestino ?? '/pendencias'}
                className={`notification-hub-item pendency${p.prioridade === 'Critica' || p.prioridade === 'Alta' ? ' critical' : ''}`}
                onClick={onClose}
              >
                <div className="notification-hub-item-title">{p.titulo}</div>
                <div className="notification-hub-item-msg">{p.descricao}</div>
                <div className="notification-hub-item-meta">
                  {p.modulo} · {p.prioridade}
                  {p.dataLimite ? ` · limite ${formatBrDateTime(p.dataLimite)}` : ''}
                </div>
              </Link>
            ))
          )}
          <Link to="/pendencias" className="notification-hub-footer-link" onClick={onClose}>
            Ver todas as pendências →
          </Link>
        </div>
      )}
    </div>
  );
}

export function NotificationHubBar() {
  const { hasPermission } = useAuth();
  const [summary, setSummary] = useState<HubSummaryDto | null>(null);
  const [pendencies, setPendencies] = useState<PendencyDto[]>([]);
  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<'notifications' | 'pendencies'>('notifications');
  const [soundEnabled, setSoundEnabled] = useState(
    () => localStorage.getItem(SOUND_PREF_KEY) !== 'false',
  );
  const prevCritical = useRef(0);
  const hubRef = useRef<HTMLDivElement>(null);

  const load = useCallback(async () => {
    try {
      const [hub, pend] = await Promise.all([
        api.getNotificationHubSummary(),
        api.getPendencies(),
      ]);
      setSummary(hub);
      setPendencies(pend);

      if (hub.criticalCount > prevCritical.current && prevCritical.current > 0 && soundEnabled) {
        try {
          const ctx = new AudioContext();
          const osc = ctx.createOscillator();
          const gain = ctx.createGain();
          osc.connect(gain);
          gain.connect(ctx.destination);
          osc.frequency.value = 880;
          gain.gain.value = 0.05;
          osc.start();
          osc.stop(ctx.currentTime + 0.15);
        } catch {
          // sound stub — browser may block without gesture
        }
      }
      prevCritical.current = hub.criticalCount;
    } catch (err) {
      console.error('Notification hub:', err);
    }
  }, [soundEnabled]);

  useEffect(() => {
    load().catch(console.error);
    const unsubComm = subscribeConnectCommRefresh(() => load().catch(console.error));
    const unsubHub = subscribeHubNotificationRefresh(() => load().catch(console.error));
    const poll = window.setInterval(() => load().catch(console.error), 60_000);
    return () => {
      unsubComm();
      unsubHub();
      window.clearInterval(poll);
    };
  }, [load]);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (!hubRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('click', onDoc);
    return () => document.removeEventListener('click', onDoc);
  }, [open]);

  async function markRead(item: HubNotificationItemDto) {
    if (item.source === 'connect') {
      await api.markConnectNotificationRead(item.id);
    } else {
      await api.markNotificationRead(item.id);
    }
    await load();
  }

  const critical = (summary?.criticalCount ?? 0) > 0;
  const bellCount = summary?.unreadNotifications ?? 0;

  return (
    <div className={`notification-hub-bar${critical ? ' notification-hub-critical' : ''}`} ref={hubRef}>
      <span className="notification-hub-status" title={`Status: ${summary?.status ?? 'green'}`}>
        {statusDot(summary?.status ?? 'green')}
      </span>

      {hasPermission('connect.read') ? (
        <>
          <Link
            to="/connect"
            className="feegow-topbar-action notification-hub-counter"
            title="E-mails"
            aria-label={`${summary?.unreadMail ?? 0} e-mails não lidos`}
          >
            📧
            {(summary?.unreadMail ?? 0) > 0 ? (
              <span className="notification-hub-badge">{summary!.unreadMail}</span>
            ) : null}
          </Link>
          <Link
            to="/connect/chat"
            className="feegow-topbar-action notification-hub-counter"
            title="Chat"
            aria-label={`${summary?.unreadChat ?? 0} mensagens não lidas`}
          >
            💬
            {(summary?.unreadChat ?? 0) > 0 ? (
              <span className="notification-hub-badge">{summary!.unreadChat}</span>
            ) : null}
          </Link>
        </>
      ) : null}

      <Link
        to="/guias"
        className="feegow-topbar-action notification-hub-counter"
        title="Guias pendentes"
        aria-label={`${summary?.pendingGuides ?? 0} guias em rascunho`}
      >
        📋
        {(summary?.pendingGuides ?? 0) > 0 ? (
          <span className="notification-hub-badge">{summary!.pendingGuides}</span>
        ) : null}
      </Link>

      <button
        type="button"
        className="feegow-topbar-action notification-hub-counter"
        title="Pendências"
        aria-label={`${summary?.pendingItemsCount ?? 0} pendências`}
        onClick={(e) => {
          e.stopPropagation();
          setTab('pendencies');
          setOpen((v) => !v);
        }}
      >
        🕒
        {(summary?.pendingItemsCount ?? 0) > 0 ? (
          <span className="notification-hub-badge">{summary!.pendingItemsCount}</span>
        ) : null}
      </button>

      <div className="notification-hub-bell-wrap">
        <button
          type="button"
          className={`feegow-topbar-action notification-hub-bell${critical ? ' pulse' : ''}`}
          title="Notificações"
          aria-expanded={open}
          aria-label={`${bellCount} notificações`}
          onClick={(e) => {
            e.stopPropagation();
            setTab('notifications');
            setOpen((v) => !v);
          }}
        >
          🔔
          {bellCount > 0 ? <span className="notification-hub-badge">{bellCount}</span> : null}
        </button>

        {open ? (
          <NotificationDropdownPanel
            summary={summary}
            pendencies={pendencies}
            tab={tab}
            onTabChange={setTab}
            onMarkRead={(item) => markRead(item).catch(console.error)}
            onClose={() => setOpen(false)}
          />
        ) : null}
      </div>

      <button
        type="button"
        className="notification-hub-sound-toggle"
        title={soundEnabled ? 'Som ativado (clique para desativar)' : 'Som desativado'}
        aria-label="Configurar som de notificação"
        onClick={() => {
          const next = !soundEnabled;
          setSoundEnabled(next);
          localStorage.setItem(SOUND_PREF_KEY, String(next));
        }}
      >
        {soundEnabled ? '🔊' : '🔇'}
      </button>
    </div>
  );
}
