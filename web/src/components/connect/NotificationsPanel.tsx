import { useCallback, useEffect, useState } from 'react';
import { api, type ConnectNotificationDto } from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';
import { subscribeConnectCommRefresh } from '../../offline/connectRealtimeSync';

export function NotificationsPanel() {
  const [items, setItems] = useState<ConnectNotificationDto[]>([]);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setError('');
    try {
      setItems(await api.getConnectNotifications());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar notificações');
    }
  }, []);

  useEffect(() => {
    load().catch(console.error);
    return subscribeConnectCommRefresh(() => load().catch(console.error));
  }, [load]);

  async function markRead(id: string) {
    await api.markConnectNotificationRead(id);
    await load();
  }

  if (error) return <p className="text-danger">{error}</p>;

  if (items.length === 0) {
    return <p className="text-muted connect-panel">Nenhuma notificação.</p>;
  }

  return (
    <ul className="connect-mail-list connect-panel">
      {items.map((n) => (
        <li
          key={n.id}
          className={`connect-mail-item${n.isRead ? '' : ' unread'}`}
          onClick={() => !n.isRead && markRead(n.id).catch(console.error)}
        >
          <div>
            <strong>{n.title}</strong>
            <div style={{ marginTop: '0.25rem' }}>{n.message}</div>
            <div className="text-muted" style={{ fontSize: '0.85rem' }}>
              {formatBrDateTime(n.createdAt)} · {n.category}
            </div>
          </div>
          {!n.isRead ? <span className="feegow-badge-novo">Nova</span> : null}
        </li>
      ))}
    </ul>
  );
}
