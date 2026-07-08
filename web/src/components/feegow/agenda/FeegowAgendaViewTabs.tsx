import { Link, useLocation } from 'react-router-dom';

const TABS = [
  { label: 'Diária', path: '/recepcao/agendamentos' },
  { label: 'Semanal', path: '/recepcao/agendamentos/semanal' },
  { label: 'Múltipla', path: '/recepcao/agendamentos/multipla' },
] as const;

export function FeegowAgendaViewTabs() {
  const { pathname } = useLocation();
  const normalized = pathname.replace(/\/$/, '') || '/';

  function isActive(path: string) {
    if (path === '/recepcao/agendamentos') {
      return normalized === path || normalized === '/recepcao/agendamentos/consultas';
    }
    return normalized === path || normalized.startsWith(`${path}/`);
  }

  return (
    <nav className="feegow-agenda-view-tabs" aria-label="Tipo de agenda">
      {TABS.map((tab) => (
        <Link
          key={tab.path}
          to={tab.path}
          className={isActive(tab.path) ? 'active' : ''}
        >
          {tab.label}
        </Link>
      ))}
    </nav>
  );
}
