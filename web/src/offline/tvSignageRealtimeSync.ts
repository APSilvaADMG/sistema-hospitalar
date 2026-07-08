import * as signalR from '@microsoft/signalr';

let connection: signalR.HubConnection | null = null;
const refreshListeners = new Set<() => void>();

export function subscribeTvSignageRefresh(listener: () => void): () => void {
  refreshListeners.add(listener);
  return () => refreshListeners.delete(listener);
}

function notifyRefresh(): void {
  refreshListeners.forEach((listener) => listener());
}

function getHubUrl(): string {
  const env = import.meta.env.VITE_HUB_URL as string | undefined;
  if (env) {
    return `${env.replace(/\/$/, '')}/hubs/tv-signage`;
  }
  return '/hubs/tv-signage';
}

export async function connectTvSignageHub(slug: string, token: string): Promise<void> {
  if (!slug || !token) return;

  if (connection) {
    await connection.stop();
    connection = null;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(getHubUrl())
    .withAutomaticReconnect()
    .build();

  connection.on('tvQueueCall', () => notifyRefresh());
  connection.on('tvStateChanged', () => notifyRefresh());

  try {
    await connection.start();
    await connection.invoke('RegisterDisplay', slug, token);
  } catch (err) {
    console.error('SignalR TV signage hub:', err);
  }
}

export async function disconnectTvSignageHub(): Promise<void> {
  if (!connection) return;
  try {
    await connection.stop();
  } catch {
    // ignore
  }
  connection = null;
}
