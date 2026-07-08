export const FILTER_STORAGE_KEYS = {
  dashboard: 'dashboardFiltros',
  appointments: 'hms-filters-appointments',
  patients: 'hms-filters-patients',
  receivable: 'receberListFiltros',
  payable: 'pagarListFiltros',
} as const;

export function loadPersistedFilters<T extends Record<string, unknown>>(key: string, defaults: T): T {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return defaults;
    const parsed = JSON.parse(raw) as Partial<T>;
    return { ...defaults, ...parsed };
  } catch {
    return defaults;
  }
}

export function savePersistedFilters<T extends Record<string, unknown>>(key: string, value: T) {
  localStorage.setItem(key, JSON.stringify(value));
}
