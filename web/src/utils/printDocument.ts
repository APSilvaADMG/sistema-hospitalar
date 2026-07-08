import {
  HOSPITAL_LOGO_ALT,
  hospitalLogoDimensions,
  hospitalLogoSrc,
  type HospitalLogoVariant,
} from '../assets/hospitalLogoAsset';
import { showPrintPreview } from './printService';
import { PROFESSIONAL_REPORT_CSS, printHtmlDocument } from './professionalReportTemplate';

const HOSPITAL_NAME = HOSPITAL_LOGO_ALT;
const BRAND = {
  primary: '#1565c0',
  primaryDark: '#0d47a1',
  primaryLight: '#e3f2fd',
  accent: '#00897b',
  accentSoft: '#b2dfdb',
  text: '#1a2332',
  muted: '#5c6b7a',
};

function assetUrl(path: string) {
  if (typeof window !== 'undefined' && window.location.origin) {
    return `${window.location.origin}${path}`;
  }
  return path;
}

function inlineHospitalLogoSvg(height = 45, variant: HospitalLogoVariant = 'full') {
  const { width } = hospitalLogoDimensions(height, variant);
  const src = assetUrl(hospitalLogoSrc(variant));
  return `<img class="badge-logo-inline" src="${src}" alt="${HOSPITAL_NAME}" width="${width}" height="${height}" style="height:${height}px;width:${width}px" />`;
}

function inlineHospitalLogo(height = 45) {
  return inlineHospitalLogoSvg(height, 'full');
}

