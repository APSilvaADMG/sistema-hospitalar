export type FuniFieldRule = {
  required?: boolean;
  maxLength?: number;
  pattern?: RegExp;
  hint?: string;
};

export type Funi21ConsultationForm = {
  ansRegistration: string;
  providerGuideNumber: string;
  operatorGuideNumber: string;
  beneficiaryCardNumber: string;
  cardValidity: string;
  newbornCare: '' | 'S' | 'N';
  beneficiaryName: string;
  beneficiaryCns: string;
  providerOperatorCode: string;
  contractedName: string;
  cnesCode: string;
  executingProfessionalName: string;
  professionalCouncil: string;
  councilNumber: string;
  councilUf: string;
  cboCode: string;
  accidentIndicator: string;
  attendanceDate: string;
  consultationType: string;
  procedureTable: string;
  procedureCode: string;
  procedureDescription: string;
  procedureValue: string;
  observation: string;
  executingProfessionalSignature: string;
  beneficiarySignature: string;
};

export const emptyFuni21Form = (): Funi21ConsultationForm => ({
  ansRegistration: '',
  providerGuideNumber: '',
  operatorGuideNumber: '',
  beneficiaryCardNumber: '',
  cardValidity: '',
  newbornCare: '',
  beneficiaryName: '',
  beneficiaryCns: '',
  providerOperatorCode: '',
  contractedName: 'APSMedCore — Unidade Hospitalar',
  cnesCode: '',
  executingProfessionalName: '',
  professionalCouncil: 'CRM',
  councilNumber: '',
  councilUf: '',
  cboCode: '',
  accidentIndicator: '0',
  attendanceDate: new Date().toISOString().slice(0, 10),
  consultationType: '1',
  procedureTable: '22',
  procedureCode: '',
  procedureDescription: '',
  procedureValue: '',
  observation: '',
  executingProfessionalSignature: '',
  beneficiarySignature: '',
});

/** Campos FUNI 21 mapeados para TISS guiaConsulta (ANS 4.03). */
export const FUNI21_FIELD_RULES: Record<keyof Funi21ConsultationForm, FuniFieldRule> = {
  ansRegistration: { required: true, maxLength: 8, hint: 'Registro ANS da operadora (campo 1)' },
  providerGuideNumber: { required: true, maxLength: 20, hint: 'Nº guia no prestador (campo 2)' },
  operatorGuideNumber: { maxLength: 20, hint: 'Nº guia atribuído pela operadora (campo 3)' },
  beneficiaryCardNumber: { required: true, maxLength: 20, hint: 'Número da carteirinha (campo 4)' },
  cardValidity: { pattern: /^\d{4}-\d{2}-\d{2}$/, hint: 'Validade da carteira (campo 5)' },
  newbornCare: { hint: 'Atendimento RN — S ou N (campo 6)' },
  beneficiaryName: { required: true, maxLength: 70, hint: 'Nome do beneficiário (campo 7)' },
  beneficiaryCns: { maxLength: 15, pattern: /^\d{0,15}$/, hint: 'CNS (campo 8)' },
  providerOperatorCode: { maxLength: 14, hint: 'Código do prestador na operadora (campo 9)' },
  contractedName: { required: true, maxLength: 70, hint: 'Nome do contratado (campo 10)' },
  cnesCode: { maxLength: 7, pattern: /^\d{0,7}$/, hint: 'CNES (campo 11)' },
  executingProfessionalName: { required: true, maxLength: 70, hint: 'Profissional executante (campo 12)' },
  professionalCouncil: { maxLength: 12, hint: 'Conselho — CRM, CRO… (campo 13)' },
  councilNumber: { required: true, maxLength: 15, hint: 'Nº no conselho (campo 14)' },
  councilUf: { required: true, maxLength: 2, hint: 'UF do conselho (campo 15)' },
  cboCode: { maxLength: 6, pattern: /^\d{0,6}$/, hint: 'CBO 2002 (campo 16)' },
  accidentIndicator: { required: true, hint: '0=Não se aplica; 1=Trabalho; 2=Trânsito; 3=Outros (campo 17)' },
  attendanceDate: { required: true, hint: 'Data do atendimento (campo 18)' },
  consultationType: { required: true, hint: '1=Primeira; 2=Retorno; 3=Pré-natal; 4=Referência (campo 19)' },
  procedureTable: { required: true, maxLength: 2, hint: 'Tabela TUSS — ex.: 22 (campo 20)' },
  procedureCode: { required: true, maxLength: 10, hint: 'Código TUSS (campo 21)' },
  procedureDescription: { maxLength: 150 },
  procedureValue: { required: true, hint: 'Valor do procedimento (campo 22)' },
  observation: { maxLength: 500, hint: 'Observação / justificativa (campo 23)' },
  executingProfessionalSignature: { hint: 'Assinatura manuscrita do profissional (campo 24)' },
  beneficiarySignature: { hint: 'Assinatura manuscrita do beneficiário (campo 25)' },
};

export const consultationTypeOptions = [
  { value: '1', label: '1 — Primeira consulta' },
  { value: '2', label: '2 — Retorno' },
  { value: '3', label: '3 — Pré-natal' },
  { value: '4', label: '4 — Encaminhamento / referência' },
];

export const accidentIndicatorOptions = [
  { value: '0', label: '0 — Não se aplica' },
  { value: '1', label: '1 — Acidente de trabalho' },
  { value: '2', label: '2 — Acidente de trânsito' },
  { value: '3', label: '3 — Outros acidentes' },
];

export function validateFuni21Form(form: Funi21ConsultationForm): string[] {
  const errors: string[] = [];
  (Object.keys(FUNI21_FIELD_RULES) as (keyof Funi21ConsultationForm)[]).forEach((key) => {
    const rule = FUNI21_FIELD_RULES[key];
    const value = String(form[key] ?? '').trim();
    if (rule.required && !value) errors.push(`${key}: obrigatório`);
    if (rule.maxLength && value.length > rule.maxLength) errors.push(`${key}: máximo ${rule.maxLength} caracteres`);
    if (rule.pattern && value && !rule.pattern.test(value)) errors.push(`${key}: formato inválido`);
  });
  const amount = Number(form.procedureValue.replace(',', '.'));
  if (!Number.isFinite(amount) || amount < 0) errors.push('procedureValue: valor inválido');
  return errors;
}
