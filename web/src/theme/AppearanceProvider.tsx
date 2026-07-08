import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import {
  applyAppearanceToDocument,
  defaultAppearance,
  loadAppearance,
  normalizeAppearance,
  saveAppearance,
  type AppearanceSettings,
  type BrandTheme,
  type ColorScheme,
  type SidebarMode,
  type UiDensity,
} from './appearanceConfig';

type AppearanceContextValue = {
  appearance: AppearanceSettings;
  setBrand: (brand: BrandTheme) => void;
  setScheme: (scheme: ColorScheme) => void;
  setDensity: (density: UiDensity) => void;
  setSidebarMode: (mode: SidebarMode) => void;
  updateAppearance: (patch: Partial<AppearanceSettings>) => void;
  resetAppearance: () => void;
};

const AppearanceContext = createContext<AppearanceContextValue | null>(null);

export function AppearanceProvider({ children }: { children: ReactNode }) {
  const [appearance, setAppearance] = useState<AppearanceSettings>(() => loadAppearance());

  const commit = useCallback((updater: AppearanceSettings | ((prev: AppearanceSettings) => AppearanceSettings)) => {
    setAppearance((prev) => {
      const draft = typeof updater === 'function' ? updater(prev) : updater;
      saveAppearance(draft);
      const next = normalizeAppearance(draft);
      applyAppearanceToDocument(next);
      return next;
    });
  }, []);

  const value = useMemo<AppearanceContextValue>(() => ({
    appearance,
    setBrand: (brand) => commit((prev) => ({ ...prev, brand })),
    setScheme: (scheme) => commit((prev) => ({ ...prev, scheme })),
    setDensity: (density) => commit((prev) => ({ ...prev, density })),
    setSidebarMode: (sidebarMode) => commit((prev) => ({ ...prev, sidebarMode })),
    updateAppearance: (patch) => commit((prev) => ({ ...prev, ...patch })),
    resetAppearance: () => commit(defaultAppearance),
  }), [appearance, commit]);

  return (
    <AppearanceContext.Provider value={value}>
      {children}
    </AppearanceContext.Provider>
  );
}

export function useAppearance() {
  const ctx = useContext(AppearanceContext);
  if (!ctx) {
    throw new Error('useAppearance deve ser usado dentro de AppearanceProvider');
  }
  return ctx;
}
