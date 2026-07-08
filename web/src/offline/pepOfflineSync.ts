import { api, type CreateMedicalRecordEntryRequest, type CreateTissGuideRequest } from '../api/client';
import {
  listQueueItems,
  removeQueueItem,
  updateQueueItem,
  type OfflineQueueItem,
} from './pepOfflineDb';

const guideIdByClientRequest = new Map<string, string>();

type SyncListener = (pending: number) => void;
const listeners = new Set<SyncListener>();

export function subscribeOfflineSync(listener: SyncListener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function notifyListeners(pending: number) {
  listeners.forEach((l) => l(pending));
}

async function processItem(item: OfflineQueueItem): Promise<void> {
  item.status = 'syncing';
  await updateQueueItem(item);

  try {
    switch (item.type) {
      case 'entry': {
        const payload = item.payload as CreateMedicalRecordEntryRequest;
        await api.addMedicalRecordEntry(item.patientId, {
          ...payload,
          clientRequestId: item.clientRequestId,
        });
        break;
      }
      case 'sign-entry': {
        const payload = item.payload as {
          entryId: string;
          professionalId: string;
          signatureImage: string;
          password?: string;
        };
        await api.signMedicalRecordEntry(item.patientId, payload.entryId, {
          professionalId: payload.professionalId,
          signatureImage: payload.signatureImage,
          password: payload.password ?? '',
          signatureType: 1,
        });
        break;
      }
      case 'tiss-create': {
        const payload = item.payload as CreateTissGuideRequest;
        const guide = await api.createTissGuide({
          ...payload,
          clientRequestId: item.clientRequestId,
        });
        guideIdByClientRequest.set(item.clientRequestId, guide.id);
        item.resolvedGuideId = guide.id;
        break;
      }
      case 'tiss-send': {
        const payload = item.payload as { guideId?: string; createClientRequestId?: string };
        const guideId = payload.guideId
          ?? (payload.createClientRequestId ? guideIdByClientRequest.get(payload.createClientRequestId) : undefined);
        if (!guideId) {
          throw new Error('Guia ainda não sincronizada para envio.');
        }
        await api.sendTissGuide(guideId);
        break;
      }
      default:
        throw new Error(`Tipo de fila desconhecido: ${item.type}`);
    }

    await removeQueueItem(item.id);
  } catch (err) {
    item.status = 'failed';
    item.error = err instanceof Error ? err.message : 'Falha na sincronização';
    await updateQueueItem(item);
    throw err;
  }
}

export async function syncOfflineQueue(patientId?: string): Promise<{ synced: number; failed: number }> {
  const items = (await listQueueItems(patientId)).filter(
    (i) => i.status === 'pending' || i.status === 'failed',
  );

  let synced = 0;
  let failed = 0;

  for (const item of items) {
    try {
      await processItem(item);
      synced++;
    } catch {
      failed++;
    }
  }

  const remaining = (await listQueueItems()).filter(
    (i) => i.status === 'pending' || i.status === 'failed',
  ).length;
  notifyListeners(remaining);
  return { synced, failed };
}

export function initOfflineSync(): void {
  const run = () => {
    if (!navigator.onLine) return;
    syncOfflineQueue().catch(console.error);
  };

  window.addEventListener('online', run);
  run();
}
