import type { TissGuideDto } from '../api/client';
import { formatBrDate, formatBrDateTime } from './dateUtils';
import { escapeHtml, logoImg } from './printDocument';

/** Estilo de impressão das guias TISS/FUNI — padrão Feegow com identidade Bayanno/teal APSMedCore. */
export const FEEGOW_GUIDE_PRINT_CSS = `
  :root {
    --fg-accent: #00a5a8;
    --fg-accent-dark: #008b8e;
    --fg-accent-soft: rgba(0, 165, 168, 0.14);
    --fg-accent-softer: rgba(0, 165, 168, 0.06);
    --fg-text: #1e293b;
    --fg-muted: #64748b;
    --fg-border: #b8d4d5;
    --fg-cell-border: #5a6a6b;
  }
  * { box-sizing: border-box; }
  .fg-guide-sheet {
    background: #fff;
    color: var(--fg-text);
    font-family: Arial, Helvetica, 'Segoe UI', sans-serif;
    font-size: 10px;
    line-height: 1.35;
    width: 100%;
  }
  .fg-guide-header {
    display: grid;
    grid-template-columns: 100px 1fr 140px;
    gap: 10px;
    align-items: start;
    padding: 10px 12px;
    margin-bottom: 8px;
    border: 1px solid var(--fg-accent);
    border-top: 3px solid var(--fg-accent);
    border-radius: 4px;
    background: #fff;
  }
  .fg-guide-logo {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 4px 6px;
    box-sizing: border-box;
  }
  .fg-guide-logo img {
    display: block;
    max-width: 124px;
    max-height: 62px;
    object-fit: contain;
  }
  .fg-guide-header-main { min-width: 0; text-align: center; }
  .fg-guide-title {
    font-weight: 800;
    font-size: 14px;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    color: var(--fg-text);
    line-height: 1.2;
  }
  .fg-guide-subtitle {
    margin-top: 3px;
    font-size: 9px;
    color: var(--fg-muted);
  }
  .fg-guide-meta {
    text-align: right;
    font-size: 8px;
    color: var(--fg-muted);
  }
  .fg-guide-meta strong { color: var(--fg-text); }
  .fg-guide-operator {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 3px;
    text-align: right;
    font-size: 8px;
  }
  .fg-guide-operator-name { font-weight: 700; max-width: 130px; line-height: 1.2; }
  .fg-guide-operator-ans { color: var(--fg-muted); }
  .fg-section {
    border: 1px solid var(--fg-border);
    margin-bottom: 5px;
    break-inside: avoid;
    page-break-inside: avoid;
    border-radius: 3px;
    overflow: hidden;
  }
  .fg-section-title {
    background: var(--fg-accent-soft);
    color: var(--fg-accent-dark);
    font-weight: 800;
    padding: 3px 8px;
    border-bottom: 1px solid var(--fg-border);
    font-size: 9px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
  .fg-section-body {
    padding: 5px 6px;
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    gap: 3px 6px;
  }
  .fg-field { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
  .fg-field label {
    font-size: 7.5px;
    font-weight: 700;
    color: var(--fg-muted);
    line-height: 1.15;
  }
  .fg-value {
    display: block;
    border: 1px solid var(--fg-cell-border);
    padding: 2px 4px;
    min-height: 18px;
    background: #fff;
    font-size: 10px;
    word-break: break-word;
  }
  .fg-value--textarea { min-height: 44px; white-space: pre-wrap; }
  .fg-span-2 { grid-column: span 2; }
  .fg-span-3 { grid-column: span 3; }
  .fg-span-4 { grid-column: span 4; }
  .fg-span-6 { grid-column: span 6; }
  .fg-span-8 { grid-column: span 8; }
  .fg-span-12 { grid-column: span 12; }
  .fg-char-row { display: flex; flex-wrap: nowrap; gap: 1px; }
  .fg-char-cell {
    width: 13px;
    height: 16px;
    text-align: center;
    font-size: 9px;
    border: 1px solid var(--fg-cell-border);
    line-height: 16px;
    display: inline-block;
    background: #fff;
  }
  .fg-table-wrap { padding: 3px; grid-column: span 12; }
  .fg-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 8px;
  }
  .fg-table th, .fg-table td {
    border: 1px solid var(--fg-cell-border);
    padding: 2px 3px;
    vertical-align: top;
    text-align: left;
  }
  .fg-table th {
    background: var(--fg-accent-softer);
    font-weight: 700;
    font-size: 7.5px;
    text-transform: uppercase;
    letter-spacing: 0.03em;
    color: var(--fg-muted);
  }
  .fg-totals-row {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 4px;
    padding: 4px 6px 6px;
    grid-column: span 12;
  }
  .fg-total-field label {
    display: block;
    font-size: 7px;
    font-weight: 700;
    color: var(--fg-muted);
    margin-bottom: 2px;
  }
  .fg-total-field .fg-value { text-align: right; font-weight: 700; }
  .fg-signatures {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px;
    margin-top: 8px;
    break-inside: avoid;
    page-break-inside: avoid;
  }
  .fg-signature-box {
    border: 1px solid var(--fg-border);
    padding: 4px 6px;
    font-size: 8px;
    min-height: 56px;
    border-radius: 3px;
  }
  .fg-signature-label { margin-bottom: 4px; font-weight: 600; color: var(--fg-muted); }
  .fg-signature-line {
    margin-top: 8px;
    min-height: 40px;
    border-bottom: 1px solid #334155;
  }
  .fg-print-footer {
    margin-top: 10px;
    padding-top: 6px;
    border-top: 1px solid var(--fg-border);
    font-size: 8px;
    color: var(--fg-muted);
    text-align: right;
    break-inside: avoid;
    page-break-inside: avoid;
  }
  .fg-print-footer-datetime { font-weight: 600; }

  /* Compatibilidade com formulários FUNI impressos via DOM */
  .funi-guide-sheet {
    background: #fff;
    color: var(--fg-text);
    font-family: Arial, Helvetica, 'Segoe UI', sans-serif;
    font-size: 10px;
    line-height: 1.35;
    width: 100%;
  }
  .funi-guide-header {
    display: grid;
    grid-template-columns: 100px 1fr 140px;
    gap: 10px;
    align-items: start;
    padding: 10px 12px;
    margin-bottom: 8px;
    border: 1px solid var(--fg-accent);
    border-top: 3px solid var(--fg-accent);
    border-radius: 4px;
  }
  .funi-guide-logo {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 4px 6px;
    box-sizing: border-box;
  }
  .funi-guide-logo img { display: block; max-width: 124px; max-height: 62px; object-fit: contain; }
  .funi-guide-title {
    text-align: center;
    font-weight: 800;
    font-size: 14px;
    letter-spacing: 0.04em;
    text-transform: uppercase;
  }
  .funi-guide-subtitle { text-align: center; font-size: 9px; color: var(--fg-muted); }
  .funi-guide-meta { text-align: right; font-size: 8px; color: var(--fg-muted); }
  .funi-guide-operator { align-items: flex-end; text-align: right; }
  .funi-section {
    border: 1px solid var(--fg-border);
    margin-bottom: 5px;
    break-inside: avoid;
    page-break-inside: avoid;
    border-radius: 3px;
    overflow: hidden;
  }
  .funi-section-title {
    background: var(--fg-accent-soft);
    color: var(--fg-accent-dark);
    font-weight: 800;
    padding: 3px 8px;
    border-bottom: 1px solid var(--fg-border);
    font-size: 9px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
  .funi-section-body {
    padding: 5px 6px;
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    gap: 3px 6px;
  }
  .funi-field label { color: var(--fg-muted); font-size: 7.5px; font-weight: 700; }
  .funi-print-value {
    display: block;
    border: 1px solid var(--fg-cell-border);
    padding: 2px 4px;
    min-height: 18px;
    background: #fff;
    font-size: 10px;
    word-break: break-word;
  }
  .funi-print-value--textarea { min-height: 48px; white-space: pre-wrap; }
  .funi-print-char {
    width: 13px;
    height: 16px;
    text-align: center;
    font-size: 9px;
    border: 1px solid var(--fg-cell-border);
    line-height: 16px;
    display: inline-block;
  }
  .funi-med-table th { background: var(--fg-accent-softer); }
  .funi-signature-box { border-color: var(--fg-border); border-radius: 3px; }
  .funi-hospital-brand, .funi-guide-hospital-brand { display: none !important; }
  .funi-operator-banner { display: none !important; }
`;

