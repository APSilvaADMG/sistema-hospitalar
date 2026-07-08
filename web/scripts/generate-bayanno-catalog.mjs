/**
 * Gera catálogo TypeScript das 73 telas Bayanno/SGHC a partir de bayanno-extract/.
 * npm run sync:bayanno-catalog
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const extractDir = path.join(repoRoot, 'Diversos/bayanno-extract');
const outDir = path.join(repoRoot, 'web/src/data/bayanno');

const LAYOUT_FILES = new Set([
  'footer.php',
  'header.php',
  'login.php',
  'four_zero_four.php',
  'includes.php',
  'index.php',
  'page_info.php',
  'install/index.php',
]);

const ROLE_LABELS = {
  accountant: 'Contador',
  admin: 'Administrador',
  doctor: 'Médico',
  laboratorist: 'Laboratorista',
  nurse: 'Enfermagem',
  patient: 'Paciente',
  pharmacist: 'Farmacêutico',
  system: 'Sistema',
};

const PT_OVERRIDES = {
  dashboard: 'Painel',
  profile: 'Meu perfil',
  manage_profile: 'Meu perfil',
  patient: 'Pacientes',
  doctor: 'Médicos',
  nurse: 'Enfermeiros',
  pharmacist: 'Farmacêuticos',
  laboratorist: 'Laboratoristas',
  accountant: 'Contadores',
  department: 'Departamentos',
  appointment: 'Agendamentos',
  manage_appointment: 'Agendamentos',
  view_appointment: 'Agendamentos',
  prescription: 'Prescrições',
  manage_prescription: 'Prescrições',
  view_prescription: 'Prescrições',
  provide_medication: 'Dispensar medicamentos',
  bed_allotment: 'Alocação de leitos',
  manage_bed_allotment: 'Alocação de leitos',
  manage_bed: 'Leitos',
  view_bed_status: 'Status de leitos',
  blood_bank: 'Banco de sangue',
  view_blood_bank: 'Banco de sangue',
  manage_blood_bank: 'Banco de sangue',
  manage_blood_donor: 'Doadores de sangue',
  manage_medicine: 'Medicamentos',
  view_medicine: 'Medicamentos',
  medicine_category: 'Categorias de medicamento',
  manage_invoice: 'Faturas',
  'invoice / take_payment': 'Faturas / receber pagamento',
  view_invoice: 'Faturas',
  view_payment: 'Pagamentos',
  payment_history: 'Histórico de pagamentos',
  take_cash_payment: 'Receber pagamento',
  monitor_hospital: 'Monitor hospitalar',
  system_settings: 'Configurações do sistema',
  backup_restore: 'Backup e restauração',
  manage_language: 'Idiomas',
  manage_noticeboard: 'Mural de avisos',
  manage_email_template: 'Modelos de e-mail',
  view_log: 'Logs do sistema',
  manage_report: 'Relatórios clínicos',
  report: 'Relatórios',
  view_operation: 'Relatório cirúrgico',
  view_birth_report: 'Relatório de nascimento',
  view_death_report: 'Relatório de óbito',
  admit_history: 'Histórico de internação',
  operation_history: 'Histórico cirúrgico',
  view_doctor: 'Médicos',
  add_diagnosis_report: 'Laudos de diagnóstico',
  calendar_schedule: 'Agenda',
  noticeboard: 'Avisos',
  settings: 'Configurações',
  login: 'Login',
  option: 'Ações',
  edit: 'Editar',
  delete: 'Excluir',
  status: 'Status',
  paid: 'Pago',
  unpaid: 'Não pago',
};

function phraseToPt(key) {
  if (!key || key === '#') return key;
  if (PT_OVERRIDES[key]) return PT_OVERRIDES[key];
  return key
    .replace(/\//g, ' / ')
    .split(/[\s_]+/)
    .filter(Boolean)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ');
}

function fileToRoute(file) {
  return file.replace(/\.php$/, '').replace(/\\/g, '/');
}

function inferModuleLink(route, action) {
  const r = route.toLowerCase();
  const a = (action || '').toLowerCase();

  if (r.endsWith('/dashboard')) return '/';
  if (r.includes('manage_profile') || r.endsWith('/manage_patient') && r.startsWith('patient/')) return '/configuracoes/aparencia';
  if (r.includes('patient') && !r.startsWith('patient/')) return '/recepcao/pacientes';
  if (r.startsWith('patient/')) return '/portal-paciente';
  if (a.includes('appointment') || r.includes('appointment')) return '/recepcao/agendamentos';
  if (a.includes('prescription') || r.includes('prescription')) {
    if (r.startsWith('pharmacist/')) return '/farmacia';
    if (r.startsWith('laboratorist/')) return '/laboratorio';
    return '/pep/prescricao';
  }
  if (r.includes('blood')) return '/hemoterapia';
  if (r.includes('bed')) return '/internacao/leitos';
  if (r.includes('medicine')) return '/farmacia';
  if (r.includes('invoice') || r.includes('payment') || r.startsWith('accountant/')) return '/financeiro';
  if (r.includes('view_report') || a.includes('report') || r.includes('manage_report')) return '/relatorios';
  if (r.includes('manage_department')) return '/configuracoes/cadastros';
  if (r.includes('manage_doctor') || r.includes('manage_nurse') || r.includes('manage_pharmacist')
    || r.includes('manage_laboratorist') || r.includes('manage_accountant')) return '/usuarios';
  if (r.includes('system_settings') || r.includes('backup') || r.includes('language')
    || r.includes('noticeboard') || r.includes('email_template')) return '/configuracoes';
  if (r.includes('view_log')) return '/auditoria';
  if (r === 'login.php' || r === 'login') return '/login';
  return null;
}

function inferScreenKind(file, action) {
  const route = fileToRoute(file);
  if (LAYOUT_FILES.has(file)) return 'layout';
  if (route.endsWith('/dashboard') || action === 'dashboard') return 'dashboard';
  return 'operational';
}

function inferTitle(view, routeEntry, route) {
  const action = routeEntry?.action || route.split('/').pop() || '';
  const pageName = routeEntry?.pageName;
  if (pageName && PT_OVERRIDES[pageName]) return PT_OVERRIDES[pageName];
  if (PT_OVERRIDES[action]) return PT_OVERRIDES[action];
  const navLabel = view.phraseKeys?.find((k) => !['edit', 'delete', 'option', 'status'].includes(k));
  if (navLabel && PT_OVERRIDES[navLabel]) return PT_OVERRIDES[navLabel];
  return phraseToPt(action.replace(/_/g, ' '));
}

function loadJson(name) {
  const p = path.join(extractDir, name);
  if (!fs.existsSync(p)) {
    console.error('Arquivo não encontrado:', p);
    process.exit(1);
  }
  return JSON.parse(fs.readFileSync(p, 'utf8'));
}

function buildRouteIndex(routesJson) {
  const map = new Map();
  for (const group of routesJson) {
    for (const r of group.routes) {
      if (r.route) map.set(r.route, r);
    }
  }
  return map;
}

function translateTabs(tabs) {
  return (tabs || []).map((tab) => ({
    id: tab.id,
    label: phraseToPt(tab.label),
    labelKey: tab.label,
  }));
}

function translateTables(tables) {
  return (tables || []).map((table) => ({
    className: table.className,
    columns: (table.columns || []).map((col) => ({
      label: phraseToPt(col.label),
      labelKey: col.label,
    })),
  }));
}

function main() {
  const views = loadJson('views.json');
  const navigation = loadJson('navigation.json');
  const routesJson = loadJson('routes.json');
  const phrasesJson = loadJson('phrases.json');

  const routeIndex = buildRouteIndex(routesJson);

  const screens = views.map((view) => {
    const route = fileToRoute(view.file);
    const parts = route.split('/');
    const role = view.role || parts[0] || 'system';
    const routeEntry = routeIndex.get(route);
    const action = routeEntry?.action || parts.slice(1).join('/') || parts[0];
    const kind = inferScreenKind(view.file, action);
    const title = inferTitle(view, routeEntry, route);
    const moduleLink = inferModuleLink(route, action);

    return {
      id: route.replace(/\//g, '-'),
      route,
      role,
      action,
      file: view.file,
      title,
      kind,
      hasBox: view.hasBox !== false,
      tabs: translateTabs(view.tabs),
      tables: translateTables(view.tables),
      phraseKeys: view.phraseKeys || [],
      icons: view.icons || [],
      moduleLink,
      path: `/sghc/${route}`,
    };
  });

  const phraseKeys = new Set(Object.keys(phrasesJson.phrases || {}));
  for (const s of screens) {
    s.phraseKeys.forEach((k) => phraseKeys.add(k));
    s.tabs.forEach((t) => phraseKeys.add(t.labelKey));
    s.tables.forEach((t) => t.columns.forEach((c) => phraseKeys.add(c.labelKey)));
  }

  const phrasesPt = {};
  for (const key of [...phraseKeys].sort()) {
    phrasesPt[key] = phraseToPt(key);
  }
  Object.assign(phrasesPt, PT_OVERRIDES);

  const navByRole = navigation.map((nav) => ({
    role: nav.role,
    roleLabel: ROLE_LABELS[nav.role] || phraseToPt(nav.role),
    items: nav.items.map((item) => {
      if (item.type === 'submenu') {
        return {
          type: 'submenu',
          label: phraseToPt(item.label),
          labelKey: item.label,
          icon: item.icon,
          submenuId: item.submenuId,
          children: (item.children || []).map((child) => ({
            type: 'link',
            route: child.route,
            label: phraseToPt(child.label),
            labelKey: child.label,
            icon: child.icon,
            path: `/sghc/${child.route}`,
          })),
        };
      }
      return {
        type: 'link',
        route: item.route,
        label: phraseToPt(item.label),
        labelKey: item.label,
        icon: item.icon,
        path: `/sghc/${item.route}`,
      };
    }),
  }));

  fs.mkdirSync(outDir, { recursive: true });

  const header = `/** Gerado por npm run sync:bayanno-catalog — não editar manualmente. */\n\n`;

  fs.writeFileSync(
    path.join(outDir, 'bayannoScreenCatalog.ts'),
    `${header}export type BayannoScreenKind = 'operational' | 'dashboard' | 'layout';

