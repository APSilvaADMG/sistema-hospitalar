import type { CreateTissGuideRequest } from '../api/client';
import type { Funi21ConsultationForm } from '../data/funiGuides/funi21Consultation';
import type { Funi55ChemotherapyForm } from '../data/funiGuides/funi55Quimioterapia';
import type { Funi56RadiotherapyForm } from '../data/funiGuides/funi56Radioterapia';
import { normalizeFuniDate } from '../data/funiGuides/funi55Quimioterapia';

export type FuniGuideNotesPayload = {
  funiCode: 'FUNI 21';
  revision: 'Rev. 01';
  fields: Partial<Funi21ConsultationForm>;
};

export type Funi55NotesPayload = {
  funiCode: 'FUNI 55';
  revision: 'Rev. 00';
  fields: Partial<Funi55ChemotherapyForm>;
};

function embedFuniJson(payload: unknown, observation?: string): string {
  const json = JSON.stringify(payload);
  const tag = `<!--funi:${json}-->`;
  return observation?.trim() ? `${observation.trim()}\n\n${tag}` : tag;
}

export function serializeFuni21Notes(form: Funi21ConsultationForm, extraObservation?: string): string {
  const payload: FuniGuideNotesPayload = {
    funiCode: 'FUNI 21',
    revision: 'Rev. 01',
    fields: {
      operatorGuideNumber: form.operatorGuideNumber,
      cardValidity: form.cardValidity,
      newbornCare: form.newbornCare,
      providerOperatorCode: form.providerOperatorCode,
      contractedName: form.contractedName,
      cnesCode: form.cnesCode,
      professionalCouncil: form.professionalCouncil,
      councilNumber: form.councilNumber,
      councilUf: form.councilUf,
      cboCode: form.cboCode,
      consultationType: form.consultationType,
      procedureTable: form.procedureTable,
      attendanceDate: form.attendanceDate,
      executingProfessionalSignature: form.executingProfessionalSignature,
      beneficiarySignature: form.beneficiarySignature,
    },
  };
  const obs = extraObservation ?? form.observation;
  return embedFuniJson(payload, obs);
}

export function mapFuni21ToTissGuide(
  form: Funi21ConsultationForm,
  patientId: string,
  healthInsuranceId: string,
  appointmentId?: string,
): CreateTissGuideRequest {
  const unitPrice = Number(form.procedureValue.replace(',', '.')) || 0;
  return {
    patientId,
    healthInsuranceId,
    appointmentId: appointmentId || undefined,
    guideType: 1,
    notes: serializeFuni21Notes(form),
    clinical: {
      accidentIndicator: Number(form.accidentIndicator),
      serviceCharacter: 1,
      executingProfessionalName: form.executingProfessionalName,
      executingProfessionalCrm: `${form.professionalCouncil} ${form.councilNumber}/${form.councilUf}`.trim(),
      clinicalJustification: form.observation || undefined,
    },
    items: [
      {
        tussCode: form.procedureCode,
        description: form.procedureDescription || `Consulta TUSS ${form.procedureCode}`,
        quantity: 1,
        unitPrice,
        priceTableSource: 1,
      },
    ],
  };
}

export function serializeFuni55Notes(form: Funi55ChemotherapyForm): string {
  const payload: Funi55NotesPayload = {
    funiCode: 'FUNI 55',
    revision: 'Rev. 00',
    fields: form,
  };
  return embedFuniJson(payload, form.observation);
}

export function mapFuni55ToTissGuide(
  form: Funi55ChemotherapyForm,
  patientId: string,
  healthInsuranceId: string,
  appointmentId?: string,
): CreateTissGuideRequest {
  const meds = form.medications.filter((m) => m.medicationCode.trim() || m.description.trim());
  return {
    patientId,
    healthInsuranceId,
    appointmentId: appointmentId || undefined,
    guideType: 17,
    notes: serializeFuni55Notes(form),
    clinical: {
      cid10Code: form.cid10Primary || undefined,
      cid10Secondary: [form.cid10Secondary, form.cid10Tertiary, form.cid10Quaternary].filter(Boolean).join(';') || undefined,
      clinicalJustification: form.observation || form.therapeuticPlan || undefined,
      requestingProfessionalName: form.requestingProfessionalName,
      admissionDate: normalizeFuniDate(form.requestDate) || undefined,
    },
    items: meds.map((m) => ({
      tussCode: m.medicationCode,
      description: m.description || `Medicamento ${m.medicationCode}`,
      quantity: 1,
      unitPrice: 0,
      priceTableSource: Number(m.tableCode) || 20,
    })),
  };
}

export type Funi56NotesPayload = {
  funiCode: 'FUNI 56';
  revision: 'Rev. 00';
  fields: Partial<Funi56RadiotherapyForm>;
};

export function serializeFuni56Notes(form: Funi56RadiotherapyForm): string {
  return embedFuniJson({ funiCode: 'FUNI 56', revision: 'Rev. 00', fields: form } satisfies Funi56NotesPayload, form.observation);
}

export function mapFuni56ToTissGuide(
  form: Funi56RadiotherapyForm,
  patientId: string,
  healthInsuranceId: string,
  appointmentId?: string,
): CreateTissGuideRequest {
  return {
    patientId,
    healthInsuranceId,
    appointmentId: appointmentId || undefined,
    guideType: 18,
    notes: serializeFuni56Notes(form),
    clinical: {
      cid10Code: form.cid10Primary || undefined,
      cid10Secondary: [form.cid10Secondary, form.cid10Tertiary, form.cid10Quaternary].filter(Boolean).join(';') || undefined,
      clinicalJustification: form.observation || form.relevantInfo || undefined,
      requestingProfessionalName: form.requestingProfessionalName,
      admissionDate: normalizeFuniDate(form.requestDate) || undefined,
    },
    items: [],
  };
}
