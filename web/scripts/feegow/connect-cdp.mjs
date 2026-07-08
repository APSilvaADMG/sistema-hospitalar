#!/usr/bin/env node
/**
 * Conecta ao seu Chrome já aberto (login manual sem loop do Playwright).
 *
 * Passo 1 — abra o Chrome com depuração (feche todas as janelas do Chrome antes):
 *   "C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="%USERPROFILE%\feegow-chrome"
 *
 * Passo 2 — no Chrome que abriu, faça login em:
 *   https://app.feegow.com/main/?P=Login
 *
 * Passo 3 — rode:
 *   npm run extract:feegow:cdp
 */
import path from 'node:path';
import {
  defaultOutDir,
  defaultSessionFile,
  ensureDir,
  gotoFeegowMain,
  loadPlaywright,
  parseArgs,
  writeCsv,
  writeJson,
} from './lib.mjs';

const args = parseArgs();
const outDir = args.out ?? defaultOutDir;
const cdpUrl = process.env.FEEGOW_CDP_URL ?? 'http://127.0.0.1:9222';

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
  let browser;

  try {
    browser = await chromium.connectOverCDP(cdpUrl);
  } catch {
    console.error('Não conectou ao Chrome em', cdpUrl);
    console.error('');
    console.error('Abra o Chrome assim (feche o Chrome antes):');
    console.error('  chrome.exe --remote-debugging-port=9222 --user-data-dir="%USERPROFILE%\\feegow-chrome"');
    console.error('');
    console.error('Depois faça login em https://app.feegow.com/main/?P=Login');
    process.exit(1);
  }

  const context = browser.contexts()[0] ?? await browser.newContext();
  const page = context.pages().find((p) => p.url().includes('feegow.com')) ?? context.pages()[0] ?? await context.newPage();

  const network = [];
  page.on('response', async (response) => {
    try {
      const url = response.url();
      if (!API_HOSTS.some((h) => new URL(url).host.includes(h))) return;
      const ct = response.headers()['content-type'] ?? '';
      if (!/json|text\/plain/.test(ct)) return;
      const body = await response.text();
      let parsed = body;
      try { parsed = JSON.parse(body); } catch { /* */ }
      network.push({ url, status: response.status(), body: parsed });
    } catch { /* */ }
  });

  const onLogin = /P=Login/i.test(page.url());
  if (onLogin) {
    console.log('Faça login no Chrome aberto, depois pressione Enter aqui...');
    await new Promise((resolve) => {
      process.stdin.resume();
      process.stdin.once('data', resolve);
    });
  }

  console.log('Indo para Ocupação...');
  await gotoFeegowMain(page, 'Ocupacao', { Pers: '1' });
  await page.waitForTimeout(5000);

  if (/P=Login/i.test(page.url())) {
    console.error('Ainda na tela de login. Conclua o login no Chrome e tente de novo.');
    process.exit(1);
  }

  const tables = await extractTables(page);
  const pageText = await page.evaluate(() => document.body.innerText.slice(0, 25000));

  ensureDir(outDir);
  writeJson(path.join(outDir, 'ocupacao-raw.json'), {
    generatedAt: new Date().toISOString(),
    method: 'CDP',
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

  await page.screenshot({ path: path.join(outDir, 'ocupacao-screenshot.png'), fullPage: true });
  await context.storageState({ path: defaultSessionFile });

  console.log('Extração OK:', outDir);
  console.log(`  Tabelas: ${tables.length} | APIs: ${network.length}`);
  console.log('(Chrome permanece aberto — feche manualmente)');
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
