export const BULA_SECTION_ORDER = [
  'INDICAÇÕES',
  'POSOLOGIA',
  'CONTRAINDICAÇÕES',
  'EFEITOS ADVERSOS',
  'INTERAÇÕES',
  'CUIDADOS NA ADMINISTRAÇÃO',
] as const;

export type BulaSectionKey = (typeof BULA_SECTION_ORDER)[number];

export interface ParsedBulaSection {
  title: BulaSectionKey | string;
  body: string;
}

const SECTION_ALIASES: Record<string, BulaSectionKey> = {
  INDICAÇÕES: 'INDICAÇÕES',
  INDICACOES: 'INDICAÇÕES',
  POSOLOGIA: 'POSOLOGIA',
  CONTRAINDICAÇÕES: 'CONTRAINDICAÇÕES',
  CONTRAINDICACOES: 'CONTRAINDICAÇÕES',
  'EFEITOS ADVERSOS': 'EFEITOS ADVERSOS',
  INTERAÇÕES: 'INTERAÇÕES',
  INTERACOES: 'INTERAÇÕES',
  'CUIDADOS NA ADMINISTRAÇÃO': 'CUIDADOS NA ADMINISTRAÇÃO',
  'CUIDADOS NA ADMINISTRACAO': 'CUIDADOS NA ADMINISTRAÇÃO',
  CONSERVAÇÃO: 'CUIDADOS NA ADMINISTRAÇÃO',
  CONSERVACAO: 'CUIDADOS NA ADMINISTRAÇÃO',
};

const QUESTION_SECTIONS: Array<{ pattern: RegExp; title: BulaSectionKey }> = [
  { pattern: /(?:,\s*)?para o que [eé] indicado e para o que serve\?/i, title: 'INDICAÇÕES' },
  { pattern: /Como (?:devo usar|usar|tomar)[^?]*\?/i, title: 'POSOLOGIA' },
  { pattern: /Quais as contraindica(?:ç|c)[oõ]es[^?]*\?/i, title: 'CONTRAINDICAÇÕES' },
  {
    pattern: /Quais (?:os )?efeitos colaterais[^?]*\?|Quais as rea(?:ç|c)[oõ]es adversas[^?]*\?/i,
    title: 'EFEITOS ADVERSOS',
  },
  { pattern: /Intera(?:ç|c)[oõ]es medicamentosas[^?]*\?/i, title: 'INTERAÇÕES' },
  { pattern: /Como devo armazenar[^?]*\?|Conserva(?:ç|c)[aã]o[^?]*\?/i, title: 'CUIDADOS NA ADMINISTRAÇÃO' },
];

function normalizeTitle(raw: string): BulaSectionKey | string {
  const key = raw.trim().toUpperCase().normalize('NFD').replace(/\p{M}/gu, '');
  return SECTION_ALIASES[key] ?? raw.trim().toUpperCase();
}

function summarizeBody(body: string, maxChars: number): string {
  const cleaned = body
    .replace(/Como o .+? funciona\?\s*/gi, '')
    .replace(/\s+/g, ' ')
    .trim();

  if (!cleaned) return body.trim().slice(0, maxChars);

  const sentences = cleaned
    .split(/(?<=[.!?])\s+/)
    .map((part) => part.trim())
    .filter((part) => part.length > 15);

  if (sentences.length === 0) return cleaned.slice(0, maxChars);

  const parts: string[] = [];
  let total = 0;
  for (const sentence of sentences) {
    if (total + sentence.length > maxChars && parts.length > 0) break;
    parts.push(sentence);
    total += sentence.length + 1;
  }

  return parts.join(' ');
}

function parseStructuredBlocks(text: string): Map<BulaSectionKey, string> {
  const blocks = new Map<BulaSectionKey, string>();
  let currentTitle: BulaSectionKey | null = null;
  let currentLines: string[] = [];

  for (const line of text.split('\n')) {
    const stripped = line.trim();
    const normalized = normalizeTitle(stripped);
    if (BULA_SECTION_ORDER.includes(normalized as BulaSectionKey)) {
      if (currentTitle) {
        blocks.set(currentTitle, currentLines.join('\n').trim());
      }
      currentTitle = normalized as BulaSectionKey;
      currentLines = [];
      continue;
    }

    if (currentTitle) currentLines.push(line);
  }

  if (currentTitle) {
    blocks.set(currentTitle, currentLines.join('\n').trim());
  }

  return blocks;
}

function parseQuestionBlocks(text: string): Map<BulaSectionKey, string> {
  const matches: Array<{ index: number; title: BulaSectionKey; pattern: RegExp }> = [];

  for (const section of QUESTION_SECTIONS) {
    section.pattern.lastIndex = 0;
    let match = section.pattern.exec(text);
    while (match) {
      matches.push({ index: match.index, title: section.title, pattern: section.pattern });
      match = section.pattern.exec(text);
    }
  }

  matches.sort((a, b) => a.index - b.index);
  const blocks = new Map<BulaSectionKey, string>();

  for (let i = 0; i < matches.length; i += 1) {
    const start = matches[i].index;
    const end = i + 1 < matches.length ? matches[i + 1].index : text.length;
    const chunk = text.slice(start, end).trim();
    const body = chunk.replace(matches[i].pattern, '').trim() || chunk;
    const existing = blocks.get(matches[i].title);
    blocks.set(matches[i].title, existing ? `${existing}\n${body}`.trim() : body);
  }

  return blocks;
}