const BASE_STYLES = `
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: 'Segoe UI', Arial, sans-serif; color: #111; background: #fff; }
  .hospital-name { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: #334155; }
  .doc-title { font-size: 13px; font-weight: 800; text-transform: uppercase; margin: 4px 0 8px; }
  .field { margin-bottom: 6px; font-size: 12px; line-height: 1.35; }
  .field strong { display: block; font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 1px; }
  .field-value { font-weight: 600; }
  .field-value.lg { font-size: 16px; }
  .field-value.xl { font-size: 22px; letter-spacing: 0.08em; }
  .divider { border-top: 1px dashed #cbd5e1; margin: 8px 0; }
  .barcode { font-family: 'Courier New', monospace; font-size: 14px; letter-spacing: 0.2em; text-align: center; padding: 6px 0; border: 1px solid #e2e8f0; margin-top: 8px; }
  .ticket-brand-header {
    text-align: center;
    margin-bottom: 12px;
  }
  .ticket-logo {
    max-height: 63px;
    max-width: 100%;
    width: auto;
    object-fit: contain;
    display: block;
    margin: 0 auto;
    border: none;
    background: transparent;
    padding: 0;
    box-shadow: none;
  }
  .ticket-qr-wrap { text-align: center; margin: 10px 0 6px; padding: 8px; border: 1px dashed ${BRAND.primary}; border-radius: 8px; background: #fafcff; }
  .ticket-qr { width: 140px; height: 140px; display: block; margin: 0 auto; }
  .ticket-qr-label { font-size: 9px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; color: ${BRAND.primaryDark}; margin-top: 6px; }
  .footer-note { font-size: 10px; color: #64748b; text-align: center; margin-top: 8px; }
  .amount { font-size: 20px; font-weight: 800; text-align: center; margin: 8px 0; }
  .row { display: flex; justify-content: space-between; gap: 8px; font-size: 12px; margin-bottom: 4px; }
  .photo { width: 56px; height: 56px; border-radius: 6px; object-fit: cover; border: 1px solid #e2e8f0; }
  .label-header { display: flex; align-items: center; gap: 10px; margin-bottom: 8px; }
  .badge-type { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 10px; font-weight: 700; text-transform: uppercase; }
  .badge-visitor { background: #fef3c7; color: #92400e; }
  .badge-patient { background: #dbeafe; color: #1e40af; }
  .visitor-badge-card {
    border: 1.5px solid ${BRAND.primary};
    border-radius: 12px;
    overflow: hidden;
    text-align: center;
    position: relative;
    background: #fff;
    box-shadow: 0 2px 10px rgba(13, 71, 161, 0.12);
  }
  .badge-hole {
    width: 12px; height: 12px; border-radius: 50%;
    border: 2px solid #94a3b8; margin: 6px auto 0;
    background: #fff; position: relative; z-index: 2;
  }
  .badge-header {
    background: #fff;
    padding: 10px 10px 8px;
    color: ${BRAND.text};
    border-bottom: 1px solid #d8e0ea;
  }
  .badge-logo-wrap {
    margin: 0 auto 6px;
    max-width: 92%;
    background: #fff;
  }
  .badge-logo {
    max-height: 45px;
    max-width: 42mm;
    width: auto;
    object-fit: contain;
    display: block;
    margin: 0 auto;
    border: none;
    background: #fff;
    padding: 0;
    box-shadow: none;
  }
  .badge-logo-inline { display: block; margin: 0 auto; }
  .badge-hospital-name {
    font-size: 8px; font-weight: 700; letter-spacing: 0.12em;
    text-transform: uppercase; color: ${BRAND.muted};
  }
  .badge-body { padding: 10px 12px 12px; }
  .badge-type-banner {
    display: inline-block;
    background: ${BRAND.accent};
    color: #fff;
    font-size: 10px; font-weight: 800;
    letter-spacing: 0.16em;
    padding: 4px 14px;
    border-radius: 999px;
    margin-bottom: 10px;
    box-shadow: 0 2px 0 rgba(0, 105, 92, 0.25);
  }
  .badge-visitor-photo {
    width: 72px; height: 72px; border-radius: 50%;
    object-fit: cover; border: 2px solid ${BRAND.primaryLight};
    margin: 0 auto 8px; display: block;
    box-shadow: 0 2px 6px rgba(13, 71, 161, 0.15);
  }
  .badge-visitor-name {
    font-size: 17px; font-weight: 800; line-height: 1.2;
    color: ${BRAND.text}; margin-bottom: 8px;
    padding: 0 4px;
  }
  .badge-visitor-number-wrap {
    background: ${BRAND.primaryLight};
    border: 1px solid #bbdefb;
    border-radius: 8px;
    padding: 6px 10px;
    margin: 0 0 10px;
  }
  .badge-visitor-number-label {
    font-size: 8px; font-weight: 700; letter-spacing: 0.12em;
    text-transform: uppercase; color: ${BRAND.primaryDark}; margin-bottom: 2px;
  }
  .badge-visitor-number {
    font-size: 22px; font-weight: 900; letter-spacing: 0.12em;
    color: ${BRAND.primaryDark}; line-height: 1;
  }
  .badge-visitor-dest {
    font-size: 11px; font-weight: 700; color: ${BRAND.primaryDark};
    background: ${BRAND.primaryLight};
    border-left: 3px solid ${BRAND.accent};
    border-radius: 6px;
    padding: 6px 8px;
    margin-bottom: 8px;
    text-align: left;
  }
  .badge-visitor-dest-label {
    display: block; font-size: 8px; font-weight: 700;
    letter-spacing: 0.08em; text-transform: uppercase;
    color: ${BRAND.muted}; margin-bottom: 2px;
  }
  .badge-meta-list { text-align: left; margin-bottom: 8px; }
  .badge-visitor-meta {
    font-size: 9px; color: ${BRAND.muted};
    padding: 3px 0; border-bottom: 1px dashed #e2e8f0;
    display: flex; justify-content: space-between; gap: 6px;
  }
  .badge-visitor-meta strong { color: ${BRAND.text}; font-weight: 600; }
  .visitor-badge-card .barcode {
    font-size: 12px; letter-spacing: 0.18em;
    border: 1px dashed ${BRAND.primary};
    background: #fafcff; color: ${BRAND.primaryDark};
    border-radius: 6px; margin-top: 4px;
  }
  .visitor-badge-card .footer-note {
    font-size: 8px; color: ${BRAND.muted};
    margin-top: 8px; line-height: 1.35;
    border-top: 1px solid #e8eef5; padding-top: 6px;
  }
  .wristband-strip {
    display: flex;
    align-items: stretch;
    border: 1.5px solid ${BRAND.primary};
    border-radius: 6px;
    overflow: hidden;
    background: #fff;
    min-height: 22mm;
  }
  .wristband-brand {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 3px;
    padding: 4px 8px;
    background: #fff;
    color: ${BRAND.text};
    flex-shrink: 0;
    min-width: 20mm;
    border-right: 1px solid #d8e0ea;
  }
  .wristband-logo {
    max-height: 25px;
    max-width: 18mm;
    width: auto;
    object-fit: contain;
    display: block;
    border: none;
    background: #fff;
    padding: 0;
    box-shadow: none;
  }
  .wristband-brand-label {
    font-size: 7px;
    font-weight: 800;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    text-align: center;
    line-height: 1.2;
    color: ${BRAND.muted};
  }
  .wristband-content {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 5px 10px;
    flex: 1;
    min-width: 0;
    flex-wrap: nowrap;
  }
  .wristband-name-block { flex-shrink: 0; max-width: 42%; }
  .wristband-name {
    font-size: 13px;
    font-weight: 800;
    color: ${BRAND.text};
    line-height: 1.15;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .wristband-social {
    font-size: 8px;
    color: ${BRAND.muted};
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .wristband-divider {
    width: 1px;
    align-self: stretch;
    background: #d8e0ea;
    flex-shrink: 0;
  }
  .wristband-meta-col {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex-shrink: 0;
  }
  .wristband-meta {
    font-size: 8px;
    color: ${BRAND.muted};
    white-space: nowrap;
  }
  .wristband-meta strong {
    color: ${BRAND.primaryDark};
    font-weight: 800;
    font-size: 9px;
  }
  .wristband-blood {
    background: ${BRAND.accent};
    color: #fff;
    font-size: 11px;
    font-weight: 900;
    padding: 4px 7px;
    border-radius: 5px;
    flex-shrink: 0;
    letter-spacing: 0.04em;
  }
  .wristband-barcode-wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4px 8px;
    background: ${BRAND.primaryLight};
    border-left: 1px dashed ${BRAND.primary};
    flex-shrink: 0;
    min-width: 22mm;
  }
  .wristband-barcode {
    font-family: 'Courier New', monospace;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.14em;
    color: ${BRAND.primaryDark};
    line-height: 1;
  }
  .wristband-barcode-label {
    font-size: 6px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: ${BRAND.muted};
    margin-top: 2px;
  }
  .wristband-footer {
    font-size: 6px;
    color: ${BRAND.muted};
    text-align: center;
    margin-top: 4px;
    letter-spacing: 0.06em;
    text-transform: uppercase;
  }
  .report-header { border-bottom: 2px solid #1565c0; padding-bottom: 8px; margin-bottom: 12px; }
  .report-logo { max-height: 54px; margin-bottom: 6px; }
  .report-title { font-size: 16px; font-weight: 800; text-transform: uppercase; }
  .report-section { margin-bottom: 12px; }
  .report-section h3 { font-size: 12px; text-transform: uppercase; color: #64748b; margin-bottom: 6px; }
  .report-table { width: 100%; border-collapse: collapse; font-size: 12px; }
  .report-table th, .report-table td { border: 1px solid #e2e8f0; padding: 6px 8px; text-align: left; }
  .report-table th { background: #f1f5f9; font-size: 10px; text-transform: uppercase; }
  .report-body { font-size: 13px; line-height: 1.55; white-space: pre-wrap; }
  .text-danger { color: #b91c1c; font-weight: 700; }

`;

