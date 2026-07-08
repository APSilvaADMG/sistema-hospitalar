export type FeegowInventoryItemType = 'geral' | 'produto' | 'material' | 'medicamento';

export type FeegowProductTab = 'cadastro' | 'movimentacao' | 'faturamento';

export type FeegowInventoryConfigId =
  | 'medicamento-convenio'
  | 'categorias'
  | 'localizacoes'
  | 'fabricantes'
  | 'kits';

export const FEEGOW_INVENTORY_ITEM_TYPES = [
  { id: 'geral' as const, label: 'Geral', icon: '🩺', typeValue: 0 },
  { id: 'produto' as const, label: 'Produto', icon: '📦', typeValue: 4 },
  { id: 'material' as const, label: 'Material', icon: '💼', typeValue: 2 },
  { id: 'medicamento' as const, label: 'Medicamento', icon: '💊', typeValue: 1 },
];

export const FEEGOW_INVENTORY_CONFIG = [
  { id: 'medicamento-convenio' as const, label: 'Medicamento Por Convênio', path: '/estoque/config/medicamento-convenio' },
  { id: 'categorias' as const, label: 'Categorias', path: '/estoque/config/categorias' },
  { id: 'localizacoes' as const, label: 'Localizações', path: '/estoque/config/localizacoes' },
  { id: 'fabricantes' as const, label: 'Fabricantes', path: '/estoque/config/fabricantes' },
  { id: 'kits' as const, label: 'Kits', path: '/estoque/kits' },
];

export const FEEGOW_PRODUCT_TABS = [
  { id: 'cadastro' as const, label: 'Cadastro', icon: '📝' },
  { id: 'movimentacao' as const, label: 'Movimentação', icon: '⇄' },
  { id: 'faturamento' as const, label: 'Faturamento', icon: '☰' },
];

export function inventoryTypeValue(tipo: FeegowInventoryItemType): number {
  return FEEGOW_INVENTORY_ITEM_TYPES.find((item) => item.id === tipo)?.typeValue ?? 0;
}

/** Filtro de tipo na listagem: geral = todos; demais abas filtram pelo enum do backend. */
export function inventoryListTypeFilter(tipo: FeegowInventoryItemType): number | undefined {
  const value = inventoryTypeValue(tipo);
  return value > 0 ? value : undefined;
}

/** Tipo de produto ao cadastrar (aba geral = General no backend). */
export function inventoryInsertProductType(tipo: FeegowInventoryItemType): number {
  const value = inventoryTypeValue(tipo);
  return value > 0 ? value : 3;
}

export function inventoryTypeFromValue(typeValue: number): FeegowInventoryItemType {
  return FEEGOW_INVENTORY_ITEM_TYPES.find((item) => item.typeValue === typeValue)?.id ?? 'geral';
}

export function inventoryTypeLabel(tipo: FeegowInventoryItemType): string {
  return FEEGOW_INVENTORY_ITEM_TYPES.find((item) => item.id === tipo)?.label ?? 'Geral';
}

export function parseInventoryTipo(raw: string | null | undefined): FeegowInventoryItemType {
  if (raw === 'produto' || raw === 'material' || raw === 'medicamento' || raw === 'geral') {
    return raw;
  }
  return 'geral';
}

export function parseProductTab(raw: string | null | undefined): FeegowProductTab {
  if (raw === 'movimentacao' || raw === 'faturamento' || raw === 'cadastro') {
    return raw;
  }
  return 'cadastro';
}

export function feegowInventoryListPath(tipo: FeegowInventoryItemType = 'geral'): string {
  return `/estoque/listar?tipo=${tipo}`;
}

export function feegowInventoryInsertPath(
  tipo: FeegowInventoryItemType = 'geral',
  options?: { id?: string; aba?: FeegowProductTab },
): string {
  const params = new URLSearchParams({ tipo });
  if (options?.id) params.set('id', options.id);
  if (options?.aba) params.set('aba', options.aba);
  return `/estoque/inserir?${params.toString()}`;
}

export type FeegowInventoryLookupConfigId = 'categorias' | 'localizacoes' | 'fabricantes';

export const FEEGOW_INVENTORY_LOOKUP_CONFIG: Record<
  FeegowInventoryLookupConfigId,
  { title: string; fieldLabel: string; lookupType: number }
> = {
  categorias: { title: 'Categorias de Produto', fieldLabel: 'Categoria', lookupType: 1 },
  localizacoes: { title: 'Localizações de Produto', fieldLabel: 'Localização', lookupType: 2 },
  fabricantes: { title: 'Fabricantes de Produto', fieldLabel: 'Fabricante', lookupType: 3 },
};

export function feegowInventoryLookupListPath(configId: FeegowInventoryLookupConfigId): string {
  return `/estoque/config/${configId}`;
}

export function feegowInventoryLookupInsertPath(
  configId: FeegowInventoryLookupConfigId,
  id?: string,
): string {
  const base = `/estoque/config/${configId}/inserir`;
  return id ? `${base}?id=${id}` : base;
}

export function resolveInventoryConfigId(pathname: string): FeegowInventoryConfigId | null {
  const path = pathname.split('?')[0].replace(/\/$/, '');
  if (path === '/estoque/kits' || path.startsWith('/estoque/kits/')) return 'kits';
  if (path === '/estoque/config/medicamento-convenio' || path.startsWith('/estoque/config/medicamento-convenio/')) {
    return 'medicamento-convenio';
  }
  if (path === '/estoque/config/categorias' || path.startsWith('/estoque/config/categorias/')) return 'categorias';
  if (path === '/estoque/config/localizacoes' || path.startsWith('/estoque/config/localizacoes/')) {
    return 'localizacoes';
  }
  if (path === '/estoque/config/fabricantes' || path.startsWith('/estoque/config/fabricantes/')) {
    return 'fabricantes';
  }
  return null;
}

export function isInventoryLookupInsertRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '');
  return (
    path === '/estoque/config/categorias/inserir'
    || path === '/estoque/config/localizacoes/inserir'
    || path === '/estoque/config/fabricantes/inserir'
  );
}

export function resolveInventoryLookupConfigId(pathname: string): FeegowInventoryLookupConfigId | null {
  const configId = resolveInventoryConfigId(pathname);
  if (configId === 'categorias' || configId === 'localizacoes' || configId === 'fabricantes') {
    return configId;
  }
  return null;
}

export function isInventoryListRoute(pathname: string): boolean {
  return pathname.split('?')[0].replace(/\/$/, '') === '/estoque/listar';
}

export function isInventoryInsertRoute(pathname: string): boolean {
  const path = pathname.split('?')[0].replace(/\/$/, '');
  return path === '/estoque/inserir' || path.startsWith('/estoque/inserir/');
}
