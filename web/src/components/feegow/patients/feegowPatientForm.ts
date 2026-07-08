import type { CreatePatientRequest, LegalResponsibleInput, PatientInsuranceInput } from '../../../api/client';
import {
  formatCnsInput,
  formatCpfInput,
  formatPhoneInput,
  formatRgInput,
  onlyDigits,
} from '../../../utils/inputMasks';
import { formatCep } from '../../../utils/cepLookup';

export type LegalResponsibleFormState = {
  name: string;
  cpf: string;
  birthDate: string;
  relationship: number;
  rg: string;
  authorizationDocumentType: number;
  authorizationDocumentReference: string;
};

export const emptyLegalResponsible = (): LegalResponsibleFormState => ({
  name: '',
  cpf: '',
  birthDate: '',
  relationship: 0,
  rg: '',
  authorizationDocumentType: 0,
  authorizationDocumentReference: '',
});

export type FeegowPatientSchedulingItem = {
  professionalId: string;
  specialtyName: string;
  procedureName: string;
  weekday: string;
  time: string;
  unit: string;
};

const SCHEDULING_NOTE_PREFIX = 'Programação agendamento: ';

export type FeegowPatientFormState = CreatePatientRequest & {
  isForeigner: boolean;
  noCpf: boolean;
  legalResponsible: LegalResponsibleFormState;
  identificationColor: string;
  priority: string;
  heightCm: string;
  weightKg: string;
  imc: string;
  chartNumber: string;
  phone2: string;
  mobilePhone2: string;
  email2: string;
  education: string;
  origin: string;
  referral: string;
  religion: string;
  skinColor: string;
  country: string;
  warnings: string;
  cns: string;
  schedulingPrograms: FeegowPatientSchedulingItem[];
};

const STRUCTURED_NOTE_LINE = {
  height: /^Altura:\s*(.+?)\s*cm\s*$/i,
  weight: /^Peso:\s*(.+?)\s*kg\s*$/i,
  imc: /^IMC:\s*(.+)\s*$/i,
  education: /^Escolaridade:\s*(.+)\s*$/i,
  origin: /^Origem:\s*(.+)\s*$/i,
  referral: /^Indicação:\s*(.+)\s*$/i,
  religion: /^Religião:\s*(.+)\s*$/i,
  skinColor: /^Cor da pele:\s*(.+)\s*$/i,
  country: /^País:\s*(.+)\s*$/i,
  phone2: /^Telefone 2:\s*(.+)\s*$/i,
  mobilePhone2: /^Celular 2:\s*(.+)\s*$/i,
  email2: /^E-mail 2:\s*(.+)\s*$/i,
  warnings: /^Avisos e pendências:\s*(.+)\s*$/i,
  chartNumber: /^Prontuário:\s*(.+)\s*$/i,
  priority: /^Prioridade:\s*(.+)\s*$/i,
  identificationColor: /^Cor de identificação:\s*(.+)\s*$/i,
  isForeigner: /^Paciente estrangeiro\s*$/i,
} as const;

function isStructuredFeegowNoteLine(line: string): boolean {
  const trimmed = line.trim();
  if (!trimmed) return false;
  if (trimmed.startsWith(SCHEDULING_NOTE_PREFIX)) return true;
  return Object.values(STRUCTURED_NOTE_LINE).some((pattern) => pattern.test(trimmed));
}

export function stripFeegowStructuredLines(notes?: string): string {
  if (!notes?.trim()) return '';
  return notes
    .split('\n')
    .filter((line) => !isStructuredFeegowNoteLine(line))
    .join('\n')
    .trim();
}