export type BayannoScreenTab = { id: string; label: string; labelKey: string };
export type BayannoScreenColumn = { label: string; labelKey: string };
export type BayannoScreenTable = { className: string; columns: BayannoScreenColumn[] };

export type BayannoScreen = {
  id: string;
  route: string;
  role: string;
  action: string;
  file: string;
  title: string;
  kind: BayannoScreenKind;
  hasBox: boolean;
  tabs: BayannoScreenTab[];
  tables: BayannoScreenTable[];
  phraseKeys: string[];
  icons: string[];
  moduleLink: string | null;
  path: string;
};

export const BAYANNO_SCREEN_COUNT = ${screens.length};

export const BAYANNO_SCREENS: BayannoScreen[] = ${JSON.stringify(screens, null, 2)};

export const BAYANNO_SCREEN_BY_ROUTE: Record<string, BayannoScreen> = Object.fromEntries(
  BAYANNO_SCREENS.map((s) => [s.route, s]),
);

export const BAYANNO_SCREEN_BY_PATH: Record<string, BayannoScreen> = Object.fromEntries(
  BAYANNO_SCREENS.map((s) => [s.path, s]),
);
`,
    'utf8',
  );

  fs.writeFileSync(
    path.join(outDir, 'bayannoPhrasesPt.ts'),
    `${header}export const BAYANNO_PHRASES_PT: Record<string, string> = ${JSON.stringify(phrasesPt, null, 2)};

