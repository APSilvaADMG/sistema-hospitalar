import type { ReportPrintLayoutKind } from '../data/reportPrintLayouts';
import {
  escapeHtml,
  formatDateTime,
  logoImg,
} from './printDocument';

export type ReportMetaField = { label: string; value: string };

export type ReportSection = {
  title: string;
  html: string;
  /** Evita quebra de página no meio da seção */
  avoidBreak?: boolean;
};

export type ProfessionalReportOptions = {
  title: string;
  subtitle?: string;
  documentType: string;
  code?: string;
  layoutKind: ReportPrintLayoutKind;
  meta: ReportMetaField[];
  sections: ReportSection[];
  showSignature?: boolean;
  generatedAt?: string;
};

function signatureBlock() {
  return `
    <div class="pr-signatures">
      <div class="pr-sig-cell">
        <div class="pr-sig-line"></div>
        <div class="pr-sig-label">Responsável / Assinatura</div>
      </div>
      <div class="pr-sig-cell">
        <div class="pr-sig-line"></div>
        <div class="pr-sig-label">Profissional / Carimbo</div>
      </div>
    </div>`;
}

function metaPanel(fields: ReportMetaField[]) {
  if (fields.length === 0) return '';

  const patientIdx = fields.findIndex((f) => f.label.toLowerCase() === 'paciente');
  const patient = patientIdx >= 0 ? fields[patientIdx] : null;
  const detailFields = patientIdx >= 0 ? fields.filter((_, i) => i !== patientIdx) : fields;

  const cpfIdx = detailFields.findIndex((f) => f.label.toLowerCase() === 'cpf');
  const cpf = cpfIdx >= 0 ? detailFields[cpfIdx] : null;
  const gridFields = cpfIdx >= 0 ? detailFields.filter((_, i) => i !== cpfIdx) : detailFields;

  const detailHtml = gridFields.length > 0
    ? `
      <div class="pr-meta-details">
        ${gridFields
          .map(
            (f) => `
          <span class="pr-meta-detail">
            <strong>${escapeHtml(f.label)}:</strong> ${escapeHtml(f.value)}
          </span>`,
          )
          .join('')}
      </div>`
    : '';

  return `
    <div class="pr-meta-panel">
      <div class="pr-meta-panel-inner">
        <div class="pr-meta-main">
          ${patient
    ? `<div class="pr-meta-patient-name">${escapeHtml(patient.value)}</div>${detailHtml}`
    : `
            <div class="pr-meta-grid">
              ${fields
    .map(
      (f) => `
                <div class="pr-meta-item">
                  <span class="pr-meta-label">${escapeHtml(f.label)}</span>
                  <span class="pr-meta-value">${escapeHtml(f.value)}</span>
                </div>`,
    )
    .join('')}
            </div>`}
        </div>
        ${cpf
    ? `<div class="pr-meta-side"><span class="pr-meta-label">${escapeHtml(cpf.label)}</span><span class="pr-meta-value">${escapeHtml(cpf.value)}</span></div>`
    : ''}
      </div>
    </div>`;
}

function buildSections(sections: ReportSection[]) {
  return sections
    .filter((s) => s.html.trim())
    .map(
      (s) => `
      <section class="pr-section${s.avoidBreak ? ' pr-avoid-break' : ''}">
        <h2 class="pr-section-title">${escapeHtml(s.title)}</h2>
        <div class="pr-section-body">${s.html}</div>
      </section>`,
    )
    .join('');
}

function letterhead(options: ProfessionalReportOptions) {
  const docTitle = options.documentType.trim() || 'Relatório';
  const subtitle = options.subtitle?.trim();

  return `
    <header class="pr-letterhead">
      <div class="pr-letterhead-logo">${logoImg(59)}</div>
      <div class="pr-letterhead-divider" aria-hidden="true"></div>
      <div class="pr-letterhead-info">
        <h1 class="pr-doc-type">${escapeHtml(docTitle)}</h1>
        ${subtitle ? `<p class="pr-doc-subtitle">${escapeHtml(subtitle)}</p>` : ''}
      </div>
      ${options.code
    ? `<div class="pr-letterhead-code"><span class="pr-code-label">Código</span><span class="pr-code-value">${escapeHtml(options.code)}</span></div>`
    : ''}
    </header>`;
}

/** HTML do corpo do relatório (sem wrapper html/head). */
export function buildProfessionalReportBody(options: ProfessionalReportOptions): string {
  const generatedAt = options.generatedAt ?? new Date().toISOString();

  return `
    <article class="pr-document kind-${options.layoutKind}" data-layout="${options.layoutKind}">
      ${letterhead(options)}

      ${metaPanel(options.meta)}

      ${buildSections(options.sections)}

      ${options.showSignature ? signatureBlock() : ''}

      <footer class="pr-print-footer">
        <span class="pr-footer-datetime">Impresso em: ${formatDateTime(generatedAt)}</span>
      </footer>
    </article>`;
}