/** CSS aplicável aos formulários FUNI (tela + impressão DOM). */
export const FUNI_GUIDE_FORM_CSS = `
  .funi-guide-sheet {
    background: #fff;
    border: 1px solid var(--fg-border, #b8d4d5);
    border-top: 3px solid var(--fg-accent, #00a5a8);
    padding: 12px 16px 20px;
    font-size: 11px;
    color: var(--fg-text, #1e293b);
    border-radius: 4px;
  }
  .funi-guide-header {
    display: grid;
    grid-template-columns: 100px 1fr 140px;
    gap: 10px;
    align-items: start;
    padding-bottom: 8px;
    margin-bottom: 8px;
    border-bottom: 1px solid var(--fg-border, #b8d4d5);
  }
  .funi-guide-logo {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 4px 6px;
    box-sizing: border-box;
  }
  .funi-guide-logo img {
    display: block;
    max-width: 124px;
    max-height: 62px;
    object-fit: contain;
  }
  .funi-guide-title {
    text-align: center;
    font-weight: 800;
    font-size: 15px;
    letter-spacing: 0.04em;
    color: var(--fg-text, #1e293b);
  }
  .funi-guide-subtitle {
    text-align: center;
    font-size: 10px;
    color: var(--fg-muted, #64748b);
  }
  .funi-guide-meta { text-align: right; font-size: 10px; }
  .funi-guide-operator {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 4px;
    text-align: right;
  }
  .funi-guide-operator-logo { max-width: 52px; max-height: 52px; border-radius: 6px; }
  .funi-guide-operator-name { font-size: 8px; font-weight: 700; max-width: 130px; }
  .funi-guide-operator-ans { font-size: 8px; color: var(--fg-muted, #64748b); }
  .funi-section {
    border: 1px solid var(--fg-border, #b8d4d5);
    margin-bottom: 6px;
    border-radius: 3px;
    overflow: hidden;
  }
  .funi-section-title {
    background: rgba(0, 165, 168, 0.14);
    color: #008b8e;
    font-weight: 800;
    padding: 3px 8px;
    border-bottom: 1px solid var(--fg-border, #b8d4d5);
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
  .funi-field input,
  .funi-field select,
  .funi-field textarea {
    border: 1px solid #8aa3a4;
    background: #fafefe;
  }
  .funi-med-table th { background: rgba(0, 165, 168, 0.08); }
`;

