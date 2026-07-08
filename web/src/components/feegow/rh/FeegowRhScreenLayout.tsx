import type { ReactNode } from 'react';

type Props = {
  children: ReactNode;
  error?: string;
  success?: string;
};

/** Layout Feegow para telas de RH — painel lateral via <code>resolveFeegowSidePanel</code>. */
export function FeegowRhScreenLayout({ children, error, success }: Props) {
  return (
    <div className="feegow-rh-screen">
      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
      {children}
    </div>
  );
}
