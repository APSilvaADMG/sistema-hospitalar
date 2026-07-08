import { type ReactNode, useLayoutEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { FeegowInventorySidebar, FEEGOW_INVENTORY_SIDEBAR_HOST_ID } from './FeegowInventorySidebar';

export { FEEGOW_INVENTORY_SIDEBAR_HOST_ID };

type Props = {
  children: ReactNode;
  error?: string;
  success?: string;
  /** Conteúdo da sidebar lateral; padrão = tipos de itens + configurações. */
  sidebar?: ReactNode;
};

export function FeegowInventoryScreenLayout({ children, error, success, sidebar }: Props) {
  const [sidebarHost, setSidebarHost] = useState<HTMLElement | null>(null);
  const [ready, setReady] = useState(false);

  useLayoutEffect(() => {
    setSidebarHost(document.getElementById(FEEGOW_INVENTORY_SIDEBAR_HOST_ID));
    setReady(true);
  }, []);

  const sidebarContent = sidebar ?? <FeegowInventorySidebar />;
  const isPortaled = ready && sidebarHost !== null;

  return (
    <div className={`feegow-inventory-screen${isPortaled ? ' feegow-inventory-screen--portaled' : ''}`}>
      {isPortaled ? createPortal(sidebarContent, sidebarHost) : null}
      {ready && !sidebarHost ? sidebarContent : null}
      <div className="feegow-inventory-main">
        {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
        {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
        {children}
      </div>
    </div>
  );
}
