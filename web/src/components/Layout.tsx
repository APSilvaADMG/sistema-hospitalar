import { useEffect, useMemo, useState } from 'react';

import { NavLink, Outlet, useLocation } from 'react-router-dom';

import { api, roleLabels } from '../api/client';

import { useAuth } from '../auth/AuthContext';

import { HospitalLogo } from './HospitalLogo';

import { NavIcon } from './NavIcon';

import { buildMobileNavItems, MobileBottomNav } from './MobileBottomNav';

import { SidebarNav } from './SidebarNav';

import { PrintPreviewHost } from './PrintPreviewModal';

import { AppFooter } from './AppFooter';
import { BayannoTopBar } from './BayannoTopBar';
import { TopBar } from './TopBar';
import { ModuleSearchProvider } from './ModuleSearchProvider';

import { initOperationsOfflineSync } from '../offline/operationsSyncEngine';

import { initOperationsRealtimeSync } from '../offline/operationsRealtimeSync';
import { initConnectRealtimeSync, subscribeConnectCommRefresh } from '../offline/connectRealtimeSync';
import { BayannoPageShell } from './bayanno/BayannoPageShell';
import { BayannoRouteChrome } from './bayanno/BayannoRouteChrome';
import { BayannoTableEnhancer } from './bayanno/BayannoTableEnhancer';
import { isBayannoBareRoute } from '../utils/bayannoRoutes';
import { useAppearance } from '../theme/AppearanceProvider';
import { isClinicShellBrand, isFeegowBrand, loadAppearance } from '../theme/appearanceConfig';
import { FeegowAppShell } from './feegow/FeegowAppShell';



function userInitials(name: string) {

  return name

    .split(' ')

    .filter(Boolean)

    .slice(0, 2)

    .map((part) => part[0]?.toUpperCase() ?? '')

    .join('');

}



