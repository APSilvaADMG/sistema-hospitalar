import {
  api,
  type ChemotherapySessionDto,
  type CreateTissGuideRequest,
  type ReportFilterParams,
  type ReportResultDto,
  type TissClinicalSourceDto,
  type TissGuideDto,
} from '../api/client';
import type { Funi21ConsultationForm } from '../data/funiGuides/funi21Consultation';
import type { Funi55ChemotherapyForm } from '../data/funiGuides/funi55Quimioterapia';
import type { Funi56RadiotherapyForm } from '../data/funiGuides/funi56Radioterapia';
import {
  mapFuni21ToTissGuide,
  mapFuni55ToTissGuide,
  mapFuni56ToTissGuide,
} from './funiGuideMapper';
import { tissGuideFormToCreateRequest, type TissGuideFormState } from './tissGuideFormTypes';

export const ClinicalDocumentKind = {
  TissGuide: 0,
  Report: 1,
} as const;

export type ClinicalDocumentContext = {
  appointmentId?: string;
  hospitalizationId?: string;
  chemotherapySessionId?: string;
  surgeryId?: string;
  labOrderId?: string;
  imagingStudyId?: string;
  label?: string;
};

/** @deprecated Use ClinicalDocumentContext */
export type ClinicalGuideContext = ClinicalDocumentContext;

export const guideTypeBySlug: Record<string, number> = {
  consulta: 1,
  'sp-sadt': 2,
  'solicitacao-internacao': 6,
  'resumo-internacao': 4,
  honorarios: 5,
  'outras-despesas': 7,
  prorrogacao: 9,
  opme: 16,
  quimioterapia: 17,
  radioterapia: 18,
  'recurso-glosas': 10,
  'demonstrativo-pagamento': 11,
};

export const guideSlugByType: Record<number, string> = Object.fromEntries(
  Object.entries(guideTypeBySlug).map(([slug, type]) => [String(type), slug]),
);

export const reportCodeBySlug: Record<string, string> = {
  'demonstrativo-analise': 'ins.glosas.by-reason',
};

export const reportSlugByCode: Record<string, string> = Object.fromEntries(
  Object.entries(reportCodeBySlug).map(([slug, code]) => [code, slug]),
);

const GUIDE_TYPE_LABELS: Record<number, string> = {
  1: 'Consulta (FUNI 21)',
  2: 'SP/SADT (FUNI 13)',
  4: 'Resumo de Internação (FUNI 23)',
  5: 'Honorários (FUNI 24)',
  6: 'Solicitação de Internação (FUNI 22)',
  7: 'Outras Despesas (FUNI 25)',
  9: 'Prorrogação (FUNI 39)',
  10: 'Recurso de Glosas (FUNI 59)',
  11: 'Demonstrativo de Pagamento (FUNI 58)',
  16: 'OPME (FUNI 54)',
  17: 'Quimioterapia (FUNI 55)',
  18: 'Radioterapia (FUNI 56)',
  19: 'Relatório de monitoramento',
};

export function guideTypeLabel(guideType: number): string {
  return GUIDE_TYPE_LABELS[guideType] ?? `Guia TISS ${guideType}`;
}

export type ReportCaptureData = {
  reportCode: string;
  reportName: string;
  filters: ReportFilterParams;
};

export async function findClinicalSource(
  patientId: string,
  guideType: number,
  context: ClinicalDocumentContext,
): Promise<TissClinicalSourceDto | null> {
  try {
    return await api.lookupClinicalSource({
      documentKind: ClinicalDocumentKind.TissGuide,
      patientId,
      guideType,
      appointmentId: context.appointmentId,
      hospitalizationId: context.hospitalizationId,
      chemotherapySessionId: context.chemotherapySessionId,
      surgeryId: context.surgeryId,
      labOrderId: context.labOrderId,
      imagingStudyId: context.imagingStudyId,
    });
  } catch {
    return null;
  }
}

export async function findReportSource(
  patientId: string,
  reportCode: string,
  context: ClinicalDocumentContext,
): Promise<TissClinicalSourceDto | null> {
  try {
    return await api.lookupClinicalSource({
      documentKind: ClinicalDocumentKind.Report,
      patientId,
      guideType: 19,
      reportCode,
      appointmentId: context.appointmentId,
      hospitalizationId: context.hospitalizationId,
      chemotherapySessionId: context.chemotherapySessionId,
      surgeryId: context.surgeryId,
      labOrderId: context.labOrderId,
      imagingStudyId: context.imagingStudyId,
    });
  } catch {
    return null;
  }
}

