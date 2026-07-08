import { Link } from 'react-router-dom';
import type { PatientDto } from '../api/client';

type Props = {
  patients: PatientDto[];
  patientId: string;
  search: string;
  onSearchChange: (value: string) => void;
  onPatientChange: (id: string) => void;
  label?: string;
};

export function HubPatientPicker({
  patients,
  patientId,
  search,
  onSearchChange,
  onPatientChange,
  label = 'Paciente',
}: Props) {
  const filtered = patients.filter((p) => {
    if (!search.trim()) return true;
    const term = search.toLowerCase();
    return p.fullName.toLowerCase().includes(term) || (p.cpf?.includes(term) ?? false);
  });

  const displayList = filtered.slice(0, 50);

  return (
    <div className="tab-pane box active hub-patient-picker" style={{ marginBottom: 15 }}>
      <div className="bayanno-panel-head">
        <span className="title">
          <i className="icon-user" aria-hidden />
          {' '}
          Seleção de {label.toLowerCase()}
        </span>
        <span className="bayanno-panel-hint">
          Busque por nome ou CPF e clique na linha para selecionar
        </span>
      </div>

      <div className="bayanno-reports-toolbar" style={{ padding: '8px 12px' }}>
        <div className="dataTables_filter">
          <label htmlFor="hubPatientSearch">
            <span>Buscar:</span>
            <input
              id="hubPatientSearch"
              type="search"
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder="Nome ou CPF…"
            />
          </label>
        </div>
      </div>

      <p className="bayanno-inline-hint">
        Exibindo {displayList.length} de {filtered.length} paciente(s)
        {patientId ? (
          <>
            {' '}
            · <Link to={`/pacientes/${patientId}/prontuario`}>Abrir prontuário completo</Link>
          </>
        ) : null}
      </p>

      <div className="table-responsive-wrap">
        <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">
          <thead>
            <tr>
              <th><div>#</div></th>
              <th><div>Nome</div></th>
              <th><div>CPF</div></th>
              <th><div>CNS</div></th>
            </tr>
          </thead>
          <tbody>
            {displayList.length === 0 ? (
              <tr>
                <td colSpan={4} className="dataTables_empty center">
                  Nenhum paciente encontrado.
                </td>
              </tr>
            ) : (
              displayList.map((p, index) => (
                <tr
                  key={p.id}
                  className={`${index % 2 === 1 ? 'even' : ''}${patientId === p.id ? ' is-selected' : ''}`.trim()}
                  onClick={() => onPatientChange(p.id)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onPatientChange(p.id);
                    }
                  }}
                  tabIndex={0}
                  role="button"
                  aria-pressed={patientId === p.id}
                >
                  <td>{index + 1}</td>
                  <td><strong>{p.fullName}</strong></td>
                  <td>{p.cpf ?? '—'}</td>
                  <td>{p.cns ?? '—'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
