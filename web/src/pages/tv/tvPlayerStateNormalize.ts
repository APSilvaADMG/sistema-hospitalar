import type { TvLayoutZoneDto, TvPlayerStateDto, TvWidgetType } from '../../api/client';
import { normalizeTvMediaList } from './tvMediaUtils';

const TV_WIDGET_BY_NAME: Record<string, TvWidgetType> = {
  MediaCarousel: 1,
  QueueCalls: 2,
  NewsTicker: 3,
  Weather: 4,
  Clock: 5,
  Dashboard: 6,
  Announcements: 7,
  Bulletin: 8,
  Schedule: 9,
};

export function normalizeTvWidgetType(widget: unknown): TvWidgetType {
  if (typeof widget === 'number' && widget >= 1 && widget <= 9) {
    return widget as TvWidgetType;
  }
  const key = String(widget ?? '');
  return TV_WIDGET_BY_NAME[key] ?? 1;
}

function normalizeZone(zone: TvLayoutZoneDto): TvLayoutZoneDto {
  return {
    ...zone,
    widget: normalizeTvWidgetType(zone.widget),
  };
}

/** API serializes enums as strings; coerce player payload for zone widgets. */
export function normalizeTvPlayerState(state: TvPlayerStateDto): TvPlayerStateDto {
  return {
    ...state,
    layout: {
      ...state.layout,
      zones: (state.layout?.zones ?? []).map(normalizeZone),
    },
    media: normalizeTvMediaList(state.media ?? []),
  };
}
