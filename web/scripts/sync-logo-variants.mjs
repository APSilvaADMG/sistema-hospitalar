import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'public');
const logoPath = path.join(root, 'logo.svg');
const markPath = path.join(root, 'logo-mark.svg');

let svg = fs.readFileSync(logoPath, 'utf8');

let minX = Infinity;
let maxX = -Infinity;
let minY = Infinity;
let maxY = -Infinity;

function absorb(x, y) {
  if (!Number.isFinite(x) || !Number.isFinite(y)) return;
  minX = Math.min(minX, x);
  maxX = Math.max(maxX, x);
  minY = Math.min(minY, y);
  maxY = Math.max(maxY, y);
}

for (const match of svg.matchAll(/x="([0-9.]+)" y="([0-9.]+)" width="([0-9.]+)" height="([0-9.]+)"/g)) {
  const x = Number(match[1]);
  const y = Number(match[2]);
  absorb(x, y);
  absorb(x + Number(match[3]), y + Number(match[4]));
}

for (const match of svg.matchAll(/points="([^"]+)"/g)) {
  for (const point of match[1].trim().split(/\s+/)) {
    const [x, y] = point.split(',').map(Number);
    absorb(x, y);
  }
}

for (const match of svg.matchAll(/class="fil4"[^>]*d="M([0-9.]+)\s+([0-9.]+)/g)) {
  absorb(Number(match[1]), Number(match[2]));
}

// Extensão estimada da letra H (paths SGH em curvas).
absorb(27200, 18850);

const pad = 100;
const full = {
  x: Math.floor(minX - pad),
  y: Math.floor(minY - pad),
  w: Math.ceil(maxX - minX + pad * 2),
  h: Math.ceil(maxY - minY + pad * 2),
};

const mark = {
  x: full.x,
  y: full.y,
  w: Math.min(8600, full.w),
  h: Math.min(8600, full.h),
};

const fullViewBox = `${full.x} ${full.y} ${full.w} ${full.h}`;
const markViewBox = `${mark.x} ${mark.y} ${mark.w} ${mark.h}`;

svg = svg.replace(/viewBox="[^"]+"/, `viewBox="${fullViewBox}"`);
fs.writeFileSync(logoPath, svg, 'utf8');

const markSvg = svg.replace(/viewBox="[^"]+"/, `viewBox="${markViewBox}"`);
fs.writeFileSync(markPath, markSvg, 'utf8');

const assetPath = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'src', 'assets', 'hospitalLogoAsset.ts');
const asset = `/** Gerado por scripts/sync-logo-variants.mjs a partir de public/logo.svg */
export const HOSPITAL_LOGO_BOUNDS = {
  full: { width: ${full.w}, height: ${full.h} },
  mark: { width: ${mark.w}, height: ${mark.h} },
} as const;

export const HOSPITAL_LOGO_SRC = {
  full: '/logo.svg',
  mark: '/logo-mark.svg',
} as const;

export type HospitalLogoVariant = 'full' | 'mark';

export const HOSPITAL_LOGO_ALT = 'SGH — Sistema de Gestão Hospitalar';

export function hospitalLogoAspect(variant: HospitalLogoVariant = 'full') {
  const bounds = HOSPITAL_LOGO_BOUNDS[variant];
  return bounds.width / bounds.height;
}

export function hospitalLogoDimensions(height: number, variant: HospitalLogoVariant = 'full') {
  const aspect = hospitalLogoAspect(variant);
  return {
    height,
    width: Math.round(height * aspect),
  };
}

export function hospitalLogoSrc(variant: HospitalLogoVariant = 'full') {
  return HOSPITAL_LOGO_SRC[variant];
}
`;

fs.writeFileSync(assetPath, asset, 'utf8');

console.log(JSON.stringify({ full, mark, fullViewBox, markViewBox }, null, 2));
