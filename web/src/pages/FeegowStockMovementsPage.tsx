import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { FeegowStockMovementsPanel } from '../components/feegow/inventory/FeegowStockMovementsPanel';

export function FeegowStockMovementsPage() {
  return (
    <FeegowInventoryScreenLayout>
      <FeegowStockMovementsPanel />
    </FeegowInventoryScreenLayout>
  );
}
