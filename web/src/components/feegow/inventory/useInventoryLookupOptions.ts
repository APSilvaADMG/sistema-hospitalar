import { useEffect, useState } from 'react';
import { api } from '../../../api/client';
import {
  DEFAULT_LOCATIONS,
  DEFAULT_MANUFACTURERS,
  DEFAULT_PRODUCT_CATEGORIES,
} from '../products/feegowProductForm';

export const INVENTORY_LOOKUP_TYPES = {
  category: 1,
  location: 2,
  manufacturer: 3,
} as const;

const LEGACY_LOOKUP_DEMO_MARKER = 'gth-inventory-lookup-demo-v1';

export function cleanLookupDisplayName(name: string): string {
  const suffix = ` |${LEGACY_LOOKUP_DEMO_MARKER}`;
  return name.endsWith(suffix) ? name.slice(0, -suffix.length).trim() : name.trim();
}

export function mergeLookupOptions(
  apiNames: string[],
  ...extras: Array<string | undefined | null | string[]>
): string[] {
  const set = new Set(apiNames.map((name) => cleanLookupDisplayName(name)).filter(Boolean));
  for (const extra of extras) {
    if (!extra) continue;
    if (Array.isArray(extra)) {
      extra.map((value) => value.trim()).filter(Boolean).forEach((value) => set.add(value));
    } else if (extra.trim()) {
      set.add(extra.trim());
    }
  }
  return [...set].sort((a, b) => a.localeCompare(b, 'pt-BR'));
}

export type InventoryLookupOptions = {
  categories: string[];
  locations: string[];
  manufacturers: string[];
  loading: boolean;
};

type ExtraValues = {
  category?: string;
  categories?: string[];
  manufacturer?: string;
  manufacturers?: string[];
  location?: string;
  locations?: string[];
  entryLocations?: string[];
};

export function useInventoryLookupOptions(extra?: ExtraValues): InventoryLookupOptions {
  const [categories, setCategories] = useState<string[]>(DEFAULT_PRODUCT_CATEGORIES);
  const [locations, setLocations] = useState<string[]>(DEFAULT_LOCATIONS);
  const [manufacturers, setManufacturers] = useState<string[]>(DEFAULT_MANUFACTURERS);
  const [loading, setLoading] = useState(true);

  const extraKey = [
    extra?.category ?? '',
    extra?.manufacturer ?? '',
    extra?.location ?? '',
    ...(extra?.categories ?? []),
    ...(extra?.manufacturers ?? []),
    ...(extra?.locations ?? []),
    ...(extra?.entryLocations ?? []),
  ].join('|');

  useEffect(() => {
    let cancelled = false;
    setLoading(true);

    Promise.all([
      api.getInventoryLookupItems(INVENTORY_LOOKUP_TYPES.category),
      api.getInventoryLookupItems(INVENTORY_LOOKUP_TYPES.location),
      api.getInventoryLookupItems(INVENTORY_LOOKUP_TYPES.manufacturer),
    ])
      .then(([categoryItems, locationItems, manufacturerItems]) => {
        if (cancelled) return;

        const apiCategories = categoryItems.map((item) => item.name);
        const apiLocations = locationItems.map((item) => item.name);
        const apiManufacturers = manufacturerItems.map((item) => item.name);

        setCategories(mergeLookupOptions(
          apiCategories.length > 0 ? apiCategories : DEFAULT_PRODUCT_CATEGORIES,
          extra?.category,
          extra?.categories,
        ));
        setLocations(mergeLookupOptions(
          apiLocations.length > 0 ? apiLocations : DEFAULT_LOCATIONS,
          extra?.location,
          extra?.locations,
          extra?.entryLocations,
        ));
        setManufacturers(mergeLookupOptions(
          apiManufacturers.length > 0 ? apiManufacturers : DEFAULT_MANUFACTURERS,
          extra?.manufacturer,
          extra?.manufacturers,
        ));
      })
      .catch(() => {
        if (cancelled) return;
        setCategories(mergeLookupOptions(DEFAULT_PRODUCT_CATEGORIES, extra?.category, extra?.categories));
        setLocations(mergeLookupOptions(
          DEFAULT_LOCATIONS,
          extra?.location,
          extra?.locations,
          extra?.entryLocations,
        ));
        setManufacturers(mergeLookupOptions(
          DEFAULT_MANUFACTURERS,
          extra?.manufacturer,
          extra?.manufacturers,
        ));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [extraKey]);

  return { categories, locations, manufacturers, loading };
}
