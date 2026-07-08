import { useCallback, useEffect, useState } from 'react';
import { isBrowserOnline } from './operationsOfflineDb';
import { isOperationsHubConnected } from './operationsRealtimeSync';
import { subscribeOperationsSync, syncOperations } from './operationsSyncEngine';

export function useOperationsOffline() {
  const [online, setOnline] = useState(isBrowserOnline());
  const [pendingCount, setPendingCount] = useState(0);
  const [syncing, setSyncing] = useState(false);
  const [realtimeConnected, setRealtimeConnected] = useState(false);
  const [lastSyncAt, setLastSyncAt] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState(0);

  const bumpRefresh = useCallback(() => {
    setRefreshToken((t) => t + 1);
  }, []);

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
    return subscribeOperationsSync(({ pending, lastSyncAt: syncAt }) => {
      setPendingCount(pending);
      setLastSyncAt(syncAt);
      bumpRefresh();
    });
  }, [bumpRefresh]);

  useEffect(() => {
    const poll = window.setInterval(() => {
      setRealtimeConnected(isOperationsHubConnected());
    }, 2000);
    return () => window.clearInterval(poll);
  }, []);

  const syncNow = useCallback(async () => {
    if (!online) return { synced: 0, failed: 0 };
    setSyncing(true);
    try {
      const result = await syncOperations();
      bumpRefresh();
      return result;
    } finally {
      setSyncing(false);
    }
  }, [online, bumpRefresh]);

  return {
    online,
    pendingCount,
    syncing,
    realtimeConnected,
    lastSyncAt,
    syncNow,
    refreshToken,
  };
}
