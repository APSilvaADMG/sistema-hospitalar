import type { AppointmentDto } from '../api/client';

function pad2(n: number) {
  return String(n).padStart(2, '0');
}

const DAY_RANGES: [number, number][] = [[8, 12], [14, 18]];

export function suggestEncaixeDateTime(
  date: string,
  appointments: AppointmentDto[],
  professionalId: string,
  slotMinutes = 30,
): string {
  const active = appointments.filter(
    (a) => (!professionalId || a.professionalId === professionalId)
      && a.status !== 5
      && a.status !== 6,
  );

  const busy = new Set(
    active.map((a) => {
      const d = new Date(a.scheduledAt);
      const total = d.getHours() * 60 + d.getMinutes();
      const snapped = Math.floor(total / slotMinutes) * slotMinutes;
      return `${pad2(Math.floor(snapped / 60))}:${pad2(snapped % 60)}`;
    }),
  );

  const now = new Date();
  const isToday = date === now.toISOString().slice(0, 10);
  const nowMinutes = now.getHours() * 60 + now.getMinutes();

  for (const [startHour, endHour] of DAY_RANGES) {
    for (let t = startHour * 60; t < endHour * 60; t += slotMinutes) {
      const time = `${pad2(Math.floor(t / 60))}:${pad2(t % 60)}`;
      if (busy.has(time)) continue;
      if (isToday && t < nowMinutes) continue;
      return `${date}T${time}`;
    }
  }

  if (isToday) {
    const rounded = Math.min(Math.ceil(nowMinutes / 15) * 15, 17 * 60 + 45);
    return `${date}T${pad2(Math.floor(rounded / 60))}:${pad2(rounded % 60)}`;
  }

  return `${date}T09:00`;
}

function localDateKey(iso: string) {
  const d = new Date(iso);
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

export function appointmentInTimeRange(
  appt: AppointmentDto,
  date: string,
  startTime: string,
  endTime: string,
): boolean {
  if (localDateKey(appt.scheduledAt) !== date) return false;
  const d = new Date(appt.scheduledAt);
  const mins = d.getHours() * 60 + d.getMinutes();
  const start = Number(startTime.split(':')[0]) * 60 + Number(startTime.split(':')[1]);
  const end = Number(endTime.split(':')[0]) * 60 + Number(endTime.split(':')[1]);
  return mins >= start && mins < end;
}
