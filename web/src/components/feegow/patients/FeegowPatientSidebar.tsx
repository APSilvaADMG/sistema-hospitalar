import { NavLink } from 'react-router-dom';
import { useOpenModuleSearch } from '../../ModuleSearchProvider';
import {
  FEEGOW_PATIENT_NAV,
  feegowPatientInsertPath,
  feegowPatientRecordPath,
} from './feegowPatientNav';

export const FEEGOW_PATIENT_SIDEBAR_HOST_ID = 'feegow-patient-sidebar-host';

type Props = {
  mode: 'insert' | 'record';
  activeSection: string;
  patientId?: string;
};

export function FeegowPatientSidebar({ mode, activeSection, patientId }: Props) {
  const openSearch = useOpenModuleSearch();

  function sectionPath(sectionId: string): string {
    if (mode === 'record' && patientId) {
      return feegowPatientRecordPath(patientId, sectionId);
    }
    return feegowPatientInsertPath(sectionId);
  }

  return (
    <div className="feegow-agenda-sidebar feegow-patient-sidebar">
      <div className="feegow-quick-search feegow-patient-search">
        <span className="feegow-search-icon" aria-hidden>🔍</span>
        <button type="button" className="feegow-search-input" onClick={openSearch}>
          Busca rápida…
        </button>
      </div>

      <button type="button" className="feegow-patient-start-btn">
        <span className="feegow-patient-start-icon" aria-hidden>▶</span>
        Iniciar Atendimento
      </button>

      <nav className="feegow-patient-nav" aria-label="Seções do prontuário">
        {FEEGOW_PATIENT_NAV.map((item) => (
          <NavLink
            key={item.id}
            to={sectionPath(item.id)}
            className={({ isActive }) => `feegow-patient-nav-item${isActive || activeSection === item.id ? ' is-active' : ''}`}
          >
            <span>{item.label}</span>
            {item.badge ? (
              <span className="feegow-patient-nav-badge">{item.badge}</span>
            ) : null}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}
