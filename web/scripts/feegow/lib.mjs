import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export const repoRoot = path.resolve(__dirname, '..', '..', '..');
export const defaultOutDir = path.join(repoRoot, 'Diversos/feegow-extract');
export const defaultSessionFile = path.join(repoRoot, 'Diversos/feegow-extract/.feegow-session.json');
export const defaultApiTokenFile = path.join(repoRoot, 'Diversos/feegow-extract/.feegow-api-token');
export const defaultBrowserProfile = path.join(repoRoot, 'Diversos/feegow-extract/.browser-profile');

export const FEEGOW_API_BASE = 'https://api.feegow.com/v1/api';
export const FEEGOW_LOGIN_URL = 'https://app.feegow.com/main/?P=Login';
export const FEEGOW_DASHBOARD_URL = 'https://app.feegow.com/main/?P=Dashboard';
export const DEFAULT_OCUPACAO_URL = 'https://app.feegow.com/main/?P=Ocupacao&Pers=1';

export async function loadPlaywright() {
  try {
    const mod = await import('playwright');
    return mod.chromium;
  } catch {
    console.error('Playwright não instalado. Rode: npm install -D playwright && npx playwright install chromium');
    process.exit(1);
  }
}

export async function createFeegowContext(chromium, { headed = true, profileDir = defaultBrowserProfile } = {}) {
  ensureDir(profileDir);
  const launchOptions = {
    headless: !headed,
    locale: 'pt-BR',
    viewport: { width: 1440, height: 900 },
    args: ['--disable-blink-features=AutomationControlled'],
  };

  try {
    const context = await chromium.launchPersistentContext(profileDir, {
      ...launchOptions,
      channel: 'chrome',
    });
    return { context, page: context.pages()[0] ?? await context.newPage() };
  } catch {
    const context = await chromium.launchPersistentContext(profileDir, launchOptions);
    return { context, page: context.pages()[0] ?? await context.newPage() };
  }
}

export async function readFeegowToken(page) {
  return page.evaluate(() => sessionStorage.getItem('tk') || localStorage.getItem('tk') || '');
}

export function buildFeegowMainUrl(pageName, extra = {}, token = '') {
  const params = new URLSearchParams({ P: pageName, ...extra });
  if (token) params.set('tk', token);
  return `https://app.feegow.com/main/?${params.toString()}`;
}

export async function gotoFeegowMain(page, pageName, extra = {}) {
  const token = await readFeegowToken(page);
  const url = buildFeegowMainUrl(pageName, extra, token);
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);
  return url;
}

export async function waitForFeegowAuth(page, timeoutMs = 180_000) {
  await page.waitForFunction(() => {
    const onLogin = /P=Login/i.test(window.location.href);
    const tk = sessionStorage.getItem('tk') || localStorage.getItem('tk');
    return Boolean(tk) && !onLogin;
  }, { timeout: timeoutMs });
  return readFeegowToken(page);
}

export async function fillLoginForm(page, email, password) {
  await page.locator('#User').fill(email);
  await page.locator('#password').fill(password);
}

export async function readLoginError(page) {
  return page.evaluate(() => {
    const box = document.querySelector('.login-erro');
    if (!box) return '';
    const style = window.getComputedStyle(box);
    if (style.display === 'none' || style.visibility === 'hidden') return '';
    return box.innerText.replace(/\s+/g, ' ').trim();
  });
}

export function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