export function parseFeegowStructuredNotes(notes?: string): {
  userNotes: string;
  fields: Partial<FeegowPatientFormState>;
} {
  const fields: Partial<FeegowPatientFormState> = {};
  if (!notes?.trim()) {
    return { userNotes: '', fields };
  }

  const userLines: string[] = [];

  for (const rawLine of notes.split('\n')) {
    const line = rawLine.trim();
    if (!line) continue;

    if (line.startsWith(SCHEDULING_NOTE_PREFIX)) {
      continue;
    }

    let matched = false;

    const height = STRUCTURED_NOTE_LINE.height.exec(line);
    if (height) {
      fields.heightCm = height[1].trim();
      matched = true;
    }

    const weight = STRUCTURED_NOTE_LINE.weight.exec(line);
    if (weight) {
      fields.weightKg = weight[1].trim();
      matched = true;
    }

    const imc = STRUCTURED_NOTE_LINE.imc.exec(line);
    if (imc) {
      fields.imc = imc[1].trim();
      matched = true;
    }

    const education = STRUCTURED_NOTE_LINE.education.exec(line);
    if (education) {
      fields.education = education[1].trim();
      matched = true;
    }

    const origin = STRUCTURED_NOTE_LINE.origin.exec(line);
    if (origin) {
      fields.origin = origin[1].trim();
      matched = true;
    }

    const referral = STRUCTURED_NOTE_LINE.referral.exec(line);
    if (referral) {
      fields.referral = referral[1].trim();
      matched = true;
    }

    const religion = STRUCTURED_NOTE_LINE.religion.exec(line);
    if (religion) {
      fields.religion = religion[1].trim();
      matched = true;
    }

    const skinColor = STRUCTURED_NOTE_LINE.skinColor.exec(line);
    if (skinColor) {
      fields.skinColor = skinColor[1].trim();
      matched = true;
    }

    const country = STRUCTURED_NOTE_LINE.country.exec(line);
    if (country) {
      fields.country = country[1].trim();
      matched = true;
    }

    const phone2 = STRUCTURED_NOTE_LINE.phone2.exec(line);
    if (phone2) {
      fields.phone2 = phone2[1].trim();
      matched = true;
    }

    const mobilePhone2 = STRUCTURED_NOTE_LINE.mobilePhone2.exec(line);
    if (mobilePhone2) {
      fields.mobilePhone2 = mobilePhone2[1].trim();
      matched = true;
    }

    const email2 = STRUCTURED_NOTE_LINE.email2.exec(line);
    if (email2) {
      fields.email2 = email2[1].trim();
      matched = true;
    }

    const warnings = STRUCTURED_NOTE_LINE.warnings.exec(line);
    if (warnings) {
      fields.warnings = warnings[1].trim();
      matched = true;
    }

    const chartNumber = STRUCTURED_NOTE_LINE.chartNumber.exec(line);
    if (chartNumber) {
      fields.chartNumber = chartNumber[1].trim();
      matched = true;
    }

    const priority = STRUCTURED_NOTE_LINE.priority.exec(line);
    if (priority) {
      fields.priority = priority[1].trim();
      matched = true;
    }

    const identificationColor = STRUCTURED_NOTE_LINE.identificationColor.exec(line);
    if (identificationColor) {
      fields.identificationColor = identificationColor[1].trim();
      matched = true;
    }

    if (STRUCTURED_NOTE_LINE.isForeigner.test(line)) {
      fields.isForeigner = true;
      matched = true;
    }

    if (!matched) {
      userLines.push(rawLine);
    }
  }

  return {
    userNotes: userLines.join('\n').trim(),
    fields,
  };
}

export const emptyFeegowSchedulingItem = (): FeegowPatientSchedulingItem => ({
  professionalId: '',
  specialtyName: '',
  procedureName: '',
  weekday: '',
  time: '',
  unit: '',
});

export const emptyFeegowPatientForm = (): FeegowPatientFormState => ({
  fullName: '',
  socialName: '',
  cpf: '',
  birthDate: '',
  gender: 0,
  email: '',
  phone: '',
  mobilePhone: '',
  addressStreet: '',
  addressNumber: '',
  addressComplement: '',
  addressNeighborhood: '',
  addressCity: '',
  addressState: '',
  addressZipCode: '',
  motherName: '',
  emergencyContactName: '',
  emergencyContactPhone: '',
  emergencyContactRelationship: '',
  notes: '',
  photoData: undefined,
  rg: '',
  nationality: 'Brasileira',
  bloodType: '',
  occupation: '',
  maritalStatus: '',
  birthPlace: '',
  insurances: [],
  isForeigner: false,
  noCpf: false,
  legalResponsible: emptyLegalResponsible(),
  identificationColor: '#1a1a1a',
  priority: '',
  heightCm: '',
  weightKg: '',
  imc: '',
  chartNumber: '',
  phone2: '',
  mobilePhone2: '',
  email2: '',
  education: '',
  origin: '',
  referral: '',
  religion: '',
  skinColor: '',
  country: 'Brasil',
  warnings: '',
  cns: '',
  schedulingPrograms: [],
});

