export type Funi56RadiotherapyForm = {
  ansRegistration: string;
  providerGuideNumber: string;
  referencedGuideNumber: string;
  operatorGuideNumber: string;
  password: string;
  authorizationDate: string;
  beneficiaryCardNumber: string;
  beneficiaryName: string;
  age: string;
  sex: '' | 'M' | 'F';
  requestingProfessionalName: string;
  phone: string;
  email: string;
  diagnosisDate: string;
  cid10Primary: string;
  cid10Secondary: string;
  cid10Tertiary: string;
  cid10Quaternary: string;
  imageDiagnosis: string;
  staging: string;
  ecog: string;
  purpose: string;
  cytHistopathology: string;
  relevantInfo: string;
  priorSurgery: string;
  priorSurgeryDate: string;
  priorChemotherapy: string;
  priorChemotherapyDate: string;
  fieldCount: string;
  dosePerDayGy: string;
  totalDoseGy: string;
  numberOfDays: string;
  plannedStartDate: string;
  observation: string;
  requestDate: string;
  requestingProfessionalSignature: string;
  authorizationResponsibleSignature: string;
};

export const emptyFuni56Form = (): Funi56RadiotherapyForm => ({
  ansRegistration: '',
  providerGuideNumber: '',
  referencedGuideNumber: '',
  operatorGuideNumber: '',
  password: '',
  authorizationDate: '',
  beneficiaryCardNumber: '',
  beneficiaryName: '',
  age: '',
  sex: '',
  requestingProfessionalName: '',
  phone: '',
  email: '',
  diagnosisDate: '',
  cid10Primary: '',
  cid10Secondary: '',
  cid10Tertiary: '',
  cid10Quaternary: '',
  imageDiagnosis: '',
  staging: '',
  ecog: '',
  purpose: '',
  cytHistopathology: '',
  relevantInfo: '',
  priorSurgery: '',
  priorSurgeryDate: '',
  priorChemotherapy: '',
  priorChemotherapyDate: '',
  fieldCount: '',
  dosePerDayGy: '',
  totalDoseGy: '',
  numberOfDays: '',
  plannedStartDate: '',
  observation: '',
  requestDate: new Date().toISOString().slice(0, 10),
  requestingProfessionalSignature: '',
  authorizationResponsibleSignature: '',
});

export function validateFuni56Form(form: Funi56RadiotherapyForm): string[] {
  const errors: string[] = [];
  if (!form.ansRegistration.trim()) errors.push('Registro ANS (1): obrigatório');
  if (!form.providerGuideNumber.trim()) errors.push('Nº guia prestador (2): obrigatório');
  if (!form.beneficiaryCardNumber.trim()) errors.push('Carteira (7): obrigatório');
  if (!form.beneficiaryName.trim()) errors.push('Nome (8): obrigatório');
  if (!form.requestingProfessionalName.trim()) errors.push('Profissional (11): obrigatório');
  return errors;
}

export { funi55PurposeOptions as funi56PurposeOptions, funi55EcogOptions as funi56EcogOptions, funi55StagingOptions as funi56StagingOptions, normalizeFuniDate } from './funi55Quimioterapia';
