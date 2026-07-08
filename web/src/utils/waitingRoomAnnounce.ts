import { api } from '../api/client';

/** Rótulo exibido na TV (consultório / sala). */
export function formatWaitingRoomCallDestination(room?: string): string {
  const trimmed = room?.trim();
  if (!trimmed) return 'Consultório indicado';

  const lower = trimmed.toLowerCase();
  if (lower.includes('guichê') || lower.includes('guiche') || lower.includes('consult') || lower.startsWith('sala')) {
    return trimmed;
  }

  return `Consultório ${trimmed}`;
}

/** Destino falado em voz (ex.: "consultório 3"). */
export function formatWaitingRoomSpeechDestination(room?: string): string {
  const label = formatWaitingRoomCallDestination(room);
  const lower = label.toLowerCase();

  if (lower.includes('guichê') || lower.includes('guiche')) {
    return label.replace(/^guichê\s*/i, 'guichê ');
  }

  if (lower.startsWith('sala ')) {
    const num = label.match(/(\d+)/)?.[1];
    return num ? `consultório ${num}` : label.replace(/^sala\s*/i, 'consultório ');
  }

  if (lower.startsWith('consultório') || lower.startsWith('consultorio')) {
    return label;
  }

  return `consultório ${label}`;
}

export function buildPatientCallSpeech(patientName: string, room?: string): string {
  const destination = formatWaitingRoomSpeechDestination(room);
  const lower = destination.toLowerCase();
  const preposition = lower.includes('guichê') || lower.includes('guiche') ? 'ao' : 'ao';
  return `${patientName}, dirija-se ${preposition} ${destination}.`;
}

export function speakPatientCall(patientName: string, room?: string) {
  if (typeof window === 'undefined' || !window.speechSynthesis) {
    return;
  }

  window.speechSynthesis.cancel();
  const utterance = new SpeechSynthesisUtterance(buildPatientCallSpeech(patientName, room));
  utterance.lang = 'pt-BR';
  utterance.rate = 0.95;
  window.speechSynthesis.speak(utterance);
}

export async function syncPatientCallToTv(
  patientName: string,
  room?: string,
  sector?: string,
  ticketNumber?: string,
) {
  const destination = formatWaitingRoomCallDestination(room);
  await api.callTvQueue({
    ticketNumber: ticketNumber ?? String(Date.now()).slice(-4),
    patientName,
    destination,
    sector,
    showPatientName: true,
  });
}
