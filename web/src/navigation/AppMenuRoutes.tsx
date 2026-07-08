import { Route } from 'react-router-dom';
import { FUNI_GUIDE_CATALOG } from '../data/funiGuides/catalog';
import { FuniGuidesHubPage } from '../pages/FuniGuidesHubPage';
import { TissPage } from '../pages/TissPage';
import { OrphanRoutePage } from '../pages/OrphanRoutePage';
import { getHubWildcardRoutes } from './hubRoutes';
import { implementedRoutes } from './routeMap';

/**
 * Rotas implementadas + FUNI/TISS (prioridade) + wildcards de hub + fallback.
 */
export function buildAppMenuRoutes() {
  const explicitPaths = Object.keys(implementedRoutes)
    .filter((path) => path !== '/')
    .sort((a, b) => b.length - a.length);

  const explicitRoutes = explicitPaths.map((path) => {
    const Page = implementedRoutes[path];
    return <Route key={`route-${path}`} path={path.slice(1)} element={<Page />} />;
  });

  const hubRoutes = getHubWildcardRoutes().map(({ path, Component }) => (
    <Route key={`hub-${path}`} path={path} element={<Component />} />
  ));

  const funiRoutes = [
    ...FUNI_GUIDE_CATALOG.map((guide) => (
      <Route
        key={`funi-${guide.slug}`}
        path={`faturamento-tiss/guias-funi/${guide.slug}`}
        element={<FuniGuidesHubPage />}
      />
    )),
    <Route key="funi-hub" path="faturamento-tiss/guias-funi/*" element={<FuniGuidesHubPage />} />,
    <Route key="tiss-hub" path="faturamento-tiss/*" element={<TissPage />} />,
  ];

  return [
    ...funiRoutes,
    ...explicitRoutes,
    ...hubRoutes,
    <Route key="orphan" path="*" element={<OrphanRoutePage />} />,
  ];
}
