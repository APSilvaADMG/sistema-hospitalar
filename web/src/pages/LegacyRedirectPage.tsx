import { Navigate, useLocation } from 'react-router-dom';
import { resolveLegacyRedirect } from '../navigation/legacyRedirects';

/** Redireciona rotas legadas para hubs centrais. */
export function LegacyRedirectPage() {
  const { pathname, search } = useLocation();
  const target = resolveLegacyRedirect(pathname);
  if (!target) {
    return <Navigate to="/" replace />;
  }
  return <Navigate to={`${target}${search}`} replace />;
}
