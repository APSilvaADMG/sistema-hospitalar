import { useCallback, useEffect, useState } from 'react';
import { loadPersistedFilters, savePersistedFilters } from '../utils/persistedFilters';

export function usePersistedFilters<T extends Record<string, unknown>>(storageKey: string, defaults: T) {
  const [filters, setFilters] = useState<T>(() => loadPersistedFilters(storageKey, defaults));

  useEffect(() => {
    savePersistedFilters(storageKey, filters);
  }, [storageKey, filters]);

  const patch = useCallback((partial: Partial<T>) => {
    setFilters((prev) => ({ ...prev, ...partial }));
  }, []);

  const reset = useCallback(() => {
    setFilters(defaults);
  }, [defaults]);

  return { filters, setFilters, patch, reset };
}
