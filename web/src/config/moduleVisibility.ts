import type { ModuleVisibilityFlags } from './clinicOnDoctorProfile';

export type ModuleVisibility = ModuleVisibilityFlags;

export const MODULE_PATH_PREFIXES: { prefix: string; module: keyof ModuleVisibility }[] = [
  { prefix: '/financeiro', module: 'financial' },
  { prefix: '/faturamento', module: 'billing' },
  { prefix: '/faturamento-tiss', module: 'billing' },
  { prefix: '/guias', module: 'billing' },
  { prefix: '/estoque', module: 'inventory' },
  { prefix: '/farmacia', module: 'inventory' },
  { prefix: '/compras', module: 'inventory' },
  { prefix: '/marketing', module: 'marketing' },
  { prefix: '/bi', module: 'bi' },
  { prefix: '/relatorios', module: 'bi' },
];

export function isPathModuleEnabled(path: string, modules: ModuleVisibility): boolean {
  const normalized = path.split('?')[0];
  for (const { prefix, module } of MODULE_PATH_PREFIXES) {
    if (normalized === prefix || normalized.startsWith(`${prefix}/`)) {
      return modules[module];
    }
  }
  return true;
}

function resolveItemPath(item: { path?: string; to?: string }): string {
  return item.path ?? item.to ?? '';
}

export function filterPathsByModules<T extends { path?: string; to?: string }>(
  items: T[],
  modules: ModuleVisibility,
): T[] {
  return items.filter((item) => isPathModuleEnabled(resolveItemPath(item), modules));
}
