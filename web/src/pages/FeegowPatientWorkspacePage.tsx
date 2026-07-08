import { Navigate, useLocation } from 'react-router-dom';
import { parseFeegowPatientRoute } from '../components/feegow/patients/feegowPatientNav';
import { FeegowPatientInsertPage } from './FeegowPatientInsertPage';
import { FeegowPatientListPage } from './FeegowPatientListPage';
import { FeegowPatientRecordPage } from './FeegowPatientRecordPage';
import { FeegowPatientSectionGatePage } from './FeegowPatientSectionGatePage';

export function FeegowPatientWorkspacePage() {
  const { pathname } = useLocation();
  const normalized = pathname.replace(/\/$/, '') || '/';

  if (normalized === '/recepcao/pacientes') {
    return <Navigate to="/recepcao/pacientes/inserir" replace />;
  }

  const route = parseFeegowPatientRoute(pathname);
  if (!route) {
    return <Navigate to="/recepcao/pacientes/listar" replace />;
  }

  if (route.mode === 'list') {
    return <FeegowPatientListPage listFilter={route.listFilter} />;
  }

  if (route.mode === 'insert') {
    if (route.section === 'dados-principais') {
      return <FeegowPatientInsertPage />;
    }
    return <FeegowPatientSectionGatePage section={route.section} />;
  }

  if (route.section === 'dados-principais') {
    return <FeegowPatientRecordPage patientId={route.patientId} />;
  }

  return <FeegowPatientRecordPage patientId={route.patientId} section={route.section} />;
}
