#!/usr/bin/env node
/**
 * Extrai Ocupação usando o token tk copiado do navegador (sem login automático).
 *
 * Como obter o tk:
 *   1. Faça login no Chrome normal em https://app.feegow.com/main/?P=Login
 *   2. F12 → Application → Session Storage → https://app.feegow.com → copie "tk"
 *   3. Rode: set FEEGOW_TK=... && npm run extract:feegow:tk
 */
import path from 'node:path';
import {
  defaultOutDir,
  defaultSessionFile,
  ensureDir,
  env,
  gotoFeegowMain,
  loadPlaywright,
  parseArgs,
  requireEnv,
  writeCsv,
  writeJson,
} from './lib.mjs';

const args = parseArgs();
const outDir = args.out ?? defaultOutDir;
const tk = requireEnv('FEEGOW_TK');

const API_HOSTS = [
  'api.feegow.com',
  'core.feegow.com',
  'quick-queries.feegow.com',
  'frontend.feegow.com',
  'frontend-v2.feegow.com',
];

async function extractTables(page) {
  return page.evaluate(() => {
    const tables = [...document.querySelectorAll('table')];
    return tables.map((table, tableIndex) => {
      const headers = [...table.querySelectorAll('thead th')].map((th) => th.innerText.trim());
      const rows = [...table.querySelectorAll('tbody tr')].map((tr) =>
        [...tr.querySelectorAll('td')].map((td) => td.innerText.replace(/\s+/g, ' ').trim()),
      );
      return { tableIndex, headers, rows };
    });
  });
}

async function main() {
  const chromium = await loadPlaywright();
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ locale: 'pt-BR' });

  await context.addInitScript((token) => {
    sessionStorage.setItem('tk', token);
  }, tk);

  const page = await context.newPage();
  const network = [];

  page.on('response', async (response) => {
    try {
      const url = response.url();
      const host = new URL(url).host;
      if (!API_HOSTS.some((item) => host.includes(item))) return;
      const contentType = response.headers()['content-type'] ?? '';
      if (!/json|text\/plain/.test(contentType)) return;
      const body = await response.text();
      let parsed = body;
      try { parsed = JSON.parse(body); } catch { /* text */ }
      network.push({ url, status: response.status(), body: parsed });
    } catch { /* ignore */ }
  });

  console.log('Abrindo Dashboard com token...');
  await page.goto(`https://app.feegow.com/main/?P=Dashboard&tk=${encodeURIComponent(tk)}`, {
    waitUntil: 'domcontentloaded',
    timeout: 120_000,
  });

  if (/P=Login/i.test(page.url())) {
    console.error('Token inválido ou expirado. Copie um tk novo após login no Chrome.');
    await browser.close();
    process.exit(1);
  }

  console.log('Abrindo Ocupação...');
  await gotoFeegowMain(page, 'Ocupacao', { Pers: '1' });
  await page.waitForTimeout(5000);

  if (/P=Login/i.test(page.url())) {
    console.error('Sem permissão para Ocupação ou token expirado.');
    await browser.close();
    process.exit(1);
  }

  const tables = await extractTables(page);
  const pageText = await page.evaluate(() => document.body.innerText.slice(0, 25000));

  ensureDir(outDir);
  writeJson(path.join(outDir, 'ocupacao-raw.json'), {
    generatedAt: new Date().toISOString(),
    method: 'FEEGOW_TK',
    url: page.url(),
    title: await page.title(),
    tables,
    network,
    pageTextPreview: pageText,
  });

  tables.forEach((table, index) => {
    const rows = table.rows.map((cells) => {
      const row = {};
      table.headers.forEach((header, i) => { row[header || `col_${i + 1}`] = cells[i] ?? ''; });
      return row;
    });
    if (rows.length) writeCsv(path.join(outDir, `ocupacao-table-${index + 1}.csv`), rows);
  });

  writeJson(path.join(outDir, 'network', 'all-responses.json'), network);
  await page.screenshot({ path: path.join(outDir, 'ocupacao-screenshot.png'), fullPage: true });
  await context.storageState({ path: defaultSessionFile });

  console.log('Extração OK:', outDir);
  console.log(`  Tabelas: ${tables.length} | APIs: ${network.length}`);

  await browser.close();
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