export function parseSchedulingFromNotes(notes?: string): FeegowPatientSchedulingItem[] {
  if (!notes?.trim()) return [];

  return notes
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.startsWith(SCHEDULING_NOTE_PREFIX))
    .map((line) => {
      const parts = line.slice(SCHEDULING_NOTE_PREFIX.length).split(' | ');
      return {
        weekday: parts[0] ?? '',
        time: parts[1] ?? '',
        specialtyName: parts[2] ?? '',
        professionalId: parts[3] ?? '',
        procedureName: parts[4] ?? '',
        unit: parts[5] ?? '',
      };
    });
}

export function stripSchedulingLines(notes?: string): string {
  return stripFeegowStructuredLines(notes);
}

function formatSchedulingLine(item: FeegowPatientSchedulingItem): string {
  return [
    SCHEDULING_NOTE_PREFIX,
    item.weekday,
    item.time,
    item.specialtyName,
    item.professionalId,
    item.procedureName,
    item.unit,
  ].join(' | ');
}

export function computeImc(heightCm: string, weightKg: string): string {
  const h = Number(heightCm.replace(',', '.'));
  const w = Number(weightKg.replace(',', '.'));
  if (!h || !w || h <= 0) return '';
  const meters = h / 100;
  const imc = w / (meters * meters);
  return Number.isFinite(imc) ? imc.toFixed(1).replace('.', ',') : '';
}

export function birthLabelFromDate(birthDate: string): string {
  if (!birthDate) return 'Nascimento não informado';
  try {
    const [y, m, d] = birthDate.split('-').map(Number);
    const date = new Date(y, m - 1, d);
    return date.toLocaleDateString('pt-BR');
  } catch {
    return 'Nascimento não informado';
  }
}

export function normalizeInsurances(items: PatientInsuranceInput[]): PatientInsuranceInput[] {
  return items
    .filter((i) => i.healthInsuranceId && i.cardNumber.trim())
    .map((i) => ({
      healthInsuranceId: i.healthInsuranceId,
      cardNumber: i.cardNumber.trim(),
      planName: i.planName?.trim() || undefined,
      cardHolderName: i.cardHolderName?.trim() || undefined,
      productCode: i.productCode?.trim() || undefined,
      cnsNumber: i.cnsNumber?.trim() || undefined,
      accommodationType: i.accommodationType?.trim() || undefined,
      validFrom: i.validFrom || undefined,
      validUntil: i.validUntil || undefined,
      isPrimary: i.isPrimary,
    }));
}

export function formatFeegowPatientFormFields(
  form: Partial<FeegowPatientFormState>,
): Partial<FeegowPatientFormState> {
  return {
    ...form,
    cpf: form.cpf != null ? formatCpfInput(form.cpf) : form.cpf,
    phone: form.phone != null ? formatPhoneInput(form.phone) : form.phone,
    phone2: form.phone2 != null ? formatPhoneInput(form.phone2) : form.phone2,
    mobilePhone: form.mobilePhone != null ? formatPhoneInput(form.mobilePhone) : form.mobilePhone,
    mobilePhone2: form.mobilePhone2 != null ? formatPhoneInput(form.mobilePhone2) : form.mobilePhone2,
    addressZipCode: form.addressZipCode != null ? formatCep(form.addressZipCode) : form.addressZipCode,
    rg: form.rg != null ? formatRgInput(form.rg) : form.rg,
    cns: form.cns != null ? formatCnsInput(form.cns) : form.cns,
  };
}

