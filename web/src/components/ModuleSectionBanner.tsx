import { resolvePageTitle } from '../navigation/sectionBreadcrumb';
import { useLocation } from 'react-router-dom';
type Props = {
  basePath: string;
};

/** Exibe o título da sub-rota quando não é a raiz do módulo. */
export function ModuleSectionBanner({ basePath }: Props) {
  const { pathname } = useLocation();
  const base = basePath.replace(/\/$/, '');
  if (pathname === base) return null;

  const title = resolvePageTitle(pathname);
  return (
    <div className="module-section-banner">
      Seção: <strong>{title}</strong>
    </div>
  );}
