import { api, type AppointmentDto, type PatientDetailDto } from '../api/client';
import { formatBrDate, parseApiDate, toIsoDateInput } from './dateUtils';

export function calcAge(birthDate: string): number {
  const birth = parseApiDate(birthDate);
  if (!birth) return 0;
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  const monthDiff = today.getMonth() - birth.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
    age--;
  }
  return age;
}

export function formatBirthDate(birthDate: string): string {
  return formatBrDate(birthDate);
}

export function formatAddress(patient: PatientDetailDto): string {
  const parts: string[] = [];
  if (patient.addressStreet) {
    let line = patient.addressStreet;
    if (patient.addressNumber) line += `, ${patient.addressNumber}`;
    if (patient.addressComplement) line += ` — ${patient.addressComplement}`;
    parts.push(line);
  }
  if (patient.addressNeighborhood) parts.push(patient.addressNeighborhood);
  const cityState = [patient.addressCity, patient.addressState].filter(Boolean).join(' — ');
  if (cityState) parts.push(cityState);
  if (patient.addressZipCode) parts.push(`CEP ${patient.addressZipCode}`);
  return parts.join(' · ') || 'Não cadastrado';
}

export function formatPhone(phone?: string): string | undefined {
  if (!phone) return undefined;
  const digits = phone.replace(/\D/g, '');
  if (digits.length === 11) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
  }
  if (digits.length === 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  }
  return phone;
}

export function phoneHref(phone?: string): string | undefined {
  if (!phone) return undefined;
  const digits = phone.replace(/\D/g, '');
  return digits ? `tel:+55${digits}` : undefined;
}

export async function loadPatientAppointments(patientId: string, days = 21): Promise<AppointmentDto[]> {
  const dates: string[] = [];
  const today = new Date();
  for (let i = 0; i < days; i++) {
    const d = new Date(today);
    d.setDate(d.getDate() - i);
    dates.push(toIsoDateInput(d));
  }

  const batches = await Promise.all(
    dates.map((date) => api.getAppointments(date).catch(() => [] as AppointmentDto[])),
  );

  const seen = new Set<string>();
  const result: AppointmentDto[] = [];
  for (const batch of batches) {
    for (const appointment of batch) {
      if (appointment.patientId === patientId && !seen.has(appointment.id)) {
        seen.add(appointment.id);
        result.push(appointment);
      }
    }
  }

  return result.sort(
    (a, b) => new Date(b.scheduledAt).getTime() - new Date(a.scheduledAt).getTime(),
  );
}

export const entryTypeIcons: Record<number, string> = {
  1: '📝',
  2: '🩺',
  3: '💊',
  4: '🔬',
  5: '⚕️',
};