export type PrintPageSize = 'label' | 'badge' | 'ticket' | 'visitor-badge' | 'wristband' | 'report' | 'funi-a4' | 'analytics-a4';

export function shortId(id: string) {
  return id.replace(/-/g, '').slice(0, 8).toUpperCase();
}

export { formatBrDate as formatDate, formatBrDateTime as formatDateTime } from './dateUtils';

export function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function maskCpf(cpf: string) {
  const digits = cpf.replace(/\D/g, '');
  if (digits.length !== 11) return cpf;
  return `***.${digits.slice(3, 6)}.${digits.slice(6, 9)}-**`;
}

export function logoImg(height = 40) {
  return inlineHospitalLogoSvg(height, 'full');
}

/** Ícone da marca para crachá (compacto, vertical). */
export function logoIconBadge(size = 45) {
  return inlineHospitalLogoSvg(size, 'mark');
}

/** Logomarca para crachá — mesma imagem institucional da pulseira. */
export function logoImgBadge(height = 45) {
  return inlineHospitalLogoSvg(height, 'mark');
}

/** Logomarca para tickets de estacionamento — centralizada no topo do ticket. */
export function logoImgTicket(height = 63) {
  return inlineHospitalLogoSvg(height, 'full');
}

/** Logomarca compacta para pulseira de identificação. */
export function logoImgWristband(height = 25) {
  return inlineHospitalLogoSvg(height, 'mark');
}

