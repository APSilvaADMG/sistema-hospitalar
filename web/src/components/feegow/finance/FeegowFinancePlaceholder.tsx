import { FeegowFinancePageHead } from './FeegowFinancePageHead';

type Props = {
  title: string;
  description?: string;
};

export function FeegowFinancePlaceholder({ title, description }: Props) {
  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead title={title} />
      <section className="feegow-finance-panel feegow-finance-placeholder">
        <h3>{title}</h3>
        <p>{description ?? 'Módulo em desenvolvimento. Utilize as telas de contas a pagar e a receber para lançamentos financeiros.'}</p>
      </section>
    </div>
  );
}
