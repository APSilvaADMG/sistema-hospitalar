#!/usr/bin/env node
/**
 * Extrai estrutura do Bayanno HMS (SGHC) a partir do código PHP local.
 *
 * Uso:
 *   npm run extract:bayanno              # tudo
 *   npm run extract:bayanno -- views     # só telas
 *   npm run extract:bayanno -- menu      # só menus
 *   npm run extract:bayanno -- routes    # só rotas
 *   npm run extract:bayanno -- phrases    # só traduções
 *   npm run extract:bayanno -- css        # só classes CSS
 *
 * Saída: Diversos/bayanno-extract/
 */
import fs from 'node:fs';
import path from 'node:path';
import {
  bayannoCssPath,
  buildMarkdownReport,
  controllersRoot,
  defaultOutDir,
  parseControllerFile,
  parseNavigationFile,
  parsePhrasesFromSql,
  parseViewFile,
  phpRoot,
  repoRoot,
  viewsRoot,
  walkPhpFiles,
  writeJson,
} from './bayanno-extract/lib.mjs';

const [, , cmd = 'all', outFlag, outPathArg] = process.argv;
const outDir = outFlag === '--out' && outPathArg
  ? path.resolve(outPathArg)
  : defaultOutDir;

function assertSource() {
  if (!fs.existsSync(phpRoot)) {
    console.error('Código PHP Bayanno não encontrado em:', phpRoot);
    console.error('Extraia o SGHC para Diversos/Scripts/_extracted/sghc-php/hospitalar/');
    process.exit(1);
  }
}

function extractViews() {
  const files = walkPhpFiles(viewsRoot).filter((f) => !f.endsWith('navigation.php'));
  return files.map(parseViewFile).sort((a, b) => a.file.localeCompare(b.file));
}

function extractNavigation() {
  const files = walkPhpFiles(viewsRoot).filter((f) => f.endsWith(`${path.sep}navigation.php`) || f.endsWith('/navigation.php'));
  return files.map(parseNavigationFile).sort((a, b) => a.role.localeCompare(b.role));
}

function extractRoutes() {
  const files = walkPhpFiles(controllersRoot);
  return files.map(parseControllerFile).sort((a, b) => a.role.localeCompare(b.role));
}

function extractPhrases(views) {
  const sql = parsePhrasesFromSql();
  const usedInViews = new Set();
  for (const view of views ?? extractViews()) {
    for (const key of view.phraseKeys) usedInViews.add(key);
  }

  const missing = [...usedInViews].filter((k) => !(k in sql.phrases)).sort();

  return {
    ...sql,
    usedInViews: [...usedInViews].sort(),
    missingInSql: missing,
  };
}

function extractCssClasses(views) {
  const usage = new Map();
  for (const view of views ?? extractViews()) {
    for (const cls of view.cssClasses) {
      usage.set(cls, (usage.get(cls) ?? 0) + 1);
    }
  }

  const sorted = [...usage.entries()]
    .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
    .map(([className, count]) => ({ className, count }));

  const escapeRegExp = (value) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const bayannoSelectors = new Set();
  if (fs.existsSync(bayannoCssPath)) {
    const css = fs.readFileSync(bayannoCssPath, 'utf8');
    for (const { className } of sorted) {
      if (new RegExp(`\\.${escapeRegExp(className)}\\b`).test(css)) {
        bayannoSelectors.add(className);
      }
    }
  }

  return {
    total: sorted.length,
    inBayannoCss: bayannoSelectors.size,
    classes: sorted,
    withBayannoCssRule: [...bayannoSelectors].sort(),
  };
}

function runAll() {
  assertSource();
  const views = extractViews();
  const navigation = extractNavigation();
  const routes = extractRoutes();
  const phrases = extractPhrases(views);
  const cssClassStats = extractCssClasses(views);

  const inventory = {
    generatedAt: new Date().toISOString(),
    source: path.relative(repoRoot, phpRoot).replace(/\\/g, '/'),
    views,
    navigation,
    routes,
    phrases: {
      count: phrases.count,
      usedInViews: phrases.usedInViews,
      missingInSql: phrases.missingInSql,
    },
    cssClassStats: {
      total: cssClassStats.total,
      inBayannoCss: cssClassStats.inBayannoCss,
    },
  };

  writeJson(path.join(outDir, 'inventory.json'), inventory);
  writeJson(path.join(outDir, 'views.json'), views);
  writeJson(path.join(outDir, 'navigation.json'), navigation);
  writeJson(path.join(outDir, 'routes.json'), routes);
  writeJson(path.join(outDir, 'phrases.json'), phrases);
  writeJson(path.join(outDir, 'css-classes.json'), cssClassStats);

  const report = buildMarkdownReport({ views, navigation, routes, phrases, cssClassStats });
  fs.writeFileSync(path.join(outDir, 'REPORT.md'), report, 'utf8');

  console.log('Extração Bayanno concluída:', outDir);
  console.log(`  views:      ${views.length} arquivos`);
  console.log(`  navigation: ${navigation.length} perfis`);
  console.log(`  routes:     ${routes.reduce((n, r) => n + r.routes.length, 0)} ações`);
  console.log(`  phrases:    ${phrases.count} no SQL, ${phrases.missingInSql.length} só nas views`);
  console.log(`  css:        ${cssClassStats.total} classes, ${cssClassStats.inBayannoCss} com regra em bayanno.css`);
  console.log('  REPORT.md   relatório legível');
}

function runPartial(name, fn) {
  assertSource();
  const data = fn();
  writeJson(path.join(outDir, `${name}.json`), data);
  console.log('OK:', path.join(outDir, `${name}.json`));
}

const handlers = {
  all: runAll,
  views: () => runPartial('views', extractViews),
  menu: () => runPartial('navigation', extractNavigation),
  navigation: () => runPartial('navigation', extractNavigation),
  routes: () => runPartial('routes', extractRoutes),
  phrases: () => runPartial('phrases', () => extractPhrases()),
  css: () => runPartial('css-classes', () => extractCssClasses()),
};

if (!handlers[cmd]) {
  console.error(`Comando desconhecido: ${cmd}`);
  console.error('Use: all | views | menu | routes | phrases | css');
  process.exit(1);
}

handlers[cmd]();
