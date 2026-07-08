import type { UserRoleName } from '../../../api/client';
import {
  BAYANNO_SCREEN_BY_ROUTE,
  BAYANNO_SGHC_NAV,
  type BayannoScreen,
} from '../../../data/bayanno';

export const FEEGOW_SGHC_BASE = '/sghc';
export const FEEGOW_SGHC_SIDEBAR_HOST_ID = 'feegow-sghc-sidebar-host';

export type SghcPath = {
  role: string;
  route: string;
  subAction?: string;
};

export function parseSghcPath(pathname: string): SghcPath | null {
  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';
  if (path === FEEGOW_SGHC_BASE) return null;
  if (!path.startsWith(`${FEEGOW_SGHC_BASE}/`)) return null;

  const rest = path.slice(`${FEEGOW_SGHC_BASE}/`.length);
  const segments = rest.split('/').filter(Boolean);
  if (segments.length === 0) return null;

  const [role, ...actionParts] = segments;
  const route = `${role}/${actionParts.join('/')}`;
  const subAction = actionParts.length > 1 ? actionParts.slice(1).join('/') : undefined;

  return { role, route, subAction };
}

export function resolveSghcScreen(pathname: string): BayannoScreen | undefined {
  const parsed = parseSghcPath(pathname);
  if (!parsed) return undefined;

  const exact = BAYANNO_SCREEN_BY_ROUTE[parsed.route];
  if (exact) return exact;

  const parts = parsed.route.split('/');
  if (parts.length > 2) {
    const parentRoute = parts.slice(0, 2).join('/');
    return BAYANNO_SCREEN_BY_ROUTE[parentRoute];
  }

  return undefined;
}

export function sghcPathForRoute(route: string): string {
  return `${FEEGOW_SGHC_BASE}/${route}`;
}

export function defaultSghcRoleForUser(role: UserRoleName): string {
  switch (role) {
    case 'Billing':
      return 'accountant';
    case 'Admin':
    case 'HospitalDirector':
    case 'IT':
      return 'admin';
    case 'Doctor':
      return 'doctor';
    case 'Nurse':
    case 'NursingTechnician':
      return 'nurse';
    case 'Patient':
      return 'patient';
    case 'Pharmacy':
      return 'pharmacist';
    default:
      return 'admin';
  }
}

export function defaultSghcDashboardPath(role: UserRoleName): string {
  return sghcPathForRoute(`${defaultSghcRoleForUser(role)}/dashboard`);
}

export function sghcRoleNav(role: string) {
  return BAYANNO_SGHC_NAV.find((entry) => entry.role === role);
}

export function bayannoRoleToUserRole(bayannoRole: string): UserRoleName {
  switch (bayannoRole) {
    case 'accountant':
      return 'Billing';
    case 'admin':
      return 'Admin';
    case 'doctor':
      return 'Doctor';
    case 'nurse':
      return 'Nurse';
    case 'patient':
      return 'Patient';
    case 'pharmacist':
      return 'Pharmacy';
    case 'laboratorist':
      return 'Reception';
    default:
      return 'Admin';
  }
}
