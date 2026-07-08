import type { PatientDetailDto, PatientInsuranceDto, TissGuidePrefillDto } from '../api/client';
import type { Funi21ConsultationForm } from '../data/funiGuides/funi21Consultation';
import { emptyFuni21Form } from '../data/funiGuides/funi21Consultation';

const ENC_PREFIX = 'ENC1:';

export function looksLikeEncryptedOrGarbage(value?: string | null): boolean {
  if (!value?.trim()) return false;
  const v = value.trim();
  if (v.startsWith(ENC_PREFIX)) return true;
  if (v.includes('<!--funi:')) return true;
  return false;
}

export function sanitizeFuniDigits(value?: string | null, maxLength = 15): string {
  if (!value || looksLikeEncryptedOrGarbage(value)) return '';
  return value.replace(/\D/g, '').slice(0, maxLength);
}

export function sanitizeFuniCardNumber(value?: string | null, maxLength = 20): string {
  if (!value || looksLikeEncryptedOrGarbage(value)) return '';
  return value.replace(/[^0-9A-Za-z]/g, '').toUpperCase().slice(0, maxLength);
}

export function pickBeneficiaryCns(
  ...candidates: Array<string | null | undefined>
): string {
  for (const raw of candidates) {
    const digits = sanitizeFuniDigits(raw, 15);
    if (digits.length >= 10) return digits;
  }
  for (const raw of candidates) {
    const digits = sanitizeFuniDigits(raw, 15);
    if (digits.length > 0) return digits;
  }
  return '';
}

export function parseProfessionalCrm(crm?: string | null): {
  council: string;
  number: string;
  uf: string;
} {
  const fallback = { council: 'CRM', number: '', uf: '' };
  if (!crm?.trim()) return fallback;

  const trimmed = crm.trim().toUpperCase();
  const councilMatch = trimmed.match(/\b(CRM|CRO|COREN|CRF|CRP|CREFITO|CRN|CRBM|CRESS|CRFA|CRMV|CRBIO|CFN|CFP)\b/);
  const council = councilMatch?.[1] ?? 'CRM';

  const ufMatch =
    trimmed.match(/(?:\/|-|\s)([A-Z]{2})\s*$/) ??
    trimmed.match(/\b([A-Z]{2})\s*$/);
  const uf = ufMatch?.[1] && ufMatch[1] !== council ? ufMatch[1] : '';

  const numberMatch = trimmed.match(/(\d{3,15})/);
  const number = numberMatch?.[1] ?? sanitizeFuniDigits(trimmed, 15);

  return { council, number, uf };
}

function resolveInsurance(
  patient: PatientDetailDto,
  prefill: TissGuidePrefillDto,
  healthInsuranceId?: string,
): PatientInsuranceDto | undefined {
  if (healthInsuranceId) {
    return patient.insurances.find((i) => i.healthInsuranceId === healthInsuranceId);
  }
  if (prefill.healthInsuranceId) {
    return patient.insurances.find((i) => i.healthInsuranceId === prefill.healthInsuranceId);
  }
  return patient.insurances.find((i) => i.isPrimary) ?? patient.insurances[0];
}

function formatCardValidity(value?: string | null): string {
  if (!value) return '';
  return value.slice(0, 10);
}

export function buildFuni21FormFromPatient(
  patient: PatientDetailDto,
  prefill: TissGuidePrefillDto,
  healthInsuranceId?: string,
): Funi21ConsultationForm {
  const insurance = resolveInsurance(patient, prefill, healthInsuranceId);
  const crm = parseProfessionalCrm(prefill.requestingProfessionalCrm);
  const suggested = prefill.suggestedItems?.[0];
  const cardValidity = formatCardValidity(
    prefill.cardValidUntil ?? insurance?.validUntil,
  );
  const operatorNotes = [
    prefill.operatorMessage,
    prefill.beneficiaryPlanName ? `Plano: ${prefill.beneficiaryPlanName}` : '',
    prefill.operatorCoverageSummary,
  ].filter(Boolean).join(' · ');

  return {
    ...emptyFuni21Form(),
    beneficiaryName: patient.fullName.trim(),
    beneficiaryCns: pickBeneficiaryCns(patient.cns, insurance?.cnsNumber, prefill.beneficiaryCns),
    beneficiaryCardNumber: sanitizeFuniCardNumber(insurance?.cardNumber ?? prefill.beneficiaryCardNumber),
    cardValidity,
    providerGuideNumber: `GC${Date.now().toString().slice(-8)}`,
    operatorGuideNumber: sanitizeFuniCardNumber(prefill.authorizationPassword),
    executingProfessionalName:
      (prefill.executingProfessionalName ?? prefill.requestingProfessionalName ?? '').trim(),
    professionalCouncil: crm.council,
    councilNumber: crm.number,
    councilUf: crm.uf,
    procedureCode: suggested?.tussCode?.trim() ?? '',
    procedureDescription: suggested?.description?.trim() ?? '',
    procedureValue:
      suggested?.unitPrice != null && suggested.unitPrice > 0
        ? String(suggested.unitPrice)
        : '',
    observation: operatorNotes,
  };
}

export function resolvePrefillInsuranceId(
  patient: PatientDetailDto,
  prefill: TissGuidePrefillDto,
  preferredId?: string,
): string {
  if (preferredId && patient.insurances.some((i) => i.healthInsuranceId === preferredId)) {
    return preferredId;
  }
  if (prefill.healthInsuranceId && patient.insurances.some((i) => i.healthInsuranceId === prefill.healthInsuranceId)) {
    return prefill.healthInsuranceId;
  }
  return patient.insurances.find((i) => i.isPrimary)?.healthInsuranceId
    ?? patient.insurances[0]?.healthInsuranceId
    ?? '';
}
