import type {
  CreateStockRequisitionRequest,
  ProductDto,
  StockRequisitionDetailDto,
} from '../../../api/client';
import { DEFAULT_LOCATIONS } from './feegowProductForm';

export type FeegowStockRequisitionItemForm = {
  key: string;
  productId: string;
  productName: string;
  productSku: string;
  productUnit: string;
  quantityOnHand: number;
  quantity: string;
  itemStatus: number;
  fulfilledQuantity: string;
  unitPrice: string;
  notes: string;
};

export type FeegowStockRequisitionFormState = {
  requestedBy: string;
  recipientName: string;
  priority: number;
  status: number;
  destinationLocation: string;
  dueDate: string;
  notes: string;
  items: FeegowStockRequisitionItemForm[];
};

export const DEFAULT_DESTINATION_LOCATIONS = [
  'Almoxarifado Central',
  'Farmácia',
  ...DEFAULT_LOCATIONS.filter((loc) => loc !== 'Almoxarifado Central' && loc !== 'Farmácia'),
];

export const FEEGOW_REQUISITION_PRIORITY_OPTIONS = [
  { value: 1, label: 'Baixíssima' },
  { value: 2, label: 'Baixa' },
  { value: 3, label: 'Normal' },
  { value: 4, label: 'Alta' },
  { value: 5, label: 'Urgente' },
] as const;

function parseDecimal(value: string, fallback = 0): number {
  const normalized = value.replace(/\./g, '').replace(',', '.').trim();
  if (!normalized) return fallback;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : fallback;
}

export function formatDecimalInput(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function formatCurrencyInput(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function emptyFeegowStockRequisitionForm(requestedBy = ''): FeegowStockRequisitionFormState {
  const today = new Date();
  const dueDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
  return {
    requestedBy,
    recipientName: '',
    priority: 1,
    status: 1,
    destinationLocation: '',
    dueDate,
    notes: '',
    items: [emptyRequisitionItemRow()],
  };
}

export function emptyRequisitionItemRow(): FeegowStockRequisitionItemForm {
  return {
    key: crypto.randomUUID(),
    productId: '',
    productName: '',
    productSku: '',
    productUnit: '',
    quantityOnHand: 0,
    quantity: '1,00',
    itemStatus: 1,
    fulfilledQuantity: '0,00',
    unitPrice: '0,00',
    notes: '',
  };
}

export function productToRequisitionItemRow(product: ProductDto): FeegowStockRequisitionItemForm {
  const unitPrice = product.averageSalePrice ?? 0;
  return {
    key: crypto.randomUUID(),
    productId: product.id,
    productName: product.name,
    productSku: product.sku,
    productUnit: product.unit,
    quantityOnHand: product.quantityOnHand,
    quantity: '1,00',
    itemStatus: 1,
    fulfilledQuantity: '0,00',
    unitPrice: formatCurrencyInput(unitPrice),
    notes: '',
  };
}

export function detailToFeegowRequisitionForm(detail: StockRequisitionDetailDto): FeegowStockRequisitionFormState {
  return {
    requestedBy: detail.requestedBy,
    recipientName: detail.recipientName ?? '',
    priority: detail.priority,
    status: detail.status,
    destinationLocation: detail.destinationLocation ?? '',
    dueDate: detail.dueDate ?? '',
    notes: detail.notes ?? '',
    items: detail.items.length > 0
      ? detail.items.map((item) => ({
          key: item.id,
          productId: item.productId,
          productName: item.productName,
          productSku: item.productSku,
          productUnit: item.productUnit,
          quantityOnHand: item.quantityOnHand,
          quantity: formatDecimalInput(item.quantity),
          itemStatus: item.itemStatus,
          fulfilledQuantity: formatDecimalInput(item.fulfilledQuantity),
          unitPrice: formatCurrencyInput(item.unitPrice),
          notes: item.notes ?? '',
        }))
      : [emptyRequisitionItemRow()],
  };
}

export function feegowRequisitionFormToPayload(
  form: FeegowStockRequisitionFormState,
): CreateStockRequisitionRequest {
  return {
    requestedBy: form.requestedBy.trim(),
    recipientName: form.recipientName.trim() || undefined,
    priority: form.priority,
    dueDate: form.dueDate || undefined,
    destinationLocation: form.destinationLocation.trim() || undefined,
    notes: form.notes.trim() || undefined,
    items: form.items
      .filter((item) => item.productId)
      .map((item) => ({
        productId: item.productId,
        quantity: parseDecimal(item.quantity, 1),
        itemStatus: item.itemStatus,
        unitPrice: parseDecimal(item.unitPrice),
        notes: item.notes.trim() || undefined,
      })),
  };
}

export function itemLineTotal(item: FeegowStockRequisitionItemForm): number {
  return parseDecimal(item.quantity, 0) * parseDecimal(item.unitPrice);
}
