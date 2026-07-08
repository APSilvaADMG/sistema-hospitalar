import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { FeegowStockReceiptForm } from '../components/feegow/inventory/FeegowStockReceiptForm';

export function FeegowStockReceiptPage() {
  return (
    <FeegowInventoryScreenLayout>
      <FeegowStockReceiptForm />
    </FeegowInventoryScreenLayout>
  );
}
