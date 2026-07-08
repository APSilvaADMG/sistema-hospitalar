import type {
  CreateTissGuideRequest,
  TissGuideClinicalRequest,
  TissGuideItemRequest,
  UpdateTissGuideItemRequest,
} from '../api/client';

export type TissGuideFormState = {
  patientId: string;
  healthInsuranceId: string;
  guideType: number;
  appointmentId: string;
  hospitalizationId: string;
  surgeryId: string;
  labOrderId: string;
  imagingStudyId: string;
  notes: string;
  clinical: TissGuideClinicalRequest;
  items: UpdateTissGuideItemRequest[];
};

export const defaultTissGuideItem = (): TissGuideItemRequest => ({
  tussCode: '',
  description: '',
  quantity: 1,
  unitPrice: 0,
});

export function emptyTissGuideClinical(): TissGuideClinicalRequest {
  return { serviceCharacter: 1, accidentIndicator: 0 };
}

export function emptyTissGuideForm(guideType = 1): TissGuideFormState {
  return {
    patientId: '',
    healthInsuranceId: '',
    guideType,
    appointmentId: '',
    hospitalizationId: '',
    surgeryId: '',
    labOrderId: '',
    imagingStudyId: '',
    notes: '',
    clinical: emptyTissGuideClinical(),
    items: [defaultTissGuideItem()],
  };
}

export function tissGuideFormToCreateRequest(form: TissGuideFormState): CreateTissGuideRequest {
  return {
    patientId: form.patientId,
    healthInsuranceId: form.healthInsuranceId,
    appointmentId: form.appointmentId || undefined,
    hospitalizationId: form.hospitalizationId || undefined,
    guideType: form.guideType,
    notes: form.notes || undefined,
    clinical: {
      ...form.clinical,
      surgeryId: form.surgeryId || form.clinical.surgeryId,
    },
    items: form.items,
  };
}
