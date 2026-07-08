import { NavLink } from 'react-router-dom';
import type { UserRoleName } from '../api/client';
import { resolveMenuProfile } from '../navigation/menuProfile';
import { NavIcon, type NavIconName } from './NavIcon';

type NavItem = {
  to: string;
  label: string;
  icon: NavIconName;
  end?: boolean;
  badge?: number;
  action?: 'menu';
};

type MobileBottomNavProps = {
  items: NavItem[];
  onOpenMenu: () => void;
};

export function MobileBottomNav({ items, onOpenMenu }: MobileBottomNavProps) {
  return (
    <nav className="mobile-bottom-nav" aria-label="Navegação principal">
      {items.map((item) => {
        if (item.action === 'menu') {
          return (
            <button
              key="menu"
              type="button"
              className="mobile-nav-item"
              onClick={onOpenMenu}
              aria-label="Abrir menu completo"
            >
              <NavIcon name={item.icon} />
              <span>{item.label}</span>
            </button>
          );
        }

        return (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) => `mobile-nav-item${isActive ? ' active' : ''}`}
          >
            <span className="mobile-nav-icon-wrap">
              <NavIcon name={item.icon} />
              {item.badge ? <span className="mobile-nav-badge">{item.badge > 9 ? '9+' : item.badge}</span> : null}
            </span>
            <span>{item.label}</span>
          </NavLink>
        );
      })}
    </nav>
  );
}

export function buildMobileNavItems(options: {
  isPatient: boolean;
  isAdmin: boolean;
  isAdminOrReception: boolean;
  unreadCount: number;
  role?: UserRoleName;
  hasPermission: (...permissions: string[]) => boolean;
}): NavItem[] {
  const { isPatient, isAdmin, isAdminOrReception, unreadCount, role, hasPermission } = options;

  if (isPatient) {
    return [
      { to: '/portal-paciente', label: 'Meu portal', icon: 'user-circle', end: true },
    ];
  }

  const profile = resolveMenuProfile({ role, isAdmin, isAdminOrReception, hasPermission });
  const profileLinks = profile.shortcuts
    .filter((s) => s.path !== '/')
    .slice(0, 3)
    .map((s) => ({
      to: s.path,
      label: s.label,
      icon: s.icon,
      end: s.end,
    }));

  const items: NavItem[] = [
    { to: '/', label: 'Início', icon: 'dashboard', end: true },
    ...profileLinks,
  ];

  const fallbacks: NavItem[] = [
    { to: '/pacientes', label: 'Pacientes', icon: 'users' },
    { to: '/agendamentos', label: 'Agenda', icon: 'calendar' },
    { to: '/emergencia', label: 'PS', icon: 'siren' },
  ];
  for (const fb of fallbacks) {
    if (items.length >= 4) break;
    if (!items.some((i) => i.to === fb.to)) items.push(fb);
  }

  items.splice(4);

  items.push(
    { to: '/notificacoes', label: 'Alertas', icon: 'bell', badge: unreadCount || undefined },
    { to: '#', label: 'Menu', icon: 'menu', action: 'menu' },
  );

  return items;
}
