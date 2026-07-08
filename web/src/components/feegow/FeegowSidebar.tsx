import { useEffect, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { resolveFeegowSidePanel } from '../../navigation/feegowMenu';
import { useOpenModuleSearch } from '../ModuleSearchProvider';

type FeegowSidebarProps = {
  mobileOpen?: boolean;
  onClose?: () => void;
};

function isPathActive(pathname: string, to: string) {
  if (to === '/') return pathname === '/';
  return pathname === to || pathname.startsWith(`${to}/`);
}

export function FeegowSidebar({ mobileOpen, onClose }: FeegowSidebarProps) {
  const { pathname } = useLocation();
  const openSearch = useOpenModuleSearch();
  const panel = resolveFeegowSidePanel(pathname);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  useEffect(() => {
    setExpanded({});
  }, [pathname]);

  const toggle = (id: string) => setExpanded((prev) => ({ ...prev, [id]: !prev[id] }));

  return (
    <aside className={`feegow-sidebar${mobileOpen ? ' open' : ''}`} aria-label="Painel lateral">
      <div className="feegow-sidebar-inner">
        <div className="feegow-quick-search">
          <span className="feegow-search-icon" aria-hidden>🔍</span>
          <button type="button" className="feegow-search-input" onClick={openSearch}>
            Busca rápida…
          </button>
        </div>

        {panel.promoButtons ? (
          <div className="feegow-promo-buttons">
            <NavLink to="/" className="feegow-btn-novidades" onClick={onClose}>PAINEL PRINCIPAL</NavLink>
            <NavLink to="/relatorios" className="feegow-btn-blog" onClick={onClose}>CENTRAL DE RELATÓRIOS</NavLink>
          </div>
        ) : null}

        {panel.locationBlock ? (
          <div className="feegow-location-block">
            <p className="feegow-location-label">LOCAL DE ATENDIMENTO</p>
            <p className="feegow-location-value">VOCÊ ESTÁ EM CONSULTÓRIO 01</p>
            <button type="button" className="feegow-location-change">Alterar local ✎</button>
          </div>
        ) : null}

        {panel.sectionLabel ? (
          <p className="feegow-side-section-label">{panel.sectionLabel}</p>
        ) : null}

        <nav className="feegow-side-nav">
          {panel.items.map((item) => {
            const hasChildren = Boolean(item.children?.length);
            const active = item.path ? isPathActive(pathname, item.path) : false;
            const isOpen = expanded[item.id] ?? active;

            if (hasChildren) {
              return (
                <div key={item.id} className={`feegow-side-group${isOpen ? ' open' : ''}`}>
                  <button type="button" className="feegow-side-group-toggle" onClick={() => toggle(item.id)}>
                    <span>{item.label}</span>
                    {item.badge ? <span className="feegow-badge-novo">{item.badge}</span> : null}
                    <span className="feegow-side-chevron" aria-hidden />
                  </button>
                  {isOpen ? (
                    <div className="feegow-side-sub">
                      {item.children!.map((child) => (
                        <NavLink
                          key={child.path + child.label}
                          to={child.path}
                          className={({ isActive }) => `feegow-side-sublink${isActive ? ' active' : ''}`}
                          onClick={onClose}
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
                key={item.id}
                to={item.path ?? '/'}
                className={({ isActive }) => `feegow-side-link${isActive || active ? ' active' : ''}`}
                onClick={onClose}
              >
                <span>{item.label}</span>
                {item.badge ? <span className="feegow-badge-novo">{item.badge}</span> : null}
              </NavLink>
            );
          })}
        </nav>
      </div>
    </aside>
  );
}