/** Estilos exclusivos do relatório profissional A4 (incluídos em buildPrintHtml). */
export const PROFESSIONAL_REPORT_CSS = `
  :root {
    --pr-accent: #00a5a8;
    --pr-accent-dark: #008b8e;
    --pr-accent-soft: rgba(0, 165, 168, 0.12);
    --pr-accent-softer: rgba(0, 165, 168, 0.06);
    --pr-text: #1e293b;
    --pr-muted: #64748b;
    --pr-border: #d1e3e4;
    --pr-surface: #f4fbfb;
  }
  .pr-document {
    color: var(--pr-text);
    font-size: 10pt;
    line-height: 1.5;
  }
  .pr-document.kind-sitrep-oms { --pr-accent: #00a5a8; }
  .pr-document.kind-hospitalrun { --pr-accent: #00a5a8; }
  .pr-document.kind-bayanno-sghc { --pr-accent: #00a5a8; }
  .pr-document.kind-database-hospital { --pr-accent: #00a5a8; }
  .pr-document.kind-regulatory-sus { --pr-accent: #00a5a8; }
  .pr-document.kind-bi-managerial { --pr-accent: #00a5a8; }
  .pr-document.kind-dev-queiroz { --pr-accent: #00a5a8; }
  .pr-document.kind-default { --pr-accent: #00a5a8; }

  .pr-letterhead {
    display: grid;
    grid-template-columns: auto 1px 1fr auto;
    gap: 14px;
    align-items: center;
    padding: 12px 14px;
    margin-bottom: 14px;
    background: #fff;
    border: 1px solid var(--pr-accent);
    border-top: 3px solid var(--pr-accent);
    border-radius: 6px;
  }
  .pr-letterhead-logo {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 4px 6px;
    box-sizing: border-box;
  }
  .pr-letterhead-logo img {
    display: block;
    margin: 0;
    max-height: 59px;
    max-width: 113px;
    object-fit: contain;
  }
  .pr-letterhead-divider {
    align-self: stretch;
    width: 1px;
    background: var(--pr-border);
    min-height: 44px;
  }
  .pr-letterhead-info { min-width: 0; }
  .pr-doc-type {
    margin: 0;
    font-size: 13pt;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--pr-text);
    line-height: 1.2;
  }
  .pr-doc-subtitle {
    margin: 4px 0 0;
    font-size: 9pt;
    color: var(--pr-muted);
    line-height: 1.4;
  }
  .pr-letterhead-code {
    text-align: right;
    flex-shrink: 0;
    min-width: 72px;
  }
  .pr-code-label {
    display: block;
    font-size: 7pt;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--pr-muted);
    margin-bottom: 2px;
  }
  .pr-code-value {
    font-family: 'Courier New', monospace;
    font-size: 9pt;
    font-weight: 700;
    color: var(--pr-accent-dark);
    word-break: break-all;
  }

  .pr-meta-panel {
    margin-bottom: 16px;
    padding: 12px 14px;
    background: var(--pr-accent-soft);
    border: 1px solid var(--pr-border);
    border-radius: 6px;
  }
  .pr-meta-panel-inner {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 16px;
  }
  .pr-meta-main { flex: 1; min-width: 0; }
  .pr-meta-patient-name {
    font-size: 13pt;
    font-weight: 800;
    color: var(--pr-text);
    margin-bottom: 6px;
    line-height: 1.2;
  }
  .pr-meta-details {
    display: flex;
    flex-wrap: wrap;
    gap: 4px 14px;
    font-size: 9pt;
    color: var(--pr-text);
  }
  .pr-meta-detail strong {
    font-weight: 700;
    color: var(--pr-muted);
  }
  .pr-meta-side {
    text-align: right;
    flex-shrink: 0;
  }
  .pr-meta-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 10px 14px;
  }
  .pr-meta-item { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
  .pr-meta-label {
    font-size: 7pt;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--pr-muted);
  }
  .pr-meta-value {
    font-size: 9.5pt;
    font-weight: 600;
    word-break: break-word;
  }

  .pr-section { margin-bottom: 16px; }
  .pr-section.pr-avoid-break { page-break-inside: avoid; break-inside: avoid; }
  .pr-section-title {
    margin: 0;
    padding: 8px 12px;
    background: var(--pr-accent-soft);
    border: 1px solid var(--pr-border);
    border-bottom: none;
    border-radius: 6px 6px 0 0;
    font-size: 9pt;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--pr-accent-dark);
  }
  .pr-section-body {
    padding: 10px 12px 12px;
    border: 1px solid var(--pr-border);
    border-radius: 0 0 6px 6px;
    background: #fff;
    font-size: 9.5pt;
  }

  .pr-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 9pt;
    page-break-inside: auto;
  }
  .pr-table thead { display: table-header-group; }
  .pr-table tr { page-break-inside: avoid; break-inside: avoid; }
  .pr-table th,
  .pr-table td {
    border: none;
    border-bottom: 1px solid var(--pr-border);
    padding: 7px 8px;
    text-align: left;
    vertical-align: top;
  }
  .pr-table th {
    background: var(--pr-accent-softer);
    border-bottom: 2px solid var(--pr-accent);
    font-size: 7.5pt;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--pr-muted);
  }
  .pr-table tbody tr:nth-child(even) td { background: var(--pr-surface); }
  .pr-table tbody tr:last-child td { border-bottom: none; }
  .pr-table .pr-empty {
    text-align: center;
    color: var(--pr-muted);
    font-style: italic;
    padding: 18px !important;
    background: transparent !important;
  }
  .pr-table .pr-danger { color: #c62828; font-weight: 700; }
  .pr-status-pill {
    display: inline-block;
    padding: 2px 10px;
    border-radius: 999px;
    font-size: 7.5pt;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: #fff;
    white-space: nowrap;
  }
  .pr-status-pill.is-success { background: #7cb342; }
  .pr-status-pill.is-warning { background: #f9a825; }
  .pr-status-pill.is-danger { background: #d32f2f; }

  .pr-kpi-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(110px, 1fr));
    gap: 10px;
  }
  .pr-kpi-card {
    border: 1px solid var(--pr-border);
    border-top: 3px solid var(--pr-accent);
    border-radius: 6px;
    padding: 10px 12px;
    text-align: center;
    background: #fff;
  }
  .pr-kpi-card.is-warning { border-top-color: #f59e0b; background: #fffbeb; }
  .pr-kpi-card.is-success { border-top-color: #16a34a; background: #f0fdf4; }
  .pr-kpi-label {
    font-size: 7.5pt;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--pr-muted);
    margin-bottom: 4px;
  }
  .pr-kpi-value { font-size: 16pt; font-weight: 800; color: var(--pr-text); line-height: 1.1; }

  .pr-bar-chart { display: flex; flex-direction: column; gap: 7px; }
  .pr-bar-row {
    display: grid;
    grid-template-columns: 90px 1fr 40px;
    align-items: center;
    gap: 8px;
    font-size: 9pt;
  }
  .pr-bar-label { font-weight: 600; text-align: right; color: var(--pr-muted); }
  .pr-bar-track {
    height: 16px;
    background: #e0f2f2;
    border-radius: 3px;
    overflow: hidden;
  }
  .pr-bar-fill {
    height: 100%;
    background: linear-gradient(90deg, var(--pr-accent), color-mix(in srgb, var(--pr-accent) 55%, #fff));
    border-radius: 3px;
    min-width: 2px;
  }
  .pr-bar-value { font-weight: 700; text-align: right; }

  .pr-prose {
    font-size: 10pt;
    line-height: 1.65;
    white-space: pre-wrap;
    padding: 0;
    border: none;
    background: transparent;
  }

  .pr-info-rows { display: flex; flex-direction: column; gap: 6px; }
  .pr-info-row {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    padding: 5px 0;
    border-bottom: 1px dashed var(--pr-border);
    font-size: 9.5pt;
  }
  .pr-info-row:last-child { border-bottom: none; }
  .pr-info-row strong { color: var(--pr-muted); font-weight: 600; font-size: 9pt; }

  .pr-signatures {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 40px;
    margin-top: 28px;
    page-break-inside: avoid;
    break-inside: avoid;
  }
  .pr-sig-line { border-top: 1px solid #475569; margin-bottom: 6px; height: 40px; }
  .pr-sig-label {
    font-size: 8pt;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--pr-muted);
    text-align: center;
  }

  .pr-print-footer {
    margin-top: 24px;
    padding-top: 8px;
    border-top: 1px solid var(--pr-border);
    font-size: 8pt;
    color: var(--pr-muted);
    line-height: 1.45;
    text-align: right;
    page-break-inside: avoid;
    break-inside: avoid;
  }
  .pr-footer-datetime { font-weight: 600; }

  @media print {
    .pr-document { font-size: 9.5pt; }
    .pr-letterhead, .pr-meta-panel { page-break-inside: avoid; }
    .pr-section-title { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .pr-table thead { display: table-header-group; }
    .pr-table tbody tr:nth-child(even) td,
    .pr-kpi-card,
    .pr-bar-fill,
    .pr-meta-panel,
    .pr-status-pill { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .pr-print-footer {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background: #fff;
      padding: 8px 0 0;
      border-top: 1px solid var(--pr-border);
    }
    .pr-document { padding-bottom: 24mm; }
  }
`;

/** Abre janela dedicada e imprime somente o documento HTML (sem UI do sistema). */
export function printHtmlDocument(html: string, _title?: string): boolean {
  const win = window.open('', '_blank', 'noopener,noreferrer,width=900,height=700');
  if (!win) return false;

  win.document.open();
  win.document.write(html);
  win.document.close();

  const trigger = () => {
    win.focus();
    win.print();
  };

  if (win.document.readyState === 'complete') {
    window.setTimeout(trigger, 250);
  } else {
    win.addEventListener('load', () => window.setTimeout(trigger, 250), { once: true });
  }

  return true;
}
