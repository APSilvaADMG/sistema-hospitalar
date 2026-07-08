import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { FeegowStockIssueForm } from '../components/feegow/inventory/FeegowStockIssueForm';

export function FeegowStockIssuePage() {
  return (
    <FeegowInventoryScreenLayout>
      <FeegowStockIssueForm />
    </FeegowInventoryScreenLayout>
  );
}
