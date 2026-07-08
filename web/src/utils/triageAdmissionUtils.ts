import { api, type TriageAdmissionSuggestionDto } from '../api/client';
import { formatBrDateTime } from './dateUtils';

export type AdmitFormWithTriage = {
  reason: string;
  diagnosis: string;
  aiTriageLogId?: string;
};

export async function fetchTriageAdmissionSuggestion(
  patientId: string,
): Promise<TriageAdmissionSuggestionDto | null> {
  if (!patientId) return null;

  return api.getTriageAdmissionSuggestion(patientId);
}

export function applyTriageAdmissionSuggestion<T extends AdmitFormWithTriage>(
  form: T,
  suggestion: TriageAdmissionSuggestionDto,
): T {
  return {
    ...form,
    reason: form.reason.trim() ? form.reason : suggestion.reason,
    diagnosis: form.diagnosis.trim() ? form.diagnosis : (suggestion.diagnosis ?? ''),
    aiTriageLogId: suggestion.triageLogId,
  };
}

export function triageAdmissionHint(suggestion: TriageAdmissionSuggestionDto): string {
  return `Preenchido pela triagem IA (${suggestion.manchesterColor} — ${suggestion.urgencyLabel}) em ${formatBrDateTime(suggestion.createdAt)}.`;
}
