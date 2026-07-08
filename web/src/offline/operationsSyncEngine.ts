import {
  api,
  type CleaningRequestDto,
  type SyncPullResponse,
  type TransportAssetDto,
  type TransportPorterDto,
  type TransportRequestDto,
} from '../api/client';
import {
  countPendingMutations,
  enqueueMutation,
  getDeviceId,
  getMeta,
  isBrowserOnline,
  listPendingMutations,
  replaceCleanings,
  replaceTransports,
  setMeta,
  updateMutation,
  upsertCachedCleaning,
  upsertCachedTransport,
  type OperationsMutationItem,
} from './operationsOfflineDb';

type SyncState = { pending: number; lastSyncAt: string | null };
type SyncListener = (state: SyncState) => void;

const listeners = new Set<SyncListener>();

export function subscribeOperationsSync(listener: SyncListener): () => void {
  listeners.add(listener);
  void notifyListeners();
  return () => listeners.delete(listener);
}

async function notifyListeners(): Promise<void> {
  const state: SyncState = {
    pending: await countPendingMutations(),
    lastSyncAt: await getMeta('lastSyncAt'),
  };
  listeners.forEach((l) => l(state));
}

export async function queueOperationsMutation(
  item: Omit<OperationsMutationItem, 'status'>,
): Promise<void> {
  await enqueueMutation({ ...item, status: 'pending' });
  await notifyListeners();
}

export async function pullOperationsData(): Promise<SyncPullResponse | null> {
  if (!isBrowserOnline()) return null;

  const since = await getMeta('lastPullSince');
  const response = await api.syncPull({ since: since ?? undefined });

  await replaceTransports(response.transportRequests);
  await replaceCleanings(response.cleaningRequests);
  await setMeta('lastPullSince', response.serverTimestamp);
  await setMeta('lastSyncAt', new Date().toISOString());
  await setMeta('porters', JSON.stringify(response.porters));
  await setMeta('transportAssets', JSON.stringify(response.transportAssets));

  await notifyListeners();
  return response;
}

export async function getCachedPorters(): Promise<TransportPorterDto[]> {
  const raw = await getMeta('porters');
  if (!raw) return [];
  try {
    return JSON.parse(raw) as TransportPorterDto[];
  } catch {
    return [];
  }
}

export async function getCachedTransportAssets(): Promise<TransportAssetDto[]> {
  const raw = await getMeta('transportAssets');
  if (!raw) return [];
  try {
    return JSON.parse(raw) as TransportAssetDto[];
  } catch {
    return [];
  }
}

function applyServerPayload(
  item: OperationsMutationItem,
  payload: unknown,
): TransportRequestDto | CleaningRequestDto | null {
  if (!payload || typeof payload !== 'object') return null;
  if (item.entity === 'TransportRequest') {
    return payload as TransportRequestDto;
  }
  if (item.entity === 'CleaningRequest') {
    return payload as CleaningRequestDto;
  }
  return null;
}

export async function pushOperationsMutations(): Promise<{ synced: number; failed: number }> {
  if (!isBrowserOnline()) return { synced: 0, failed: 0 };

  const pending = await listPendingMutations();
  if (pending.length === 0) return { synced: 0, failed: 0 };

  const response = await api.syncPush({
    deviceId: getDeviceId(),
    mutations: pending.map((m) => ({
      clientMutationId: m.clientMutationId,
      entity: m.entity,
      action: m.action,
      payload: m.payload,
      clientTimestamp: m.clientTimestamp,
    })),
  });

  let synced = 0;
  let failed = 0;

  for (const result of response.results) {
    const item = pending.find((m) => m.clientMutationId === result.clientMutationId);
    if (!item) continue;

    if (result.status === 'Applied' || result.status === 'Duplicate') {
      item.status = 'synced';
      synced++;
      const entity = applyServerPayload(item, result.serverPayload);
      if (entity && item.entity === 'TransportRequest') {
        await upsertCachedTransport(entity as TransportRequestDto);
      } else if (entity && item.entity === 'CleaningRequest') {
        await upsertCachedCleaning(entity as CleaningRequestDto);
      }
    } else {
      item.status = 'failed';
      item.error = result.message ?? result.status;
      failed++;
    }
    await updateMutation(item);
  }

  await notifyListeners();
  return { synced, failed };
}

export async function syncOperations(): Promise<{ synced: number; failed: number }> {
  const pushResult = await pushOperationsMutations();
  await pullOperationsData();
  return pushResult;
}

export function initOperationsOfflineSync(): () => void {
  const run = () => {
    if (!isBrowserOnline()) return;
    if (!localStorage.getItem('hospital_token')) return;
    syncOperations().catch(console.error);
  };

  window.addEventListener('online', run);
  run();

  return () => {
    window.removeEventListener('online', run);
  };
}
