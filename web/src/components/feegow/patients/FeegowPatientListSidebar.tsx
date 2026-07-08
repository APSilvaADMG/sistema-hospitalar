import { NavLink } from 'react-router-dom';
import { useOpenModuleSearch } from '../../ModuleSearchProvider';
import {
  type FeegowPatientListFilter,
  feegowPatientListPath,
} from './feegowPatientNav';

type Props = {
  filter: FeegowPatientListFilter;
  chartSearch: string;
  onChartSearchChange: (value: string) => void;
};

export function FeegowPatientListSidebar({
  filter,
  chartSearch,
  onChartSearchChange,
}: Props) {
  const openSearch = useOpenModuleSearch();

  return (
    <div className="feegow-agenda-sidebar feegow-patient-sidebar feegow-patient-list-sidebar">
      <div className="feegow-quick-search feegow-patient-search">
        <span className="feegow-search-icon" aria-hidden>🔍</span>
        <button type="button" className="feegow-search-input" onClick={openSearch}>
          Busca rápida…
        </button>
      </div>

      <p className="feegow-patient-list-section-label">OUTROS PACIENTES</p>

      <nav className="feegow-patient-list-nav" aria-label="Filtros de pacientes">
        <NavLink
          to={feegowPatientListPath('active')}
          className={({ isActive }) => `feegow-patient-list-nav-item${isActive || filter === 'active' ? ' is-active' : ''}`}
        >
          ATIVOS
        </NavLink>
        <NavLink
          to={feegowPatientListPath('inactive')}
          className={({ isActive }) => `feegow-patient-list-nav-item${isActive || filter === 'inactive' ? ' is-active' : ''}`}
        >
          INATIVOS
        </NavLink>
        <NavLink
          to={feegowPatientListPath('chart-search')}
          className={({ isActive }) => `feegow-patient-list-nav-item${isActive || filter === 'chart-search' ? ' is-active' : ''}`}
        >
          Busca no prontuário
        </NavLink>
      </nav>

      {filter === 'chart-search' ? (
        <label className="feegow-field feegow-patient-chart-search">
          <span>Nº do prontuário</span>
          <input
            type="search"
            value={chartSearch}
            onChange={(e) => onChartSearchChange(e.target.value)}
            placeholder="Digite o número…"
          />
        </label>
      ) : null}
    </div>
  );
}