type PrintOptions = {
  title: string;
  body: string;
  pageSize?: PrintPageSize;
  autoPrint?: boolean;
  html?: string;
  previewWidth?: 'md' | 'lg';
};

function escapeHtml(text: string) {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function pageConfig(pageSize: PrintPageSize) {
  switch (pageSize) {
    case 'label':
      return { rule: '@page { size: 62mm 29mm; margin: 2mm; }', className: 'print-label' };
    case 'visitor-badge':
      return { rule: '@page { size: 54mm 86mm; margin: 3mm; }', className: 'print-visitor-badge' };
    case 'wristband':
      return { rule: '@page { size: 254mm 30mm; margin: 2mm; }', className: 'print-wristband' };
    case 'report':
      return {
        rule: '@page { size: A4 portrait; margin: 14mm 12mm 18mm 12mm; }',
        className: 'print-professional-a4',
      };
    case 'analytics-a4':
      return {
        rule: '@page { size: A4 portrait; margin: 14mm 12mm 18mm 12mm; }',
        className: 'print-professional-a4',
      };
    case 'funi-a4':
      return { rule: '@page { size: A4 portrait; margin: 10mm; }', className: 'print-funi-a4' };
    case 'badge':
      return { rule: '@page { size: 90mm 54mm; margin: 3mm; }', className: 'print-badge' };
    default:
      return { rule: '@page { size: 80mm auto; margin: 4mm; }', className: 'print-ticket' };
  }
}

export function buildPrintHtml(title: string, body: string, pageSize: PrintPageSize = 'ticket') {
  const { rule, className } = pageConfig(pageSize);
  const safeTitle = escapeHtml(title);
  const isProfessionalA4 = pageSize === 'analytics-a4' || pageSize === 'report';
  const professionalCss = isProfessionalA4 ? PROFESSIONAL_REPORT_CSS : '';
  const fontLink = isProfessionalA4
    ? '<link href="https://fonts.googleapis.com/css2?family=Source+Sans+3:wght@400;600;700;800&display=swap" rel="stylesheet" />'
    : '';

  return `<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>${safeTitle}</title>
  ${fontLink}
  <style>
    ${rule}
    ${BASE_STYLES}
    ${professionalCss}
    body {
      padding: ${isProfessionalA4 ? '0' : '4px'};
      ${isProfessionalA4 ? "font-family: 'Source Sans 3', 'Segoe UI', Arial, sans-serif;" : ''}
    }
    .${className} {
      width: 100%;
      ${isProfessionalA4 ? 'max-width: 210mm; margin: 0 auto;' : ''}
    }
    @media print {
      html, body { margin: 0; padding: 0; background: #fff; }
      body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
      .${className} { border: none; box-shadow: none; max-width: none; width: auto; }
    }
  </style>
</head>
<body>
  <div class="${className}">
    ${body}
  </div>
</body>
</html>`;
}

export function printDocument({
  title,
  body,
  pageSize = 'ticket',
  autoPrint = false,
  html,
  previewWidth,
}: PrintOptions) {
  const documentHtml = html ?? buildPrintHtml(title, body, pageSize);
  const shown = showPrintPreview({
    title,
    html: documentHtml,
    autoPrint,
    width: previewWidth ?? (pageSize === 'funi-a4' || pageSize === 'analytics-a4' ? 'lg' : 'md'),
  });
  if (!shown) {
    printHtmlDocument(documentHtml, title);
  }
}

export { BRAND, HOSPITAL_NAME, escapeHtml, inlineHospitalLogo };
