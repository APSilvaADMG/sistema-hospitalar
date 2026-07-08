/** Gerado por scripts/sync-logo-variants.mjs a partir de public/logo.svg */
export const HOSPITAL_LOGO_BOUNDS = {
  full: { width: 22816, height: 16707 },
  mark: { width: 8600, height: 8600 },
} as const;

export const HOSPITAL_LOGO_SRC = {
  full: '/logo.svg',
  mark: '/logo-mark.svg',
} as const;

export type HospitalLogoVariant = 'full' | 'mark';

export const HOSPITAL_LOGO_ALT = 'IASGH — Sistema de Gestão Hospitalar';

export function hospitalLogoAspect(variant: HospitalLogoVariant = 'full') {
  const bounds = HOSPITAL_LOGO_BOUNDS[variant];
  return bounds.width / bounds.height;
}

export function hospitalLogoDimensions(height: number, variant: HospitalLogoVariant = 'full') {
  const aspect = hospitalLogoAspect(variant);
  return {
    height,
    width: Math.round(height * aspect),
  };
}

export function hospitalLogoSrc(variant: HospitalLogoVariant = 'full') {
  return HOSPITAL_LOGO_SRC[variant];
}
