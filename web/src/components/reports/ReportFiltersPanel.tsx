import type { HealthInsuranceDto, PatientDto, ProfessionalDto, SpecialtyDto } from '../../api/client';

export type ReportFiltersState = {
  search: string;
  dateFrom: string;
  dateTo: string;
  patientId: string;
  professionalId: string;
  specialtyId: string;
  healthInsuranceId: string;
  essentialOnly: boolean;
  implementedOnly: boolean;
};

type Props = {
  filters: ReportFiltersState;
  patients: PatientDto[];
  professionals: ProfessionalDto[];
  specialties: SpecialtyDto[];
  insurances: HealthInsuranceDto[];
  loading?: boolean;
  onChange: (patch: Partial<ReportFiltersState>) => void;
  onSearch: () => void;
  onClear: () => void;
};

export function ReportFiltersPanel({
  filters,
  patients,
  professionals,
  specialties,
  insurances,
  loading,
  onChange,
  onSearch,
  onClear,
}: Props) {
  return (
    <div className="reports-filter-panel">
      <div className="reports-filter-row">
        <div className="reports-filter-field search-grow">
          <label htmlFor="reportSearch">Buscar relatório</label>
          <input
            id="reportSearch"
            type="search"
            placeholder="Nome ou palavra-chave…"
            value={filters.search}
            onChange={(e) => onChange({ search: e.target.value })}
            onKeyDown={(e) => e.key === 'Enter' && onSearch()}
          />
        </div>
        <div className="reports-filter-field">
          <label htmlFor="reportDateFrom">Data inicial</label>
          <input
            id="reportDateFrom"
            type="date"
            value={filters.dateFrom}
            onChange={(e) => onChange({ dateFrom: e.target.value })}
          />
        </div>
        <div className="reports-filter-field">
          <label htmlFor="reportDateTo">Data final</label>
          <input
            id="reportDateTo"
            type="date"
            value={filters.dateTo}
            onChange={(e) => onChange({ dateTo: e.target.value })}
          />
        </div>
      </div>

      <div className="reports-filter-row">
        <div className="reports-filter-field">
          <label htmlFor="reportPatient">Paciente</label>
          <select
            id="reportPatient"
            value={filters.patientId}
            onChange={(e) => onChange({ patientId: e.target.value })}
          >
            <option value="">Todos</option>
            {patients.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </select>
        </div>
        <div className="reports-filter-field">
          <label htmlFor="reportProfessional">Profissional</label>
          <select
            id="reportProfessional"
            value={filters.professionalId}
            onChange={(e) => onChange({ professionalId: e.target.value })}
          >
            <option value="">Todos</option>
            {professionals.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </select>
        </div>
        <div className="reports-filter-field">
          <label htmlFor="reportSpecialty">Especialidade</label>
          <select
            id="reportSpecialty"
            value={filters.specialtyId}
            onChange={(e) => onChange({ specialtyId: e.target.value })}
          >
            <option value="">Todas</option>
            {specialties.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </div>
        <div className="reports-filter-field">
          <label htmlFor="reportInsurance">Convênio</label>
          <select
            id="reportInsurance"
            value={filters.healthInsuranceId}
            onChange={(e) => onChange({ healthInsuranceId: e.target.value })}
          >
            <option value="">Todos</option>
            {insurances.map((i) => (
              <option key={i.id} value={i.id}>{i.name}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="reports-filter-checks">
        <label>
          <input
            type="checkbox"
            checked={filters.implementedOnly}
            onChange={(e) => onChange({ implementedOnly: e.target.checked })}
          />
          Somente disponíveis agora
        </label>
        <label>
          <input
            type="checkbox"
            checked={filters.essentialOnly}
            onChange={(e) => onChange({ essentialOnly: e.target.checked })}
          />
          Apenas essenciais (MVP)
        </label>
      </div>

      <div className="reports-filter-actions">
        <button type="button" className="btn btn-green" disabled={loading} onClick={onSearch}>
          <i className="icon-search" aria-hidden /> Pesquisar
        </button>
        <button type="button" className="btn btn-secondary" disabled={loading} onClick={onClear}>
          Limpar
        </button>
      </div>
    </div>
  );
}
