import type { ReactNode } from 'react';
import { hospitalLogoDimensions, hospitalLogoSrc, HOSPITAL_LOGO_ALT } from '../../assets/hospitalLogoAsset';

type Props = {
  title: string;
  subtitle: string;
  meta?: ReactNode;
  operator?: ReactNode;
};

export function FuniGuidePrintHeader({ title, subtitle, meta, operator }: Props) {
  const logo = hospitalLogoDimensions(56, 'full');

  return (
    <header className="funi-guide-header">
      <div className="funi-guide-logo">
        <img
          src={hospitalLogoSrc('full')}
          alt={HOSPITAL_LOGO_ALT}
          width={logo.width}
          height={logo.height}
          style={{ height: 50, width: 'auto', maxWidth: 111 }}
        />
      </div>
      <div className="funi-guide-header-main">
        <div className="funi-guide-title">{title}</div>
        <div className="funi-guide-subtitle">{subtitle}</div>
      </div>
      <div className="funi-guide-meta">
        {operator ? <div className="funi-guide-operator">{operator}</div> : meta}
      </div>
    </header>
  );
}
