import { Link } from 'react-router-dom';

const VERSION = '2026.06.1';

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

export function AppFooter() {
  const institution = loadInstitutionName();

  return (
    <footer className="app-footer desktop-only">
      <div className="app-footer-brand">
        <span>{institution}</span>
        <span style={{ color: 'var(--muted)', fontWeight: 500 }}>· Versão {VERSION}</span>
      </div>
      <nav className="app-footer-links" aria-label="Links de suporte">
        <Link to="/configuracoes/aparencia">Aparência</Link>
        <Link to="/configuracoes">Configurações</Link>
        <Link to="/relatorios">Relatórios</Link>
        <Link to="/bi">Dashboard BI</Link>
      </nav>
    </footer>
  );
}