export function buildGuidePrintFooter(generatedAt?: string): string {
  const when = formatBrDateTime(generatedAt ?? new Date().toISOString());
  return `
    <footer class="fg-print-footer">
      <span class="fg-print-footer-datetime">Impresso em: ${escapeHtml(when)}</span>
    </footer>`;
}

type GuideHeaderOptions = {
  title: string;
  subtitle?: string;
  metaHtml?: string;
  operatorName?: string;
  operatorAns?: string;
};

export function buildGuidePrintHeader(options: GuideHeaderOptions): string {
  const operatorBlock = options.operatorName
    ? `
      <div class="fg-guide-operator">
        <div class="fg-guide-operator-name">${escapeHtml(options.operatorName)}</div>
        ${options.operatorAns ? `<div class="fg-guide-operator-ans">ANS ${escapeHtml(options.operatorAns)}</div>` : ''}
      </div>`
    : (options.metaHtml ?? '<div class="fg-guide-meta"></div>');

  return `
    <header class="fg-guide-header">
      <div class="fg-guide-logo">${logoImg(56)}</div>
      <div class="fg-guide-header-main">
        <div class="fg-guide-title">${escapeHtml(options.title)}</div>
        ${options.subtitle ? `<div class="fg-guide-subtitle">${escapeHtml(options.subtitle)}</div>` : ''}
      </div>
      ${operatorBlock}
    </header>`;
}

