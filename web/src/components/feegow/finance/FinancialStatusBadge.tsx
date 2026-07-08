import {
  financialStatusLabel,
  financialStatusValue,
  type FinancialAccountDto,
} from '../../../api/client';

function financialStatusCssClass(status: FinancialAccountDto['status']): string {
  switch (financialStatusValue(status)) {
    case 1: return 'feegow-finance-status-open';
    case 2: return 'feegow-finance-status-partial';
    case 3: return 'feegow-finance-status-paid';
    case 4: return 'feegow-finance-status-cancelled';
    default: return '';
  }
}

type Props = {
  status: FinancialAccountDto['status'];
};

export function FinancialStatusBadge({ status }: Props) {
  return (
    <span className={`feegow-finance-status ${financialStatusCssClass(status)}`}>
      {financialStatusLabel(status)}
    </span>
  );
}
