import { useEffect, useMemo, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { BAYANNO_ROLE_LABELS, BAYANNO_SGHC_NAV, type BayannoNavItem } from '../../../data/bayanno';
import { useOpenModuleSearch } from '../../ModuleSearchProvider';
import { FEEGOW_SGHC_BASE } from './feegowSghcNav';

type Props = {
  activeRole: string;
  activeRoute?: string;
};

function isTopLevelLink(item: BayannoNavItem): boolean {
  return item.type === 'link' && item.icon.includes('icon-2x');
}

/** Converte lista plana do extract em grupos (submenu + filhos). */
function foldNavItems(items: BayannoNavItem[]) {
  const folded: BayannoNavItem[] = [];
  let pendingSubmenu: Extract<BayannoNavItem, { type: 'submenu' }> | null = null;

  for (const item of items) {
    if (item.type === 'submenu') {
      if (pendingSubmenu) folded.push(pendingSubmenu);
      pendingSubmenu = { ...item, children: [] };
      continue;
    }

    if (pendingSubmenu && !isTopLevelLink(item)) {
      pendingSubmenu.children.push(item);
      continue;
    }

    if (pendingSubmenu) {
      folded.push(pendingSubmenu);
      pendingSubmenu = null;
    }

    folded.push(item);
  }

  if (pendingSubmenu) folded.push(pendingSubmenu);
  return folded;
}

export function FeegowSghcSidebar({ activeRole, activeRoute }: Props) {
  const { pathname } = useLocation();
  const openSearch = useOpenModuleSearch();
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  useEffect(() => {
    setExpanded({});
  }, [pathname]);

  const roleNav = useMemo(
    () => BAYANNO_SGHC_NAV.find((entry) => entry.role === activeRole),
    [activeRole],
  );

  const items = useMemo(
    () => foldNavItems(roleNav?.items ?? []),
    [roleNav],
  );

  function toggleSubmenu(id: string) {
    setExpanded((prev) => ({ ...prev, [id]: !prev[id] }));
  }

  return (
    <div className="feegow-agenda-sidebar feegow-sghc-sidebar">
      <div className="feegow-sghc-sidebar-head">
        <span className="feegow-sghc-sidebar-kicker">SGHC Bayanno</span>
        <strong>{BAYANNO_ROLE_LABELS[activeRole] ?? activeRole}</strong>
      </div>

      <div className="feegow-quick-search feegow-patient-search">
        <span className="feegow-search-icon" aria-hidden>🔍</span>
        <button type="button" className="feegow-search-input" onClick={openSearch}>
          Busca rápida…
        </button>
      </div>

      <div className="feegow-sghc-role-switch" role="tablist" aria-label="Perfis SGHC">
        {BAYANNO_SGHC_NAV.map((entry) => (
          <NavLink
            key={entry.role}
            to={`${FEEGOW_SGHC_BASE}/${entry.role}/dashboard`}
            className={({ isActive }) => `feegow-sghc-role-pill${isActive || entry.role === activeRole ? ' is-active' : ''}`}
            title={entry.roleLabel}
          >
            {entry.roleLabel.slice(0, 3)}
          </NavLink>
        ))}
      </div>

      <nav className="feegow-sghc-nav" aria-label="Menu SGHC">
        {items.map((item) => {
          if (item.type === 'submenu') {
            const open = expanded[item.submenuId] ?? true;
            return (
              <div key={item.submenuId} className="feegow-sghc-nav-group">
                <button
                  type="button"
                  className="feegow-sghc-nav-group-title"
                  onClick={() => toggleSubmenu(item.submenuId)}
                  aria-expanded={open}
                >
                  <span>{item.label}</span>
                  <span aria-hidden>{open ? '▾' : '▸'}</span>
                </button>
                {open ? (
                  <div className="feegow-sghc-nav-group-items">
                    {item.children.map((child) => (
                      <NavLink
                        key={child.route}
                        to={child.path}
                        className={({ isActive }) => `feegow-sghc-nav-item feegow-sghc-nav-item-nested${isActive || activeRoute === child.route ? ' is-active' : ''}`}
                      >
                        {child.label}
                      </NavLink>
                    ))}
                  </div>
                ) : null}
              </div>
            );
          }

          return (
            <NavLink
              key={item.route}
              to={item.path}
              className={({ isActive }) => `feegow-sghc-nav-item${isActive || activeRoute === item.route ? ' is-active' : ''}`}
            >
              {item.label}
            </NavLink>
          );
        })}
      </nav>
    </div>
  );
}
