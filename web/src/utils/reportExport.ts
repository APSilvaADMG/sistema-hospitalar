import type { ReportResultDto } from '../api/client';

function escapeCsv(value: string | number) {
  const text = String(value ?? '');
  if (text.includes(';') || text.includes('"') || text.includes('\n')) {
    return `"${text.replace(/"/g, '""')}"`;
  }
  return text;
}

function fileBaseName(result: ReportResultDto) {
  const slug = result.title
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .toLowerCase();
  return `${slug || result.code}-${new Date().toISOString().slice(0, 10)}`;
}

export function exportReportCsv(result: ReportResultDto) {
  const header = result.columns.map((c) => escapeCsv(c.label)).join(';');
  const lines = result.rows.map((row) =>
    result.columns.map((c) => escapeCsv(String(row[c.key] ?? ''))).join(';'),
  );
  const blob = new Blob([`\uFEFF${[header, ...lines].join('\n')}`], {
    type: 'text/csv;charset=utf-8;',
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${fileBaseName(result)}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

/** Exporta planilha compatível com Excel (HTML table). */
export function exportReportExcel(result: ReportResultDto) {
  const head = result.columns.map((c) => `<th>${escapeHtml(c.label)}</th>`).join('');
  const body = result.rows
    .map(
      (row) => `<tr>${result.columns
        .map((c) => `<td>${escapeHtml(String(row[c.key] ?? ''))}</td>`)
        .join('')}</tr>`,
    )
    .join('');

  const kpiBlock = result.kpis.length
    ? `<table border="1"><tr>${result.kpis.map((k) => `<th>${escapeHtml(k.label)}</th>`).join('')}</tr>
       <tr>${result.kpis.map((k) => `<td>${escapeHtml(k.value)}</td>`).join('')}</tr></table><br/>`
    : '';

  const html = `
    <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel">
    <head><meta charset="utf-8"><title>${escapeHtml(result.title)}</title></head>
    <body>
      <h2>${escapeHtml(result.title)}</h2>
      ${result.subtitle ? `<p>${escapeHtml(result.subtitle)}</p>` : ''}
      ${kpiBlock}
      <table border="1"><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table>
    </body></html>`;

  const blob = new Blob([`\uFEFF${html}`], { type: 'application/vnd.ms-excel;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `${fileBaseName(result)}.xls`;
  link.click();
  URL.revokeObjectURL(url);
}

function escapeHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/** Abre diálogo de impressão para salvar como PDF. */
export function exportReportPdf(printFn: () => void) {
  printFn();
}
