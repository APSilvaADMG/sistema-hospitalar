#!/usr/bin/env node
/**
 * Login no Feegow com perfil persistente do Chrome (evita loop de sessão).
 *
 * IMPORTANTE: abra sempre a URL de login simples, sem ?P=Ocupacao na primeira vez.
 *
 * Uso:
 *   npm run feegow:login
 *   npm run feegow:login -- --headed
 */
import path from 'node:path';
import {
  FEEGOW_DASHBOARD_URL,
  FEEGOW_LOGIN_URL,
  createFeegowContext,
  defaultBrowserProfile,
  defaultOutDir,
  defaultSessionFile,
  ensureDir,
  env,
  fillLoginForm,
  gotoFeegowMain,
  loadPlaywright,
  parseArgs,
  readFeegowToken,
  readLoginError,
  waitForFeegowAuth,
} from './lib.mjs';

const args = parseArgs();
const sessionFile = args.session ?? defaultSessionFile;
const headed = args.headed !== false;

async function main() {
  const chromium = await loadPlaywright();
  ensureDir(defaultOutDir);

  const { context, page } = await createFeegowContext(chromium, { headed });
  const email = env('FEEGOW_EMAIL');
  const password = env('FEEGOW_PASSWORD');

  console.log('');
  console.log('=== Login Feegow ===');
  console.log('1. Use SEMPRE a tela de login simples (sem redirect para Ocupação).');
  console.log('2. Resolva o reCAPTCHA e clique em Entrar.');
  console.log('3. O script detecta o token automaticamente e abre a Ocupação.');
  console.log('');

  await page.goto(FEEGOW_LOGIN_URL, { waitUntil: 'domcontentloaded' });

  if (email && password) {
    await fillLoginForm(page, email, password);
    console.log('E-mail e senha preenchidos. Resolva o reCAPTCHA e clique em Entrar.');
  }

  console.log('Aguardando login (até 3 min)...');

  try {
    const token = await waitForFeegowAuth(page, 180_000);
    console.log('Login OK. Token obtido:', `${token.slice(0, 8)}...`);
  } catch {
    const err = await readLoginError(page);
    console.error(err ? `Erro no login: ${err}` : 'Tempo esgotado aguardando login.');
    console.error('Dica: limpe cookies de app.feegow.com e tente de novo em janela anônima.');
    await context.close();
    process.exit(1);
  }

  console.log('Abrindo Dashboard para estabilizar sessão...');
  await page.goto(FEEGOW_DASHBOARD_URL, { waitUntil: 'domcontentloaded' });
  await page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);

  const tk = await readFeegowToken(page);
  if (!tk) {
    console.error('Sessão perdida após Dashboard. Limpe cookies e tente novamente.');
    await context.close();
    process.exit(1);
  }

  console.log('Abrindo Ocupação...');
  await gotoFeegowMain(page, 'Ocupacao', { Pers: '1' });

  if (/P=Login/i.test(page.url())) {
    console.error('Ainda redirecionando para login. Possíveis causas:');
    console.error('  - cookies bloqueados (desative bloqueio de terceiros para app.feegow.com)');
    console.error('  - conta sem permissão na tela Ocupação');
    console.error('  - sessão corrompida (limpe dados do site no navegador)');
    await context.close();
    process.exit(1);
  }

  await context.storageState({ path: sessionFile });
  console.log('');
  console.log('Sessão salva:', sessionFile);
  console.log('Perfil do navegador:', defaultBrowserProfile);
  console.log('URL atual:', page.url());
  console.log('');
  console.log('Próximo passo: npm run extract:feegow:ocupacao');

  await context.close();
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
