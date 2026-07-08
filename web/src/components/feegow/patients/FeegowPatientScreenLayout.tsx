import { type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { FEEGOW_PATIENT_SIDEBAR_HOST_ID } from './FeegowPatientSidebar';

export { FEEGOW_PATIENT_SIDEBAR_HOST_ID };

type Props = {
  children: ReactNode;
  error?: string;
  success?: string;
  sidebar?: ReactNode;
};

export function FeegowPatientScreenLayout({ children, error, success, sidebar }: Props) {
  const sidebarContent = sidebar ?? null;
  const sidebarHost = typeof document !== 'undefined'
    ? document.getElementById(FEEGOW_PATIENT_SIDEBAR_HOST_ID)
    : null;

  return (
    <div className="feegow-patient-screen">
      {sidebarHost ? createPortal(sidebarContent, sidebarHost) : sidebarContent}
      <div className="feegow-patient-main">
        {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
        {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
        {children}
      </div>
    </div>
  );
}
