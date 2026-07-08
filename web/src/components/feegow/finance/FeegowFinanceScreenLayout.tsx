import type { ReactNode } from 'react';

type Props = {
  children: ReactNode;
  error?: string;
  success?: string;
};

export function FeegowFinanceScreenLayout({ children, error, success }: Props) {
  return (
    <div className="feegow-finance-screen">
      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
      {children}
    </div>
  );
}
