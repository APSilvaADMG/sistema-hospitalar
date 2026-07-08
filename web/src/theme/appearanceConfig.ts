export type BrandTheme = 'feegow' | 'bayanno';

export type ColorScheme = 'light';

export type UiDensity = 'comfortable' | 'compact';

export type SidebarMode = 'expanded';



export type AppearanceSettings = {

  brand: BrandTheme;

  scheme: ColorScheme;

  density: UiDensity;

  sidebarMode: SidebarMode;

  showTestBanner: boolean;

  bannerMessage: string;

};



export const APPEARANCE_STORAGE_KEY = 'hms-appearance';

// Bump when persisted appearance shape or enforced brand default changes.
export const APPEARANCE_STORAGE_VERSION = 6;



export const defaultAppearance: AppearanceSettings = {

  brand: 'feegow',

  scheme: 'light',

  density: 'comfortable',

  sidebarMode: 'expanded',

  showTestBanner: false,

  bannerMessage: '',

};



export const brandThemeLabels: Record<BrandTheme, { title: string; description: string }> = {

  feegow: {

    title: 'IASGH',

    description: 'Layout clínico (referência Feegow) com identidade IASGH.',

  },

  bayanno: {

    title: 'Bayanno HMS (SGHC)',

    description: 'Visual oficial do sistema hospitalar Bayanno.',

  },

};



type StoredAppearance = Partial<AppearanceSettings> & { v?: number; brand?: string };



/** Feegow is the only active shell brand; persisted bayanno values are upgraded here. */
export function normalizeAppearance(parsed: StoredAppearance = {}): AppearanceSettings {
  return { ...defaultAppearance, ...parsed, brand: 'feegow', scheme: 'light', sidebarMode: 'expanded' };
}



export function loadAppearance(): AppearanceSettings {

  try {

    const raw = localStorage.getItem(APPEARANCE_STORAGE_KEY);

    if (!raw) return defaultAppearance;

    const parsed = JSON.parse(raw) as StoredAppearance;

    const next = normalizeAppearance(parsed);

    if ((parsed.v ?? 1) < APPEARANCE_STORAGE_VERSION) {

      saveAppearance(next);

    }

    return next;

  } catch {

    return defaultAppearance;

  }

}



export function saveAppearance(settings: AppearanceSettings) {
  const next = normalizeAppearance(settings);
  localStorage.setItem(
    APPEARANCE_STORAGE_KEY,
    JSON.stringify({ ...next, v: APPEARANCE_STORAGE_VERSION }),
  );
}



export function applyAppearanceToDocument(settings: AppearanceSettings) {
  const normalized = normalizeAppearance(settings);
  const root = document.documentElement;

  root.dataset.brand = normalized.brand;

  root.dataset.scheme = 'light';

  root.dataset.density = normalized.density;

  root.dataset.sidebar = 'expanded';

  const themeColor = '#00b4fc';

  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', themeColor);

}



export function hydrateAppearance() {

  applyAppearanceToDocument(loadAppearance());

}



export function isClinicShellBrand(brand: BrandTheme) {

  return brand === 'feegow' || brand === 'bayanno';

}



export function isFeegowBrand(brand: BrandTheme) {

  return brand === 'feegow';

}


