#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const stylesDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '../src/styles');

for (const file of fs.readdirSync(stylesDir)) {
  if (!file.startsWith('bayanno') || !file.endsWith('.css')) continue;
  const filePath = path.join(stylesDir, file);
  let content = fs.readFileSync(filePath, 'utf8');
  const lines = content.split('\n').filter((line) => line.trim() !== ',');
  content = lines.join('\n');
  content = content.replace(/\[data-brand='bayanno'\] +/g, "[data-brand='bayanno'] ");
  content = content.replace(/ +,/g, ',');
  content = content.replace(/,\s*,/g, ',');
  fs.writeFileSync(filePath, content, 'utf8');
  console.log('Cleaned:', file);
}
