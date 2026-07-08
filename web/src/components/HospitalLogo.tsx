import {
  HOSPITAL_LOGO_ALT,
  hospitalLogoDimensions,
  hospitalLogoSrc,
  type HospitalLogoVariant,
} from '../assets/hospitalLogoAsset';

type HospitalLogoProps = {
  /** `full` — logomarca completa; `mark` — emblema (cruz + H) para sidebar estreita e mobile. */
  variant?: HospitalLogoVariant;
  /** Altura em pixels; largura proporcional ao viewBox do SVG. */
  height?: number;
  className?: string;
};

export function HospitalLogo({ variant = 'full', height = 125, className }: HospitalLogoProps) {
  const { width } = hospitalLogoDimensions(height, variant);

  return (
    <img
      src={hospitalLogoSrc(variant)}
      alt={HOSPITAL_LOGO_ALT}
      className={['hospital-logo', className].filter(Boolean).join(' ')}
      style={{ height, width }}
      height={height}
      width={width}
      decoding="async"
      draggable={false}
    />
  );
}
