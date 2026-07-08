import type { AppointmentDto } from '../api/client';

export type FeegowAgendaViewMode =
  | 'diaria'
  | 'semanal'
  | 'multipla'
  | 'check-in'
  | 'confirmar'
  | 'equipamentos'
  | 'mapa';

/** Semanal e múltipla — tarde até 17:30 (print Feegow 17-55-59). */
export const AGENDA_DAY_RANGES: [number, number][] = [[8, 12], [14, 18]];

/** Diária — tarde até 19:00 (print Feegow 16-33-13). */
export const AGENDA_DAY_RANGES_DAILY: [number, number][] = [[8, 12], [14, 19.5]];

export function pad2(n: number) {
  return String(n).padStart(2, '0');
}

export function toIsoDate(d: Date) {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

export function parseAgendaViewMode(section: string, pathname: string): FeegowAgendaViewMode {
  const path = pathname.toLowerCase();
  if (section === 'check-in' || path.includes('/check-in')) return 'check-in';
  if (section === 'confirmar' || path.includes('/confirmar')) return 'confirmar';
  if (section === 'equipamentos' || path.includes('/equipamentos')) return 'equipamentos';
  if (section === 'mapa' || path.includes('/mapa')) return 'mapa';
  if (section === 'semanal' || path.includes('/semanal')) return 'semanal';
  if (section === 'multipla' || path.includes('/multipla') || path.includes('/múltipla')) return 'multipla';
  return 'diaria';
}

/** Domingo da semana que contém a data (padrão Feegow). */
export function startOfWeekSunday(dateStr: string) {
  const d = new Date(`${dateStr}T12:00:00`);
  d.setDate(d.getDate() - d.getDay());
  return toIsoDate(d);
}

export function weekDatesFrom(dateStr: string): string[] {
  const start = new Date(`${startOfWeekSunday(dateStr)}T12:00:00`);
  return Array.from({ length: 7 }, (_, i) => {
    const d = new Date(start);
    d.setDate(start.getDate() + i);
    return toIsoDate(d);
  });
}

export function shiftWeek(dateStr: string, deltaWeeks: number) {
  const d = new Date(`${dateStr}T12:00:00`);
  d.setDate(d.getDate() + deltaWeeks * 7);
  return toIsoDate(d);
}

export function formatWeekRangeLabel(dateStr: string) {
  const dates = weekDatesFrom(dateStr);
  const first = new Date(`${dates[0]}T12:00:00`);
  const last = new Date(`${dates[6]}T12:00:00`);
  const fmt = (d: Date) => d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' });
  return `${fmt(first)} — ${fmt(last)}`;
}

export function formatWeekRangeLabelLong(dateStr: string) {
  const dates = weekDatesFrom(dateStr);
  const first = new Date(`${dates[0]}T12:00:00`);
  const last = new Date(`${dates[6]}T12:00:00`);
  const fmt = (d: Date) => {
    const day = String(d.getDate()).padStart(2, '0');
    const month = d.toLocaleDateString('pt-BR', { month: 'long' });
    const year = d.getFullYear();
    return `${day} de ${month} de ${year}`;
  };
  return `${fmt(first)} a ${fmt(last)}`;
}

export function formatWeeklyColumnHeader(dateStr: string) {
  const d = new Date(`${dateStr}T12:00:00`);
  const weekday = d.toLocaleDateString('pt-BR', { weekday: 'short' }).replace('.', '').toUpperCase();
  const datePart = d.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
  return {
    label: `${weekday}, ${datePart}`,
    isToday: dateStr === toIsoDate(new Date()),
  };
}

export function buildAgendaTimeSlots(
  slotMinutes = 30,
  ranges: [number, number][] = AGENDA_DAY_RANGES,
) {
  const slots: string[] = [];
  for (const [startHour, endHour] of ranges) {
    for (let t = startHour * 60; t < endHour * 60; t += slotMinutes) {
      slots.push(`${pad2(Math.floor(t / 60))}:${pad2(t % 60)}`);
    }
  }
  return slots;
}

export function slotKeyFromIso(iso: string, slotMinutes: number) {
  const d = new Date(iso);
  const total = d.getHours() * 60 + d.getMinutes();
  const snapped = Math.floor(total / slotMinutes) * slotMinutes;
  return `${pad2(Math.floor(snapped / 60))}:${pad2(snapped % 60)}`;
}

export function localDateKey(iso: string) {
  const d = new Date(iso);
  return toIsoDate(d);
}

export function groupAppointmentsByDateAndSlot(
  appointments: AppointmentDto[],
  slotMinutes = 30,
) {
  const map = new Map<string, AppointmentDto[]>();
  for (const appt of appointments) {
    const day = localDateKey(appt.scheduledAt);
    const slot = slotKeyFromIso(appt.scheduledAt, slotMinutes);
    const key = `${day}|${slot}`;
    const list = map.get(key) ?? [];
    list.push(appt);
    map.set(key, list);
  }
  return map;
}

export function groupAppointmentsByProfessionalAndSlot(
  appointments: AppointmentDto[],
  date: string,
  slotMinutes = 30,
) {
  const map = new Map<string, AppointmentDto[]>();
  for (const appt of appointments) {
    if (localDateKey(appt.scheduledAt) !== date) continue;
    const slot = slotKeyFromIso(appt.scheduledAt, slotMinutes);
    const key = `${appt.professionalId}|${slot}`;
    const list = map.get(key) ?? [];
    list.push(appt);
    map.set(key, list);
  }
  return map;
}

export function formatDayColumnHeader(dateStr: string) {
  const d = new Date(`${dateStr}T12:00:00`);
  const weekday = d.toLocaleDateString('pt-BR', { weekday: 'short' }).replace('.', '').toUpperCase();
  const dayMonth = d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
  return { weekday, dayMonth, isToday: dateStr === toIsoDate(new Date()) };
}
