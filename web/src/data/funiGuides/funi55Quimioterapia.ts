export type FuniChemotherapyMedicationRow = {
  plannedStartDate: string;
  tableCode: string;
  medicationCode: string;
  description: string;
  totalDosage: string;
  dosageUnit: string;
  administrationRoute: string;
  frequency: string;
};

export type Funi55ChemotherapyForm = {
  ansRegistration: string;
  providerGuideNumber: string;
  referencedGuideNumber: string;
  operatorGuideNumber: string;
  password: string;
  authorizationDate: string;
  beneficiaryCardNumber: string;
  beneficiaryName: string;
  requestingProfessionalName: string;
  phone: string;
  email: string;
  age: string;
  sex: '' | 'M' | 'F';
  weightKg: string;
  heightCm: string;
  bodySurfaceM2: string;
  diagnosisDate: string;
  cid10Primary: string;
  cid10Secondary: string;
  cid10Tertiary: string;
  cid10Quaternary: string;
  staging: string;
  chemotherapyType: string;
  purpose: string;
  ecog: string;
  tumor: string;
  nodule: string;
  metastasis: string;
  therapeuticPlan: string;
  cytHistopathology: string;
  relevantInfo: string;
  priorSurgery: string;
  priorSurgeryDate: string;
  priorRadiotherapyArea: string;
  priorRadiotherapyDate: string;
  observation: string;
  cyclesPlanned: string;
  currentCycle: string;
  daysInCurrentCycle: string;
  intervalBetweenCyclesDays: string;
  requestDate: string;
  medications: FuniChemotherapyMedicationRow[];
  requestingProfessionalSignature: string;
  authorizationResponsibleSignature: string;
};

export const FUNI55_MEDICATION_ROWS = 8;

export const emptyFuni55MedicationRow = (): FuniChemotherapyMedicationRow => ({
  plannedStartDate: '',
  tableCode: '20',
  medicationCode: '',
  description: '',
  totalDosage: '',
  dosageUnit: '',
  administrationRoute: '',
  frequency: '',
});

export const emptyFuni55Form = (): Funi55ChemotherapyForm => ({
  ansRegistration: '',
  providerGuideNumber: '',
  referencedGuideNumber: '',
  operatorGuideNumber: '',
  password: '',
  authorizationDate: '',
  beneficiaryCardNumber: '',
  beneficiaryName: '',
  requestingProfessionalName: '',
  phone: '',
  email: '',
  age: '',
  sex: '',
  weightKg: '',
  heightCm: '',
  bodySurfaceM2: '',
  diagnosisDate: '',
  cid10Primary: '',
  cid10Secondary: '',
  cid10Tertiary: '',
  cid10Quaternary: '',
  staging: '',
  chemotherapyType: '',
  purpose: '',
  ecog: '',
  tumor: '',
  nodule: '',
  metastasis: '',
  therapeuticPlan: '',
  cytHistopathology: '',
  relevantInfo: '',
  priorSurgery: '',
  priorSurgeryDate: '',
  priorRadiotherapyArea: '',
  priorRadiotherapyDate: '',
  observation: '',
  cyclesPlanned: '',
  currentCycle: '',
  daysInCurrentCycle: '',
  intervalBetweenCyclesDays: '',
  requestDate: new Date().toISOString().slice(0, 10),
  medications: Array.from({ length: FUNI55_MEDICATION_ROWS }, emptyFuni55MedicationRow),
  requestingProfessionalSignature: '',
  authorizationResponsibleSignature: '',
});

export const funi55PurposeOptions = [
  { value: '1', label: '1 — Curativo' },
  { value: '2', label: '2 — Neoadjuvante' },
  { value: '3', label: '3 — Adjuvante' },
  { value: '4', label: '4 — Paliativo' },
];

export const funi55ChemoTypeOptions = [
  { value: '1', label: '1 — 1ª linha' },
  { value: '2', label: '2 — 2ª linha' },
  { value: '3', label: '3 — 3ª linha' },
  { value: '4', label: '4 — Outras linhas' },
];

export const funi55EcogOptions = [
  { value: '0', label: '0 — Totalmente ativo' },
  { value: '1', label: '1 — Restrição física' },
  { value: '2', label: '2 — Deambula, autocuidado' },
  { value: '3', label: '3 — Autocuidado limitado' },
  { value: '4', label: '4 — Acamado' },
];

export const funi55StagingOptions = [
  { value: '1', label: '1 — I' },
  { value: '2', label: '2 — II' },
  { value: '3', label: '3 — III' },
  { value: '4', label: '4 — IV' },
  { value: '5', label: '5 — Não se aplica' },
];

export function computeBodySurfaceM2(weightKg: string, heightCm: string): string {
  const w = Number(weightKg.replace(',', '.'));
  const h = Number(heightCm.replace(',', '.'));
  if (!Number.isFinite(w) || !Number.isFinite(h) || w <= 0 || h <= 0) return '';
  const bsa = Math.sqrt((h * w) / 3600);
  return bsa.toFixed(2).replace('.', ',');
}

export function normalizeFuniDate(value: string): string {
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
  return '';
}

export function validateFuni55Form(form: Funi55ChemotherapyForm): string[] {
  const errors: string[] = [];
  if (!form.ansRegistration.trim()) errors.push('Registro ANS (1): obrigatório');
  if (!form.providerGuideNumber.trim()) errors.push('Nº guia prestador (2): obrigatório');
  if (!form.beneficiaryCardNumber.trim()) errors.push('Carteira (7): obrigatório');
  if (!form.beneficiaryName.trim()) errors.push('Nome beneficiário (8): obrigatório');
  if (!form.requestingProfessionalName.trim()) errors.push('Profissional solicitante (14): obrigatório');
  if (!normalizeFuniDate(form.requestDate)) errors.push('Data solicitação (49): obrigatória');
  const meds = form.medications.filter((m) => m.medicationCode.trim() || m.description.trim());
  if (!meds.length) errors.push('Informe ao menos um medicamento (32–39)');
  return errors;
}
