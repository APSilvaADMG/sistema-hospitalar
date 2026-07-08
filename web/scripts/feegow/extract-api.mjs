#!/usr/bin/env node
/**
 * Extrai dados do Feegow via API oficial (recomendado).
 *
 * Token: Configurações > Integrações > API no painel Feegow.
 *
 * Uso:
 *   set FEEGOW_TOKEN=seu_token
 *   npm run extract:feegow:api
 *   npm run extract:feegow:api -- --report schedule-appointments --from 01/06/2026 --to 10/06/2026
 */
import path from 'node:path';
import {
  daysAgoBr,
  defaultOutDir,
  feegowApiRequest,
  parseArgs,
  resolveApiToken,
  todayBr,
  writeCsv,
  writeJson,
} from './lib.mjs';

const args = parseArgs();
const outDir = args.out ?? defaultOutDir;
const token = resolveApiToken();
const dataInicio = args.from ?? daysAgoBr(30);
const dataFim = args.to ?? todayBr();

function pickOccupationReports(reports) {
  const list = Array.isArray(reports) ? reports : [];
  const pattern = /ocup|occup|agenda|schedule|atendimento|mapa/i;
  return list.filter((item) => pattern.test(`${item.Relatorio ?? ''} ${item.Arquivo ?? ''} ${item.Ct ?? ''}`));
}

async function generateReport(reportFile, extra = {}) {
  return feegowApiRequest(token, 'reports/generate', {
    method: 'POST',
    body: {
      report: reportFile,
      DATA_INICIO: dataInicio,
      DATA_FIM: dataFim,
      ...extra,
    },
  });
}

async function searchAppointments() {
  const start = dataInicio.replace(/\//g, '-').replace(/^(\d{2})-(\d{2})-(\d{4})$/, '$1-$2-$3');
  const end = dataFim.replace(/\//g, '-').replace(/^(\d{2})-(\d{2})-(\d{4})$/, '$1-$2-$3');
  return feegowApiRequest(
    token,
    `appoints/search?data_start=${encodeURIComponent(start)}&data_end=${encodeURIComponent(end)}&list_procedures=1`,
  );
}

async function main() {
  console.log('Consultando API Feegow...');

  const reports = await feegowApiRequest(token, 'reports/list');
  const occupationReports = pickOccupationReports(reports);

  const generated = [];
  const targets = args.report
    ? occupationReports.filter((item) => item.Arquivo === args.report)
    : occupationReports;

  if (!targets.length && args.report) {
    generated.push({
      report: args.report,
      result: await generateReport(args.report),
    });
  } else {
    for (const item of targets) {
      try {
        const result = await generateReport(item.Arquivo);
        generated.push({ meta: item, result });
        console.log(`  OK relatório: ${item.Relatorio} (${item.Arquivo})`);
      } catch (error) {
        generated.push({ meta: item, error: error.message });
        console.warn(`  Falha relatório ${item.Arquivo}: ${error.message}`);
      }
    }
  }

  let appointments = null;
  try {
    appointments = await searchAppointments();
    console.log('  OK agendamentos no período');
  } catch (error) {
    console.warn(`  Falha agendamentos: ${error.message}`);
  }

  const payload = {
    generatedAt: new Date().toISOString(),
    period: { from: dataInicio, to: dataFim },
    reportsAvailable: reports,
    occupationReports,
    generatedReports: generated,
    appointments,
  };

  writeJson(path.join(outDir, 'api-inventory.json'), payload);

  for (const entry of generated) {
    const rows = entry.result?.data;
    if (!Array.isArray(rows) || !rows.length) continue;
    const file = entry.meta?.Arquivo ?? entry.report ?? 'report';
    writeCsv(path.join(outDir, `report-${file}.csv`), rows);
    writeJson(path.join(outDir, `report-${file}.json`), rows);
  }

  if (appointments?.content && Array.isArray(appointments.content)) {
    writeCsv(path.join(outDir, 'appointments.csv'), appointments.content);
    writeJson(path.join(outDir, 'appointments.json'), appointments.content);
  } else if (Array.isArray(appointments)) {
    writeCsv(path.join(outDir, 'appointments.csv'), appointments);
    writeJson(path.join(outDir, 'appointments.json'), appointments);
  }

  console.log('Extração API salva em:', outDir);
  console.log(`Relatórios de ocupação/agenda encontrados: ${occupationReports.length}`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
