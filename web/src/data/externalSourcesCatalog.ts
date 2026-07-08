/** Catálogo de repositórios e features open source mapeadas para o APSMedCore. */
export type ExternalSourceStatus = 'integrated' | 'partial' | 'reference' | 'planned';

export type ExternalFeatureMapping = {
  external: string;
  localModule: string;
  localPath?: string;
  reportCodes?: string[];
  status: ExternalSourceStatus;
  notes?: string;
};

export type ExternalSource = {
  id: string;
  name: string;
  url: string;
  license: string;
  stack: string;
  description: string;
  features: ExternalFeatureMapping[];
};

export const EXTERNAL_SOURCES: ExternalSource[] = [
  {
    id: 'hospitalrun',
    name: 'HospitalRun',
    url: 'https://github.com/HospitalRun/hospitalrun-frontend',
    license: 'MIT',
    stack: 'React + PouchDB',
    description: 'HMS open source com pacientes, agendamento, labs, medicamentos, imagens e incidentes.',
    features: [
      {
        external: 'Patients',
        localModule: 'Pacientes',
        localPath: '/pacientes',
        status: 'integrated',
      },
      {
        external: 'Scheduling',
        localModule: 'Agendamentos',
        localPath: '/agendamentos',
        status: 'integrated',
      },
      {
        external: 'Labs',
        localModule: 'Laboratório',
        localPath: '/laboratorio',
        reportCodes: ['lab.orders.requested', 'lab.orders.completed', 'lab.production'],
        status: 'integrated',
      },
      {
        external: 'Medications',
        localModule: 'Farmácia',
        localPath: '/farmacia',
        reportCodes: ['pharmacy.stock.current', 'pharmacy.dispensed', 'pharmacy.expiring-soon'],
        status: 'integrated',
      },
      {
        external: 'Imagings',
        localModule: 'Diagnóstico por imagem',
        localPath: '/imagem',
        reportCodes: ['img.xray', 'img.ct', 'img.mri', 'img.ultrasound'],
        status: 'integrated',
      },
      {
        external: 'Incidents',
        localModule: 'Qualidade / Segurança',
        localPath: '/qualidade',
        reportCodes: ['quality.adverse-events'],
        status: 'partial',
        notes: 'Incidentes mapeados para SecurityIncident e eventos adversos.',
      },
    ],
  },
  {
    id: 'epirhandbook',
    name: 'EpiR Handbook (PT)',
    url: 'https://www.epirhandbook.com/pt/',
    license: 'CC BY-NC-SA 4.0',
    stack: 'R / documentação',
    description:
      'Manual de R para epidemiologia aplicada (Applied Epi/OMS/MSF): vigilância, surtos, indicadores e relatórios reproduzíveis.',
    features: [
      {
        external: 'R Markdown — relatórios parametrizados',
        localModule: 'Central de Relatórios',
        localPath: '/relatorios',
        reportCodes: [
          'ccih.epidemic.curve',
          'ccih.mortality.surveillance',
          'ccih.outbreak.indicators',
        ],
        status: 'reference',
        notes:
          'Padrão YAML + narrativa + gráficos; reimplementado em ReportsService + impressão A4 (sem runtime R).',
      },
      {
        external: 'Relatórios de rotina / vigilância',
        localModule: 'CCIH',
        localPath: '/ccih',
        reportCodes: [
          'ccih.infection-rate',
          'ccih.monitored-cases',
          'ccih.antibiotics',
        ],
        status: 'reference',
        notes: 'Estrutura sitrep (semana epidemiológica, filtros por unidade/período).',
      },
      {
        external: 'Terminologia epidemiológica (PT)',
        localModule: 'CCIH / Qualidade / Regulatório',
        reportCodes: ['reg.compulsory-notifications', 'quality.indicators'],
        status: 'reference',
        notes: 'Glossário e fluxos de investigação; alinhar rótulos em reportFieldMappings.',
      },
      {
        external: 'R4EPI outbreak templates',
        localModule: 'CCIH',
        status: 'reference',
        notes: 'Cap. R Markdown referencia R4EPI; templates concretos em sitrep (GPL-3.0).',
      },
    ],
  },
  {
    id: 'sitrep',
    name: 'R4EPI sitrep',
    url: 'https://github.com/R4EPI/sitrep',
    license: 'GPL-3.0',
    stack: 'R / RMarkdown',
    description: 'Templates OMS/MSF para situação epidemiológica (cólera, sarampo, mortalidade, vacinação).',
    features: [
      {
        external: 'Epidemic curve',
        localModule: 'CCIH',
        reportCodes: ['ccih.epidemic.curve'],
        status: 'integrated',
        notes:
          'Curva semanal de infecções hospitalares; export CSV compatível com R. Metodologia: EpiR Handbook cap. R Markdown.',
      },
      {
        external: 'Mortality surveillance',
        localModule: 'CCIH / Internação',
        reportCodes: ['ccih.mortality.surveillance', 'hosp.deaths', 'bi.deaths'],
        status: 'integrated',
      },
      {
        external: 'Vaccination coverage',
        localModule: 'Prontuário / CCIH',
        reportCodes: ['ccih.vaccination.coverage', 'pep.vaccinations'],
        status: 'integrated',
      },
      {
        external: 'Outbreak indicators',
        localModule: 'CCIH',
        reportCodes: ['ccih.outbreak.indicators', 'ccih.infection-rate'],
        status: 'integrated',
      },
      {
        external: 'Cholera / Measles sitreps',
        localModule: 'CCIH',
        status: 'reference',
        notes: 'Reimplementação nativa; templates R não embarcados (GPL-3.0).',
      },
    ],
  },
  {
    id: 'epimodel',
    name: 'EpiModel',
    url: 'https://github.com/EpiModel/EpiModel',
    license: 'GPL-3.0',
    stack: 'R',
    description: 'Modelagem estatística de epidemias (SIR, parceiros, redes).',
    features: [
      {
        external: 'Network / compartment models',
        localModule: 'CCIH / BI',
        reportCodes: ['ccih.epidemic.curve'],
        status: 'reference',
        notes: 'Export CSV agregado para análise externa em R.',
      },
    ],
  },
  {
    id: 'database-hospital',
    name: 'DataBase-Hospital (FabiolaCosta)',
    url: 'https://github.com/FabiolaCosta/DataBase-Hospital',
    license: 'MIT',
    stack: 'MySQL',
    description: 'Schema ER hospitalar clássico (paciente, médico, internação, leito).',
    features: [
      {
        external: 'Paciente / Médico / Internação',
        localModule: 'Core clínico',
        localPath: '/internacao',
        status: 'integrated',
        notes: 'Entidades EF Core cobrem e expandem o modelo MySQL.',
      },
      {
        external: 'Leito / Ala',
        localModule: 'Internação',
        reportCodes: ['hosp.beds.occupancy', 'hosp.beds.turnover', 'admin.beds.occupancy'],
        status: 'integrated',
      },
    ],
  },
  {
    id: 'dev-queiroz',
    name: 'sistema-hospitalar (dev-queiroz)',
    url: 'https://github.com/APSilvaADMG/sistema-hospitalar',
    license: 'Apache-2.0',
    stack: 'Node + Supabase + Groq',
    description: 'API clínica com triagem, prontuário PDF e IA epidemiológica.',
    features: [
      {
        external: 'Triagens',
        localModule: 'Pronto atendimento',
        localPath: '/pronto-atendimento',
        reportCodes: ['er.visits.by-triage', 'er.wait.by-triage'],
        status: 'integrated',
      },
      {
        external: 'IA surto respiratório',
        localModule: 'CCIH',
        reportCodes: ['ccih.outbreak.indicators'],
        status: 'integrated',
        notes: 'Análise agregada nativa em /ia/epidemiologia; histórico persistido.',
      },
      {
        external: 'IA paciente recorrente',
        localModule: 'Prontuário / IA',
        localPath: '/ia/epidemiologia',
        status: 'integrated',
        notes: 'Endpoint Groq opcional — AnalyzeRecurrentPatientAsync.',
      },
      {
        external: 'IA triagem operacional PS',
        localModule: 'Pronto atendimento / IA',
        localPath: '/ia/epidemiologia',
        status: 'integrated',
        notes: 'AnalyzeTriageOperationalAsync com enriquecimento Groq opcional.',
      },
      {
        external: 'Notificações compulsórias / LGPD',
        localModule: 'Regulatório',
        reportCodes: ['reg.compulsory-notifications'],
        status: 'partial',
      },
    ],
  },
  {
    id: 'datasus',
    name: 'DATASUS / SIA-SUS / SIH-SUS',
    url: 'https://datasus.saude.gov.br',
    license: 'Público',
    stack: 'Arquivos regulatórios',
    description: 'Exportação BPA, APAC, AIH e CIHA para faturamento SUS.',
    features: [
      {
        external: 'BPA — Boletim de Produção Ambulatorial',
        localModule: 'Faturamento SUS',
        localPath: '/faturamento/sus/bpa',
        reportCodes: ['reg.bpa', 'reg.ambulatory-production'],
        status: 'integrated',
        notes: 'Prévia + download .txt layout DATASUS.',
      },
      {
        external: 'APAC — Alta complexidade',
        localModule: 'Oncologia / Diálise',
        localPath: '/faturamento/sus/apac',
        reportCodes: ['reg.apac', 'reg.ciha'],
        status: 'integrated',
        notes: 'Sessões de quimioterapia e diálise → APAC/CIHA.',
      },
      {
        external: 'AIH — Internação hospitalar',
        localModule: 'Internação',
        localPath: '/faturamento/sus/aih',
        reportCodes: ['reg.aih', 'reg.sih-sus'],
        status: 'integrated',
      },
      {
        external: 'Exportação oficial arquivos',
        localModule: 'Faturamento SUS',
        localPath: '/faturamento/sus/exportacoes',
        status: 'integrated',
        notes: 'Download BPA/APAC/AIH/CIHA .txt com checksum SHA-256.',
      },
    ],
  },
  {
    id: 'inventory-abc',
    name: 'Curva ABC (gestão de estoque)',
    url: '',
    license: '—',
    stack: 'APSMedCore BI',
    description: 'Classificação ABC por valor de consumo — farmácia e almoxarifado.',
    features: [
      {
        external: 'ABC Farmácia',
        localModule: 'Farmácia',
        localPath: '/farmacia',
        reportCodes: ['pharmacy.abc-curve'],
        status: 'integrated',
      },
      {
        external: 'ABC Almoxarifado',
        localModule: 'Suprimentos',
        localPath: '/estoque',
        reportCodes: ['supply.abc-curve'],
        status: 'integrated',
      },
    ],
  },
  {
    id: 'prontomed',
    name: 'Prontomed',
    url: 'https://github.com/CarlosSLoureiro/prontomed',
    license: 'GPL-3.0',
    stack: 'Laravel 9 + PHP 8 + MySQL + JWT',
    description:
      'PEP ambulatorial com cadastro de pacientes, agendamento de consultas e observações por consulta. API REST para médicos.',
    features: [
      {
        external: 'Pacientes',
        localModule: 'Pacientes / PEP',
        localPath: '/pacientes',
        status: 'integrated',
        notes: 'Entidade Patient + MedicalRecord já cobre e expande o modelo Paciente.',
      },
      {
        external: 'Consultas / observações',
        localModule: 'PEP / Agendamentos',
        localPath: '/pep',
        status: 'partial',
        notes:
          'Consulta+Observacoes mapeados para Appointment + MedicalRecordEntry; anamnese estruturada e assinatura digital são superiores no APSMedCore.',
      },
      {
        external: 'Agenda médica',
        localModule: 'Agendamentos',
        localPath: '/agendamentos',
        status: 'integrated',
      },
      {
        external: 'Autenticação JWT médico',
        localModule: 'Auth / Profissionais',
        status: 'reference',
        notes: 'Stack incompatível (PHP); fluxo de login médico reimplementado em .NET.',
      },
    ],
  },
  {
    id: 'prontuario-medico',
    name: 'ProNele (prontuario-medico)',
    url: 'https://github.com/FelipeFelipeRenan/prontuario-medico',
    license: 'Sem licença declarada',
    stack: 'React Native + Strapi (Node) + Docker',
    description:
      'App mobile (ProNele) com backend headless Strapi: paciente, médico, enfermeiro e consulta com campo anamnesis.',
    features: [
      {
        external: 'Consulta.anamnesis',
        localModule: 'PEP — Anamnese',
        localPath: '/pep/anamnese',
        status: 'integrated',
        notes: 'Formulário estruturado em ClinicalEntryForm (entryType=Anamnesis).',
      },
      {
        external: 'Paciente / Médico / Enfermeiro',
        localModule: 'Pacientes / Profissionais',
        localPath: '/pacientes',
        status: 'integrated',
      },
      {
        external: 'App mobile React Native',
        localModule: 'PEP web + offline',
        localPath: '/pep',
        status: 'reference',
        notes: 'Mobile nativo não portável; pepActions + IndexedDB cobrem offline parcial na web.',
      },
      {
        external: 'Strapi CMS',
        localModule: 'API .NET',
        status: 'reference',
        notes: 'Microserviço side-by-side não recomendado — schema mínimo, sem FHIR/TISS/SUS.',
      },
    ],
  },
];

export function countByStatus(status: ExternalSourceStatus): number {
  return EXTERNAL_SOURCES.flatMap((s) => s.features).filter((f) => f.status === status).length;
}
