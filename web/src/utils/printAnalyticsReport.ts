import type { ReportResultDto } from '../api/client';
import { resolveReportPrintLayout } from '../data/reportPrintLayouts';
import { getReportFieldMapping, resolveSectionTitle } from '../data/reportFieldMappings';
import { escapeHtml, printDocument } from './printDocument';
import {
  buildProfessionalReportBody,
  type ReportMetaField,
} from './professionalReportTemplate';

export type AnalyticsReportPrintMeta = {
  dateFrom: string;
  dateTo: string;
  patientName?: string;
  reportName?: string;
  moduleLabel?: string;
};

function formatPeriodBr(dateFrom: string, dateTo: string) {
  const fmt = (iso: string) => iso.split('-').reverse().join('/');
  return `${fmt(dateFrom)} a ${fmt(dateTo)}`;
}

function buildKpiSection(result: ReportResultDto) {
  if (result.kpis.length === 0) return '';
  const cells = result.kpis
    .map(
      (kpi) => `
        <div class="pr-kpi-card${kpi.variant === 'warning' ? ' is-warning' : kpi.variant === 'success' ? ' is-success' : ''}">
          <div class="pr-kpi-label">${escapeHtml(kpi.label)}</div>
          <div class="pr-kpi-value">${escapeHtml(kpi.value)}</div>
        </div>`,
    )
    .join('');
  return `<div class="pr-kpi-grid">${cells}</div>`;
}

function buildDataTable(result: ReportResultDto) {
  const head = result.columns
    .map((c) => `<th>${escapeHtml(c.label)}</th>`)
    .join('');
  const body = result.rows.length === 0
    ? `<tr><td colspan="${result.columns.length}" class="pr-empty">Sem dados no período selecionado.</td></tr>`
    : result.rows
      .map(
        (row, i) => `<tr class="${i % 2 === 1 ? 'even' : ''}">${result.columns
          .map((c) => `<td>${escapeHtml(String(row[c.key] ?? '—'))}</td>`)
          .join('')}</tr>`,
      )
      .join('');

  return `
    <table class="pr-table">
      <thead><tr>${head}</tr></thead>
      <tbody>${body}</tbody>
    </table>`;
}

function buildBarChart(result: ReportResultDto) {
  const numericCol = result.columns.find(
    (c) => c.key === 'cases' || c.key === 'count' || c.key === 'total' || c.key === 'visits',
  );
  const labelCol = result.columns.find(
    (c) => c.key === 'week' || c.key === 'label' || c.key === 'urgency' || c.key === 'name' || c.key === 'product' || c.key === 'class',
  ) ?? result.columns[0];

  if (!numericCol || !labelCol || result.rows.length === 0) return '';

  const values = result.rows.map((r) => {
    const v = Number(r[numericCol.key]);
    return Number.isFinite(v) ? v : 0;
  });
  const max = Math.max(...values, 1);

  const bars = result.rows
    .map((row, i) => {
      const val = values[i] ?? 0;
      const pct = Math.round((val / max) * 100);
      const label = String(row[labelCol.key] ?? '');
      return `
        <div class="pr-bar-row">
          <div class="pr-bar-label">${escapeHtml(label)}</div>
          <div class="pr-bar-track">
            <div class="pr-bar-fill" style="width:${pct}%"></div>
          </div>
          <div class="pr-bar-value">${val}</div>
        </div>`;
    })
    .join('');

  return `<div class="pr-bar-chart">${bars}</div>`;
}

/**
 * Imprime relatório analítico em A4 profissional (somente o documento, sem UI do sistema).
 */
export function printAnalyticsReport(result: ReportResultDto, meta: AnalyticsReportPrintMeta) {
  const layout = resolveReportPrintLayout(result.code);
  const fieldMap = getReportFieldMapping(result.code);
  const title = meta.reportName ?? result.title;
  const subtitle = fieldMap?.printSubtitle ?? result.subtitle ?? undefined;

  const metaFields: ReportMetaField[] = [
    { label: 'Período', value: formatPeriodBr(meta.dateFrom, meta.dateTo) },
    { label: 'Registros', value: String(result.rows.length) },
  ];
  if (meta.patientName) metaFields.push({ label: 'Paciente', value: meta.patientName });
  if (meta.moduleLabel) metaFields.push({ label: 'Módulo', value: meta.moduleLabel });

  const sections = [];
  if (result.kpis.length > 0) {
    sections.push({
      title: resolveSectionTitle(result.code, 'summary', 'Resumo e indicadores'),
      html: buildKpiSection(result),
      avoidBreak: true,
    });
  }
  if (layout.showChart && result.rows.length > 0) {
    sections.push({
      title: resolveSectionTitle(result.code, 'chart', 'Visualização gráfica'),
      html: buildBarChart(result),
      avoidBreak: true,
    });
  }
  sections.push({
    title: resolveSectionTitle(result.code, 'data', 'Dados detalhados'),
    html: buildDataTable(result),
  });

  const body = buildProfessionalReportBody({
    title,
    subtitle,
    documentType: fieldMap?.documentType ?? layout.documentType,
    code: result.code,
    layoutKind: layout.kind,
    meta: metaFields,
    sections,
    showSignature: layout.showSignatureBlock,
    generatedAt: result.generatedAt,
  });

  printDocument({
    title,
    body,
    pageSize: 'analytics-a4',
    previewWidth: 'lg',
  });
}
