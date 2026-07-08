import type { HealthInsuranceDto, PatientDto, ProfessionalDto, ServiceUnitDto, SpecialtyDto } from '../../api/client';
import { GUIDE_STATUS_OPTIONS } from '../../data/guideFunctionalGroups';

export type GuideFiltersState = {
  dateFrom: string;
  dateTo: string;
  patientId: string;
  healthInsuranceId: string;
  professionalId: string;
  specialtyId: string;
  procedureSearch: string;
  guideNumber: string;
  status: string;
  serviceUnitId: string;
};

type Props = {
  filters: GuideFiltersState;
  patients: PatientDto[];
  professionals: ProfessionalDto[];
  specialties: SpecialtyDto[];
  insurances: HealthInsuranceDto[];
  serviceUnits: ServiceUnitDto[];
  loading?: boolean;
  onChange: (patch: Partial<GuideFiltersState>) => void;
  onSearch: () => void;
  onClear: () => void;
};

export function GuideFiltersPanel({
  filters,
  patients,
  professionals,
  specialties,
  insurances,
  serviceUnits,
  loading,
  onChange,
  onSearch,
  onClear,
}: Props) {
  return (
    <div className="guides-filter-panel">
      <div className="guides-filter-row">
        <div className="guides-filter-field">
          <label htmlFor="guideDateFrom">Data inicial</label>
          <input
            id="guideDateFrom"
            type="date"
            value={filters.dateFrom}
            onChange={(e) => onChange({ dateFrom: e.target.value })}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="guideDateTo">Data final</label>
          <input
            id="guideDateTo"
            type="date"
            value={filters.dateTo}
            onChange={(e) => onChange({ dateTo: e.target.value })}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="guideNumber">Número da guia</label>
          <input
            id="guideNumber"
            type="search"
            placeholder="TISS-…"
            value={filters.guideNumber}
            onChange={(e) => onChange({ guideNumber: e.target.value })}
            onKeyDown={(e) => e.key === 'Enter' && onSearch()}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="guideStatus">Status</label>
          <select
            id="guideStatus"
            value={filters.status}
            onChange={(e) => onChange({ status: e.target.value })}
          >
            {GUIDE_STATUS_OPTIONS.map((opt) => (
              <option key={opt.value || 'all'} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="guides-filter-row">
        <div className="guides-filter-field">
          <label htmlFor="guidePatient">Paciente</label>
          <select
            id="guidePatient"
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
          <label htmlFor="guideInsurance">Convênio</label>
          <select
            id="guideInsurance"
            value={filters.healthInsuranceId}
            onChange={(e) => onChange({ healthInsuranceId: e.target.value })}
          >
            <option value="">Todos</option>
            {insurances.map((i) => (
              <option key={i.id} value={i.id}>{i.name}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-field">
          <label htmlFor="guideProfessional">Médico</label>
          <select
            id="guideProfessional"
            value={filters.professionalId}
            onChange={(e) => onChange({ professionalId: e.target.value })}
          >
            <option value="">Todos</option>
            {professionals.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-field">
          <label htmlFor="guideSpecialty">Especialidade</label>
          <select
            id="guideSpecialty"
            value={filters.specialtyId}
            onChange={(e) => onChange({ specialtyId: e.target.value })}
          >
            <option value="">Todas</option>
            {specialties.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="guides-filter-row">
        <div className="guides-filter-field search-grow">
          <label htmlFor="guideProcedure">Procedimento</label>
          <input
            id="guideProcedure"
            type="search"
            placeholder="TUSS ou descrição…"
            value={filters.procedureSearch}
            onChange={(e) => onChange({ procedureSearch: e.target.value })}
            onKeyDown={(e) => e.key === 'Enter' && onSearch()}
          />
        </div>
        <div className="guides-filter-field">
          <label htmlFor="guideUnit">Unidade</label>
          <select
            id="guideUnit"
            value={filters.serviceUnitId}
            onChange={(e) => onChange({ serviceUnitId: e.target.value })}
          >
            <option value="">Todas</option>
            {serviceUnits.filter((u) => u.isActive).map((u) => (
              <option key={u.id} value={u.id}>{u.name}</option>
            ))}
          </select>
        </div>
        <div className="guides-filter-actions">
          <button type="button" className="btn btn-primary" onClick={onSearch} disabled={loading}>
            {loading ? 'Buscando…' : 'Filtrar'}
          </button>
          <button type="button" className="btn btn-secondary" onClick={onClear} disabled={loading}>
            Limpar
          </button>
        </div>
      </div>
    </div>
  );
}
