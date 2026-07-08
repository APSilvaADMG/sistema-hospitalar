import type { TvMediaDto, TvMediaType } from '../../api/client';

const TV_MEDIA_TYPE_BY_NAME: Record<string, TvMediaType> = {
  Image: 1,
  Video: 2,
  Pdf: 3,
  Slideshow: 4,
};

export function normalizeTvMediaType(mediaType: unknown): TvMediaType {
  if (typeof mediaType === 'number' && mediaType >= 1 && mediaType <= 4) {
    return mediaType as TvMediaType;
  }
  const key = String(mediaType ?? '');
  return TV_MEDIA_TYPE_BY_NAME[key] ?? 1;
}

export function isTvVideoMedia(mediaType: unknown): boolean {
  return normalizeTvMediaType(mediaType) === 2;
}

/** Resolve /tv-demo e /uploads para a origem correta no player. */
export function resolveTvMediaUrl(url: string): string {
  if (!url) return url;
  if (/^https?:\/\//i.test(url)) return url;
  if (url.startsWith('/uploads/')) {
    const apiBase = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '')
      ?? `${window.location.origin}/api`;
    return `${apiBase.replace(/\/api$/, '')}${url}`;
  }
  const path = url.startsWith('/') ? url : `/${url}`;
  return `${window.location.origin}${path}`;
}

export function normalizeTvMediaItem(item: TvMediaDto): TvMediaDto {
  return {
    ...item,
    mediaType: normalizeTvMediaType(item.mediaType),
    url: resolveTvMediaUrl(item.url),
  };
}

export function normalizeTvMediaList(media: TvMediaDto[]): TvMediaDto[] {
  return media.map(normalizeTvMediaItem);
}
