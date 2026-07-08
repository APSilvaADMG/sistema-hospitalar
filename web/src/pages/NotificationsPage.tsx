import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { api, notificationTypeLabels, type NotificationDto } from '../api/client';
import { ModuleTabs } from '../components/ModuleTabs';
import { PageHeader } from '../components/PageHeader';
import { dashboardTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { formatBrDateTime } from '../utils/dateUtils';

export function NotificationsPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);

  async function load() {
    const [list, count] = await Promise.all([
      api.getNotifications(),
      api.getUnreadNotificationCount(),
    ]);
    setNotifications(list);
    setUnreadCount(count.count);
  }

  useEffect(() => { load().catch(console.error); }, []);

  async function markRead(id: string) {
    await api.markNotificationRead(id);
    await load();
  }

  return (
    <>
      <PageHeader
        eyebrow="Dashboard"
        title={breadcrumb.title || 'Notificações'}
        subtitle={`${unreadCount} não lida(s)`}
      />

      <ModuleTabs basePath="/" tabs={dashboardTabs} />

      <div className="card">
        {notifications.map((n) => (
          <div
            key={n.id}
            className={`order-block ${!n.isRead ? 'alert-info' : ''}`}
            style={{ opacity: n.isRead ? 0.7 : 1 }}
          >
            <div className="order-header">
              <strong>{n.title}</strong>
              <span className="badge">{notificationTypeLabels[n.type] ?? n.type}</span>
              {!n.isRead && (
                <button className="btn btn-secondary" onClick={() => markRead(n.id)}>Marcar lida</button>
              )}
            </div>
            <p>{n.message}</p>
            <p style={{ fontSize: '0.85rem', color: 'var(--muted)' }}>
              {formatBrDateTime(n.createdAt)}
            </p>
          </div>
        ))}
        {notifications.length === 0 && <p>Nenhuma notificação.</p>}
      </div>
    </>
  );
}
