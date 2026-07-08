import type { ComponentType } from 'react';
import { implementedRoutes } from './routeMap';

/**
 * Módulos cujo componente raiz atende sub-rotas via abas internas (ModuleNav).
 * Registramos `prefix/*` para qualquer subcaminho funcionar sem centenas de rotas no menu.
 */
export const HUB_MODULE_PREFIXES = [
  '/recepcao',
  '/ambulatorio',
  '/emergencia',
  '/internacao',
  '/centro-cirurgico',
  '/uti',
  '/pep',
  '/enfermagem',
  '/laboratorio',
  '/imagem',
  '/farmacia',
  '/hemoterapia',
  '/nutricao',
  '/oncologia',
  '/faturamento',
  '/financeiro',
  '/estoque',
  '/compras',
  '/rh',
  '/ccih',
  '/relatorios',
  '/guias',
  '/bi',
  '/qualidade',
  '/regulacao',
  '/acesso-fisico',
  '/integracoes-gov',
  '/integracoes',
  '/transportes',
  '/hotelaria',
  '/dialise',
  '/fisioterapia',
  '/lavanderia',
  '/ambulancias',
  '/seguranca-lgpd',
  '/configuracoes',
  '/automacao',
  '/engenharia-clinica',
  '/connect',
  '/ia',
  '/telemedicina',
  '/cme',
  '/seguranca',
  '/sghc',
  '/convenios',
  '/profissionais',
  '/auditoria',
  '/notificacoes',
  '/usuarios',
  '/dashboard',
] as const;

export function getHubWildcardRoutes(): { path: string; Component: ComponentType }[] {
  return HUB_MODULE_PREFIXES
    .filter((prefix) => implementedRoutes[prefix])
    .map((prefix) => ({
      path: `${prefix.slice(1)}/*`,
      Component: implementedRoutes[prefix],
    }));
}

/** Redireciona URL órfã para o módulo-pai implementado mais próximo. */
export function findHubRedirect(pathname: string): string | null {
  if (implementedRoutes[pathname]) return null;

  const normalized = pathname.replace(/\/$/, '') || '/';
  const sorted = Object.keys(implementedRoutes)
    .filter((k) => k !== '/')
    .sort((a, b) => b.length - a.length);

  for (const root of sorted) {
    if (normalized === root || normalized.startsWith(`${root}/`)) {
      return root;
    }
  }
  return null;
}
