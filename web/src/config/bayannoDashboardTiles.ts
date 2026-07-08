import type { UserRoleName } from '../api/client';

/** Ícone FontAwesome do Bayanno (classe icon-* do bayanno.css). */
export type BayannoDashboardTile = {
  to: string;
  label: string;
  icon: string;
};

/** admin/dashboard.php — linhas 1–3 (18 tiles). */
export const BAYANNO_ADMIN_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/rh/profissionais', label: 'Médicos', icon: 'icon-user-md' },
    { to: '/recepcao/pacientes', label: 'Pacientes', icon: 'icon-user' },
    { to: '/enfermagem', label: 'Enfermeiros', icon: 'icon-plus-sign-alt' },
    { to: '/farmacia', label: 'Farmácia', icon: 'icon-medkit' },
    { to: '/laboratorio', label: 'Laboratório', icon: 'icon-beaker' },
    { to: '/financeiro', label: 'Contador', icon: 'icon-money' },
  ],
  [
    { to: '/recepcao/agendamentos', label: 'Agendamentos', icon: 'icon-exchange' },
    { to: '/financeiro', label: 'Pagamentos', icon: 'icon-credit-card' },
    { to: '/hemoterapia', label: 'Banco de sangue', icon: 'icon-tint' },
    { to: '/farmacia', label: 'Medicamentos', icon: 'icon-medkit' },
    { to: '/centro-cirurgico', label: 'Relatório cirurgia', icon: 'icon-reorder' },
    { to: '/relatorios', label: 'Relatório nascimento', icon: 'icon-github-alt' },
  ],
  [
    { to: '/relatorios', label: 'Relatório óbito', icon: 'icon-minus-sign' },
    { to: '/internacao/leitos', label: 'Alocação de leitos', icon: 'icon-hdd' },
    { to: '/configuracoes', label: 'Mural de avisos', icon: 'icon-columns' },
    { to: '/configuracoes/parametros', label: 'Configurações', icon: 'icon-h-sign' },
    { to: '/configuracoes/aparencia', label: 'Idioma', icon: 'icon-globe' },
    { to: '/configuracoes', label: 'Backup', icon: 'icon-download-alt' },
  ],
];

/** doctor/dashboard.php */
export const BAYANNO_DOCTOR_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/recepcao/pacientes', label: 'Pacientes', icon: 'icon-user' },
    { to: '/recepcao/agendamentos', label: 'Agendamentos', icon: 'icon-exchange' },
    { to: '/pacientes', label: 'Prescrições', icon: 'icon-stethoscope' },
    { to: '/internacao/leitos', label: 'Alocação de leitos', icon: 'icon-hdd' },
    { to: '/hemoterapia', label: 'Banco de sangue', icon: 'icon-tint' },
    { to: '/relatorios', label: 'Relatórios', icon: 'icon-hospital' },
  ],
];

/** nurse/dashboard.php */
export const BAYANNO_NURSE_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/recepcao/pacientes', label: 'Pacientes', icon: 'icon-user' },
    { to: '/internacao/leitos', label: 'Alocação de leitos', icon: 'icon-hdd' },
    { to: '/hemoterapia', label: 'Banco de sangue', icon: 'icon-tint' },
    { to: '/relatorios', label: 'Relatórios', icon: 'icon-hospital' },
  ],
];

/** patient/dashboard.php */
export const BAYANNO_PATIENT_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/portal-paciente', label: 'Médicos', icon: 'icon-stethoscope' },
    { to: '/portal-paciente', label: 'Agendamentos', icon: 'icon-exchange' },
    { to: '/portal-paciente', label: 'Prescrições', icon: 'icon-stethoscope' },
    { to: '/portal-paciente', label: 'Histórico internação', icon: 'icon-hdd' },
    { to: '/portal-paciente', label: 'Banco de sangue', icon: 'icon-tint' },
    { to: '/portal-paciente', label: 'Faturas', icon: 'icon-credit-card' },
  ],
];

/** pharmacist/dashboard.php */
export const BAYANNO_PHARMACY_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/farmacia', label: 'Medicamentos', icon: 'icon-medkit' },
    { to: '/farmacia', label: 'Prescrições', icon: 'icon-stethoscope' },
    { to: '/estoque', label: 'Categorias', icon: 'icon-reorder' },
  ],
];

/** laboratorist/dashboard.php */
export const BAYANNO_LAB_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/laboratorio', label: 'Prescrições', icon: 'icon-stethoscope' },
    { to: '/hemoterapia', label: 'Banco de sangue', icon: 'icon-tint' },
    { to: '/laboratorio', label: 'Doadores', icon: 'icon-user' },
  ],
];

/** accountant/dashboard.php */
export const BAYANNO_BILLING_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/financeiro', label: 'Pagamentos', icon: 'icon-credit-card' },
    { to: '/faturamento', label: 'Faturas', icon: 'icon-money' },
    { to: '/financeiro', label: 'Histórico', icon: 'icon-reorder' },
  ],
];

/** reception / default staff */
export const BAYANNO_RECEPTION_TILES: BayannoDashboardTile[][] = [
  [
    { to: '/recepcao/pacientes', label: 'Pacientes', icon: 'icon-user' },
    { to: '/recepcao/agendamentos', label: 'Agendamentos', icon: 'icon-exchange' },
    { to: '/recepcao/checkin', label: 'Check-in', icon: 'icon-exchange' },
    { to: '/internacao', label: 'Internação', icon: 'icon-hdd' },
  ],
];

export function getBayannoDashboardTilesForRole(role: UserRoleName): BayannoDashboardTile[][] {
  switch (role) {
    case 'Admin':
    case 'HospitalDirector':
    case 'IT':
      return BAYANNO_ADMIN_TILES;
    case 'Doctor':
      return BAYANNO_DOCTOR_TILES;
    case 'Nurse':
    case 'NursingTechnician':
      return BAYANNO_NURSE_TILES;
    case 'Patient':
      return BAYANNO_PATIENT_TILES;
    case 'Pharmacy':
      return BAYANNO_PHARMACY_TILES;
    case 'Billing':
      return BAYANNO_BILLING_TILES;
    case 'Reception':
      return BAYANNO_RECEPTION_TILES;
    default:
      return BAYANNO_RECEPTION_TILES;
  }
}

/** @deprecated use getBayannoDashboardTilesForRole */
export const BAYANNO_DASHBOARD_TILES_ROW1 = BAYANNO_ADMIN_TILES[0];
export const BAYANNO_DASHBOARD_TILES_ROW2 = BAYANNO_ADMIN_TILES[1];
