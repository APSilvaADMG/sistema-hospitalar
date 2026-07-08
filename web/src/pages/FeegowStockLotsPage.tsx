import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { FeegowStockLotsPanel } from '../components/feegow/inventory/FeegowStockLotsPanel';

export function FeegowStockLotsPage() {
  return (
    <FeegowInventoryScreenLayout>
      <FeegowStockLotsPanel />
    </FeegowInventoryScreenLayout>
  );
}
