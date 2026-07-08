#!/usr/bin/env node
/**
 * Orquestrador de extração Feegow.
 *
 *   npm run extract:feegow          # API + ocupação (se houver sessão)
 *   npm run extract:feegow -- api
 *   npm run extract:feegow -- ocupacao
 */
import fs from 'node:fs';
import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defaultSessionFile } from './feegow/lib.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const [, , cmd = 'all', ...rest] = process.argv;

function run(script, extraArgs = []) {
  return new Promise((resolve, reject) => {
    const child = spawn(process.execPath, [path.join(__dirname, script), ...extraArgs, ...rest], {
      stdio: 'inherit',
      env: process.env,
    });
    child.on('exit', (code) => {
      if (code === 0) resolve();
      else reject(new Error(`${script} falhou com código ${code}`));
    });
  });
}

async function main() {
  if (cmd === 'api' || cmd === 'all') {
    if (!process.env.FEEGOW_TOKEN?.trim()) {
      console.warn('FEEGOW_TOKEN não definido — pulando extração via API.');
    } else {
      await run('feegow/extract-api.mjs');
    }
  }

  if (cmd === 'ocupacao' || cmd === 'all') {
    if (!fs.existsSync(defaultSessionFile)) {
      console.warn('Sessão do navegador não encontrada — pulando extração da tela Ocupação.');
      console.warn('Rode: npm run feegow:login');
    } else {
      await run('feegow/extract-ocupacao.mjs');
    }
  }

  if (!['all', 'api', 'ocupacao'].includes(cmd)) {
    console.error(`Comando desconhecido: ${cmd}`);
    console.error('Use: all | api | ocupacao');
    process.exit(1);
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
