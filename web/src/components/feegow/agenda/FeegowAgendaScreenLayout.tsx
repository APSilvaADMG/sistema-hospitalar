import { type ReactNode, useLayoutEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { FEEGOW_AGENDA_SIDEBAR_HOST_ID } from './FeegowAgendaSidebar';

type Props = {
  sidebar: ReactNode;
  children: ReactNode;
  error?: string;
  success?: string;
};

export function FeegowAgendaScreenLayout({ sidebar, children, error, success }: Props) {
  const [sidebarHost, setSidebarHost] = useState<HTMLElement | null>(null);
  const [ready, setReady] = useState(false);

  useLayoutEffect(() => {
    setSidebarHost(document.getElementById(FEEGOW_AGENDA_SIDEBAR_HOST_ID));
    setReady(true);
  }, []);

  const isPortaled = ready && sidebarHost !== null;

  return (
    <div className={`feegow-daily-agenda${isPortaled ? ' feegow-daily-agenda--portaled' : ''}`}>
      {isPortaled ? createPortal(sidebar, sidebarHost) : null}
      {ready && !sidebarHost ? sidebar : null}
      <div className="feegow-agenda-main">
        {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
        {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
        {children}
      </div>
    </div>
  );
}