export function feegowFormToCreatePayload(form: FeegowPatientFormState): CreatePatientRequest {
  const phoneDigits = (value?: string) => {
    const digits = onlyDigits(value ?? '');
    return digits || undefined;
  };

  const extraLines = [
    form.heightCm ? `Altura: ${form.heightCm} cm` : '',
    form.weightKg ? `Peso: ${form.weightKg} kg` : '',
    form.imc ? `IMC: ${form.imc}` : '',
    form.education ? `Escolaridade: ${form.education}` : '',
    form.origin ? `Origem: ${form.origin}` : '',
    form.referral ? `Indicação: ${form.referral}` : '',
    form.religion ? `Religião: ${form.religion}` : '',
    form.skinColor ? `Cor da pele: ${form.skinColor}` : '',
    form.country && form.country !== 'Brasil' ? `País: ${form.country}` : '',
    form.phone2 ? `Telefone 2: ${phoneDigits(form.phone2)}` : '',
    form.mobilePhone2 ? `Celular 2: ${phoneDigits(form.mobilePhone2)}` : '',
    form.email2 ? `E-mail 2: ${form.email2}` : '',
    form.warnings ? `Avisos e pendências: ${form.warnings}` : '',
    form.isForeigner ? 'Paciente estrangeiro' : '',
    form.chartNumber ? `Prontuário: ${form.chartNumber}` : '',
    form.priority ? `Prioridade: ${form.priority}` : '',
    form.identificationColor && form.identificationColor !== '#1a1a1a'
      ? `Cor de identificação: ${form.identificationColor}`
      : '',
    ...form.schedulingPrograms
      .filter((item) => item.weekday || item.time || item.professionalId || item.procedureName)
      .map(formatSchedulingLine),
  ].filter(Boolean);

  const baseNotes = stripFeegowStructuredLines(form.notes);
  const notes = [baseNotes, ...extraLines].filter(Boolean).join('\n');

  const insurances = normalizeInsurances(form.insurances ?? []);
  if (form.cns.trim() && insurances.length === 0) {
    // CNS fica disponível quando convênio for cadastrado depois
  } else if (form.cns.trim() && insurances[0]) {
    insurances[0] = { ...insurances[0], cnsNumber: onlyDigits(form.cns) };
  }

  return {
    fullName: form.fullName.trim(),
    usesResponsibleCpf: form.noCpf,
    cpf: form.noCpf ? onlyDigits(form.legalResponsible.cpf) : onlyDigits(form.cpf),
    legalResponsible: form.noCpf ? mapLegalResponsibleInput(form.legalResponsible) : undefined,
    birthDate: form.birthDate,
    gender: form.gender,
    socialName: form.socialName?.trim() || undefined,
    email: form.email?.trim() || undefined,
    phone: phoneDigits(form.phone),
    mobilePhone: phoneDigits(form.mobilePhone),
    addressStreet: form.addressStreet?.trim() || undefined,
    addressNumber: form.addressNumber?.trim() || undefined,
    addressComplement: form.addressComplement?.trim() || undefined,
    addressNeighborhood: form.addressNeighborhood?.trim() || undefined,
    addressCity: form.addressCity?.trim() || undefined,
    addressState: form.addressState?.trim() || undefined,
    addressZipCode: form.addressZipCode?.trim() || undefined,
    motherName: form.motherName?.trim() || undefined,
    emergencyContactName: form.emergencyContactName?.trim() || undefined,
    emergencyContactPhone: form.emergencyContactPhone?.trim() || undefined,
    emergencyContactRelationship: form.emergencyContactRelationship?.trim() || undefined,
    notes: notes || undefined,
    photoData: form.photoData || undefined,
    rg: form.rg?.trim() || undefined,
    nationality: form.isForeigner ? (form.nationality?.trim() || 'Estrangeiro') : (form.nationality?.trim() || undefined),
    bloodType: form.bloodType?.trim() || undefined,
    occupation: form.occupation?.trim() || undefined,
    maritalStatus: form.maritalStatus?.trim() || undefined,
    birthPlace: form.birthPlace?.trim() || undefined,
    insurances,
  };
}

function mapLegalResponsibleInput(form: LegalResponsibleFormState): LegalResponsibleInput {
  return {
    name: form.name.trim(),
    cpf: onlyDigits(form.cpf),
    birthDate: form.birthDate,
    relationship: form.relationship,
    rg: form.rg.trim(),
    authorizationDocumentType:
      form.relationship === 4 && form.authorizationDocumentType > 0
        ? form.authorizationDocumentType
        : undefined,
    authorizationDocumentReference:
      form.relationship === 4 ? form.authorizationDocumentReference.trim() || undefined : undefined,
  };
}
