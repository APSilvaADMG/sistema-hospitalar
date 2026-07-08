import { Link, Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { BAYANNO_SCREEN_COUNT } from '../data/bayanno';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';
import { FeegowSghcScreenContent } from '../components/feegow/sghc/FeegowSghcScreenContent';
import { FeegowSghcScreenLayout } from '../components/feegow/sghc/FeegowSghcScreenLayout';
import {
  defaultSghcDashboardPath,
  defaultSghcRoleForUser,
  parseSghcPath,
  resolveSghcScreen,
} from '../components/feegow/sghc/feegowSghcNav';

export function FeegowSghcWorkspacePage() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const { appearance } = useAppearance();

  if (!isFeegowBrand(appearance.brand)) {
    return <Navigate to="/" replace />;
  }

  const parsed = parseSghcPath(pathname);

  if (!parsed) {
    const role = user?.role ? defaultSghcRoleForUser(user.role) : 'admin';
    return <Navigate to={`/sghc/${role}/dashboard`} replace />;
  }

  const screen = resolveSghcScreen(pathname);

  if (!screen) {
    const fallback = defaultSghcDashboardPath(user?.role ?? 'Admin');
    return (
      <FeegowSghcScreenLayout activeRole={parsed.role} activeRoute={parsed.route}>
        <div className="feegow-patient-card">
          <h1>Tela não encontrada</h1>
          <p>
            A rota <code>{pathname}</code> não está no catálogo SGHC ({BAYANNO_SCREEN_COUNT} telas).
          </p>
          <Link to={fallback} className="btn btn-sm">Voltar ao painel</Link>
        </div>
      </FeegowSghcScreenLayout>
    );
  }

  return (
    <FeegowSghcScreenLayout activeRole={parsed.role} activeRoute={parsed.route}>
      <FeegowSghcScreenContent screen={screen} pathname={pathname} />
    </FeegowSghcScreenLayout>
  );
}
