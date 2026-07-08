import { useMemo } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

export type ModuleTab = {
  slug: string;
  label: string;
  /** Rota absoluta quando a aba pertence a outro módulo (ex.: dashboard). */
  to?: string;
};

export function pathToSection(pathname: string, basePath: string): string {
  const base = basePath.replace(/\/$/, '');
  const path = pathname.replace(/\/$/, '') || '/';
  if (path === base) return '';
  if (!path.startsWith(`${base}/`)) return '';
  return path.slice(base.length + 1);
}

export function useModuleSection(basePath: string) {
  const { pathname } = useLocation();
  const navigate = useNavigate();

  const section = useMemo(() => pathToSection(pathname, basePath), [pathname, basePath]);

  function goToSection(slug: string) {
    const base = basePath.replace(/\/$/, '');
    navigate(slug ? `${base}/${slug}` : base);
  }

  return { section, goToSection };
}
