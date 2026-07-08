export type AnamnesisData = {
  chiefComplaint: string;
  illnessHistory: string;
  personalHistory: string;
  familyHistory: string;
  surgicalHistory: string;
  medicationsInUse: string;
  allergies: string;
  smoking: '' | 'nao' | 'sim' | 'ex';
  alcohol: '' | 'nao' | 'social' | 'sim';
  illicitDrugs: '' | 'nao' | 'sim';
  physicalActivity: string;
  diet: string;
  systemsReview: string;
  vitalSigns: string;
  physicalExam: string;
  hypothesis: string;
  conduct: string;
  freeNotes: string;
};

export const emptyAnamnesis = (): AnamnesisData => ({
  chiefComplaint: '',
  illnessHistory: '',
  personalHistory: '',
  familyHistory: '',
  surgicalHistory: '',
  medicationsInUse: '',
  allergies: '',
  smoking: '',
  alcohol: '',
  illicitDrugs: '',
  physicalActivity: '',
  diet: '',
  systemsReview: '',
  vitalSigns: '',
  physicalExam: '',
  hypothesis: '',
  conduct: '',
  freeNotes: '',
});

export const ENTRY_TYPE_HINTS: Record<number, string> = {
  1: 'Anamnese estruturada — preencha as seções ou use textos pré-definidos.',
  2: 'Evolução diária — estado atual, exame, conduta.',
  3: 'Prescrição — selecione medicamentos do catálogo e complemente.',
  4: 'Solicitação de exames — laboratório e/ou imagem.',
  5: 'Procedimento — descreva o realizado e intercorrências.',
};

export const TEXT_TEMPLATES: Record<number, string[]> = {
  2: [
    'Paciente estável, sem queixas ativas no momento.',
    'Evolução favorável, mantendo conduta vigente.',
    'Paciente referindo melhora parcial dos sintomas.',
    'Sem intercorrências nas últimas 24 horas.',
    'Mantém acompanhamento clínico e reavaliação programada.',
    'Orientações reforçadas; família ciente do plano terapêutico.',
  ],
  3: [
    'Manter medicações de uso contínuo.',
    'Hidratação oral e repouso relativo.',
    'Retorno em 7 dias ou se piora.',
    'Uso conforme orientação médica; não suspender sem avaliação.',
  ],
  4: [
    'Solicito exames para investigação diagnóstica conforme quadro clínico.',
    'Exames de controle conforme evolução.',
    'Jejum de 8 horas para coleta laboratorial.',
  ],
  5: [
    'Procedimento realizado sem intercorrências imediatas.',
    'Paciente tolerou bem o procedimento.',
    'Material encaminhado conforme protocolo institucional.',
    'Curativo realizado; orientações de cuidado domiciliar fornecidas.',
  ],
};

export const ANAMNESIS_SNIPPETS: Partial<Record<keyof AnamnesisData, string[]>> = {
  illnessHistory: [
    'Início há ___ dias, de forma progressiva, sem fatores de melhora.',
    'Início súbito, sem relação com esforço ou trauma.',
    'Quadro associado a febre, mal-estar e perda de apetite.',
    'Sintomas pioram ao final do dia; nega trauma.',
  ],
  personalHistory: [
    'HAS em tratamento. DM2 em acompanhamento.',
    'Nega comorbidades conhecidas.',
    'Asma na infância, sem crises recentes.',
  ],
  familyHistory: [
    'Pai com HAS. Mãe com DM2.',
    'Negativa para doenças hereditárias relevantes.',
    'História familiar de cardiopatia precoce.',
  ],
  surgicalHistory: [
    'Nega cirurgias prévias.',
    'Apendicectomia há ___ anos, sem intercorrências.',
  ],
  medicationsInUse: [
    'Nega uso regular de medicações.',
    'Losartana 50 mg 1x/dia. Metformina 850 mg 2x/dia.',
  ],
  allergies: [
    'Nega alergias medicamentosas conhecidas.',
    'Alergia a dipirona (rash cutâneo).',
    'Alergia a penicilina (anafilaxia).',
  ],
  systemsReview: [
    'Nega dispneia, dor torácica, síncope ou edema.',
    'Nega alterações urinárias, gastrointestinais ou neurológicas.',
    'Revisão de sistemas sem outras queixas.',
  ],
  vitalSigns: [
    'PA ___x___ mmHg | FC ___ bpm | FR ___ irpm | SatO₂ ___% | T ___°C',
    'Estável hemodinamicamente à admissão.',
  ],
  physicalExam: [
    'BEG, corado, hidratado, acianótico, anictérico.',
    'ACV: RCR em 2T, BNF, sem sopros.',
    'AR: MV+ bilateralmente, sem RA.',
    'Abdome flácido, indolor, RHA+.',
    'Extremidades sem edema; perfusão periférica preservada.',
  ],
  hypothesis: [
    'Hipótese diagnóstica principal conforme CID selecionado.',
    'Diagnósticos diferenciais a esclarecer com exames complementares.',
  ],
  conduct: [
    'Internação para investigação e suporte.',
    'Medicação sintomática e reavaliação clínica.',
    'Solicitação de exames complementares.',
    'Alta com orientações e retorno programado.',
  ],
};

