#!/usr/bin/env node
/**
 * Extrai dados da tela Ocupação do Feegow.
 * Usa o perfil persistente salvo por npm run feegow:login
 */
import fs from 'node:fs';
import path from 'node:path';
import {
  DEFAULT_OCUPACAO_URL,
  createFeegowContext,
  defaultOutDir,
  defaultSessionFile,
  ensureDir,
  env,
  flattenTableFromDom,
  gotoFeegowMain,
  loadPlaywright,
  loginFeegow,
  parseArgs,
  readFeegowToken,
  waitForFeegowAuth,
  writeCsv,
  writeJson,
} from './lib.mjs';

const args = parseArgs();
const outDir = args.out ?? defaultOutDir;
const sessionFile = args.session ?? defaultSessionFile;
const headed = args.headed === true;

const API_HOSTS = [
  'api.feegow.com',
  'core.feegow.com',
  'quick-queries.feegow.com',
  'frontend.feegow.com',
  'frontend-v2.feegow.com',
];

function safeJsonParse(text) {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function slugify(value) {
  return String(value)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 80) || 'response';
}

async function extractTables(page) {
  return page.evaluate(() => {
    const tables = [...document.querySelectorAll('table')];
    return tables.map((table, tableIndex) => {
      const headers = [...table.querySelectorAll('thead th')].map((th) => th.innerText.trim());
      const bodyRows = [...table.querySelectorAll('tbody tr')].map((tr) =>
        [...tr.querySelectorAll('td')].map((td) => td.innerText.replace(/\s+/g, ' ').trim()),
      );

      if (!headers.length && bodyRows[0]) {
        return {
          tableIndex,
          headers: bodyRows[0].map((_, i) => `col_${i + 1}`),
          rows: bodyRows.slice(1),
        };
      }

      return { tableIndex, headers, rows: bodyRows };
    });
  });
}

async function extractCards(page) {
  return page.evaluate(() => {
    const selectors = ['.card', '.panel', '.box', '[class*="ocup"]', '[class*="metric"]', '[class*="kpi"]'];
    const seen = new Set();
    const items = [];

    for (const selector of selectors) {
      for (const el of document.querySelectorAll(selector)) {
        const text = el.innerText?.replace(/\s+/g, ' ').trim();
        if (!text || text.length < 3 || text.length > 500 || seen.has(text)) continue;
        seen.add(text);
        items.push({ selector, text });
      }
    }
    return items;
  });
}

async function openOcupacao(page) {
  await gotoFeegowMain(page, 'Ocupacao', { Pers: '1' });
  if (!/P=Login/i.test(page.url())) return;

  const email = env('FEEGOW_EMAIL');
  const password = env('FEEGOW_PASSWORD');
  if (email && password) {
    await loginFeegow(page, { email, password });
    await waitForFeegowAuth(page, 120_000);
    await gotoFeegowMain(page, 'Ocupacao', { Pers: '1' });
  }
}

async function main() {
  const chromium = await loadPlaywright();
  const { context, page } = await createFeegowContext(chromium, { headed: headed || !fs.existsSync(sessionFile) });

  const network = [];
  page.on('response', async (response) => {
    try {
      const url = response.url();
      const host = new URL(url).host;
      if (!API_HOSTS.some((item) => host.includes(item))) return;
      const contentType = response.headers()['content-type'] ?? '';
      if (!/json|text\/plain/.test(contentType)) return;
      const body = await response.text();
      network.push({
        url,
        status: response.status(),
        method: response.request().method(),
        contentType,
        body: safeJsonParse(body),
      });
    } catch {
      // ignore
    }
  });

  console.log('Abrindo Ocupação (com token de sessão)...');
  await openOcupacao(page);

  if (/P=Login/i.test(page.url())) {
    console.error('Não autenticado. Rode primeiro: npm run feegow:login');
    console.error('Se o loop continuar, limpe cookies de app.feegow.com e faça login na URL simples:');
    console.error('  https://app.feegow.com/main/?P=Login');
    await context.close();
    process.exit(1);
  }

  const tk = await readFeegowToken(page);
  if (!tk) {
    console.warn('Página aberta mas sem token tk — os dados podem estar incompletos.');
  }

  await page.waitForTimeout(4000);

  const tables = await extractTables(page);
  const cards = await extractCards(page);
  const pageText = await page.evaluate(() => document.body.innerText.slice(0, 20000));

  const payload = {
    generatedAt: new Date().toISOString(),
    url: page.url(),
    title: await page.title(),
    hasToken: Boolean(tk),
    tables,
    cards,
    network,
    pageTextPreview: pageText,
  };

  ensureDir(outDir);
  writeJson(path.join(outDir, 'ocupacao-raw.json'), payload);
  await page.screenshot({ path: path.join(outDir, 'ocupacao-screenshot.png'), fullPage: true });
  await context.storageState({ path: sessionFile });

  tables.forEach((table, index) => {
    const rows = table.rows.map((cells) => {
      const row = {};
      table.headers.forEach((header, i) => {
        const key = header || `col_${i + 1}`;
        row[key] = cells[i] ?? '';
      });
      return row;
    });
    if (rows.length) {
      writeCsv(path.join(outDir, `ocupacao-table-${index + 1}.csv`), rows);
    }
  });

  network.forEach((entry, index) => {
    const name = `${String(index + 1).padStart(2, '0')}-${slugify(new URL(entry.url).pathname)}`;
    writeJson(path.join(outDir, 'network', `${name}.json`), entry);
  });

  if (!tables.length) {
    const fallbackRows = flattenTableFromDom(
      pageText
        .split('\n')
        .map((line) => line.trim())
        .filter(Boolean)
        .slice(0, 200)
        .map((line) => line.split(/\s{2,}|\t/)),
    );
    writeCsv(path.join(outDir, 'ocupacao-text-fallback.csv'), fallbackRows);
  }

  console.log('Extração concluída:', outDir);
  console.log(`  Tabelas HTML: ${tables.length}`);
  console.log(`  Respostas de API: ${network.length}`);
  console.log(`  Cards/painéis: ${cards.length}`);

  await context.close();
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
