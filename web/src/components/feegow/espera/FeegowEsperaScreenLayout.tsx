import { type ReactNode, useLayoutEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { FeegowEsperaSidebar, FEEGOW_ESPERA_SIDEBAR_HOST_ID, type EsperaSidebarCounts } from './FeegowEsperaSidebar';

type Props = {
  children: ReactNode;
  error?: string;
  success?: string;
  sidebarCounts?: EsperaSidebarCounts;
};

export function FeegowEsperaScreenLayout({ children, error, success, sidebarCounts }: Props) {
  const [sidebarHost, setSidebarHost] = useState<HTMLElement | null>(null);
  const [ready, setReady] = useState(false);

  useLayoutEffect(() => {
    setSidebarHost(document.getElementById(FEEGOW_ESPERA_SIDEBAR_HOST_ID));
    setReady(true);
  }, []);

  const sidebarContent = <FeegowEsperaSidebar counts={sidebarCounts} />;
  const isPortaled = ready && sidebarHost !== null;

  return (
    <div className={`feegow-espera-screen${isPortaled ? ' feegow-espera-screen--portaled' : ''}`}>
      {isPortaled ? createPortal(sidebarContent, sidebarHost) : null}
      {ready && !sidebarHost ? sidebarContent : null}
      <div className="feegow-espera-main">
        {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
        {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
        {children}
      </div>
    </div>
  );
}