function habitLabel(smoking: AnamnesisData['smoking'], alcohol: AnamnesisData['alcohol'], drugs: AnamnesisData['illicitDrugs']): string {
  const parts: string[] = [];
  if (smoking === 'nao') parts.push('Tabagismo: não');
  else if (smoking === 'sim') parts.push('Tabagismo: sim');
  else if (smoking === 'ex') parts.push('Tabagismo: ex-tabagista');
  if (alcohol === 'nao') parts.push('Etilismo: não');
  else if (alcohol === 'social') parts.push('Etilismo: social');
  else if (alcohol === 'sim') parts.push('Etilismo: sim');
  if (drugs === 'nao') parts.push('Drogas ilícitas: nega');
  else if (drugs === 'sim') parts.push('Drogas ilícitas: relata uso');
  return parts.join(' | ');
}

function section(title: string, value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? `${title}:\n${trimmed}` : null;
}

export function buildAnamnesisContent(data: AnamnesisData): string {
  const habits = habitLabel(data.smoking, data.alcohol, data.illicitDrugs);
  const blocks = [
    section('QP (Queixa principal)', data.chiefComplaint),
    section('HDA (História da doença atual)', data.illnessHistory),
    section('Antecedentes pessoais', data.personalHistory),
    section('Antecedentes familiares', data.familyHistory),
    section('Antecedentes cirúrgicos', data.surgicalHistory),
    section('Medicações em uso', data.medicationsInUse),
    section('Alergias', data.allergies),
    habits ? section('Hábitos de vida', habits) : null,
    section('Atividade física', data.physicalActivity),
    section('Alimentação', data.diet),
    section('Revisão de sistemas', data.systemsReview),
    section('Sinais vitais', data.vitalSigns),
    section('Exame físico', data.physicalExam),
    section('Hipótese diagnóstica', data.hypothesis),
    section('Conduta', data.conduct),
    section('Observações adicionais', data.freeNotes),
  ].filter((b): b is string => Boolean(b));
  return blocks.join('\n\n');
}

export function buildEvolutionContent(freeText: string, extraSections: { subjective?: string; objective?: string; assessment?: string; plan?: string }): string {
  const blocks = [
    section('Subjetivo (S)', extraSections.subjective ?? ''),
    section('Objetivo (O)', extraSections.objective ?? ''),
    section('Avaliação (A)', extraSections.assessment ?? ''),
    section('Plano (P)', extraSections.plan ?? ''),
    section('Texto livre', freeText),
  ].filter((b): b is string => Boolean(b));
  return blocks.join('\n\n');
}

export type EvolutionSoap = {
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
};

export const emptyEvolutionSoap = (): EvolutionSoap => ({
  subjective: '',
  objective: '',
  assessment: '',
  plan: '',
});

const ANAMNESIS_TITLE_TO_FIELD: Partial<Record<string, keyof AnamnesisData>> = {
  'QP (Queixa principal)': 'chiefComplaint',
  'HDA (História da doença atual)': 'illnessHistory',
  'Antecedentes pessoais': 'personalHistory',
  'Antecedentes familiares': 'familyHistory',
  'Antecedentes cirúrgicos': 'surgicalHistory',
  'Medicações em uso': 'medicationsInUse',
  Alergias: 'allergies',
  'Atividade física': 'physicalActivity',
  Alimentação: 'diet',
  'Revisão de sistemas': 'systemsReview',
  'Sinais vitais': 'vitalSigns',
  'Exame físico': 'physicalExam',
  'Hipótese diagnóstica': 'hypothesis',
  Conduta: 'conduct',
  'Observações adicionais': 'freeNotes',
};

