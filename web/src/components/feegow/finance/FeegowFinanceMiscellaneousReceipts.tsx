import { MiscellaneousReceiptsPanel } from '../../finance/MiscellaneousReceiptsPanel';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';

export function FeegowFinanceMiscellaneousReceipts() {
  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead title="Recibos Diversos" icon="🧾" />
      <MiscellaneousReceiptsPanel variant="feegow" />
    </div>
  );
}