export function bayannoPhrase(key: string): string {
  return BAYANNO_PHRASES_PT[key] ?? key;
}
`,
    'utf8',
  );

  fs.writeFileSync(
    path.join(outDir, 'bayannoSghcNav.ts'),
    `${header}export type BayannoNavLink = {
  type: 'link';
  route: string;
  label: string;
  labelKey: string;
  icon: string;
  path: string;
};

export type BayannoNavSubmenu = {
  type: 'submenu';
  label: string;
  labelKey: string;
  icon: string;
  submenuId: string;
  children: BayannoNavLink[];
};

export type BayannoNavItem = BayannoNavLink | BayannoNavSubmenu;

export type BayannoRoleNav = {
  role: string;
  roleLabel: string;
  items: BayannoNavItem[];
};

export const BAYANNO_ROLE_LABELS: Record<string, string> = ${JSON.stringify(ROLE_LABELS, null, 2)};

export const BAYANNO_SGHC_NAV: BayannoRoleNav[] = ${JSON.stringify(navByRole, null, 2)};

export const BAYANNO_SGHC_ROLES = BAYANNO_SGHC_NAV.map((n) => n.role);
`,
    'utf8',
  );

  fs.writeFileSync(
    path.join(outDir, 'index.ts'),
    `${header}export * from './bayannoScreenCatalog';
export * from './bayannoPhrasesPt';
export * from './bayannoSghcNav';
`,
    'utf8',
  );

  console.log(`OK: ${screens.length} telas → ${outDir}`);
  console.log(`  operational: ${screens.filter((s) => s.kind === 'operational').length}`);
  console.log(`  dashboard: ${screens.filter((s) => s.kind === 'dashboard').length}`);
  console.log(`  layout: ${screens.filter((s) => s.kind === 'layout').length}`);
}

main();
