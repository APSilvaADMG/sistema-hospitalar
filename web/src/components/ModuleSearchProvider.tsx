import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';
import { ModuleSearchDialog, useModuleSearchShortcut } from './ModuleSearchDialog';

type ModuleSearchContextValue = {
  open: () => void;
};

const ModuleSearchContext = createContext<ModuleSearchContextValue>({ open: () => undefined });

export function useOpenModuleSearch() {
  return useContext(ModuleSearchContext).open;
}

export function ModuleSearchProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const openSearch = useCallback(() => setOpen(true), []);

  useModuleSearchShortcut(openSearch);

  return (
    <ModuleSearchContext.Provider value={{ open: openSearch }}>
      {children}
      <ModuleSearchDialog open={open} onClose={() => setOpen(false)} />
    </ModuleSearchContext.Provider>
  );
}
