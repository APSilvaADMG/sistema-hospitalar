import type { PatientDto, ProfessionalDto, WardDto } from '../../api/client';
import {
  HOSPITALIZATION_MODALITY_OPTIONS,
  HOSPITALIZATION_STATUS_OPTIONS,
} from '../../data/hospitalizationFunctionalGroups';

export type HospitalizationFiltersState = {
  dateFrom: string;
  dateTo: string;
  patientId: string;
  wardId: string;
  professionalId: string;
  modality: string;
  status: string;
  search: string;
};

type Props = {
  filters: HospitalizationFiltersState;
  patients: PatientDto[];
  professionals: ProfessionalDto[];
  wards: WardDto[];
  loading?: boolean;
  onChange: (patch: Partial<HospitalizationFiltersState>) => void;
  onSearch: () => void;
  onClear: () => void;
};

export function HospitalizationFiltersPanel({
  filters,
  patients,
  professionals,
  wards,
  loading,
  onChange,
  onSearch,
  onClear,
}: Props) {
  return (
    <div className="guides-filter-panel">
      <div className="guides-filter-row">
        <div className="guides-filter-field">
          <label htmlFor="hospDateFrom">Data inicial</label>
          <input
            id="hospDateFrom"
            type="date"
            value={filters.dateFrom}
            onChange={(e) => onChange({ dateFrom: e.target.value })}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="hospDateTo">Data final</label>
          <input
            id="hospDateTo"
            type="date"
            value={filters.dateTo}
            onChange={(e) => onChange({ dateTo: e.target.value })}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="hospSearch">Busca</label>
          <input
            id="hospSearch"
            type="search"
            placeholder="Paciente, ala, leito, médico…"
            value={filters.search}
            onChange={(e) => onChange({ search: e.target.value })}
            onKeyDown={(e) => e.key === 'Enter' && onSearch()}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="hospStatus">Status</label>
          <select
            id="hospStatus"
            value={filters.status}
            onChange={(e) => onChange({ status: e.target.value })}
          >
            {HOSPITALIZATION_STATUS_OPTIONS.map((opt) => (
              <option key={opt.value || 'all'} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-field">
          <label htmlFor="hospModality">Modalidade</label>
          <select
            id="hospModality"
            value={filters.modality}
            onChange={(e) => onChange({ modality: e.target.value })}
          >
            {HOSPITALIZATION_MODALITY_OPTIONS.map((opt) => (
              <option key={opt.value || 'all'} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
      </div>
      <div className="guides-filter-row">
        <div className="guides-filter-field">
          <label htmlFor="hospPatient">Paciente</label>
          <select
            id="hospPatient"
            value={filters.patientId}
            onChange={(e) => onChange({ patientId: e.target.value })}
          >
            <option value="">Todos</option>
            {patients.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-field">
          <label htmlFor="hospWard">Ala</label>
          <select
            id="hospWard"
            value={filters.wardId}
            onChange={(e) => onChange({ wardId: e.target.value })}
          >
            <option value="">Todas</option>
            {wards.map((w) => (
              <option key={w.id} value={w.id}>{w.name}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-field">
          <label htmlFor="hospProfessional">Médico responsável</label>
          <select
            id="hospProfessional"
            value={filters.professionalId}
            onChange={(e) => onChange({ professionalId: e.target.value })}
          >
            <option value="">Todos</option>
            {professionals.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-actions">
          <button type="button" className="btn btn-primary" onClick={onSearch} disabled={loading}>
            Buscar
          </button>
          <button type="button" className="btn btn-secondary" onClick={onClear} disabled={loading}>
            Limpar
          </button>
        </div>
      </div>
    </div>
  );
}
