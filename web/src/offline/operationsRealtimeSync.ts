import * as signalR from '@microsoft/signalr';
import { syncOperations } from './operationsSyncEngine';

let connection: signalR.HubConnection | null = null;
const refreshListeners = new Set<() => void>();

export function subscribeOperationsRefresh(listener: () => void): () => void {
  refreshListeners.add(listener);
  return () => refreshListeners.delete(listener);
}

function notifyRefresh(): void {
  refreshListeners.forEach((l) => l());
}

export function getHubUrl(): string {
  const env = import.meta.env.VITE_HUB_URL as string | undefined;
  if (env) {
    return `${env.replace(/\/$/, '')}/hubs/operations`;
  }
  return '/hubs/operations';
}

export function isOperationsHubConnected(): boolean {
  return connection?.state === signalR.HubConnectionState.Connected;
}

export async function connectOperationsHub(): Promise<void> {
  const token = localStorage.getItem('hospital_token');
  if (!token) {
    await disconnectOperationsHub();
    return;
  }

  const url = `${getHubUrl()}?access_token=${encodeURIComponent(token)}`;

  if (connection) {
    await connection.stop();
    connection = null;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(url)
    .withAutomaticReconnect()
    .build();

  const onEvent = () => {
    syncOperations()
      .then(() => notifyRefresh())
      .catch(console.error);
  };

  connection.on('syncRequired', onEvent);
  connection.on('transportChanged', onEvent);
  connection.on('cleaningChanged', onEvent);
  connection.on('bedsChanged', onEvent);

  try {
    await connection.start();
    notifyRefresh();
  } catch (err) {
    console.error('SignalR operations hub:', err);
  }
}

export async function disconnectOperationsHub(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}

export function initOperationsRealtimeSync(): () => void {
  const onOnline = () => {
    connectOperationsHub().catch(console.error);
    syncOperations().then(() => notifyRefresh()).catch(console.error);
  };

  connectOperationsHub().catch(console.error);

  window.addEventListener('online', onOnline);

  const tokenPoll = window.setInterval(() => {
    const token = localStorage.getItem('hospital_token');
    if (token && !isOperationsHubConnected()) {
      connectOperationsHub().catch(console.error);
    }
    if (!token && connection) {
      disconnectOperationsHub().catch(console.error);
    }
  }, 30_000);

  return () => {
    window.removeEventListener('online', onOnline);
    window.clearInterval(tokenPoll);
    disconnectOperationsHub().catch(console.error);
  };
}
