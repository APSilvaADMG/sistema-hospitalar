import { NavLink } from 'react-router-dom';
import { loadHospitalParams } from '../config/clinicOnDoctorProfile';
import { filterPathsByModules } from '../config/moduleVisibility';
import { SIDEBAR_ICON_RAIL_ITEMS } from '../navigation/sidebarIconRail';
import { useOpenModuleSearch } from './ModuleSearchProvider';
import { NavIcon } from './NavIcon';

type SidebarIconRailProps = {
  onExpand: () => void;
};

export function SidebarIconRail({ onExpand }: SidebarIconRailProps) {
  const openSearch = useOpenModuleSearch();
  const railItems = filterPathsByModules(SIDEBAR_ICON_RAIL_ITEMS, loadHospitalParams().modules);

  return (
    <div className="sidebar-icon-rail">
      <button
        type="button"
        className="sidebar-rail-btn"
        onClick={openSearch}
        title="Buscar módulos (Ctrl+K)"
        aria-label="Buscar módulos"
      >
        <NavIcon name="search" />
      </button>

      <div className="sidebar-rail-items" role="navigation" aria-label="Atalhos principais">
        {railItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            end={item.end ?? item.path === '/'}
            className={({ isActive }) => `sidebar-rail-link${isActive ? ' active' : ''}`}
            title={item.label}
            aria-label={item.label}
          >
            <NavIcon name={item.icon} />
          </NavLink>
        ))}
      </div>

      <button
        type="button"
        className="sidebar-rail-btn sidebar-rail-expand"
        onClick={onExpand}
        title="Expandir menu"
        aria-label="Expandir menu lateral"
      >
        <NavIcon name="menu" />
      </button>
    </div>
  );
}
