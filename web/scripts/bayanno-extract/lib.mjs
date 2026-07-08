import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export const repoRoot = path.resolve(__dirname, '..', '..', '..');
export const phpRoot = path.join(
  repoRoot,
  'Diversos/Scripts/_extracted/sghc-php/hospitalar',
);
export const viewsRoot = path.join(phpRoot, 'application/views');
export const controllersRoot = path.join(phpRoot, 'application/controllers');
export const sqlPath = path.join(phpRoot, 'uploads/hms.sql');
export const bayannoCssPath = path.join(phpRoot, 'template/css/bayanno.css');

export const defaultOutDir = path.join(repoRoot, 'Diversos/bayanno-extract');

export function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

export function writeJson(filePath, data) {
  ensureDir(path.dirname(filePath));
  fs.writeFileSync(filePath, `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

export function walkPhpFiles(dir, files = []) {
  if (!fs.existsSync(dir)) return files;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walkPhpFiles(full, files);
    else if (entry.name.endsWith('.php')) files.push(full);
  }
  return files;
}

export function relViewPath(absPath) {
  return path.relative(viewsRoot, absPath).replace(/\\/g, '/');
}

/** Extrai chaves get_phrase('...') */
export function extractPhraseKeys(content) {
  const keys = new Set();
  const re = /get_phrase\s*\(\s*['"]([^'"]+)['"]\s*\)/g;
  let m;
  while ((m = re.exec(content)) !== null) keys.add(m[1]);
  return [...keys];
}

/** Abas em box-header (nav-tabs) */
export function extractTabs(content) {
  const tabs = [];
  const tabBlock = content.match(/<ul[^>]*class="[^"]*nav-tabs[^"]*"[^>]*>([\s\S]*?)<\/ul>/i);
  if (!tabBlock) return tabs;

  const re = /<a[^>]*href="#([^"]+)"[^>]*>([\s\S]*?)<\/a>/gi;
  let m;
  while ((m = re.exec(tabBlock[1])) !== null) {
    const id = m[1];
    const inner = m[2].replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    const phraseMatch = m[0].match(/get_phrase\s*\(\s*['"]([^'"]+)['"]\s*\)/);
    tabs.push({
      id,
      label: phraseMatch ? phraseMatch[1] : inner || id,
      labelSource: phraseMatch ? 'phrase' : 'html',
    });
  }
  return tabs;
}

/** Colunas de tabelas dTable */
export function extractTables(content) {
  const tables = [];
  const re = /<table[^>]*class="([^"]*dTable[^"]*)"[^>]*>([\s\S]*?)<\/table>/gi;
  let m;
  while ((m = re.exec(content)) !== null) {
    const tableClass = m[1].trim();
    const thead = m[2].match(/<thead>([\s\S]*?)<\/thead>/i);
    const columns = [];
    if (thead) {
      const thRe = /<th[^>]*>([\s\S]*?)<\/th>/gi;
      let th;
      while ((th = thRe.exec(thead[1])) !== null) {
        const raw = th[1].replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
        const phraseMatch = th[1].match(/get_phrase\s*\(\s*['"]([^'"]+)['"]\s*\)/);
        columns.push({
          label: phraseMatch ? phraseMatch[1] : raw || '#',
          labelSource: phraseMatch ? 'phrase' : 'html',
        });
      }
    }
    tables.push({ className: tableClass, columns });
  }
  return tables;
}

const VALID_CLASS = /^[a-zA-Z_][\w-]*$/;

/** Classes CSS usadas no HTML (class="...") */
export function extractHtmlClasses(content) {
  const classes = new Set();
  const re = /class\s*=\s*["']([^"']+)["']/gi;
  let m;
  while ((m = re.exec(content)) !== null) {
    for (const token of m[1].split(/\s+/)) {
      const t = token.trim();
      if (t && VALID_CLASS.test(t)) classes.add(t);
    }
  }
  return [...classes].sort();
}

export function parseViewFile(absPath) {
  const content = fs.readFileSync(absPath, 'utf8');
  const rel = relViewPath(absPath);
  const role = rel.includes('/') ? rel.split('/')[0] : 'root';

  return {
    file: rel,
    role,
    hasBox: /\bclass="box"/.test(content) || /\bclass='box'/.test(content),
    tabs: extractTabs(content),
    tables: extractTables(content),
    phraseKeys: extractPhraseKeys(content),
    cssClasses: extractHtmlClasses(content),
    icons: [...content.matchAll(/\b(icon-[a-z0-9-]+)/gi)].map((x) => x[1]),
  };
}

/** Itens de menu a partir de navigation.php */
export function parseNavigationFile(absPath) {
  const content = fs.readFileSync(absPath, 'utf8');
  const role = path.basename(path.dirname(absPath));
  const items = [];

  const linkRe = /<a\s+([^>]*?)>([\s\S]*?)<\/a>/gi;
  let m;
  while ((m = linkRe.exec(content)) !== null) {
    const attrs = m[1];
    const inner = m[2];
    const phraseMatch = inner.match(/get_phrase\s*\(\s*['"]([^'"]+)['"]\s*\)/);
    const iconMatch = inner.match(/class\s*=\s*["']([^"']*icon-[^"']+)["']/i)
      ?? attrs.match(/class\s*=\s*["']([^"']*icon-[^"']+)["']/i);

    if (/accordion-toggle/i.test(attrs) && /data-toggle\s*=\s*["']collapse["']/i.test(attrs)) {
      const submenuId = attrs.match(/href\s*=\s*["']#([^"']+)["']/i)?.[1];
      items.push({
        type: 'submenu',
        label: phraseMatch?.[1] ?? 'submenu',
        icon: iconMatch?.[1] ?? null,
        submenuId,
      });
      continue;
    }

    const routeMatch = `${attrs}${inner}`.match(/index\.php\?([a-z0-9_/]+)/i);
    if (!routeMatch) continue;

    const route = routeMatch[1].replace(/&amp;/g, '&');
    const pageNameMatch = attrs.match(/page_name\s*==\s*['"]([^'"]+)['"]/);

    items.push({
      type: 'link',
      route,
      label: phraseMatch?.[1] ?? route,
      icon: iconMatch?.[1] ?? null,
      pageNameHint: pageNameMatch?.[1] ?? null,
    });
  }

  return { role, file: relViewPath(absPath), items };
}

/** Rotas dos controllers PHP */
export function parseControllerFile(absPath) {
  const content = fs.readFileSync(absPath, 'utf8');
  const role = path.basename(absPath, '.php');
  const routes = [];

  const fnRe = /function\s+(\w+)\s*\([^)]*\)\s*\{([\s\S]*?)(?=\n\tfunction\s|\n\}$|\n\}\s*$)/g;
  let fn;
  while ((fn = fnRe.exec(content)) !== null) {
    const fnName = fn[1];
    if (fnName === '__construct') continue;
    const body = fn[2];
    const pageMatch = body.match(/\$page_data\s*\[\s*['"]page_name['"]\s*\]\s*=\s*['"]([^'"]+)['"]/);
    routes.push({
      action: fnName,
      route: `${role}/${fnName}`,
      pageName: pageMatch?.[1] ?? null,
      viewFile: pageMatch ? `${role}/${pageMatch[1]}.php` : null,
    });
  }

  return { role, file: path.relative(controllersRoot, absPath).replace(/\\/g, '/'), routes };
}

/** Frases do dump SQL (tabela language) */
export function parsePhrasesFromSql(sqlFile = sqlPath) {
  if (!fs.existsSync(sqlFile)) {
    return { source: sqlFile, phrases: {}, error: 'SQL não encontrado' };
  }

  const content = fs.readFileSync(sqlFile, 'utf8');
  const phrases = {};
  const insertRe = /INSERT INTO `language`[^;]+;/gi;
  const rowRe = /\(\s*\d+\s*,\s*'((?:\\'|[^'])*)'\s*,\s*'((?:\\'|[^'])*)'\s*\)/g;

  for (const block of content.match(insertRe) ?? []) {
    let m;
    while ((m = rowRe.exec(block)) !== null) {
      const key = m[1].replace(/\\'/g, "'");
      const en = m[2].replace(/\\'/g, "'");
      phrases[key] = en;
    }
  }

  return { source: path.relative(repoRoot, sqlFile).replace(/\\/g, '/'), count: Object.keys(phrases).length, phrases };
}

export function buildMarkdownReport(data) {
  const lines = [
    '# Inventário Bayanno HMS (SGHC)',
    '',
    `Gerado em: ${new Date().toISOString()}`,
    `Fonte: \`${path.relative(repoRoot, phpRoot).replace(/\\/g, '/')}\``,
    '',
    '## Resumo',
    '',
    `- Telas (views): **${data.views.length}**`,
    `- Perfis com menu: **${data.navigation.length}**`,
    `- Controllers: **${data.routes.length}**`,
    `- Frases no SQL: **${data.phrases.count}**`,
    `- Classes CSS distintas nas views: **${data.cssClassStats.total}**`,
    '',
    '## Menus por perfil',
    '',
  ];

  for (const nav of data.navigation) {
    lines.push(`### ${nav.role}`, '');
    for (const item of nav.items) {
      if (item.type === 'submenu') {
        lines.push(`- **${item.label}** (submenu)`);
      } else {
        lines.push(`- \`${item.route}\` — ${item.label}${item.icon ? ` (${item.icon})` : ''}`);
      }
    }
    lines.push('');
  }

  lines.push('## Telas com abas', '');
  for (const view of data.views.filter((v) => v.tabs.length > 0)) {
    lines.push(`### ${view.file}`, '');
    lines.push(`Abas: ${view.tabs.map((t) => t.label).join(' | ')}`, '');
  }

  lines.push('## Telas com tabela dTable', '');
  for (const view of data.views.filter((v) => v.tables.length > 0)) {
    lines.push(`### ${view.file}`, '');
    for (const table of view.tables) {
      lines.push(`- Colunas: ${table.columns.map((c) => c.label).join(', ')}`, '');
    }
  }

  lines.push('## Arquivos gerados', '');
  lines.push('- `inventory.json` — inventário completo');
  lines.push('- `views.json` — estrutura de cada tela');
  lines.push('- `navigation.json` — menus laterais');
  lines.push('- `routes.json` — rotas dos controllers');
  lines.push('- `phrases.json` — traduções (SQL + uso nas views)');
  lines.push('- `css-classes.json` — classes CSS usadas nas views');
  lines.push('');

  return lines.join('\n');
}
