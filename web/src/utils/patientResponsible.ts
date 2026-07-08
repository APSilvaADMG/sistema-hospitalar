import type { PatientDetailDto, PatientDto } from '../api/client';

export const RESPONSIBLE_RELATIONSHIPS = [
  'Pai',
  'Mãe',
  'Avô/Avó',
  'Tutor(a)',
  'Cônjuge',
  'Filho(a)',
  'Irmão(ã)',
  'Outro',
] as const;

export type ResponsiblePatient = PatientDto | PatientDetailDto;

export function calculateAgeYears(birthDate: string): number {
  const birth = new Date(`${birthDate}T12:00:00`);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  const monthDiff = today.getMonth() - birth.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
    age -= 1;
  }
  return age;
}

export function isMinorPatient(birthDate: string): boolean {
  return calculateAgeYears(birthDate) < 18;
}

export function patientHasResponsible(patient: ResponsiblePatient): boolean {
  const hasContact = Boolean(
    patient.emergencyContactName?.trim() && patient.emergencyContactPhone?.trim(),
  );
  const hasMother = Boolean(patient.motherName?.trim());
  return hasContact || hasMother;
}

export function responsibleStatusLabel(patient: ResponsiblePatient): string {
  if (patientHasResponsible(patient)) return 'Completo';
  if (isMinorPatient(patient.birthDate)) return 'Obrigatório (menor)';
  return 'Pendente';
}
