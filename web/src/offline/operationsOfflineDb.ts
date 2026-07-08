import type { CleaningRequestDto, TransportRequestDto } from '../api/client';

const DB_NAME = 'medicore-operations-offline';
const DB_VERSION = 1;

const MUTATIONS = 'mutations';
const TRANSPORTS = 'transports';
const CLEANINGS = 'cleanings';
const META = 'meta';

export type OperationsMutationItem = {
  clientMutationId: string;
  entity: 'TransportRequest' | 'CleaningRequest';
  action: string;
  payload: Record<string, unknown>;
  clientTimestamp: string;
  status: 'pending' | 'synced' | 'failed';
  error?: string;
};

export type CachedTransport = TransportRequestDto & { cachedAt: string };
export type CachedCleaning = CleaningRequestDto & { cachedAt: string };

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(MUTATIONS)) {
        const store = db.createObjectStore(MUTATIONS, { keyPath: 'clientMutationId' });
        store.createIndex('status', 'status', { unique: false });
      }
      if (!db.objectStoreNames.contains(TRANSPORTS)) {
        db.createObjectStore(TRANSPORTS, { keyPath: 'id' });
      }
      if (!db.objectStoreNames.contains(CLEANINGS)) {
        db.createObjectStore(CLEANINGS, { keyPath: 'id' });
      }
      if (!db.objectStoreNames.contains(META)) {
        db.createObjectStore(META, { keyPath: 'key' });
      }
    };
  });
}

async function withStore<T>(
  storeName: string,
  mode: IDBTransactionMode,
  fn: (store: IDBObjectStore) => IDBRequest<T>,
): Promise<T> {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, mode);
    const store = tx.objectStore(storeName);
    const request = fn(store);
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
    tx.oncomplete = () => db.close();
    tx.onerror = () => reject(tx.error);
  });
}

export function getDeviceId(): string {
  const key = 'operations_device_id';
  let id = localStorage.getItem(key);
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem(key, id);
  }
  return id;
}

export async function enqueueMutation(item: OperationsMutationItem): Promise<void> {
  await withStore(MUTATIONS, 'readwrite', (store) => store.put(item));
}

export async function listPendingMutations(): Promise<OperationsMutationItem[]> {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(MUTATIONS, 'readonly');
    const store = tx.objectStore(MUTATIONS);
    const request = store.getAll();
    request.onsuccess = () => {
      const items = (request.result as OperationsMutationItem[])
        .filter((m) => m.status === 'pending' || m.status === 'failed')
        .sort((a, b) => new Date(a.clientTimestamp).getTime() - new Date(b.clientTimestamp).getTime());
      resolve(items);
    };
    request.onerror = () => reject(request.error);
    tx.oncomplete = () => db.close();
  });
}

export async function updateMutation(item: OperationsMutationItem): Promise<void> {
  await withStore(MUTATIONS, 'readwrite', (store) => store.put(item));
}

export async function countPendingMutations(): Promise<number> {
  return (await listPendingMutations()).length;
}

export async function replaceTransports(items: TransportRequestDto[]): Promise<void> {
  const db = await openDb();
  const now = new Date().toISOString();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(TRANSPORTS, 'readwrite');
    const store = tx.objectStore(TRANSPORTS);
    store.clear();
    for (const item of items) {
      store.put({ ...item, cachedAt: now } as CachedTransport);
    }
    tx.oncomplete = () => { db.close(); resolve(); };
    tx.onerror = () => reject(tx.error);
  });
}

export async function listCachedTransports(): Promise<CachedTransport[]> {
  const items = await withStore<CachedTransport[]>(TRANSPORTS, 'readonly', (store) => store.getAll());
  return items.sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime());
}

export async function upsertCachedTransport(item: TransportRequestDto): Promise<void> {
  await withStore(TRANSPORTS, 'readwrite', (store) => store.put({
    ...item,
    cachedAt: new Date().toISOString(),
  } as CachedTransport));
}

export async function replaceCleanings(items: CleaningRequestDto[]): Promise<void> {
  const db = await openDb();
  const now = new Date().toISOString();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(CLEANINGS, 'readwrite');
    const store = tx.objectStore(CLEANINGS);
    store.clear();
    for (const item of items) {
      store.put({ ...item, cachedAt: now } as CachedCleaning);
    }
    tx.oncomplete = () => { db.close(); resolve(); };
    tx.onerror = () => reject(tx.error);
  });
}

export async function listCachedCleanings(): Promise<CachedCleaning[]> {
  const items = await withStore<CachedCleaning[]>(CLEANINGS, 'readonly', (store) => store.getAll());
  return items.sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime());
}

export async function upsertCachedCleaning(item: CleaningRequestDto): Promise<void> {
  await withStore(CLEANINGS, 'readwrite', (store) => store.put({
    ...item,
    cachedAt: new Date().toISOString(),
  } as CachedCleaning));
}

export async function getMeta(key: string): Promise<string | null> {
  const row = await withStore<{ key: string; value: string } | undefined>(META, 'readonly', (store) => store.get(key));
  return row?.value ?? null;
}

export async function setMeta(key: string, value: string): Promise<void> {
  await withStore(META, 'readwrite', (store) => store.put({ key, value }));
}

export function isBrowserOnline(): boolean {
  return typeof navigator !== 'undefined' ? navigator.onLine : true;
}
