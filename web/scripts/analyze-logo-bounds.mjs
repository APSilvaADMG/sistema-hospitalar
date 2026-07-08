import fs from 'fs';

const svg = fs.readFileSync(new URL('../public/logo.svg', import.meta.url), 'utf8');

let minX = Infinity;
let maxX = -Infinity;
let minY = Infinity;
let maxY = -Infinity;

function absorb(x, y) {
  if (Number.isFinite(x)) {
    minX = Math.min(minX, x);
    maxX = Math.max(maxX, x);
  }
  if (Number.isFinite(y)) {
    minY = Math.min(minY, y);
    maxY = Math.max(maxY, y);
  }
}

for (const match of svg.matchAll(/x="([0-9.]+)" y="([0-9.]+)" width="([0-9.]+)" height="([0-9.]+)"/g)) {
  const x = Number(match[1]);
  const y = Number(match[2]);
  const w = Number(match[3]);
  const h = Number(match[4]);
  absorb(x, y);
  absorb(x + w, y + h);
}

for (const match of svg.matchAll(/points="([^"]+)"/g)) {
  for (const point of match[1].trim().split(/\s+/)) {
    const [x, y] = point.split(',').map(Number);
    absorb(x, y);
  }
}

for (const match of svg.matchAll(/x="([0-9.]+)" y="([0-9.]+)"/g)) {
  absorb(Number(match[1]), Number(match[2]));
  absorb(Number(match[1]) + 3200, Number(match[2]));
}

const pad = 120;
const full = {
  x: Math.floor(minX - pad),
  y: Math.floor(minY - pad),
  w: Math.ceil(maxX - minX + pad * 2),
  h: Math.ceil(maxY - minY + pad * 2),
};

const mark = {
  x: 4584,
  y: 2343,
  w: 3200,
  h: 3400,
};

console.log(JSON.stringify({ full, mark, raw: { minX, maxX, minY, maxY } }, null, 2));
