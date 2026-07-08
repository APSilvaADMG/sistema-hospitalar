#!/usr/bin/env node
/** Corrige seletores quebrados após expandir bayanno → feegow */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const stylesDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '../src/styles');

for (const file of fs.readdirSync(stylesDir)) {
  if (!file.startsWith('bayanno') || !file.endsWith('.css')) continue;
  const filePath = path.join(stylesDir, file);
  let content = fs.readFileSync(filePath, 'utf8');
  const fixed = content.replace(
    /\[data-brand='bayanno'\],\s*\n\[data-brand='feegow'\]([^\n{]*)/g,
    (_, suffix) => `[data-brand='bayanno']${suffix},\n[data-brand='feegow']${suffix}`,
  );
  if (fixed !== content) {
    fs.writeFileSync(filePath, fixed, 'utf8');
    console.log('Fixed:', file);
  }
}
