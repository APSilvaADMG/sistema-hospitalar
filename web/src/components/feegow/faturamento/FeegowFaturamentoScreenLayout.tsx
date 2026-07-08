import type { ReactNode } from 'react';

type Props = {
  children: ReactNode;
  error?: string;
  success?: string;
};

/** Layout Feegow para telas de faturamento — painel lateral via resolveFeegowSidePanel. */
export function FeegowFaturamentoScreenLayout({ children, error, success }: Props) {
  return (
    <div className="feegow-faturamento-screen">
      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
      {children}
    </div>
  );
}
