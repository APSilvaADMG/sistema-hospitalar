import { loadHospitalParams } from './clinicOnDoctorProfile';

export const IASGH_SYSTEM_NAME = 'IASGH';
export const IASGH_SYSTEM_VERSION = '2026.06';

export function getInstitutionName() {
  const name = loadHospitalParams().hospitalName?.trim();
  if (!name) return IASGH_SYSTEM_NAME;
  if (name.toLowerCase().includes('iasgh')) return name;
  return `${IASGH_SYSTEM_NAME} · ${name}`;
}

export function getInstitutionShortName() {
  const name = loadHospitalParams().hospitalName?.trim();
  if (name?.toLowerCase().includes('iasgh')) return 'IASGH';
  return IASGH_SYSTEM_NAME;
}

/** Saudação conforme o horário local (Bom dia / Boa tarde / Boa noite). */
export function getTimeOfDayGreeting(date = new Date()): string {
  const hour = date.getHours();
  if (hour >= 5 && hour < 12) return 'Bom dia!';
  if (hour >= 12 && hour < 18) return 'Boa tarde!';
  return 'Boa noite!';
}

export function formatFooterDateTime(date = new Date()): string {
  return date.toLocaleString('pt-BR', {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}
