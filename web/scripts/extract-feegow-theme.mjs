#!/usr/bin/env node
/**
 * Baixa assets públicos do Feegow Clinic para referência local.
 * Uso: npm run sync:feegow-theme
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const outDir = path.join(repoRoot, 'web/public/feegow');

const ASSETS = [
  {
    url: 'https://cdn.feegow.com/feegowclinic-v7/assets/img/login_logo.svg',
    file: 'login_logo.svg',
  },
  {
    url: 'https://cdn.feegow.com/marketing/assets/fw-login/login_bem_vindo.webp',
    file: 'login_bem_vindo.webp',
  },
  {
    url: 'https://cdn.feegow.com/feegowclinic-v7/vendor/bootstrap/4.2.1/bootstrap.min.css',
    file: 'bootstrap.min.css',
  },
];

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const manifest = [];

  for (const asset of ASSETS) {
    const target = path.join(outDir, asset.file);
    const response = await fetch(asset.url);
    if (!response.ok) {
      console.warn('Falha:', asset.url, response.status);
      continue;
    }
    const buffer = Buffer.from(await response.arrayBuffer());
    fs.writeFileSync(target, buffer);
    manifest.push({ ...asset, bytes: buffer.length, saved: path.relative(repoRoot, target) });
    console.log('OK:', asset.file);
  }

  fs.writeFileSync(
    path.join(outDir, 'manifest.json'),
    `${JSON.stringify({ generatedAt: new Date().toISOString(), assets: manifest }, null, 2)}\n`,
  );
  console.log('Assets em:', outDir);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