export async function saveClinicalSource(
  patientId: string,
  guideType: number,
  healthInsuranceId: string | undefined,
  context: ClinicalDocumentContext,
  formData: unknown,
): Promise<TissClinicalSourceDto> {
  return api.upsertClinicalSource({
    documentKind: ClinicalDocumentKind.TissGuide,
    patientId,
    guideType,
    healthInsuranceId: healthInsuranceId || undefined,
    appointmentId: context.appointmentId,
    hospitalizationId: context.hospitalizationId,
    chemotherapySessionId: context.chemotherapySessionId,
    surgeryId: context.surgeryId,
    labOrderId: context.labOrderId,
    imagingStudyId: context.imagingStudyId,
    label: context.label?.trim() || guideTypeLabel(guideType),
    formDataJson: JSON.stringify(formData),
  });
}

export async function saveReportSource(
  patientId: string,
  reportCode: string,
  reportName: string,
  context: ClinicalDocumentContext,
  capture: ReportCaptureData,
): Promise<TissClinicalSourceDto> {
  return api.upsertClinicalSource({
    documentKind: ClinicalDocumentKind.Report,
    patientId,
    guideType: 19,
    reportCode,
    appointmentId: context.appointmentId,
    hospitalizationId: context.hospitalizationId,
    chemotherapySessionId: context.chemotherapySessionId,
    surgeryId: context.surgeryId,
    labOrderId: context.labOrderId,
    imagingStudyId: context.imagingStudyId,
    label: context.label?.trim() || reportName,
    formDataJson: JSON.stringify(capture),
  });
}

function buildCreateRequest(
  guideType: number,
  formData: unknown,
  patientId: string,
  healthInsuranceId: string,
  context: ClinicalDocumentContext,
): CreateTissGuideRequest {
  switch (guideType) {
    case 1:
      return mapFuni21ToTissGuide(
        formData as Funi21ConsultationForm,
        patientId,
        healthInsuranceId,
        context.appointmentId,
      );
    case 17:
      return mapFuni55ToTissGuide(
        formData as Funi55ChemotherapyForm,
        patientId,
        healthInsuranceId,
        context.appointmentId,
      );
    case 18:
      return mapFuni56ToTissGuide(
        formData as Funi56RadiotherapyForm,
        patientId,
        healthInsuranceId,
        context.appointmentId,
      );
    default:
      return tissGuideFormToCreateRequest({
        ...(formData as TissGuideFormState),
        patientId,
        healthInsuranceId,
        guideType,
        appointmentId: context.appointmentId ?? (formData as TissGuideFormState).appointmentId ?? '',
        hospitalizationId: context.hospitalizationId ?? (formData as TissGuideFormState).hospitalizationId ?? '',
        surgeryId: context.surgeryId ?? (formData as TissGuideFormState).surgeryId ?? '',
        labOrderId: context.labOrderId ?? (formData as TissGuideFormState).labOrderId ?? '',
        imagingStudyId: context.imagingStudyId ?? (formData as TissGuideFormState).imagingStudyId ?? '',
      });
  }
}

export async function generateGuideFromClinicalData(
  patientId: string,
  guideType: number,
  healthInsuranceId: string,
  context: ClinicalDocumentContext,
  formData: unknown,
  existingSourceId?: string,
): Promise<{ guide: TissGuideDto; source: TissClinicalSourceDto }> {
  const payload = buildCreateRequest(guideType, formData, patientId, healthInsuranceId, context);
  const guide = await api.createTissGuide(payload);

  const source = existingSourceId
    ? await api.linkClinicalSourceGuide(existingSourceId, guide.id)
    : await saveClinicalSource(patientId, guideType, healthInsuranceId, context, formData).then((s) =>
        api.linkClinicalSourceGuide(s.id, guide.id),
      );

  if (!source) {
    throw new Error('Falha ao vincular guia aos dados clínicos.');
  }

  return { guide, source };
}

export async function generateReportFromClinicalData(
  patientId: string,
  reportCode: string,
  reportName: string,
  context: ClinicalDocumentContext,
  capture: ReportCaptureData,
  existingSourceId?: string,
): Promise<{ result: ReportResultDto; source: TissClinicalSourceDto }> {
  const result = await api.runReport(reportCode, capture.filters);

  const source = existingSourceId
    ? await api.linkClinicalSourceArtifact(existingSourceId, JSON.stringify(result))
    : await saveReportSource(patientId, reportCode, reportName, context, capture).then((s) =>
        api.linkClinicalSourceArtifact(s.id, JSON.stringify(result)),
      );

  if (!source) {
    throw new Error('Falha ao vincular relatório aos dados salvos.');
  }

  return { result, source };
}

export function buildChemoClinicalLabel(session: ChemotherapySessionDto): string {
  return `Quimio ${session.protocolName} — ciclo ${session.cycleNumber}/${session.totalCycles}`;
}

export function parseClinicalFormData<T>(json: string): T | null {
  try {
    return JSON.parse(json) as T;
  } catch {
    return null;
  }
}

export function isFuniFormGuideType(guideType: number): boolean {
  return guideType === 1 || guideType === 17 || guideType === 18;
}