export function parseBulaSections(text: string): ParsedBulaSection[] {
  const normalizedText = text.trim().replace(/^\s*>\s*/, '');
  if (!normalizedText) return [];

  const hasStructuredHeaders = BULA_SECTION_ORDER.some((title) =>
    new RegExp(`^${title}$`, 'im').test(normalizedText),
  );

  const blocks = hasStructuredHeaders
    ? parseStructuredBlocks(normalizedText)
    : parseQuestionBlocks(normalizedText);

  return BULA_SECTION_ORDER.flatMap((title) => {
    const body = blocks.get(title)?.trim();
    if (!body) return [];
    const maxChars = title === 'POSOLOGIA' ? 220 : 320;
    return [{ title, body: summarizeBody(body, maxChars) }];
  });
}

export function extractPosologiaFromPackageInsert(text?: string | null): string | null {
  if (!text) return null;
  const section = parseBulaSections(text).find((item) => item.title === 'POSOLOGIA');
  return section?.body?.trim() || null;
}

export function formatMedicationDisplayName(name?: string | null, strength?: string | null): string {
  if (!name) return '';
  const trimmedName = name.trim();
  if (!strength?.trim()) return trimmedName;

  const normalizedStrength = strength.trim();
  if (trimmedName.toLowerCase().includes(normalizedStrength.toLowerCase())) {
    return trimmedName;
  }

  const compactName = trimmedName.replace(/[\s/]+/g, '').toLowerCase();
  const compactStrength = normalizedStrength.replace(/[\s/]+/g, '').toLowerCase();
  if (compactName.includes(compactStrength)) return trimmedName;

  return `${trimmedName} ${normalizedStrength}`;
}

export function inferStrengthFromName(name?: string | null): string | null {
  if (!name) return null;
  const match = name.match(
    /(\d+(?:[.,]\d+)?\s*(?:mg|mcg|µg|g|UI|U|%|mL|mg\/mL|mcg\/mL|UI\/mL)(?:\s*\/\s*\d+(?:[.,]\d+)?\s*(?:mg|mcg|g|UI|mL|mg\/mL|mcg\/mL|UI\/mL))*)/i,
  );
  return match?.[1]?.trim() ?? null;
}

export function inferFormFromText(text?: string | null): string | null {
  if (!text) return null;
  const lower = text.toLowerCase();
  const checks: Array<[string, string]> = [
    ['comprim', 'Comprimido'],
    ['cáps', 'Cápsula'],
    ['caps', 'Cápsula'],
    ['gotas', 'Gotas'],
    ['xarope', 'Suspensão oral'],
    ['suspens', 'Suspensão oral'],
    ['solução oral', 'Solução oral'],
    ['solucao oral', 'Solução oral'],
    ['injet', 'Solução injetável'],
    ['creme', 'Creme'],
    ['pomada', 'Pomada'],
    ['gel', 'Gel'],
    ['colírio', 'Solução oftálmica'],
    ['colirio', 'Solução oftálmica'],
    ['suposit', 'Supositório'],
  ];
  for (const [token, label] of checks) {
    if (lower.includes(token)) return label;
  }
  return null;
}

export function inferRouteFromForm(form?: string | null, packageInsert?: string | null): string | null {
  const corpus = `${form ?? ''}\n${packageInsert ?? ''}`.toLowerCase();
  if (/\bvia oral\b|\bpor via oral\b/.test(corpus)) return 'VO';
  if (/\bintravenos/.test(corpus)) return 'IV';
  if (/\bintramuscular\b/.test(corpus)) return 'IM';
  if (/\bsubcut[aâ]ne/.test(corpus)) return 'SC';
  if (/\bt[oó]pic/.test(corpus)) return 'Tópica';
  if (/\boft[aá]lmic|\bcol[ií]rio/.test(corpus)) return 'Oftálmica';
  if (/\bnasal\b/.test(corpus)) return 'Nasal';
  if (/\bretal\b|\bsuposit/.test(corpus)) return 'Retal';
  if (/\binhal/.test(corpus)) return 'Inalatória';

  const normalized = form?.toLowerCase() ?? '';
  if (['comprim', 'caps', 'cáps', 'gotas', 'xarope', 'suspens', 'solução oral', 'solucao oral'].some((t) => normalized.includes(t))) {
    return 'VO';
  }
  if (normalized.includes('injet')) return 'IV';
  if (normalized.includes('creme') || normalized.includes('pomada') || normalized.includes('gel')) return 'Tópica';
  if (normalized.includes('oftalm') || normalized.includes('colírio') || normalized.includes('colirio')) return 'Oftálmica';
  if (normalized.includes('spray') && normalized.includes('nasal')) return 'Nasal';
  if (normalized.includes('suposit')) return 'Retal';
  return null;
}
