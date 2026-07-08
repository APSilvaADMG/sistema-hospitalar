import type {
  CreateProductKitRequest,
  ProductDetailDto,
  ProductDto,
  ProductKitDetailDto,
} from '../../../api/client';

export type FeegowProductKitItemForm = {
  key: string;
  productId: string;
  productName: string;
  productSku: string;
  quantity: string;
  insuranceCode: string;
  unitPrice: string;
  variablePrice: boolean;
};

export type FeegowProductKitFormState = {
  name: string;
  priceTable: string;
  items: FeegowProductKitItemForm[];
};

export const DEFAULT_PRICE_TABLES = [
  'Particular',
  'TUSS',
  'AMB',
  'CBHPM',
];

function parseDecimal(value: string, fallback = 0): number {
  const normalized = value.replace(',', '.').trim();
  if (!normalized) return fallback;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : fallback;
}

export function emptyFeegowProductKitForm(): FeegowProductKitFormState {
  return {
    name: '',
    priceTable: '',
    items: [emptyKitItemRow()],
  };
}

export function emptyKitItemRow(): FeegowProductKitItemForm {
  return {
    key: crypto.randomUUID(),
    productId: '',
    productName: '',
    productSku: '',
    quantity: '1',
    insuranceCode: '',
    unitPrice: '0',
    variablePrice: false,
  };
}

export function productToKitItemRow(product: ProductDto | ProductDetailDto): FeegowProductKitItemForm {
  const code = 'tussCode' in product && product.tussCode
    ? product.tussCode
    : product.barcode ?? product.sku;
  return {
    key: crypto.randomUUID(),
    productId: product.id,
    productName: product.name,
    productSku: product.sku,
    quantity: '1',
    insuranceCode: code,
    unitPrice: String(product.averageSalePrice ?? 0),
    variablePrice: false,
  };
}

export function detailToFeegowKitForm(detail: ProductKitDetailDto): FeegowProductKitFormState {
  return {
    name: detail.name,
    priceTable: detail.priceTable ?? '',
    items: detail.items.length > 0
      ? detail.items.map((item) => ({
          key: item.id,
          productId: item.productId,
          productName: item.productName,
          productSku: item.productSku ?? '',
          quantity: String(item.quantity),
          insuranceCode: item.insuranceCode ?? '',
          unitPrice: String(item.unitPrice),
          variablePrice: item.variablePrice,
        }))
      : [emptyKitItemRow()],
  };
}

export function feegowKitFormToPayload(form: FeegowProductKitFormState): CreateProductKitRequest {
  return {
    name: form.name.trim(),
    priceTable: form.priceTable.trim() || undefined,
    items: form.items
      .filter((item) => item.productId)
      .map((item) => ({
        productId: item.productId,
        quantity: parseDecimal(item.quantity, 1),
        insuranceCode: item.insuranceCode.trim() || undefined,
        unitPrice: parseDecimal(item.unitPrice),
        variablePrice: item.variablePrice,
      })),
  };
}

export function kitFormTotalPrice(form: FeegowProductKitFormState): number {
  return form.items.reduce((sum, item) => {
    if (!item.productId) return sum;
    return sum + parseDecimal(item.quantity, 0) * parseDecimal(item.unitPrice);
  }, 0);
}
