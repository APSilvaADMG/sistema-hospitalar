import {
  applyAppearanceToDocument,
  saveAppearance,
  type AppearanceSettings,
} from '../theme/appearanceConfig';

export const HOSPITAL_PARAMS_KEY = 'hms-hospital-params';
export const LAYOUT_PREFS_KEY = 'hms-layout-prefs';

export type ModuleVisibilityFlags = {
  financial: boolean;
  inventory: boolean;
  billing: boolean;
  marketing: boolean;
  bi: boolean;
};

export type HospitalParams = {
  hospitalName: string;
  cnes: string;
  cnpj: string;
  timezone: string;
  defaultLocale: string;
  appointmentSlotMinutes: number;
  autoGenerateMrn: boolean;
  modules: ModuleVisibilityFlags;
};

export const defaultModuleVisibility: ModuleVisibilityFlags = {
  financial: true,
  inventory: true,
  billing: true,
  marketing: false,
  bi: true,
};

export type LayoutPrefs = {
  showLogoOnLabels: boolean;
  showLogoOnWristband: boolean;
  wristbandBarcode: boolean;
  visitorBadgePhoto: boolean;
  reportFooter: string;
};

/** Perfil padrão Bayanno HMS (SGHC) — visual teal escuro e tiles no dashboard. */
export const bayannoHospitalParams: HospitalParams = {
  hospitalName: 'IASGH',
  cnes: '',
  cnpj: '',
  timezone: 'America/Sao_Paulo',
  defaultLocale: 'pt-BR',
  appointmentSlotMinutes: 30,
  autoGenerateMrn: true,
  modules: { ...defaultModuleVisibility },
};

/** Default clinic shell — Feegow top bar + contextual sidebar (IASGH). */
export const feegowAppearance: AppearanceSettings = {
  brand: 'feegow',
  scheme: 'light',
  density: 'comfortable',
  sidebarMode: 'expanded',
  showTestBanner: false,
  bannerMessage: '',
};

/** @deprecated Use feegowAppearance — kept for profile helpers that only set hospital params. */
export const bayannoAppearance: AppearanceSettings = { ...feegowAppearance };

export const bayannoLayoutPrefs: LayoutPrefs = {
  showLogoOnLabels: true,
  showLogoOnWristband: true,
  wristbandBarcode: true,
  visitorBadgePhoto: true,
  reportFooter: '',
};

export const bayannoChecklistDone: Record<string, boolean> = {
  env: true,
  empresa: true,
  aparencia: true,
  equipe: false,
  convenios: true,
  salas: false,
  procedimentos: false,
  exames: false,
  agenda: true,
  pacientes: true,
  impressao: true,
  financeiro: false,
};

export function applyBayannoProfile(): void {
  localStorage.setItem(HOSPITAL_PARAMS_KEY, JSON.stringify(bayannoHospitalParams));
  localStorage.setItem(LAYOUT_PREFS_KEY, JSON.stringify(bayannoLayoutPrefs));
  localStorage.setItem('hms-setup-checklist', JSON.stringify(bayannoChecklistDone));
  saveAppearance(bayannoAppearance);
  applyAppearanceToDocument(bayannoAppearance);
}

/** Valores extraídos do snapshot OnDoctor (IASGH / Anderson Pereira Silva / Sala 1). */
export const onDoctorHospitalParams: HospitalParams = {
  hospitalName: 'IASGH Clínica',
  cnes: '',
  cnpj: '',
  timezone: 'America/Sao_Paulo',
  defaultLocale: 'pt-BR',
  appointmentSlotMinutes: 30,
  autoGenerateMrn: true,
  modules: { ...defaultModuleVisibility },
};

export const onDoctorAppearance: AppearanceSettings = { ...feegowAppearance };

export const onDoctorLayoutPrefs: LayoutPrefs = {
  showLogoOnLabels: true,
  showLogoOnWristband: true,
  wristbandBarcode: true,
  visitorBadgePhoto: true,
  reportFooter: '',
};

export const onDoctorChecklistDone: Record<string, boolean> = {
  env: true,
  empresa: true,
  aparencia: true,
  equipe: true,
  convenios: true,
  salas: true,
  procedimentos: false,
  exames: false,
  agenda: true,
  pacientes: true,
  impressao: true,
  financeiro: false,
};

export function applyOnDoctorClinicProfile(): void {
  localStorage.setItem(HOSPITAL_PARAMS_KEY, JSON.stringify(onDoctorHospitalParams));
  localStorage.setItem(LAYOUT_PREFS_KEY, JSON.stringify(onDoctorLayoutPrefs));
  localStorage.setItem('hms-setup-checklist', JSON.stringify(onDoctorChecklistDone));
  saveAppearance(onDoctorAppearance);
  applyAppearanceToDocument(onDoctorAppearance);
}

export function loadHospitalParams(): HospitalParams {
  try {
    const raw = localStorage.getItem(HOSPITAL_PARAMS_KEY);
    if (!raw) return bayannoHospitalParams;
    const parsed = JSON.parse(raw) as Partial<HospitalParams>;
    return {
      ...bayannoHospitalParams,
      ...parsed,
      modules: { ...defaultModuleVisibility, ...parsed.modules },
    };
  } catch {
    return bayannoHospitalParams;
  }
}

export function loadLayoutPrefs(): LayoutPrefs {
  try {
    const raw = localStorage.getItem(LAYOUT_PREFS_KEY);
    if (!raw) return bayannoLayoutPrefs;
    return { ...bayannoLayoutPrefs, ...JSON.parse(raw) };
  } catch {
    return bayannoLayoutPrefs;
  }
}
