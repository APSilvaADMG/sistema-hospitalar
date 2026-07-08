import { Navigate, useLocation } from 'react-router-dom';
import { resolveLegacyRedirect } from '../navigation/legacyRedirects';
import { implementedRoutes } from '../navigation/routeMap';
import { ModulePlaceholderPage } from './ModulePlaceholderPage';

/**
 * Fallback quando nenhuma rota estática/splat casou.
 * Renderiza o componente do prefixo mais específico (mantém a URL / aba).
 */
export function OrphanRoutePage() {
  const { pathname, search } = useLocation();
  const normalized = pathname.replace(/\/$/, '') || '/';

  const legacyTarget = resolveLegacyRedirect(normalized);
  if (legacyTarget) {
    return <Navigate to={`${legacyTarget}${search}`} replace />;
  }

  if (implementedRoutes[normalized]) {
    const Page = implementedRoutes[normalized];
    return <Page />;
  }

  const sorted = Object.keys(implementedRoutes)
    .filter((k) => k !== '/')
    .sort((a, b) => b.length - a.length);

  for (const root of sorted) {
    if (normalized.startsWith(`${root}/`)) {
      const Page = implementedRoutes[root];
      if (Page) return <Page />;
    }
  }

  return <ModulePlaceholderPage />;
}
