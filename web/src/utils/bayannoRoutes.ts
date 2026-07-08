/** Rotas com layout próprio (já trazem box Feegow completo). Demais telas usam o box do shell. */
const BARE_PREFIXES = ['/', '/dashboard', '/relatorios'];

export function isBayannoBareRoute(pathname: string, _brand: 'feegow' | 'bayanno' = 'feegow'): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  return BARE_PREFIXES.some((prefix) => path === prefix || (prefix !== '/' && path.startsWith(prefix)));
}
