import type { ModuleTab } from './useModuleSection';

export type PatientWorkspaceModuleId =
  | 'reception'
  | 'ambulatory'
  | 'emergency'
  | 'hospitalization'
  | 'icu'
  | 'surgery'
  | 'pep'
  | 'nursing'
  | 'ccih';

/** Abas do paciente — informações pertinentes em uma única tela por módulo. */
export const patientWorkspaceTabs: Record<PatientWorkspaceModuleId, ModuleTab[]> = {
  reception: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'cadastro', label: 'Cadastro' },
    { slug: 'agendamentos', label: 'Agendamentos' },
    { slug: 'documentos', label: 'Documentos' },
    { slug: 'convenio', label: 'Convênio' },
  ],
  ambulatory: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'consultas', label: 'Consultas' },
    { slug: 'evolucoes', label: 'Evoluções' },
    { slug: 'prescricoes', label: 'Prescrições' },
    { slug: 'exames', label: 'Exames' },
  ],
  emergency: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'triagem', label: 'Triagem' },
    { slug: 'atendimento', label: 'Atendimento' },
    { slug: 'evolucoes', label: 'Evoluções' },
    { slug: 'prescricoes', label: 'Prescrições' },
    { slug: 'alta', label: 'Alta' },
  ],
  hospitalization: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'internacao', label: 'Internação' },
    { slug: 'evolucoes', label: 'Evoluções' },
    { slug: 'prescricoes', label: 'Prescrições' },
    { slug: 'exames', label: 'Exames' },
    { slug: 'alta', label: 'Alta' },
  ],
  icu: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'monitorizacao', label: 'Monitorização' },
    { slug: 'evolucoes', label: 'Evoluções' },
    { slug: 'escalas', label: 'Escalas' },
  ],
  surgery: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'cirurgias', label: 'Cirurgias' },
    { slug: 'pre-op', label: 'Pré-operatório' },
    { slug: 'evolucoes', label: 'Evoluções' },
  ],
  pep: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'evolucoes', label: 'Evoluções' },
    { slug: 'prescricoes', label: 'Prescrições' },
    { slug: 'diagnosticos', label: 'CID' },
    { slug: 'exames', label: 'Exames' },
    { slug: 'anexos', label: 'Anexos' },
  ],
  nursing: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'sae', label: 'SAE' },
    { slug: 'medicamentos', label: 'Medicamentos' },
    { slug: 'sinais-vitais', label: 'Sinais Vitais' },
    { slug: 'curativos', label: 'Curativos' },
  ],
  ccih: [
    { slug: 'resumo', label: 'Resumo' },
    { slug: 'vigilancia', label: 'Vigilância' },
    { slug: 'isolamento', label: 'Isolamento' },
    { slug: 'infeccoes', label: 'Infecções' },
  ],
};

export const receptionHubTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral' },
  { slug: 'pacientes', label: 'Pacientes' },
  { slug: 'agendamentos', label: 'Agendamentos' },
  { slug: 'check-in', label: 'Check-in' },
  { slug: 'registro-nascimento', label: 'Registro de Nascimento' },
  { slug: 'convenios', label: 'Convênios' },
];

export const ambulatoryHubTabs: ModuleTab[] = [
  { slug: '', label: 'Visão Geral' },
  { slug: 'agenda', label: 'Agenda' },
  { slug: 'consultorios', label: 'Consultórios' },
  { slug: 'atendimentos', label: 'Atendimentos' },
];

export const PATIENT_QUERY_KEY = 'paciente';
export const PATIENT_VIEW_QUERY_KEY = 'visao';

/** Abas do paciente que exibem formulário/fila operacional em vez do painel estático. */
export const moduleOperationalViews: Partial<Record<PatientWorkspaceModuleId, string[]>> = {
  emergency: ['triagem', 'atendimento', 'evolucoes', 'prescricoes', 'alta'],
  pep: ['evolucoes', 'prescricoes', 'diagnosticos', 'exames', 'anexos'],
  nursing: ['sae', 'medicamentos', 'sinais-vitais', 'curativos'],
};
