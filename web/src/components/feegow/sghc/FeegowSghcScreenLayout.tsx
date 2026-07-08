import { type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { FeegowSghcSidebar } from './FeegowSghcSidebar';

export const FEEGOW_SGHC_SIDEBAR_HOST_ID = 'feegow-sghc-sidebar-host';

type Props = {
  children: ReactNode;
  activeRole: string;
  activeRoute?: string;
  error?: string;
  success?: string;
};

export function FeegowSghcScreenLayout({
  children,
  activeRole,
  activeRoute,
  error,
  success,
}: Props) {
  const sidebarHost = typeof document !== 'undefined'
    ? document.getElementById(FEEGOW_SGHC_SIDEBAR_HOST_ID)
    : null;

  const sidebar = (
    <FeegowSghcSidebar activeRole={activeRole} activeRoute={activeRoute} />
  );

  return (
    <div className="feegow-sghc-screen">
      {sidebarHost ? createPortal(sidebar, sidebarHost) : sidebar}
      <div className="feegow-sghc-main">
        {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
        {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
        {children}
      </div>
    </div>
  );
}
