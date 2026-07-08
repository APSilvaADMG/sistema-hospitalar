const PT_SMALL_WORDS = new Set([
  'de', 'da', 'do', 'das', 'dos', 'e', 'em', 'na', 'no', 'nas', 'nos', 'a', 'o', 'as', 'os',
]);

function isLetter(ch: string): boolean {
  return /\p{L}/u.test(ch);
}

function isMostlyUppercase(text: string): boolean {
  let upper = 0;
  let total = 0;
  for (const ch of text) {
    if (!isLetter(ch)) continue;
    total++;
    if (ch === ch.toUpperCase() && ch !== ch.toLowerCase()) upper++;
  }
  return total > 0 && upper / total > 0.7;
}

function isPreservedAcronym(core: string): boolean {
  if (core.length < 2 || core.length > 4) return false;
  if (!/^[A-Z0-9]+$/u.test(core)) return false;
  return core === core.toUpperCase();
}

function formatWord(word: string, isFirstWord: boolean): string {
  const match = word.match(/^([^A-Za-zÀ-ÿ]*)([\p{L}0-9]+)(.*)$/u);
  if (!match) return word;

  const [, prefix, core, suffix] = match;
  const lower = core.toLowerCase();
  if (!isFirstWord && PT_SMALL_WORDS.has(lower)) return `${prefix}${lower}${suffix}`;
  if (isPreservedAcronym(core)) return `${prefix}${core}${suffix}`;

  const titled = lower.charAt(0).toUpperCase() + lower.slice(1);
  return `${prefix}${titled}${suffix}`;
}

/** Readable title case for TUSS descriptions imported in ALL CAPS. */
export function formatTussDescription(text: string): string {
  if (!text) return '';

  const normalized = text.replace(/\s+/g, ' ').trim();
  if (!normalized) return '';

  if (!isMostlyUppercase(normalized)) return normalized;

  return normalized
    .split(' ')
    .map((word, index) => formatWord(word, index === 0))
    .join(' ');
}
