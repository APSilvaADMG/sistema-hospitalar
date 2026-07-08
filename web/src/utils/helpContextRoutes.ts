/** Rotas com botão contextual [? Ajuda]. */
export const CONTEXTUAL_HELP_ROUTE_PREFIXES = [
  '/recepcao/pacientes',
  '/pacientes',
  '/recepcao/agendamentos',
  '/agenda',
  '/guias',
  '/faturamento-tiss',
  '/faturamento',
  '/financeiro',
  '/estoque',
  '/internacao',
  '/connect',
];

export function shouldShowContextualHelp(pathname: string): boolean {
  const path = pathname.split('?')[0];
  return CONTEXTUAL_HELP_ROUTE_PREFIXES.some(
    (prefix) => path === prefix || path.startsWith(`${prefix}/`),
  );
}
