import type { ConsultingRoomScheduleDto } from '../api/client';

const DAY_OF_WEEK_NAMES = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
] as const;

/** Resolve a sala/consultório do profissional conforme escala cadastrada. */
export function resolveProfessionalRoom(
  professionalId: string,
  scheduledAt: string,
  schedules: ConsultingRoomScheduleDto[],
): string {
  if (!professionalId) return '';

  const profSchedules = schedules.filter((s) => s.professionalId === professionalId);
  if (profSchedules.length === 0) return '';

  if (scheduledAt) {
    const date = new Date(scheduledAt);
    if (!Number.isNaN(date.getTime())) {
      const dayName = DAY_OF_WEEK_NAMES[date.getDay()];
      const dayMatch = profSchedules.find((s) => s.dayOfWeek === dayName);
      if (dayMatch) return dayMatch.roomName;
    }
  }

  return profSchedules[0].roomName;
}
