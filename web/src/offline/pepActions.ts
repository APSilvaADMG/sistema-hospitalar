import {
  api,
  type CreateMedicalRecordEntryRequest,
  type CreateTissGuideRequest,
  type MedicalRecordEntryDto,
  type TissGuideDto,
} from '../api/client';
import {
  createClientRequestId,
  enqueueOfflineItem,
  isBrowserOnline,
  type OfflineQueueItem,
} from './pepOfflineDb';

function queueItem(
  patientId: string,
  type: OfflineQueueItem['type'],
  payload: unknown,
  clientRequestId = createClientRequestId(),
): OfflineQueueItem {
  return {
    id: clientRequestId,
    clientRequestId,
    patientId,
    type,
    payload,
    createdAt: new Date().toISOString(),
    status: 'pending',
  };
}

export async function saveMedicalEntry(
  patientId: string,
  data: CreateMedicalRecordEntryRequest,
): Promise<{ entry?: MedicalRecordEntryDto; queued: boolean; clientRequestId: string }> {
  const clientRequestId = data.clientRequestId ?? createClientRequestId();
  const body = { ...data, clientRequestId };

  if (!isBrowserOnline()) {
    await enqueueOfflineItem(queueItem(patientId, 'entry', body, clientRequestId));
    return { queued: true, clientRequestId };
  }

  try {
    const entry = await api.addMedicalRecordEntry(patientId, body);
    return { entry, queued: false, clientRequestId };
  } catch (err) {
    if (!isBrowserOnline()) {
      await enqueueOfflineItem(queueItem(patientId, 'entry', body, clientRequestId));
      return { queued: true, clientRequestId };
    }
    throw err;
  }
}

export async function signMedicalEntry(
  patientId: string,
  entryId: string,
  professionalId: string,
  signatureImage: string,
  password: string,
): Promise<{ queued: boolean }> {
  const payload = { entryId, professionalId, signatureImage, password };
  const clientRequestId = createClientRequestId();

  if (!isBrowserOnline()) {
    await enqueueOfflineItem(queueItem(patientId, 'sign-entry', payload, clientRequestId));
    return { queued: true };
  }

  try {
    await api.signMedicalRecordEntry(patientId, entryId, {
      professionalId,
      signatureImage,
      password,
      signatureType: 1,
    });
    return { queued: false };
  } catch (err) {
    if (!isBrowserOnline()) {
      await enqueueOfflineItem(queueItem(patientId, 'sign-entry', payload, clientRequestId));
      return { queued: true };
    }
    throw err;
  }
}

export async function createTissGuideAction(
  patientId: string,
  data: CreateTissGuideRequest,
): Promise<{ guide?: TissGuideDto; queued: boolean; clientRequestId: string }> {
  const clientRequestId = data.clientRequestId ?? createClientRequestId();
  const body = { ...data, clientRequestId };

  if (!isBrowserOnline()) {
    await enqueueOfflineItem(queueItem(patientId, 'tiss-create', body, clientRequestId));
    return { queued: true, clientRequestId };
  }

  try {
    const guide = await api.createTissGuide(body);
    return { guide, queued: false, clientRequestId };
  } catch (err) {
    if (!isBrowserOnline()) {
      await enqueueOfflineItem(queueItem(patientId, 'tiss-create', body, clientRequestId));
      return { queued: true, clientRequestId };
    }
    throw err;
  }
}

export async function sendTissGuideAction(
  patientId: string,
  guideId: string,
): Promise<{ queued: boolean }> {
  const clientRequestId = createClientRequestId();

  if (!isBrowserOnline()) {
    await enqueueOfflineItem(queueItem(patientId, 'tiss-send', { guideId }, clientRequestId));
    return { queued: true };
  }

  try {
    await api.sendTissGuide(guideId);
    return { queued: false };
  } catch (err) {
    if (!isBrowserOnline()) {
      await enqueueOfflineItem(queueItem(patientId, 'tiss-send', { guideId }, clientRequestId));
      return { queued: true };
    }
    throw err;
  }
}

/** Queue send for a guide that will be created offline first. */
export async function queueTissSendAfterCreate(
  patientId: string,
  createClientRequestId: string,
): Promise<void> {
  await enqueueOfflineItem(
    queueItem(patientId, 'tiss-send', { createClientRequestId }, `${createClientRequestId}-send`),
  );
}
