import {
  api,
  type AcceptTransportRequestRequest,
  type CleaningChecklistItemDto,
  type CleaningRequestDto,
  type TransportRequestDto,
} from '../api/client';
import {
  isBrowserOnline,
  listCachedCleanings,
  listCachedTransports,
  upsertCachedCleaning,
  upsertCachedTransport,
} from './operationsOfflineDb';
import { getCachedPorters, getCachedTransportAssets, queueOperationsMutation } from './operationsSyncEngine';

function mutationId(): string {
  return crypto.randomUUID();
}

async function patchCachedTransport(
  id: string,
  patch: Partial<TransportRequestDto>,
): Promise<TransportRequestDto | null> {
  const items = await listCachedTransports();
  const existing = items.find((t) => t.id === id);
  if (!existing) return null;
  const updated = { ...existing, ...patch };
  await upsertCachedTransport(updated);
  return updated;
}

async function patchCachedCleaning(
  id: string,
  patch: Partial<CleaningRequestDto>,
): Promise<CleaningRequestDto | null> {
  const items = await listCachedCleanings();
  const existing = items.find((c) => c.id === id);
  if (!existing) return null;
  const updated = { ...existing, ...patch };
  await upsertCachedCleaning(updated);
  return updated;
}

async function runOrQueueTransport(
  requestId: string,
  action: string,
  payload: Record<string, unknown>,
  optimistic: Partial<TransportRequestDto>,
  onlineCall: () => Promise<TransportRequestDto>,
): Promise<{ queued: boolean; result?: TransportRequestDto }> {
  const clientMutationId = mutationId();
  const clientTimestamp = new Date().toISOString();

  if (!isBrowserOnline()) {
    await queueOperationsMutation({
      clientMutationId,
      entity: 'TransportRequest',
      action,
      payload: { requestId, ...payload },
      clientTimestamp,
    });
    const result = await patchCachedTransport(requestId, optimistic);
    return { queued: true, result: result ?? undefined };
  }

  try {
    const result = await onlineCall();
    await upsertCachedTransport(result);
    return { queued: false, result };
  } catch (err) {
    if (!isBrowserOnline()) {
      await queueOperationsMutation({
        clientMutationId,
        entity: 'TransportRequest',
        action,
        payload: { requestId, ...payload },
        clientTimestamp,
      });
      const result = await patchCachedTransport(requestId, optimistic);
      return { queued: true, result: result ?? undefined };
    }
    throw err;
  }
}

async function runOrQueueCleaning(
  requestId: string,
  action: string,
  payload: Record<string, unknown>,
  optimistic: Partial<CleaningRequestDto>,
  onlineCall: () => Promise<CleaningRequestDto>,
): Promise<{ queued: boolean; result?: CleaningRequestDto }> {
  const clientMutationId = mutationId();
  const clientTimestamp = new Date().toISOString();

  if (!isBrowserOnline()) {
    await queueOperationsMutation({
      clientMutationId,
      entity: 'CleaningRequest',
      action,
      payload: { requestId, ...payload },
      clientTimestamp,
    });
    const result = await patchCachedCleaning(requestId, optimistic);
    return { queued: true, result: result ?? undefined };
  }

  try {
    const result = await onlineCall();
    await upsertCachedCleaning(result);
    return { queued: false, result };
  } catch (err) {
    if (!isBrowserOnline()) {
      await queueOperationsMutation({
        clientMutationId,
        entity: 'CleaningRequest',
        action,
        payload: { requestId, ...payload },
        clientTimestamp,
      });
      const result = await patchCachedCleaning(requestId, optimistic);
      return { queued: true, result: result ?? undefined };
    }
    throw err;
  }
}

export async function acceptTransportAction(
  requestId: string,
  data: AcceptTransportRequestRequest,
): Promise<{ queued: boolean; result?: TransportRequestDto }> {
  const porters = await getCachedPorters();
  const assets = await getCachedTransportAssets();
  const porter = porters.find((p) => p.id === data.employeeId);
  const asset = assets.find((a) => a.id === data.transportAssetId);

  return runOrQueueTransport(
    requestId,
    'Accept',
    {
      employeeId: data.employeeId,
      transportAssetId: data.transportAssetId,
    },
    {
      status: 'Accepted',
      assignedEmployeeId: data.employeeId,
      assignedEmployeeName: porter?.fullName,
      transportAssetId: data.transportAssetId,
      transportAssetCode: asset?.code,
      acceptedAt: new Date().toISOString(),
    },
    () => api.acceptTransportRequest(requestId, data),
  );
}

export async function advanceTransportAction(
  requestId: string,
  status: string,
): Promise<{ queued: boolean; result?: TransportRequestDto }> {
  const now = new Date().toISOString();
  const optimistic: Partial<TransportRequestDto> = { status };
  if (status === 'InTransit') optimistic.departedAt = now;
  if (status === 'Completed') optimistic.completedAt = now;

  return runOrQueueTransport(
    requestId,
    'Advance',
    { status },
    optimistic,
    () => api.advanceTransportRequest(requestId, status),
  );
}

export async function cancelTransportAction(
  requestId: string,
): Promise<{ queued: boolean; result?: TransportRequestDto }> {
  return runOrQueueTransport(
    requestId,
    'Cancel',
    {},
    { status: 'Cancelled' },
    () => api.cancelTransportRequest(requestId),
  );
}

export async function startCleaningAction(
  requestId: string,
  assignedTeam?: string,
  assignedEmployeeId?: string,
): Promise<{ queued: boolean; result?: CleaningRequestDto }> {
  return runOrQueueCleaning(
    requestId,
    'Start',
    { assignedTeam, assignedEmployeeId },
    {
      status: 'InProgress',
      assignedTeam,
      assignedEmployeeId,
      startedAt: new Date().toISOString(),
    },
    () => api.startCleaningRequest(requestId, { assignedTeam, assignedEmployeeId }),
  );
}

export async function updateCleaningChecklistAction(
  requestId: string,
  checklist: CleaningChecklistItemDto[],
): Promise<{ queued: boolean; result?: CleaningRequestDto }> {
  return runOrQueueCleaning(
    requestId,
    'UpdateChecklist',
    { checklist },
    { checklist },
    () => api.updateCleaningChecklist(requestId, checklist),
  );
}

export async function completeCleaningAction(
  requestId: string,
  notes?: string,
): Promise<{ queued: boolean; result?: CleaningRequestDto }> {
  return runOrQueueCleaning(
    requestId,
    'Complete',
    { notes },
    {
      status: 'Completed',
      completedAt: new Date().toISOString(),
      notes,
    },
    () => api.completeCleaningRequest(requestId, notes),
  );
}
