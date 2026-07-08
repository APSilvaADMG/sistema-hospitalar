import { Link } from 'react-router-dom';
import { roleLabels } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { loadHospitalParams } from '../config/clinicOnDoctorProfile';

type BayannoTopBarProps = {
  onMenuToggle?: () => void;
};

/**
 * Cópia de application/views/header.php
 */
export function BayannoTopBar({ onMenuToggle }: BayannoTopBarProps) {
  const { user, logout } = useAuth();
  if (!user) return null;

  const systemName = loadHospitalParams().hospitalName;
  const roleLabel = roleLabels[user.role] ?? user.role;

  return (
    <div className="navbar navbar-top navbar-inverse">
      <div className="navbar-inner">
        <div className="container-fluid">
          <Link className="brand" to="/">{systemName}</Link>

          <ul className="nav pull-right bayanno-topbar-mobile">
            <li>
              <button type="button" className="btn btn-navbar mobile-only" onClick={onMenuToggle} aria-label="Abrir menu">
                <i className="icon-th-list" aria-hidden />
              </button>
            </li>
          </ul>

          <div className="nav-collapse nav-collapse-top collapse">
            <ul className="nav pull-right">
              <li className="dropdown">
                <a href="#account" className="dropdown-toggle" onClick={(e) => e.preventDefault()}>
                  Conta <b className="caret" />
                </a>
                <ul className="dropdown-menu">
                  <li className="with-image">
                    <span>{user.fullName}</span>
                  </li>
                  <li className="divider" />
                  <li>
                    <Link to="/configuracoes">
                      <i className="icon-user" aria-hidden />
                      <span> Perfil</span>
                    </Link>
                  </li>
                  <li>
                    <button type="button" className="bayanno-logout-link" onClick={logout}>
                      <i className="icon-off" aria-hidden />
                      <span> Sair</span>
                    </button>
                  </li>
                </ul>
              </li>
            </ul>
            <ul className="nav pull-right">
              <li>
                <a href="#panel" onClick={(e) => e.preventDefault()}>
                  <i className="icon-user" aria-hidden />
                  {roleLabel} panel
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
