#!/usr/bin/env node
/**
 * Remove perfil/sessão corrompidos do Feegow (use se estiver em loop).
 */
import fs from 'node:fs';
import { defaultBrowserProfile, defaultSessionFile } from './lib.mjs';

function rm(target) {
  if (!fs.existsSync(target)) return false;
  fs.rmSync(target, { recursive: true, force: true });
  return true;
}

const removed = [
  rm(defaultBrowserProfile) && '.browser-profile',
  rm(defaultSessionFile) && '.feegow-session.json',
].filter(Boolean);

if (removed.length) {
  console.log('Removido:', removed.join(', '));
} else {
  console.log('Nada para remover.');
}
console.log('Agora use o Chrome normal ou npm run extract:feegow:cdp');
