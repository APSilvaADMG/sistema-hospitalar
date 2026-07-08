import { useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { FEEGOW_TOP_MENU, isFeegowTopItemActive } from '../../navigation/feegowMenu';
import { useOpenModuleSearch } from '../ModuleSearchProvider';
import { HospitalLogo } from '../HospitalLogo';
import { NotificationHubBar } from './NotificationHubBar';
import { api, type PatientDto } from '../../api/client';

type FeegowTopBarProps = {
  onMenuToggle?: () => void;
  minimal?: boolean;
};

export function FeegowTopBar({ onMenuToggle, minimal }: FeegowTopBarProps) {
  const { pathname: locationPath } = useLocation();
  const { user, logout } = useAuth();
  const openSearch = useOpenModuleSearch();
  const [openMenu, setOpenMenu] = useState<string | null>(null);
  const [patientSearch, setPatientSearch] = useState('');
  const [patientResults, setPatientResults] = useState<PatientDto[]>([]);
  const [searchOpen, setSearchOpen] = useState(false);
  const navRef = useRef<HTMLDivElement>(null);

  const closeTopMenus = () => {
    setOpenMenu(null);
    setSearchOpen(false);
  };

  useEffect(() => {
    closeTopMenus();
  }, [locationPath]);

  useEffect(() => {
    if (!openMenu) return;
    const onDocClick = (e: MouseEvent) => {
      if (!navRef.current?.contains(e.target as Node)) setOpenMenu(null);
    };
    document.addEventListener('click', onDocClick);
    return () => document.removeEventListener('click', onDocClick);
  }, [openMenu]);

  useEffect(() => {
    const term = patientSearch.trim();
    if (term.length < 2) {
      setPatientResults([]);
      return;
    }

    const timeout = window.setTimeout(() => {
      api.quickSearchPatients(term, 8)
        .then((items) => setPatientResults(items))
        .catch(() => setPatientResults([]));
    }, 220);

    return () => window.clearTimeout(timeout);
  }, [patientSearch]);

  return (
    <header className="feegow-topbar">
      <div className="feegow-topbar-inner" ref={navRef}>
        <div className="feegow-topbar-left">
          <button type="button" className="feegow-menu-toggle mobile-only" onClick={onMenuToggle} aria-label="Menu">
            <span />
            <span />
            <span />
          </button>
          <Link to="/" className="feegow-brand-link" aria-label="IASGH — início" onClick={closeTopMenus}>
            <HospitalLogo variant="full" height={38} className="feegow-brand-logo" />
          </Link>
        </div>

        {!minimal ? (
          <nav className="feegow-topnav" aria-label="Menu principal">
            {FEEGOW_TOP_MENU.map((item) => {
              const active = isFeegowTopItemActive(locationPath, item);
              const hasChildren = Boolean(item.children?.length);
              return (
                <div
                  key={item.id}
                  className={`feegow-topnav-item${active ? ' active' : ''}${openMenu === item.id ? ' open' : ''}`}
                >
                  {hasChildren ? (
                    <button
                      type="button"
                      className="feegow-topnav-link"
                      aria-expanded={openMenu === item.id}
                      onClick={(e) => {
                        e.stopPropagation();
                        setOpenMenu((prev) => (prev === item.id ? null : item.id));
                      }}
                    >
                      {item.label}
                      <span className="feegow-caret" aria-hidden />
                    </button>
                  ) : (
                    <Link to={item.path ?? '/'} className="feegow-topnav-link" onClick={closeTopMenus}>
                      {item.label}
                    </Link>
                  )}
                  {hasChildren && openMenu === item.id ? (
                    <div className="feegow-topnav-dropdown" onClick={closeTopMenus}>
                      {item.children!.map((child) => (
                        <Link
                          key={child.path + child.label}
                          to={child.path}
                          className="feegow-topnav-dropdown-item"
                        >
                          <span className="feegow-dropdown-icon">+</span>
                          {child.label}
                          {child.badge ? <span className="feegow-badge-novo">{child.badge}</span> : null}
                        </Link>
                      ))}
                    </div>
                  ) : null}
                </div>
              );
            })}
            <Link to="/ajuda" className="feegow-topnav-icon" title="Central de Ajuda" aria-label="Ajuda" onClick={closeTopMenus}>
              ?
            </Link>
            <Link to="/configuracoes" className="feegow-topnav-icon" title="Configurações" aria-label="Configurações" onClick={closeTopMenus}>
              ⚙
            </Link>
          </nav>
        ) : (
          <div className="feegow-topnav feegow-topnav-minimal">
            <Link to="/portal-paciente" className="feegow-topnav-link active">Meu Portal</Link>
          </div>
        )}

        <div className="feegow-topbar-right">
          {!minimal ? (
            <div className="feegow-patient-quick-search">
              <input
                type="search"
                placeholder="Paciente: nome ou ID"
                value={patientSearch}
                onFocus={() => setSearchOpen(true)}
                onChange={(e) => setPatientSearch(e.target.value)}
              />
              {searchOpen && patientResults.length > 0 ? (
                <div className="feegow-patient-search-dropdown">
                  {patientResults.map((p) => (
                    <Link
                      key={p.id}
                      to={`/recepcao/pacientes/listar?paciente=${encodeURIComponent(p.id)}`}
                      onClick={() => {
                        setSearchOpen(false);
                        setPatientSearch('');
                      }}
                    >
                      <strong>{p.fullName}</strong>
                      <span>{p.id.slice(0, 8)}</span>
                    </Link>
                  ))}
                </div>
              ) : null}
            </div>
          ) : null}
          {!minimal ? <NotificationHubBar /> : null}
          <div className="feegow-user-menu">
            <button type="button" className="feegow-user-avatar" aria-label="Conta">
              {user?.fullName?.charAt(0)?.toUpperCase() ?? 'U'}
            </button>
            <div className="feegow-user-dropdown">
              <p className="feegow-user-name">{user?.fullName}</p>
              <button type="button" className="feegow-user-logout" onClick={logout}>Sair</button>
            </div>
          </div>
        </div>
      </div>
      <button type="button" className="feegow-quick-search-mobile mobile-only" onClick={openSearch}>
        Busca rápida…
      </button>
    </header>
  );
}
