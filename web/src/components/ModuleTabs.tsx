import { NavLink, useLocation } from 'react-router-dom';
import type { ModuleTab } from '../navigation/useModuleSection';

type Props = {
  basePath: string;
  tabs: ModuleTab[];
};

function isTabActive(pathname: string, to: string, end: boolean) {
  if (end || to === '/') return pathname === to;
  return pathname === to || pathname.startsWith(`${to}/`);
}

/**
 * Abas no padrão Bayanno: nav nav-tabs nav-tabs-left dentro do box-header.
 */
export function ModuleTabs({ basePath, tabs }: Props) {
  const base = basePath.replace(/\/$/, '');
  const { pathname } = useLocation();

  return (
    <div className="box-header bayanno-module-tabs-header">
      <ul className="nav nav-tabs nav-tabs-left" aria-label="Seções do módulo">
        {tabs.map((tab) => {
          const to = tab.to ?? (tab.slug ? `${base}/${tab.slug}` : base);
          const end = tab.to === '/' ? true : tab.to ? false : !tab.slug;
          const active = isTabActive(pathname, to, end);
          return (
            <li key={(tab.to ?? tab.slug) || '__root'} className={active ? 'active' : undefined}>
              <NavLink to={to} end={end}>
                <i className="icon-align-justify" aria-hidden />
                {' '}
                {tab.label}
              </NavLink>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
