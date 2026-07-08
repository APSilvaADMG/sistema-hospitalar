import { useCallback, useEffect, useState } from 'react';
import {
  loadSghcScreenData,
  mergeSghcColumns,
  resolveSghcDataModule,
  type SghcColumn,
  type SghcDataModule,
  type SghcRow,
} from '../components/feegow/sghc/sghcScreenData';

type State = {
  module: SghcDataModule | null;
  columns: SghcColumn[];
  rows: SghcRow[];
  summary?: string;
  moduleLink?: string | null;
  loading: boolean;
  error: string;
};

export function useSghcScreenData(route: string, enabled: boolean) {
  const [state, setState] = useState<State>({
    module: resolveSghcDataModule(route),
    columns: [],
    rows: [],
    loading: false,
    error: '',
  });

  const reload = useCallback(async () => {
    const module = resolveSghcDataModule(route);
    if (!enabled || !module) {
      setState((prev) => ({
        ...prev,
        module,
        columns: [],
        rows: [],
        loading: false,
        error: '',
      }));
      return;
    }

    setState((prev) => ({ ...prev, module, loading: true, error: '' }));
    try {
      const result = await loadSghcScreenData(route);
      setState({
        module: result.module,
        columns: result.columns,
        rows: result.rows,
        summary: result.summary,
        moduleLink: result.moduleLink,
        loading: false,
        error: '',
      });
    } catch (err) {
      setState((prev) => ({
        ...prev,
        loading: false,
        error: err instanceof Error ? err.message : 'Erro ao carregar dados',
      }));
    }
  }, [route, enabled]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { ...state, reload };
}

export function useSghcColumns(
  module: SghcDataModule | null,
  bayannoColumns: { label: string; labelKey: string }[],
): SghcColumn[] {
  if (!module) return [];
  return mergeSghcColumns(module, bayannoColumns);
}
