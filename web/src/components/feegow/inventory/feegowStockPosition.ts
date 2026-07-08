import type { StockMovementDto } from '../../../api/client';

export type FeegowStockPositionRow = {
  id: string;
  batchNumber: string;
  expiryDate?: string;
  individualCode: string;
  location: string;
  responsibleName: string;
  quantity: number;
  unitPrice?: number;
};

export function buildStockPositionRows(movements: StockMovementDto[]): FeegowStockPositionRow[] {
  return movements
    .filter((movement) => movement.type === 1)
    .map((movement) => ({
      id: movement.id,
      batchNumber: movement.batchNumber ?? '—',
      expiryDate: movement.expiryDate,
      individualCode: movement.individualCode ?? '—',
      location: movement.location ?? '—',
      responsibleName: movement.responsibleName ?? '—',
      quantity: movement.quantity,
      unitPrice: movement.unitPrice,
    }));
}

export function stockPositionTotals(
  quantityOnHand: number,
  contentQuantity?: number,
): { conjunto: number; unidade: number } {
  const unidade = quantityOnHand;
  const content = contentQuantity && contentQuantity > 0 ? contentQuantity : 1;
  return {
    conjunto: Number((unidade / content).toFixed(2)),
    unidade,
  };
}
