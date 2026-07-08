import { Link } from 'react-router-dom';



import { roleLabels } from '../api/client';



import { useAuth } from '../auth/AuthContext';



import { formatBrLongDate } from '../utils/dateUtils';

import { loadHospitalParams } from '../config/clinicOnDoctorProfile';
import { filterPathsByModules } from '../config/moduleVisibility';
import { resolveMenuProfile } from '../navigation/menuProfile';

import { HospitalLogo } from './HospitalLogo';



import { NavIcon } from './NavIcon';

import { ModuleSearchTrigger } from './ModuleSearchTrigger';







function userInitials(name: string) {



  return name



    .split(' ')



    .filter(Boolean)



    .slice(0, 2)



    .map((part) => part[0]?.toUpperCase() ?? '')



    .join('');



}



function loadInstitutionName(): string {

  try {

    const raw = localStorage.getItem('hms-hospital-params');

    if (!raw) return 'APSMedCore';

    const parsed = JSON.parse(raw) as { hospitalName?: string };

    return parsed.hospitalName?.trim() || 'APSMedCore';

  } catch {

    return 'APSMedCore';

  }

}







type TopBarProps = {



  onMenuToggle?: () => void;



};







export function TopBar({ onMenuToggle }: TopBarProps) {



  const { user, hasRole, hasPermission } = useAuth();






  if (!user) return null;







  const now = formatBrLongDate(new Date());

  const institution = loadInstitutionName();

  const isAdmin = hasRole('Admin') || hasPermission('users.manage', 'security.manage');

  const isAdminOrReception = hasRole('Admin', 'Reception')

    || hasPermission('patients.create', 'billing.write');

  const profile = resolveMenuProfile({

    role: user.role,

    isAdmin,

    isAdminOrReception,

    hasPermission,

  });

  const shortcuts = filterPathsByModules(profile.shortcuts.slice(0, 6), loadHospitalParams().modules);







  return (



    <header className="topbar">



      <div className="topbar-left">



        <button



          type="button"



          className="topbar-menu-btn mobile-only"



          onClick={onMenuToggle}



          aria-label="Abrir menu"



        >



          <NavIcon name="menu" />



        </button>



        <HospitalLogo variant="mark" height={67} className="topbar-logo mobile-only" />



        <div className="topbar-profile desktop-only">

          <div className="topbar-avatar">{userInitials(user.fullName)}</div>

          <div className="topbar-profile-text">

            <span className="topbar-profile-name">{user.fullName}</span>

            <span className="topbar-profile-org">{institution}</span>

          </div>

        </div>



        <span className="topbar-date desktop-only">{now}</span>



      </div>



      <div className="topbar-center desktop-only">

        <ModuleSearchTrigger />

      </div>



      <div className="topbar-right">



        <nav className="topbar-shortcuts desktop-only" aria-label="Atalhos rápidos">

          {shortcuts.map((item) => (

            <Link key={item.path} to={item.path} className="topbar-shortcut" title={item.label}>

              <NavIcon name={item.icon} />

              <span>{item.label}</span>

            </Link>

          ))}

        </nav>



        <div className="topbar-center mobile-only" style={{ flex: 1, minWidth: 0 }}>

          <ModuleSearchTrigger />

        </div>



        <Link to="/configuracoes/aparencia" className="topbar-icon-btn desktop-only" title="Aparência do sistema">

          <NavIcon name="palette" />

        </Link>



        {hasPermission('connect.read') ? (
          <Link to="/connect" className="topbar-icon-btn" title="Caixa de Entrada">
            <NavIcon name="mail" />
          </Link>
        ) : null}



        <Link to="/notificacoes" className="topbar-icon-btn" title="Notificações">



          <NavIcon name="bell" />



        </Link>



        <div className="topbar-user desktop-only">



          <div className="topbar-avatar">{userInitials(user.fullName)}</div>



          <div className="topbar-user-text">



            <span className="topbar-user-name">{user.fullName}</span>



            <span className="topbar-user-role">{roleLabels[user.role]}</span>



          </div>



        </div>



      </div>



    </header>



  );



}