const EVOLUTION_TITLE_TO_FIELD: Partial<Record<string, keyof EvolutionSoap>> = {
  'Subjetivo (S)': 'subjective',
  'Objetivo (O)': 'objective',
  'Avaliação (A)': 'assessment',
  'Plano (P)': 'plan',
};

function parseStoredSections(content: string): Record<string, string> {
  const result: Record<string, string> = {};
  for (const block of content.split(/\n\n+/)) {
    const trimmed = block.trim();
    if (!trimmed) continue;
    const colonIdx = trimmed.indexOf(':');
    if (colonIdx === -1) continue;
    const title = trimmed.slice(0, colonIdx).trim();
    const body = trimmed.slice(colonIdx + 1).replace(/^\n/, '').trim();
    if (title) result[title] = body;
  }
  return result;
}

function applyHabitsFromStored(line: string, data: AnamnesisData): void {
  if (line.includes('Tabagismo: não')) data.smoking = 'nao';
  else if (line.includes('Tabagismo: sim')) data.smoking = 'sim';
  else if (line.includes('Tabagismo: ex-tabagista')) data.smoking = 'ex';
  if (line.includes('Etilismo: não')) data.alcohol = 'nao';
  else if (line.includes('Etilismo: social')) data.alcohol = 'social';
  else if (line.includes('Etilismo: sim')) data.alcohol = 'sim';
  if (line.includes('Drogas ilícitas: nega')) data.illicitDrugs = 'nao';
  else if (line.includes('Drogas ilícitas: relata uso')) data.illicitDrugs = 'sim';
}

function assignAnamnesisField(data: AnamnesisData, field: keyof AnamnesisData, value: string): void {
  if (field === 'smoking') {
    data.smoking = value as AnamnesisData['smoking'];
    return;
  }
  if (field === 'alcohol') {
    data.alcohol = value as AnamnesisData['alcohol'];
    return;
  }
  if (field === 'illicitDrugs') {
    data.illicitDrugs = value as AnamnesisData['illicitDrugs'];
    return;
  }
  data[field] = value;
}

export function seedAnamnesisFromPatient(
  patient: { occupation?: string; bloodType?: string; maritalStatus?: string; notes?: string } | null | undefined,
): AnamnesisData {
  const base = emptyAnamnesis();
  if (!patient) return base;
  const personal: string[] = [];
  if (patient.occupation) personal.push(`Ocupação: ${patient.occupation}`);
  if (patient.bloodType) personal.push(`Tipo sanguíneo: ${patient.bloodType}`);
  if (patient.maritalStatus) personal.push(`Estado civil: ${patient.maritalStatus}`);
  if (personal.length) base.personalHistory = `${personal.join('. ')}.`;
  if (patient.notes?.toLowerCase().includes('alerg')) base.allergies = patient.notes;
  return base;
}

/** Reidrata campos estruturados a partir do texto salvo no prontuário. */
export function hydrateAnamnesisFromStored(
  content: string,
  patient?: { occupation?: string; bloodType?: string; maritalStatus?: string; notes?: string } | null,
): AnamnesisData {
  const base = seedAnamnesisFromPatient(patient);
  const sections = parseStoredSections(content);
  if (Object.keys(sections).length === 0) {
    if (content.trim()) base.freeNotes = content.trim();
    return base;
  }

  for (const [title, body] of Object.entries(sections)) {
    if (title === 'Hábitos de vida') {
      applyHabitsFromStored(body, base);
      continue;
    }
    const field = ANAMNESIS_TITLE_TO_FIELD[title];
    if (field) {
      assignAnamnesisField(base, field, body);
    } else if (title !== 'Texto livre') {
      base.freeNotes = base.freeNotes
        ? `${base.freeNotes}\n\n${title}:\n${body}`
        : `${title}:\n${body}`;
    }
  }
  return base;
}

/** Reidrata evolução SOAP a partir do texto salvo no prontuário. */
export function hydrateEvolutionFromStored(content: string): { soap: EvolutionSoap; freeText: string } {
  const soap = emptyEvolutionSoap();
  let freeText = '';
  const sections = parseStoredSections(content);
  if (Object.keys(sections).length === 0) {
    freeText = content.trim();
    return { soap, freeText };
  }

  for (const [title, body] of Object.entries(sections)) {
    if (title === 'Texto livre') {
      freeText = body;
      continue;
    }
    const field = EVOLUTION_TITLE_TO_FIELD[title];
    if (field) soap[field] = body;
    else freeText = freeText ? `${freeText}\n\n${title}:\n${body}` : `${title}:\n${body}`;
  }
  return { soap, freeText };
}
