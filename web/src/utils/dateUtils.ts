const BR_LOCALE = 'pt-BR';
const BR_TIMEZONE = 'America/Sao_Paulo';

const DATE_ONLY_RE = /^(\d{4})-(\d{2})-(\d{2})$/;

export function parseApiDate(value: string | Date | null | undefined): Date | null {
  if (value == null || value === '') return null;
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  const trimmed = value.trim();
  const dateOnly = DATE_ONLY_RE.exec(trimmed);
  if (dateOnly) {
    const year = Number(dateOnly[1]);
    const month = Number(dateOnly[2]);
    const day = Number(dateOnly[3]);
    return new Date(year, month - 1, day, 12, 0, 0, 0);
  }

  const parsed = new Date(trimmed);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

export function formatBrDate(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function formatBrDateTime(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function formatBrDateTimeSeconds(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function formatBrTime(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function formatBrLongDate(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function formatBrMonthYear(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    month: 'long',
    year: 'numeric',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function formatBrWeekdayShort(value: string | Date | null | undefined, fallback = '—'): string {
  const date = parseApiDate(value);
  if (!date) return fallback;
  return new Intl.DateTimeFormat(BR_LOCALE, {
    weekday: 'short',
    day: '2-digit',
    month: 'short',
    timeZone: BR_TIMEZONE,
  }).format(date);
}

export function todayIsoDate(): string {
  return toIsoDateInput(new Date());
}

export function todayBrazilIsoDate(): string {
  return new Date().toLocaleDateString('en-CA', { timeZone: BR_TIMEZONE });
}

export function toIsoDateInput(value: string | Date): string {
  const date = parseApiDate(value);
  if (!date) return '';
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function shiftIsoDate(value: string, days: number): string {
  const date = parseApiDate(value) ?? new Date();
  date.setDate(date.getDate() + days);
  return toIsoDateInput(date);
}

export function isTodayIso(value: string): boolean {
  return value.slice(0, 10) === todayIsoDate();
}

export function addDaysIso(days: number, from = new Date()): string {
  const date = new Date(from);
  date.setDate(date.getDate() + days);
  return toIsoDateInput(date);
}

export function calculateAgeYears(birthDate: string): number | null {
  if (!birthDate) return null;
  const [y, m, d] = birthDate.split('-').map(Number);
  if (!y || !m || !d) return null;
  const today = new Date();
  let age = today.getFullYear() - y;
  const monthDiff = today.getMonth() + 1 - m;
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < d)) {
    age -= 1;
  }
  return age;
}
