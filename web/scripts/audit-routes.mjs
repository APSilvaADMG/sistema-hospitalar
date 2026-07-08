import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const src = path.join(__dirname, '../src');

const routeMapSrc = fs.readFileSync(path.join(src, 'navigation/routeMap.tsx'), 'utf8');
const implemented = [...routeMapSrc.matchAll(/'(\/[^']+)':/g)].map((m) => m[1]);
const hubSrc = fs.readFileSync(path.join(src, 'navigation/hubRoutes.ts'), 'utf8');
const hubPrefixes = [...hubSrc.matchAll(/'(\/[^']+)'/g)].map((m) => m[1]).filter((p) => p.startsWith('/'));

function resolves(pathname) {
  if (implemented.includes(pathname)) return { kind: 'exact', root: pathname };
  const sorted = implemented.filter((k) => k !== '/').sort((a, b) => b.length - a.length);
  for (const root of sorted) {
    if (pathname === root || pathname.startsWith(`${root}/`)) {
      return { kind: 'prefix', root };
    }
  }
  return null;
}

// Collect all Link/to/href paths from src
const linkPaths = new Set();
function scanDir(dir) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory() && ent.name !== 'node_modules') scanDir(full);
    else if (/\.(tsx?|jsx?)$/.test(ent.name)) {
      const text = fs.readFileSync(full, 'utf8');
      for (const m of text.matchAll(/(?:to|path):\s*['"](\/[^'"]+)['"]/g)) linkPaths.add(m[1]);
      for (const m of text.matchAll(/navigate\(['"](\/[^'"]+)['"]/g)) linkPaths.add(m[1]);
    }
  }
}
scanDir(src);

const orphans = [...linkPaths].filter((p) => !resolves(p)).sort();
console.log('=== Links internos sem rota implementada ===');
orphans.forEach((p) => console.log(p));

// Menu paths
const menuSrc = fs.readFileSync(path.join(src, 'navigation/sidebarMenu.ts'), 'utf8');
const menuPaths = [...menuSrc.matchAll(/leaf\([^)]*'(\/[^']+)'/g)].map((m) => m[1]);
const menuOrphans = [...new Set(menuPaths)].filter((p) => !resolves(p));
console.log('\n=== Itens do menu sem rota ===');
menuOrphans.forEach((p) => console.log(p));

// Tab paths from moduleSections
const modSrc = fs.readFileSync(path.join(src, 'navigation/moduleSections.ts'), 'utf8');
const tabBlocks = [...modSrc.matchAll(/export const (\w+): ModuleTab\[\] = \[([\s\S]*?)\];/g)];
const bases = {
  emergencyTabs: '/emergencia',
  hospitalizationTabs: '/internacao',
  surgeryTabs: '/centro-cirurgico',
  icuTabs: '/uti',
  appointmentTabs: '/agendamentos',
  patientTabs: '/pacientes',
  configTabs: '/configuracoes',
  automacaoTabs: '/automacao',
  pepTabs: '/pep',
  agendaTabs: '/agenda',
  nursingTabs: '/enfermagem',
  pharmacyTabs: '/farmacia',
  inventoryTabs: '/estoque',
  labTabs: '/laboratorio',
  imagingTabs: '/imagem',
  hemotherapyTabs: '/hemoterapia',
  nutritionTabs: '/nutricao',
  susBillingTabs: '/faturamento',
  financialTabs: '/financeiro',
  tissTabs: '/faturamento-tiss',
  purchasingTabs: '/compras',
  hrTabs: '/rh',
  ccihTabs: '/ccih',
  securityLgpdTabs: '/seguranca-lgpd',
  qualityTabs: '/qualidade',
  regulationTabs: '/regulacao',
  clinicalEngTabs: '/engenharia-clinica',
  biTabs: '/bi',
  hotelariaTabs: '/hotelaria',
  transportTabs: '/transportes',
  physicalAccessTabs: '/acesso-fisico',
  govIntegrationTabs: '/integracoes-gov',
  reportsTabs: '/relatorios',
  connectTabs: '/connect',
  consultingRoomsTabs: '/consultorios',
  cmeTabs: '/cme',
  securityPortariaTabs: '/seguranca',
  aiTabs: '/ia',
  integrationTabs: '/integracoes',
};

const tabPaths = [];
for (const [, name, body] of tabBlocks) {
  const base = bases[name];
  if (!base) continue;
  for (const m of body.matchAll(/slug: '([^']*)'/g)) {
    const slug = m[1];
    tabPaths.push(slug ? `${base}/${slug}` : base);
  }
  for (const m of body.matchAll(/to: '([^']+)'/g)) tabPaths.push(m[1]);
}

const tabOrphans = [...new Set(tabPaths)].filter((p) => !resolves(p));
console.log('\n=== Abas de módulo sem rota (resolvePageComponent) ===');
console.log('Count:', tabOrphans.length);
tabOrphans.slice(0, 20).forEach((p) => console.log(p));

// Duplicate menu paths
const dupes = menuPaths.filter((p, i, arr) => arr.indexOf(p) !== i);
console.log('\n=== Paths duplicados no menu ===');
[...new Set(dupes)].forEach((p) => console.log(p));