export function writeJson(filePath, data) {
  ensureDir(path.dirname(filePath));
  fs.writeFileSync(filePath, `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

export function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

export function env(name, fallback = '') {
  const value = process.env[name]?.trim();
  return value || fallback;
}

export function requireEnv(name) {
  const value = env(name);
  if (!value) {
    console.error(`Defina a variável de ambiente ${name}`);
    process.exit(1);
  }
  return value;
}

/** Token API: FEEGOW_TOKEN ou arquivo Diversos/feegow-extract/.feegow-api-token */
export function resolveApiToken() {
  const fromEnv = env('FEEGOW_TOKEN');
  if (fromEnv) return fromEnv;
  if (fs.existsSync(defaultApiTokenFile)) {
    return fs.readFileSync(defaultApiTokenFile, 'utf8').trim();
  }
  console.error('Defina FEEGOW_TOKEN ou salve o token em:');
  console.error(' ', path.relative(repoRoot, defaultApiTokenFile));
  process.exit(1);
}

export function parseArgs(argv = process.argv.slice(2)) {
  const args = { _: [] };
  for (let i = 0; i < argv.length; i += 1) {
    const token = argv[i];
    if (token === '--out') {
      args.out = path.resolve(argv[i + 1]);
      i += 1;
    } else if (token === '--url') {
      args.url = argv[i + 1];
      i += 1;
    } else if (token === '--from') {
      args.from = argv[i + 1];
      i += 1;
    } else if (token === '--to') {
      args.to = argv[i + 1];
      i += 1;
    } else if (token === '--report') {
      args.report = argv[i + 1];
      i += 1;
    } else if (token === '--session') {
      args.session = path.resolve(argv[i + 1]);
      i += 1;
    } else if (token === '--headed') {
      args.headed = true;
    } else {
      args._.push(token);
    }
  }
  return args;
}

export function toCsv(rows) {
  if (!rows.length) return '';
  const headers = [...new Set(rows.flatMap((row) => Object.keys(row)))];
  const escape = (value) => {
    const text = value == null ? '' : String(value);
    if (/[",\n\r]/.test(text)) return `"${text.replace(/"/g, '""')}"`;
    return text;
  };
  const lines = [headers.join(',')];
  for (const row of rows) {
    lines.push(headers.map((key) => escape(row[key])).join(','));
  }
  return `${lines.join('\n')}\n`;
}

export function writeCsv(filePath, rows) {
  ensureDir(path.dirname(filePath));
  fs.writeFileSync(filePath, toCsv(rows), 'utf8');
}

export function todayBr() {
  const now = new Date();
  const dd = String(now.getDate()).padStart(2, '0');
  const mm = String(now.getMonth() + 1).padStart(2, '0');
  const yyyy = now.getFullYear();
  return `${dd}/${mm}/${yyyy}`;
}

export function daysAgoBr(days) {
  const date = new Date();
  date.setDate(date.getDate() - days);
  const dd = String(date.getDate()).padStart(2, '0');
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const yyyy = date.getFullYear();
  return `${dd}/${mm}/${yyyy}`;
}

export async function feegowApiRequest(token, endpoint, { method = 'GET', body } = {}) {
  const url = endpoint.startsWith('http')
    ? endpoint
    : `${FEEGOW_API_BASE}/${endpoint.replace(/^\//, '')}`;

  const response = await fetch(url, {
    method,
    headers: {
      'x-access-token': token,
      ...(body ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  const text = await response.text();
  let data;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }

  if (!response.ok) {
    const message = typeof data === 'object' && data?.content
      ? data.content
      : `HTTP ${response.status}`;
    throw new Error(`${method} ${url} — ${message}`);
  }

  return data;
}

export async function loginFeegow(page, { email, password, loginUrl = FEEGOW_LOGIN_URL } = {}) {
  if (!email || !password) {
    throw new Error('FEEGOW_EMAIL e FEEGOW_PASSWORD são obrigatórios para login automático');
  }

  // Nunca use URL com ?qs=... no login — isso causa loop login ↔ ocupação.
  await page.goto(FEEGOW_LOGIN_URL, { waitUntil: 'domcontentloaded', timeout: 60_000 });
  await fillLoginForm(page, email, password);
  await page.locator('#Entrar').click();

  try {
    await waitForFeegowAuth(page, 90_000);
    return;
  } catch {
    const err = await readLoginError(page);
    throw new Error(
      err
        ? `Login falhou: ${err}`
        : 'Login não concluído (reCAPTCHA ou credenciais). Use npm run feegow:login e faça login manual.',
    );
  }
}

export function flattenTableFromDom(rows) {
  return rows.map((cells) => {
    const row = {};
    cells.forEach((value, index) => {
      row[`col_${index + 1}`] = value;
    });
    return row;
  });
}