export function Layout() {

  const { user, logout, hasRole, hasPermission } = useAuth();

  const location = useLocation();

  const isPatient = hasRole('Patient');

  const isStaff = !isPatient;

  const isAdminOrReception = hasRole('Admin', 'Reception')
    || hasPermission('patients.create', 'billing.write');

  const isAdmin = hasRole('Admin')
    || hasPermission('users.manage', 'security.manage');

  const hasSecurityLgpd = hasPermission(
    'audit.read',
    'security.manage',
    'lgpd.manage',
    'lgpd.consent.manage',
    'lgpd.subject_requests',
    'incidents.manage',
  );

  const [unreadCount, setUnreadCount] = useState(0);

  const [sidebarOpen, setSidebarOpen] = useState(false);
  const { appearance } = useAppearance();
  const isFeegow = isFeegowBrand(appearance.brand);
  const isClinicShell = isClinicShellBrand(appearance.brand) && !isFeegow;
  const iconSidebar = false;



  useEffect(() => {
    if (isPatient) return;

    const refreshUnread = () => {
      Promise.all([
        api.getUnreadNotificationCount().catch(() => ({ count: 0 })),
        hasPermission('connect.read')
          ? api.getConnectCommSummary().catch(() => null)
          : Promise.resolve(null),
      ]).then(([system, connect]) => {
        const connectTotal = connect
          ? connect.unreadMailCount +
            connect.unreadChatCount +
            connect.unreadNotificationCount +
            connect.unviewedBulletinCount
          : 0;
        setUnreadCount(system.count + connectTotal);
      });
    };

    refreshUnread();
    const timer = window.setInterval(refreshUnread, 45_000);
    const onFocus = () => refreshUnread();
    const stopComm = subscribeConnectCommRefresh(refreshUnread);
    window.addEventListener('focus', onFocus);

    return () => {
      window.clearInterval(timer);
      window.removeEventListener('focus', onFocus);
      stopComm();
    };
  }, [isPatient, hasPermission]);



  useEffect(() => {

    if (isPatient || !user) return;

    const stopOffline = initOperationsOfflineSync();

    const stopRealtime = initOperationsRealtimeSync();
    const stopConnect = initConnectRealtimeSync();

    return () => {

      stopOffline();

      stopRealtime();
      stopConnect();

    };

  }, [isPatient, user]);



  useEffect(() => {

    setSidebarOpen(false);

  }, [location.pathname]);



  useEffect(() => {

    document.body.classList.toggle('mobile-menu-open', sidebarOpen);

    return () => document.body.classList.remove('mobile-menu-open');

  }, [sidebarOpen]);



  const mobileNavItems = useMemo(

    () => buildMobileNavItems({
      isPatient,
      isAdmin,
      isAdminOrReception,
      unreadCount,
      role: user?.role,
      hasPermission,
    }),

    [isPatient, isAdmin, isAdminOrReception, unreadCount, user?.role, hasPermission],

  );



  const shell = (
    <>
      <button

        type="button"

        className="sidebar-backdrop"

        aria-label="Fechar menu"

        onClick={() => setSidebarOpen(false)}

      />

      {isClinicShell && (
        <div className="sidebar-background" aria-hidden>
          <div className="primary-sidebar-background" />
        </div>
      )}

      <aside className={`sidebar${iconSidebar ? ' sidebar-icons' : ''}${isClinicShell ? ' primary-sidebar' : ''}`} aria-label="Menu lateral">

        <div className="sidebar-header">

          <HospitalLogo
            variant={iconSidebar ? 'mark' : 'full'}
            height={iconSidebar ? 80 : 164}
            className="sidebar-logo"
          />


          <button

            type="button"

            className="sidebar-close-btn mobile-only"

            onClick={() => setSidebarOpen(false)}

            aria-label="Fechar menu"

          >

            ×

          </button>

        </div>



        <nav className="sidebar-nav">

          {isPatient ? (

            <NavLink to="/portal-paciente" className={({ isActive }) => `nav-sublink nav-sublink-standalone${isActive ? ' active' : ''}`}>

              Meu Portal

            </NavLink>

          ) : (

            <SidebarNav
              isStaff={isStaff}
              isAdminOrReception={isAdminOrReception}
              isAdmin={isAdmin}
              hasSecurityLgpd={hasSecurityLgpd}
              unreadCount={unreadCount}
            />

          )}

        </nav>



        {!iconSidebar && (
          <div className="sidebar-user">

            <div className="user-card">

              <div className="user-avatar">{user ? userInitials(user.fullName) : '?'}</div>

              <div className="user-info">

                <p className="user-name">{user?.fullName}</p>

                <p className="user-role">{user ? roleLabels[user.role] : ''}</p>

              </div>

            </div>

            <button type="button" className="sidebar-logout" onClick={logout}>

              <NavIcon name="log-out" />

              <span>Sair</span>

            </button>

          </div>
        )}

      </aside>



      <div className={`app-shell${isClinicShell ? ' main-content' : ''}`}>

        {isPatient ? (

          <header className="mobile-patient-bar">

            <HospitalLogo variant="mark" height={73} className="mobile-patient-logo" />

            <button type="button" className="btn btn-secondary btn-sm" onClick={logout}>

              Sair

            </button>

          </header>

        ) : isClinicShell ? (

          <BayannoTopBar onMenuToggle={() => setSidebarOpen(true)} />

        ) : (

          <TopBar onMenuToggle={() => setSidebarOpen(true)} />

        )}

        <main className="content">
          {isClinicShell && !isBayannoBareRoute(location.pathname, loadAppearance().brand) && (
            <BayannoRouteChrome />
          )}
          {isClinicShell && <BayannoTableEnhancer />}
          {isClinicShell ? (
            <BayannoPageShell bare={isBayannoBareRoute(location.pathname, loadAppearance().brand)}>
              <Outlet />
            </BayannoPageShell>
          ) : (
            <Outlet />
          )}
        </main>

        {!isPatient && <AppFooter />}

        {!isPatient && (
          <MobileBottomNav items={mobileNavItems} onOpenMenu={() => setSidebarOpen(true)} />
        )}

      </div>

      <PrintPreviewHost />
    </>
  );

  if (isFeegow) {
    return (
      <ModuleSearchProvider>
        <FeegowAppShell />
        <PrintPreviewHost />
      </ModuleSearchProvider>
    );
  }

  return (

    <div className={`layout${sidebarOpen ? ' sidebar-open' : ''}${isPatient ? ' layout-patient' : ''}`}>

      {isPatient ? shell : <ModuleSearchProvider>{shell}</ModuleSearchProvider>}

    </div>

  );

}


