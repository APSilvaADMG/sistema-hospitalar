const DB_NAME = 'medicore-pep-offline';
const DB_VERSION = 1;
const QUEUE_STORE = 'queue';
const SNAPSHOT_STORE = 'snapshots';

export type OfflineQueueType = 'entry' | 'sign-entry' | 'tiss-create' | 'tiss-send';

export type OfflineQueueItem = {
  id: string;
  clientRequestId: string;
  patientId: string;
  type: OfflineQueueType;
  payload: unknown;
  createdAt: string;
  status: 'pending' | 'syncing' | 'failed';
  error?: string;
  /** Maps local create clientRequestId to server guide id after sync */
  resolvedGuideId?: string;
};

export type PepSnapshot = {
  patientId: string;
  savedAt: string;
  patient: unknown;
  digital: unknown;
};

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(QUEUE_STORE)) {
        const store = db.createObjectStore(QUEUE_STORE, { keyPath: 'id' });
        store.createIndex('patientId', 'patientId', { unique: false });
        store.createIndex('status', 'status', { unique: false });
      }
      if (!db.objectStoreNames.contains(SNAPSHOT_STORE)) {
        db.createObjectStore(SNAPSHOT_STORE, { keyPath: 'patientId' });
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

export async function enqueueOfflineItem(item: OfflineQueueItem): Promise<void> {
  await withStore(QUEUE_STORE, 'readwrite', (store) => store.put(item));
}

export async function listQueueItems(patientId?: string): Promise<OfflineQueueItem[]> {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(QUEUE_STORE, 'readonly');
    const store = tx.objectStore(QUEUE_STORE);
    const request = patientId ? store.index('patientId').getAll(patientId) : store.getAll();
    request.onsuccess = () => resolve((request.result as OfflineQueueItem[]).sort(
      (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    ));
    request.onerror = () => reject(request.error);
    tx.oncomplete = () => db.close();
  });
}

export async function updateQueueItem(item: OfflineQueueItem): Promise<void> {
  await withStore(QUEUE_STORE, 'readwrite', (store) => store.put(item));
}

export async function removeQueueItem(id: string): Promise<void> {
  await withStore(QUEUE_STORE, 'readwrite', (store) => store.delete(id));
}

export async function countPendingQueue(): Promise<number> {
  const items = await listQueueItems();
  return items.filter((i) => i.status === 'pending' || i.status === 'failed').length;
}

export async function savePepSnapshot(snapshot: PepSnapshot): Promise<void> {
  await withStore(SNAPSHOT_STORE, 'readwrite', (store) => store.put(snapshot));
}

export async function getPepSnapshot(patientId: string): Promise<PepSnapshot | null> {
  const result = await withStore<PepSnapshot | undefined>(SNAPSHOT_STORE, 'readonly', (store) => store.get(patientId));
  return result ?? null;
}

export function createClientRequestId(): string {
  return crypto.randomUUID();
}

export function isBrowserOnline(): boolean {
  return typeof navigator !== 'undefined' ? navigator.onLine : true;
}