function field(label: string, value: string, span = 3): string {
  const safe = escapeHtml(value || '—');
  return `
    <div class="fg-field fg-span-${span}">
      <label>${escapeHtml(label)}</label>
      <div class="fg-value">${safe}</div>
    </div>`;
}

function textareaField(label: string, value: string): string {
  const safe = escapeHtml(value || '—');
  return `
    <div class="fg-field fg-span-12">
      <label>${escapeHtml(label)}</label>
      <div class="fg-value fg-value--textarea">${safe}</div>
    </div>`;
}

function section(title: string, body: string): string {
  return `
    <div class="fg-section">
      <div class="fg-section-title">${escapeHtml(title)}</div>
      <div class="fg-section-body">${body}</div>
    </div>`;
}

function money(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function guideTypeName(type: number) {
  const map: Record<number, string> = {
    1: 'Guia de Consulta',
    2: 'Guia SP/SADT',
    3: 'Internação',
    4: 'Resumo de Internação',
    5: 'Honorários Individuais',
    6: 'Solicitação de Internação',
    7: 'Outras Despesas',
    9: 'Prorrogação de Internação',
    10: 'Recurso de Glosas',
    11: 'Demonstrativo de Pagamento',
    12: 'Tratamento Odontológico',
    16: 'Anexo OPME',
    17: 'Anexo Quimioterapia',
    18: 'Anexo Radioterapia',
  };
  return map[type] ?? `Tipo ${type}`;
}

function funiCodeForType(type: number): string {
  const map: Record<number, string> = {
    1: 'FUNI 21 — Rev. 01',
    2: 'FUNI 13 — Rev. 01',
    4: 'FUNI 23 — Rev. 01',
    5: 'FUNI 24 — Rev. 01',
    6: 'FUNI 22 — Rev. 02',
    7: 'FUNI 25 — Rev. 01',
    9: 'FUNI 39 — Rev. 02',
    10: 'FUNI 59 — Rev. 00',
    11: 'FUNI 58 — Rev. 00',
    16: 'FUNI 54 — Rev. 01',
    17: 'FUNI 55 — Rev. 00',
    18: 'FUNI 56 — Rev. 00',
  };
  return map[type] ?? 'Padrão TISS — ANS';
}

function statusName(status: number) {
  const map: Record<number, string> = {
    1: 'Rascunho', 2: 'Enviada', 3: 'Paga', 4: 'Glosa', 5: 'Cancelada',
  };
  return map[status] ?? String(status);
}

function proceduresTable(guide: TissGuideDto, extended = false): string {
  const rows = guide.items.length > 0
    ? guide.items.map((item) => `
      <tr>
        <td>${escapeHtml(item.tussCode)}</td>
        <td>${escapeHtml(item.description)}</td>
        <td style="text-align:center">${item.quantity}</td>
        ${extended ? '<td>—</td><td>—</td>' : ''}
        <td style="text-align:right">${money(item.unitPrice)}</td>
        <td style="text-align:right">${money(item.total)}</td>
      </tr>`).join('')
    : `<tr><td colspan="${extended ? 7 : 5}" style="text-align:center;color:#64748b;font-style:italic">Nenhum procedimento registrado</td></tr>`;

  return `
    <div class="fg-table-wrap">
      <table class="fg-table">
        <thead>
          <tr>
            <th>Código</th>
            <th>Descrição</th>
            <th>Qtd</th>
            ${extended ? '<th>Téc</th><th>Tabela</th>' : ''}
            <th>Valor Unit.</th>
            <th>Valor Total</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
        <tfoot>
          <tr>
            <td colspan="${extended ? 6 : 4}" style="text-align:right;font-weight:700">Total Geral</td>
            <td style="text-align:right;font-weight:700">${money(guide.totalAmount)}</td>
          </tr>
        </tfoot>
      </table>
    </div>`;
}

function beneficiarySection(guide: TissGuideDto, extra = ''): string {
  return section('Dados do Beneficiário', `
    ${field('Nome', guide.patientName, 8)}
    ${field('Convênio', guide.healthInsuranceName, 4)}
    ${field('Nº da Carteira', guide.beneficiaryCardNumber ?? '', 4)}
    ${field('Plano', guide.beneficiaryPlanName ?? '', 4)}
    ${field('Cartão Nacional de Saúde', guide.beneficiaryCns ?? '', 4)}
    ${field('Nº da Guia no Prestador', guide.guideNumber, 4)}
    ${field('Senha / Autorização', guide.authorizationPassword ?? '', 4)}
    ${field('Data de Emissão', formatBrDate(guide.createdAt), 4)}
  ${extra}`);
}

function professionalSection(guide: TissGuideDto): string {
  const c = guide.clinical;
  return section('Dados do Contratado / Profissional', `
    ${field('Profissional Solicitante', c.requestingProfessionalName ?? '', 6)}
    ${field('CRM / Conselho', c.requestingProfessionalCrm ?? '', 3)}
    ${field('Profissional Executante', c.executingProfessionalName ?? '', 6)}
    ${field('CRM Executante', c.executingProfessionalCrm ?? '', 3)}
    ${field('CID Principal', c.cid10Code ?? '', 3)}
    ${field('CID Secundário', c.cid10Secondary ?? '', 3)}
    ${textareaField('Indicação Clínica / Justificativa', c.clinicalJustification ?? '')}
  `);
}

function hospitalizationSection(guide: TissGuideDto): string {
  const c = guide.clinical;
  return section('Dados da Internação', `
    ${field('Data de Admissão', c.admissionDate ? formatBrDate(c.admissionDate) : '', 3)}
    ${field('Data de Alta', c.dischargeDate ? formatBrDate(c.dischargeDate) : '', 3)}
    ${field('Tipo de Acomodação', c.requestedBedType ?? '', 3)}
    ${field('Local / Unidade', guide.serviceUnitName ?? '', 6)}
    ${field('Caráter do Atendimento', c.serviceCharacter != null ? String(c.serviceCharacter) : '', 3)}
    ${field('Indicação de Acidente', c.accidentIndicator != null ? String(c.accidentIndicator) : '', 3)}
  `);
}

function signatureSection(): string {
  return `
    <div class="fg-signatures">
      <div class="fg-signature-box">
        <div class="fg-signature-label">Assinatura do Profissional Executante</div>
        <div class="fg-signature-line"></div>
      </div>
      <div class="fg-signature-box">
        <div class="fg-signature-label">Assinatura do Beneficiário ou Responsável</div>
        <div class="fg-signature-line"></div>
      </div>
    </div>`;
}

function totalsSection(guide: TissGuideDto): string {
  const total = money(guide.totalAmount);
  return `
    <div class="fg-section">
      <div class="fg-section-title">Totais</div>
      <div class="fg-totals-row">
        <div class="fg-total-field"><label>Procedimentos</label><div class="fg-value">${total}</div></div>
        <div class="fg-total-field"><label>Taxas e Aluguéis</label><div class="fg-value">R$ 0,00</div></div>
        <div class="fg-total-field"><label>Materiais</label><div class="fg-value">R$ 0,00</div></div>
        <div class="fg-total-field"><label>Total Geral</label><div class="fg-value">${total}</div></div>
      </div>
    </div>`;
}

function buildByGuideType(guide: TissGuideDto): string {
  const notesBlock = guide.notes
    ? section('Observações / Justificativa', textareaField('Observações', guide.notes))
    : '';

  switch (guide.guideType) {
    case 1:
      return `
        ${beneficiarySection(guide)}
        ${professionalSection(guide)}
        ${section('Dados do Atendimento — Procedimento Realizado', proceduresTable(guide))}
        ${notesBlock}
        ${signatureSection()}`;

    case 2:
      return `
        ${beneficiarySection(guide, field('CID', guide.clinical.cid10Code ?? '', 4))}
        ${professionalSection(guide)}
        ${section('Dados de Execução', `
          ${field('Local / Contratado', guide.serviceUnitName ?? '', 6)}
          ${field('Regime de Atendimento', 'Ambulatorial', 3)}
          ${field('Caráter do Atendimento', guide.clinical.serviceCharacter != null ? String(guide.clinical.serviceCharacter) : '', 3)}
        `)}
        ${section('Procedimentos e Exames Realizados', proceduresTable(guide, true))}
        ${totalsSection(guide)}
        ${notesBlock}
        ${signatureSection()}`;

    case 5:
      return `
        ${beneficiarySection(guide)}
        ${section('Dados do Contratado — Local do Procedimento', `
          ${field('Hospital / Local', guide.serviceUnitName ?? '', 8)}
          ${field('Código CNES', '', 4)}
        `)}
        ${hospitalizationSection(guide)}
        ${section('Procedimentos Realizados', proceduresTable(guide, true))}
        ${section('Resumo Financeiro', `
          ${field('Total Honorário', money(guide.totalAmount), 4)}
          ${field('Data de Emissão', formatBrDate(guide.createdAt), 4)}
        `)}
        ${notesBlock}
        ${signatureSection()}`;

    case 6:
      return `
        ${beneficiarySection(guide)}
        ${professionalSection(guide)}
        ${hospitalizationSection(guide)}
        ${section('Procedimentos Solicitados', proceduresTable(guide, true))}
        ${section('Dados da Autorização', `
          ${field('Senha', guide.authorizationPassword ?? '', 4)}
          ${field('Status', statusName(guide.status), 4)}
        `)}
        ${notesBlock}
        ${signatureSection()}`;

    case 4:
      return `
        ${beneficiarySection(guide)}
        ${hospitalizationSection(guide)}
        ${section('Procedimentos e Exames Realizados', proceduresTable(guide, true))}
        ${totalsSection(guide)}
        ${notesBlock}
        ${signatureSection()}`;

    default:
      return `
        ${beneficiarySection(guide)}
        ${professionalSection(guide)}
        ${section('Procedimentos / Itens', proceduresTable(guide, true))}
        ${totalsSection(guide)}
        ${notesBlock}
        ${signatureSection()}`;
  }
}

export function buildTissGuidePrintBody(guide: TissGuideDto): string {
  const title = guideTypeName(guide.guideType).toUpperCase();
  const subtitle = `${funiCodeForType(guide.guideType)} · Padrão TISS — ANS · ${guide.guideNumber}`;

  return `
    <article class="fg-guide-sheet">
      ${buildGuidePrintHeader({
        title,
        subtitle,
        operatorName: guide.healthInsuranceName,
      })}
      ${buildByGuideType(guide)}
      ${buildGuidePrintFooter()}
    </article>`;
}

export function buildGuidePrintHtmlDocument(title: string, body: string): string {
  const safeTitle = escapeHtml(title);
  return `<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8" />
  <title>${safeTitle}</title>
  <style>
    @page { size: A4 portrait; margin: 10mm; }
    html, body { margin: 0; padding: 0; background: #fff; }
    body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .print-funi-a4 { width: 100%; max-width: 190mm; margin: 0 auto; }
    ${FEEGOW_GUIDE_PRINT_CSS}
    @media print {
      .print-funi-a4 { max-width: none; width: auto; }
      .fg-print-footer {
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        background: #fff;
        padding: 6px 0 0;
      }
      .fg-guide-sheet { padding-bottom: 16mm; }
      .fg-section-title, .fg-table th, .fg-guide-header {
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
      }
    }
  </style>
</head>
<body>
  <div class="print-funi-a4">
    ${body}
  </div>
</body>
</html>`;
}
