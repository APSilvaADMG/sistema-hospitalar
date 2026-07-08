import { useEffect, useState, type ReactNode } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import {
  isFeegowAgendaMapRoute,
  isFeegowAgendaRoute,
  isFeegowAgendaSidebarRoute,
  isFeegowDashboardRoute,
  isFeegowEsperaRoute,
  isFeegowEsperaSidebarRoute,
  isFeegowInventoryRoute,
  isFeegowInventorySidebarRoute,
  isFeegowPatientRoute,
  isFeegowPatientSidebarRoute,
  isFeegowPlainContentRoute,
  isFeegowRequisitionRoute,
  isFeegowSghcRoute,
  isFeegowSghcSidebarRoute,
  isFeegowVaccinationRoute,
} from '../../utils/feegowRoutes';
import { FeegowFooter } from './FeegowFooter';
import { FeegowPageChrome } from './FeegowPageChrome';
import { FEEGOW_AGENDA_SIDEBAR_HOST_ID } from './agenda/FeegowAgendaSidebar';
import { FEEGOW_ESPERA_SIDEBAR_HOST_ID } from './espera/FeegowEsperaSidebar';
import { FEEGOW_INVENTORY_SIDEBAR_HOST_ID } from './inventory/FeegowInventorySidebar';
import { FEEGOW_SGHC_SIDEBAR_HOST_ID } from './sghc/FeegowSghcScreenLayout';
import { FEEGOW_PATIENT_SIDEBAR_HOST_ID } from './patients/FeegowPatientSidebar';
import { FeegowSidebar } from './FeegowSidebar';
import { FeegowTopBar } from './FeegowTopBar';
import { BayannoTableEnhancer } from '../bayanno/BayannoTableEnhancer';
import { HelpAssistantFab } from '../help/HelpAssistantFab';

type FeegowAppShellProps = {
  children?: ReactNode;
};

export function FeegowAppShell({ children }: FeegowAppShellProps) {
  const location = useLocation();
  const { hasRole } = useAuth();
  const isPatient = hasRole('Patient');
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const isDashboard = isFeegowDashboardRoute(location.pathname);
  const isPlain = isFeegowPlainContentRoute(location.pathname);
  const isAgenda = isFeegowAgendaRoute(location.pathname);
  const isAgendaMap = isFeegowAgendaMapRoute(location.pathname);
  const isEspera = isFeegowEsperaRoute(location.pathname);
  const isPatientScreen = isFeegowPatientRoute(location.pathname);
  const isInventoryScreen = isFeegowInventoryRoute(location.pathname);
  const isRequisitionScreen = isFeegowRequisitionRoute(location.pathname);
  const isVaccinationScreen = isFeegowVaccinationRoute(location.pathname);
  const isSghcScreen = isFeegowSghcRoute(location.pathname);
  const showInventorySidebar = isFeegowInventorySidebarRoute(location.pathname);
  const showSghcSidebar = isFeegowSghcSidebarRoute(location.pathname);
  const showAgendaSidebar = isFeegowAgendaSidebarRoute(location.pathname);
  const showEsperaSidebar = isFeegowEsperaSidebarRoute(location.pathname);
  const showPatientSidebar = isFeegowPatientSidebarRoute(location.pathname);

  useEffect(() => {
    setSidebarOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    document.body.classList.toggle('feegow-mobile-menu-open', sidebarOpen);
    document.body.classList.add('feegow-shell-active');
    return () => {
      document.body.classList.remove('feegow-mobile-menu-open');
      document.body.classList.remove('feegow-shell-active');
    };
  }, [sidebarOpen]);

  const pageContent = children ?? <Outlet />;

  return (
    <div className={`feegow-layout${sidebarOpen ? ' sidebar-open' : ''}${isPatient ? ' feegow-layout-patient' : ''}${showAgendaSidebar ? ' feegow-layout-agenda' : ''}${showEsperaSidebar ? ' feegow-layout-espera' : ''}${isPatientScreen ? ' feegow-layout-patient-insert' : ''}${showInventorySidebar ? ' feegow-layout-inventory' : ''}${showSghcSidebar ? ' feegow-layout-sghc' : ''}`}>
      <FeegowTopBar onMenuToggle={() => setSidebarOpen(true)} minimal={isPatient} />
      <div className="feegow-body">
        <button
          type="button"
          className="feegow-sidebar-backdrop"
          aria-label="Fechar menu"
          onClick={() => setSidebarOpen(false)}
        />
        {!isPatient && showAgendaSidebar ? (
          <aside
            id={FEEGOW_AGENDA_SIDEBAR_HOST_ID}
            className="feegow-sidebar feegow-agenda-sidebar-host"
            aria-label="Controles da agenda"
          />
        ) : null}
        {!isPatient && showEsperaSidebar ? (
          <aside
            id={FEEGOW_ESPERA_SIDEBAR_HOST_ID}
            className="feegow-sidebar feegow-espera-sidebar-host"
            aria-label="Sala de espera"
          />
        ) : null}
        {!isPatient && showPatientSidebar ? (
          <aside
            id={FEEGOW_PATIENT_SIDEBAR_HOST_ID}
            className="feegow-sidebar feegow-patient-sidebar-host"
            aria-label="Prontuário do paciente"
          />
        ) : null}
        {!isPatient && showInventorySidebar ? (
          <aside
            id={FEEGOW_INVENTORY_SIDEBAR_HOST_ID}
            className="feegow-sidebar feegow-inventory-sidebar-host"
            aria-label="Estoque"
          />
        ) : null}
        {!isPatient && showSghcSidebar ? (
          <aside
            id={FEEGOW_SGHC_SIDEBAR_HOST_ID}
            className="feegow-sidebar feegow-sghc-sidebar-host"
            aria-label="Módulos SGHC"
          />
        ) : null}
        {!isPatient && !isAgenda && !isAgendaMap && !isEspera && !isPatientScreen && !isRequisitionScreen && !showInventorySidebar && !showSghcSidebar ? (
          <FeegowSidebar mobileOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
        ) : null}
        <main className="feegow-main">
          <div className="feegow-main-scroll">
            <BayannoTableEnhancer />
            {isDashboard || isAgenda || isAgendaMap || isEspera || isPatientScreen || isInventoryScreen || isVaccinationScreen || isSghcScreen ? (
              <div className="feegow-page-bare">{pageContent}</div>
            ) : (
              <FeegowPageChrome variant={isPlain ? 'plain' : 'card'}>
                {pageContent}
              </FeegowPageChrome>
            )}
          </div>
          <FeegowFooter />
          {!isPatient ? <HelpAssistantFab /> : null}
        </main>
      </div>
    </div>
  );
}
