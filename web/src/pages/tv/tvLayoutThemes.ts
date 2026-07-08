import type { TvLayoutDto } from '../../api/client';

export const HOSPITAL_SGH_LAYOUT_NAME = 'Sala de Espera — SGH';

/** Slugs de TVs de sala de espera — usam o painel institucional SGH. */
export const WAITING_ROOM_SLUGS = new Set([
  'recepcao',
  'ambulatorio',
  'laboratorio',
  'sala-espera',
  'espera',
  'sala-de-casa',
  'sala-casa',
]);

const WAITING_ROOM_LABEL_KEYWORDS = ['recep', 'espera', 'ambulat', 'sala', 'casa'] as const;
const IPV4_SLUG = /^\d{1,3}(\.\d{1,3}){3}$/;

function normalizeLabel(value: string): string {
  return value
    .normalize('NFKD')
    .replace(/[\u2010-\u2015\u2212]/g, '-')
    .toLowerCase();
}

function waitingRoomLabelMatches(...parts: Array<string | undefined | null>): boolean {
  const label = normalizeLabel(parts.filter(Boolean).join(' '));
  return WAITING_ROOM_LABEL_KEYWORDS.some((keyword) => label.includes(keyword));
}

function isIpv4DisplaySlug(displaySlug?: string): boolean {
  return Boolean(displaySlug && IPV4_SLUG.test(displaySlug));
}

/** TV cadastrada como sala de espera (slug, setor ou nome). */
export function isWaitingRoomDisplay(
  displaySlug?: string,
  displaySector?: string,
  displayName?: string,
): boolean {
  if (displaySlug && WAITING_ROOM_SLUGS.has(displaySlug)) {
    return true;
  }

  if (isIpv4DisplaySlug(displaySlug)) {
    return true;
  }

  return waitingRoomLabelMatches(displaySector, displayName);
}

function layoutNameIndicatesSgh(layoutName?: string, displayLayoutName?: string): boolean {
  for (const name of [layoutName, displayLayoutName]) {
    if (!name) continue;
    const normalized = normalizeLabel(name);
    if (
      normalized === normalizeLabel(HOSPITAL_SGH_LAYOUT_NAME)
      || normalized.includes('sala de espera')
      || normalized.includes('sgh')
    ) {
      return true;
    }
  }
  return false;
}

export function isHospitalSghLayout(
  layout: TvLayoutDto,
  displaySlug?: string,
  displaySector?: string,
  displayName?: string,
  displayLayoutName?: string,
): boolean {
  if (isWaitingRoomDisplay(displaySlug, displaySector, displayName)) {
    return true;
  }

  if (layoutNameIndicatesSgh(layout.name, displayLayoutName)) {
    return true;
  }

  return (layout.zones ?? []).some((z) => z.id === 'sgh-template');
}

function parseDestination(destination: string): { guiche: string; sala: string } {
  const text = destination.trim();
  const lower = text.toLowerCase();
  if (lower.includes('guich')) {
    return { guiche: text, sala: '—' };
  }
  if (lower.includes('sala') || lower.includes('consult')) {
    return { guiche: '—', sala: text };
  }
  return { guiche: text, sala: '—' };
}

export { parseDestination };
