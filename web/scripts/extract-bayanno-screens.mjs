/**
 * Extrai trechos do bayanno.css (SGHC) para web/src/styles/bayanno-php-screens.css
 * Fonte: Diversos/Scripts/_extracted/sghc-php/hospitalar/template/css/bayanno.css
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const srcCss = path.join(
  repoRoot,
  'Diversos/Scripts/_extracted/sghc-php/hospitalar/template/css/bayanno.css',
);
const outCss = path.join(repoRoot, 'web/src/styles/bayanno-php-screens.css');
const publicDir = path.join(repoRoot, 'web/public/bayanno');

const ICONS = [
  'icon-user-md', 'icon-user', 'icon-plus-sign-alt', 'icon-medkit', 'icon-beaker', 'icon-money',
  'icon-exchange', 'icon-credit-card', 'icon-tint', 'icon-reorder', 'icon-github-alt', 'icon-minus-sign',
  'icon-hdd', 'icon-columns', 'icon-h-sign', 'icon-globe', 'icon-download-alt', 'icon-calendar',
  'icon-tag', 'icon-info-sign', 'icon-stethoscope', 'icon-hospital', 'icon-envelope', 'icon-key',
  'icon-desktop', 'icon-sitemap', 'icon-screenshot', 'icon-wrench', 'icon-lock', 'icon-off',
  'icon-2x', 'icon-caret-down', 'icon-th-list', 'icon-align-justify', 'icon-ok',
  'icon-plus', 'icon-wrench', 'icon-plus-sign',
];

const LINE_RANGES = [
  [251, 327],
  [1105, 1200],
  [1312, 1420],
  [3384, 3441],
  [5622, 5650],
  [5698, 5925],
  [8242, 8314],
  [8532, 8578],
  [9085, 9105],
  [9477, 9559],
  [11472, 11526],
];

function extractIconRules(lines) {
  const rules = [
    `[data-brand='bayanno'] [class^="icon-"],`,
    `[data-brand='bayanno'] [class*=" icon-"] {`,
    `  font-family: FontAwesome;`,
    `  font-weight: 400;`,
    `  font-style: normal;`,
    `  text-decoration: inherit;`,
    `  -webkit-font-smoothing: antialiased;`,
    `}`,
    `[data-brand='bayanno'] [class^="icon-"]:before,`,
    `[data-brand='bayanno'] [class*=" icon-"]:before {`,
    `  text-decoration: inherit;`,
    `  display: inline-block;`,
    `}`,
    '',
  ];

  for (const icon of ICONS) {
    const escaped = icon.replace(/-/g, '\\-');
    const pattern = new RegExp(`^\\.${escaped}:before`);
    for (let i = 0; i < lines.length; i += 1) {
      if (!pattern.test(lines[i])) continue;
      const contentLine = lines[i + 1]?.trim() ?? '';
      if (!contentLine.includes('content:')) break;
      rules.push(`[data-brand='bayanno'] .${icon}:before { ${contentLine.replace(/;$/, '')}; }`);
      break;
    }
  }

  return rules.join('\n');
}

const EXTRA_CSS = `
[data-brand='bayanno'] .bayanno-login-php {
  min-height: 100vh;
  background: #eaeaea;
}

[data-brand='bayanno'] .bayanno-login-column {
  max-width: 420px;
  margin: 24px auto;
}

[data-brand='bayanno'] .bayanno-login-logo-wrap {
  text-align: center;
}

[data-brand='bayanno'] .bayanno-php-screen a.brand {
  color: #ecf0f1;
  text-decoration: none;
  font-weight: 700;
  font-size: 1.1rem;
}

[data-brand='bayanno'] .action-nav-button a {
  text-decoration: none;
  color: inherit;
}

[data-brand='bayanno'] .pull-left { float: left; }
[data-brand='bayanno'] .pull-right { float: right; }
[data-brand='bayanno'] .clearfix::after { content: ''; display: table; clear: both; }
[data-brand='bayanno'] ul.inline { list-style: none; margin: 0; padding: 0; }
[data-brand='bayanno'] ul.inline li { display: inline-block; }

[data-brand='bayanno'] .box-section.news {
  min-height: 85px;
  padding: 12px 0;
  border-bottom: 1px solid #e0e0e0;
}

[data-brand='bayanno'] .news .avatar {
  float: left;
  height: 36px;
  width: 36px;
  line-height: 36px;
  text-align: center;
  border-radius: 50%;
}

[data-brand='bayanno'] .news .avatar.blue {
  background: #7fb3d4;
  border: 1px solid #60a1ca;
}

[data-brand='bayanno'] .news .avatar i {
  color: white;
  line-height: 36px;
}

[data-brand='bayanno'] .news.with-icons .news-content {
  margin-left: 55px;
}

[data-brand='bayanno'] .news-title {
  color: #636364;
  font-weight: 600;
  font-size: 16px;
}

[data-brand='bayanno'] .news-text {
  color: #777;
  font-size: 13px;
}

[data-brand='bayanno'] .news-time {
  float: right;
  color: #bbb;
  font-size: 14px;
  text-align: center;
}

[data-brand='bayanno'] .news-time span {
  display: block;
  font-size: 24px;
  font-weight: 600;
}
`;

function fixCssSemicolons(css) {
  return css.replace(/([^;{}\s])\s*\n(\s*[}])/g, '$1;\n$2');
}

function balanceCssBlocks(css) {
  let depth = 0;
  const out = [];
  for (const line of css.split('\n')) {
    const opens = (line.match(/{/g) ?? []).length;
    const closes = (line.match(/}/g) ?? []).length;
    if (depth === 0 && opens === 0 && closes === 0 && line.trim() && !line.trim().startsWith('/*') && !line.includes(':')) {
      continue;
    }
    out.push(line);
    depth += opens - closes;
    if (depth < 0) depth = 0;
  }
  while (depth > 0) {
    out.push('}');
    depth -= 1;
  }
  return out.join('\n');
}

function main() {
  if (!fs.existsSync(srcCss)) {
    console.error('bayanno.css não encontrado:', srcCss);
    process.exit(1);
  }

  fs.mkdirSync(publicDir, { recursive: true });
  fs.copyFileSync(srcCss, path.join(publicDir, 'bayanno-original.css'));

  const lines = fs.readFileSync(srcCss, 'utf8').split('\n');
  const chunks = LINE_RANGES.map(([start, end]) => lines.slice(start - 1, end).join('\n'));

  const header = `/**
 * CSS extraído do Bayanno HMS (SGHC) — template/css/bayanno.css
 * Gerado por: npm run sync:bayanno-screens
 * Não edite manualmente; altere o script ou o CSS original e regenere.
 */

[data-brand='bayanno'] .bayanno-php-screen {
  font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
  font-size: 13px;
  color: #333;
}

`;

  const iconBase = extractIconRules(lines);
  const body = balanceCssBlocks(fixCssSemicolons(chunks.join('\n\n')));

  fs.writeFileSync(
    outCss,
    `${header}${EXTRA_CSS}\n\n${iconBase}\n\n${body}\n`,
    'utf8',
  );

  console.log('OK:', outCss);
  console.log('OK:', path.join(publicDir, 'bayanno-original.css'));
}

main();
