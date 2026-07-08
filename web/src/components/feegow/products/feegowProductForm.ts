import type { CreateProductRequest, ProductDetailDto } from '../../../api/client';

export type FeegowProductFormState = {
  name: string;
  type: number;
  presentation: string;
  contentQuantity: string;
  unit: string;
  barcode: string;
  category: string;
  manufacturer: string;
  defaultLocation: string;
  tussCode: string;
  minimumStock: string;
  maximumStock: string;
  expiryWarningDays: string;
  averagePurchasePrice: string;
  averageSalePrice: string;
  allowOutboundFromRegister: boolean;
  entryLocations: string[];
  description: string;
  photoData?: string;
  sku: string;
};

export const FEEGOW_PRODUCT_TYPE_OPTIONS = [
  { value: 3, label: 'Geral' },
  { value: 4, label: 'Produto' },
  { value: 2, label: 'Material' },
  { value: 1, label: 'Medicamento' },
] as const;

export const DEFAULT_PRODUCT_CATEGORIES = [
  'Antialérgico',
  'Vacina',
  'Material',
  'Medicamento',
  'Descartável',
  'Limpeza',
  'Curativo',
];

export const DEFAULT_MANUFACTURERS = [
  'Genérico',
  'EMS',
  'Eurofarma',
  'Medley',
  'Outro',
];

export const DEFAULT_LOCATIONS = [
  'Almoxarifado Central',
  'Farmácia',
  'Consultório 01',
  'Centro Cirúrgico',
  'Enfermaria',
];

export const DEFAULT_PRESENTATIONS = [
  'Caixa',
  'Frasco',
  'Ampola',
  'Comprimido',
  'Unidade',
  'Kit',
];

export const DEFAULT_UNITS = ['UN', 'CX', 'FR', 'CP', 'PC', 'KIT', 'GL', 'ML'];

export function emptyFeegowProductForm(): FeegowProductFormState {
  return {
    name: '',
    type: 1,
    presentation: '',
    contentQuantity: '',
    unit: 'UN',
    barcode: '',
    category: '',
    manufacturer: '',
    defaultLocation: '',
    tussCode: '',
    minimumStock: '0',
    maximumStock: '0',
    expiryWarningDays: '30',
    averagePurchasePrice: '0',
    averageSalePrice: '0',
    allowOutboundFromRegister: true,
    entryLocations: [],
    description: '',
    sku: '',
  };
}

function parseDecimal(value: string, fallback = 0): number {
  const normalized = value.replace(',', '.').trim();
  if (!normalized) return fallback;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : fallback;
}

export function feegowFormToCreatePayload(form: FeegowProductFormState): CreateProductRequest {
  return {
    name: form.name.trim(),
    sku: form.sku.trim(),
    type: form.type,
    unit: form.unit.trim() || 'UN',
    minimumStock: parseDecimal(form.minimumStock),
    maximumStock: parseDecimal(form.maximumStock),
    description: form.description.trim() || undefined,
    presentation: form.presentation.trim() || undefined,
    contentQuantity: form.contentQuantity.trim() ? parseDecimal(form.contentQuantity) : undefined,
    barcode: form.barcode.trim() || undefined,
    category: form.category.trim() || undefined,
    manufacturer: form.manufacturer.trim() || undefined,
    defaultLocation: form.defaultLocation.trim() || undefined,
    tussCode: form.tussCode.trim() || undefined,
    expiryWarningDays: Math.max(0, Math.trunc(parseDecimal(form.expiryWarningDays))),
    averagePurchasePrice: parseDecimal(form.averagePurchasePrice),
    averageSalePrice: parseDecimal(form.averageSalePrice),
    allowOutboundFromRegister: form.allowOutboundFromRegister,
    entryLocations: form.entryLocations.length > 0 ? form.entryLocations.join(';') : undefined,
    photoData: form.photoData,
  };
}

export function detailToFeegowForm(detail: ProductDetailDto): FeegowProductFormState {
  return {
    name: detail.name,
    type: detail.type,
    presentation: detail.presentation ?? '',
    contentQuantity: detail.contentQuantity != null ? String(detail.contentQuantity) : '',
    unit: detail.unit,
    barcode: detail.barcode ?? '',
    category: detail.category ?? '',
    manufacturer: detail.manufacturer ?? '',
    defaultLocation: detail.defaultLocation ?? '',
    tussCode: detail.tussCode ?? '',
    minimumStock: String(detail.minimumStock),
    maximumStock: String(detail.maximumStock),
    expiryWarningDays: String(detail.expiryWarningDays),
    averagePurchasePrice: String(detail.averagePurchasePrice),
    averageSalePrice: String(detail.averageSalePrice),
    allowOutboundFromRegister: detail.allowOutboundFromRegister,
    entryLocations: detail.entryLocations
      ? detail.entryLocations.split(';').map((item) => item.trim()).filter(Boolean)
      : [],
    description: detail.description ?? '',
    photoData: detail.photoData ?? undefined,
    sku: detail.sku,
  };
}

export function feegowProductTypeLabel(type: number): string {
  return FEEGOW_PRODUCT_TYPE_OPTIONS.find((option) => option.value === type)?.label ?? 'Produto';
}

export function parseFeegowProductRoute(pathname: string): 'insert' | 'list' | 'kits-list' | 'kits-insert' | 'requisitions-list' | 'requisitions-insert' | 'warehouse-dashboard' | 'warehouse-receipt' | 'warehouse-issue' | 'config' | null {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  if (path === '/estoque/dashboard') return 'warehouse-dashboard';
  if (path === '/estoque/entrada') return 'warehouse-receipt';
  if (path === '/estoque/saida') return 'warehouse-issue';
  if (path === '/estoque/inserir' || path.startsWith('/estoque/inserir/')) return 'insert';
  if (path === '/estoque/listar') return 'list';
  if (path === '/estoque/kits/inserir' || path.startsWith('/estoque/kits/inserir/')) return 'kits-insert';
  if (path === '/estoque/kits') return 'kits-list';
  if (path === '/estoque/requisicoes/inserir' || path.startsWith('/estoque/requisicoes/inserir/')) return 'requisitions-insert';
  if (path === '/estoque/requisicoes') return 'requisitions-list';
  if (path.startsWith('/estoque/config/')) return 'config';
  if (path === '/estoque') return 'insert';
  return null;
}
