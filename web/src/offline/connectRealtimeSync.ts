import * as signalR from '@microsoft/signalr';

let connection: signalR.HubConnection | null = null;
const inboxListeners = new Set<() => void>();
const commListeners = new Set<() => void>();
const mailListeners = new Set<() => void>();
const ticketListeners = new Set<() => void>();
const taskListeners = new Set<() => void>();
const calendarListeners = new Set<() => void>();
const slaListeners = new Set<() => void>();
const hubListeners = new Set<() => void>();
const chatListeners = new Set<(roomId: string) => void>();

export function subscribeConnectChatRefresh(listener: (roomId: string) => void): () => void {
  chatListeners.add(listener);
  return () => chatListeners.delete(listener);
}

export function subscribeConnectInboxRefresh(listener: () => void): () => void {
  inboxListeners.add(listener);
  return () => inboxListeners.delete(listener);
}

export function subscribeConnectCommRefresh(listener: () => void): () => void {
  commListeners.add(listener);
  return () => commListeners.delete(listener);
}

export function subscribeConnectMailRefresh(listener: () => void): () => void {
  mailListeners.add(listener);
  return () => mailListeners.delete(listener);
}

export function subscribeConnectTicketRefresh(listener: () => void): () => void {
  ticketListeners.add(listener);
  return () => ticketListeners.delete(listener);
}

export function subscribeConnectTaskRefresh(listener: () => void): () => void {
  taskListeners.add(listener);
  return () => taskListeners.delete(listener);
}

export function subscribeConnectCalendarRefresh(listener: () => void): () => void {
  calendarListeners.add(listener);
  return () => calendarListeners.delete(listener);
}

export function subscribeConnectSlaAlert(listener: () => void): () => void {
  slaListeners.add(listener);
  return () => slaListeners.delete(listener);
}

export function subscribeHubNotificationRefresh(listener: () => void): () => void {
  hubListeners.add(listener);
  return () => hubListeners.delete(listener);
}

function notifyInboxRefresh(): void {
  inboxListeners.forEach((listener) => listener());
}

function notifyCommRefresh(): void {
  commListeners.forEach((listener) => listener());
}

function notifyMailRefresh(): void {
  mailListeners.forEach((listener) => listener());
}

function notifyTicketRefresh(): void {
  ticketListeners.forEach((listener) => listener());
}

function notifyTaskRefresh(): void {
  taskListeners.forEach((listener) => listener());
}

function notifyCalendarRefresh(): void {
  calendarListeners.forEach((listener) => listener());
}

function notifySlaRefresh(): void {
  slaListeners.forEach((listener) => listener());
}

function notifyHubRefresh(): void {
  hubListeners.forEach((listener) => listener());
}

function notifyChatRefresh(roomId: string): void {
  chatListeners.forEach((listener) => listener(roomId));
}

export function getConnectHubUrl(): string {
  const env = import.meta.env.VITE_HUB_URL as string | undefined;
  if (env) {
    return `${env.replace(/\/$/, '')}/hubs/connect`;
  }
  return '/hubs/connect';
}

export function isConnectHubConnected(): boolean {
  return connection?.state === signalR.HubConnectionState.Connected;
}

export async function connectConnectHub(): Promise<void> {
  const token = localStorage.getItem('hospital_token');
  if (!token) {
    await disconnectConnectHub();
    return;
  }

  const url = `${getConnectHubUrl()}?access_token=${encodeURIComponent(token)}`;

  if (connection) {
    await connection.stop();
    connection = null;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(url)
    .withAutomaticReconnect()
    .build();

  const onInboxEvent = () => notifyInboxRefresh();
  const onCommEvent = () => {
    notifyCommRefresh();
    notifyHubRefresh();
  };
  const onMailEvent = () => {
    notifyCommRefresh();
    notifyMailRefresh();
  };
  const onTicketEvent = () => notifyTicketRefresh();
  const onTaskEvent = () => notifyTaskRefresh();
  const onCalendarEvent = () => notifyCalendarRefresh();
  const onSlaEvent = () => {
    notifyCommRefresh();
    notifySlaRefresh();
    notifyTicketRefresh();
    notifyHubRefresh();
  };
  const onHubEvent = () => notifyHubRefresh();

  connection.on('connectMessageReceived', onInboxEvent);
  connection.on('connectMessageSent', onInboxEvent);
  connection.on('connectConversationUpdated', onInboxEvent);
  connection.on('connectAwaitingHuman', onInboxEvent);
  connection.on('connectInboxSummaryChanged', onInboxEvent);
  connection.on('connectChatMessage', (payload: { roomId?: string }) => {
    if (payload?.roomId) notifyChatRefresh(payload.roomId);
    onCommEvent();
  });
  connection.on('connectNotification', onCommEvent);
  connection.on('connectCommSummaryChanged', onCommEvent);
  connection.on('connectMailUpdated', onMailEvent);
  connection.on('connectTicketUpdated', onTicketEvent);
  connection.on('connectTaskUpdated', onTaskEvent);
  connection.on('connectCalendarUpdated', onCalendarEvent);
  connection.on('connectSlaAlert', onSlaEvent);
  connection.on('hubNotificationUpdated', onHubEvent);

  try {
    await connection.start();
  } catch (err) {
    console.error('SignalR connect hub:', err);
  }
}

export async function disconnectConnectHub(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}

export function initConnectRealtimeSync(): () => void {
  connectConnectHub().catch(console.error);

  const tokenPoll = window.setInterval(() => {
    const token = localStorage.getItem('hospital_token');
    if (token && !isConnectHubConnected()) {
      connectConnectHub().catch(console.error);
    }
    if (!token && connection) {
      disconnectConnectHub().catch(console.error);
    }
  }, 30_000);

  return () => {
    window.clearInterval(tokenPoll);
    disconnectConnectHub().catch(console.error);
  };
}
