import { useCallback, useEffect, useState } from 'react';
import {
  countPendingQueue,
  isBrowserOnline,
  listQueueItems,
  type OfflineQueueItem,
} from './pepOfflineDb';
import { subscribeOfflineSync, syncOfflineQueue } from './pepOfflineSync';

export function usePepOffline(patientId?: string) {
  const [online, setOnline] = useState(isBrowserOnline());
  const [pendingCount, setPendingCount] = useState(0);
  const [queue, setQueue] = useState<OfflineQueueItem[]>([]);
  const [syncing, setSyncing] = useState(false);

  const refresh = useCallback(async () => {
    const [count, items] = await Promise.all([
      countPendingQueue(),
      patientId ? listQueueItems(patientId) : listQueueItems(),
    ]);
    setPendingCount(count);
    setQueue(items.filter((i) => i.status !== 'syncing'));
  }, [patientId]);

  useEffect(() => {
    const onOnline = () => setOnline(true);
    const onOffline = () => setOnline(false);
    window.addEventListener('online', onOnline);
    window.addEventListener('offline', onOffline);
    return () => {
      window.removeEventListener('online', onOnline);
      window.removeEventListener('offline', onOffline);
    };
  }, []);

  useEffect(() => {
    refresh().catch(console.error);
    return subscribeOfflineSync(() => {
      refresh().catch(console.error);
    });
  }, [refresh]);

  const syncNow = useCallback(async () => {
    if (!online) return { synced: 0, failed: 0 };
    setSyncing(true);
    try {
      const result = await syncOfflineQueue(patientId);
      await refresh();
      return result;
    } finally {
      setSyncing(false);
    }
  }, [online, patientId, refresh]);

  return { online, pendingCount, queue, syncing, syncNow, refresh };
}
