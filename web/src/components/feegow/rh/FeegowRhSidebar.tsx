import { NavLink, useLocation } from 'react-router-dom';
import { useOpenModuleSearch } from '../../ModuleSearchProvider';
import { FEEGOW_RH_NAV_ITEMS, resolveFeegowRhSection } from './feegowRhNav';

export function FeegowRhSidebar() {
  const { pathname } = useLocation();
  const openSearch = useOpenModuleSearch();
  const activeSection = resolveFeegowRhSection(pathname);

  return (
    <div className="feegow-inventory-sidebar feegow-rh-sidebar">
      <div className="feegow-quick-search feegow-inventory-search">
        <span className="feegow-search-icon" aria-hidden>🔍</span>
        <button type="button" className="feegow-search-input" onClick={openSearch}>
          Busca rápida…
        </button>
      </div>

      <p className="feegow-inventory-section-label">RECURSOS HUMANOS</p>
      <nav className="feegow-inventory-nav" aria-label="Módulos de RH">
        {FEEGOW_RH_NAV_ITEMS.map((item) => (
          <NavLink
            key={item.id}
            to={item.path}
            end={item.path === '/rh'}
            className={() =>
              `feegow-inventory-nav-item${activeSection === item.id ? ' is-active' : ''}`
            }
          >
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <p className="feegow-inventory-section-label">ATALHOS</p>
      <nav className="feegow-inventory-nav" aria-label="Atalhos de RH">
        <NavLink to="/profissionais" className="feegow-inventory-nav-item">
          <span>Profissionais de saúde</span>
        </NavLink>
      </nav>
    </div>
  );
}
