import { isHospitalizationActive, type HospitalizationDto, type PatientDto } from '../api/client';

/** Monta o destino do visitante com base no paciente selecionado. */
export function buildVisitorDestination(
  patientId: string,
  hospitalizations: HospitalizationDto[],
  patients: PatientDto[],
): string {
  if (!patientId) return '';

  const active = hospitalizations.find(
    (h) => h.patientId === patientId && isHospitalizationActive(h.status),
  );
  if (active) {
    return `${active.wardName} — Leito ${active.bedNumber}`;
  }

  const patient = patients.find((p) => p.id === patientId);
  if (!patient) return '';

  if (patient.addressCity) {
    return `Ambulatório — ${patient.addressCity}`;
  }

  return `Ambulatório — ${patient.fullName}`;
}

export function patientVisitorLabel(
  patient: PatientDto,
  hospitalizations: HospitalizationDto[],
): string {
  const active = hospitalizations.find(
    (h) => h.patientId === patient.id && h.status === 1,
  );
  if (active) {
    return `${patient.fullName} (${active.wardName} — Leito ${active.bedNumber})`;
  }
  return patient.fullName;
}
