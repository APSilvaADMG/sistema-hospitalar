import { useMemo } from 'react';
import {
  isBedAvailable,
  isWardCompatible,
  wardCategoryLabels,
  wardCategoryValue,
  wardModalityLabels,
  wardModalityValue,
  type BedDto,
} from '../api/client';

type Props = {
  beds: BedDto[];
  value: string;
  onChange: (bedId: string) => void;
  patientModality: number;
  planName?: string;
  loading?: boolean;
  requirePatient?: boolean;
  hasPatient?: boolean;
  /** Leitos já filtrados pelo servidor conforme cobertura do paciente */
  serverFiltered?: boolean;
};

export function AvailableBedsPicker({
  beds,
  value,
  onChange,
  patientModality,
  planName,
  loading = false,
  requirePatient = false,
  hasPatient = true,
  serverFiltered = false,
}: Props) {
  const compatibleBeds = useMemo(
    () => beds.filter((b) => {
      if (!isBedAvailable(b.status)) return false;
      if (serverFiltered) return true;
      return isWardCompatible(b.wardCoverageModality, patientModality);
    }),
    [beds, patientModality, serverFiltered],
  );

  if (requirePatient && !hasPatient) {
    return (
      <p className="form-hint">Selecione o paciente para listar os leitos disponíveis conforme a cobertura.</p>
    );
  }

  return (
    <div className="available-beds-picker">
      <div className="pep-info-box">
        Cobertura detectada: <strong>{wardModalityLabels[patientModality]}</strong>
        {planName && <> · {planName}</>}
        {' '}— {compatibleBeds.length} leito(s) disponível(is) compatível(is).
      </div>

      {loading && <p className="form-hint">Carregando leitos disponíveis...</p>}

      {!loading && compatibleBeds.length > 0 && (
        <div className="bed-availability-grid">
          {compatibleBeds.map((b) => (
            <button
              key={b.id}
              type="button"
              className={`bed-availability-card${value === b.id ? ' selected' : ''}`}
              onClick={() => onChange(b.id)}
            >
              <strong>Leito {b.bedNumber}</strong>
              <span>{b.wardName}</span>
              <span className="bed-availability-meta">
                {wardCategoryLabels[wardCategoryValue(b.wardCategory)]} · {wardModalityLabels[wardModalityValue(b.wardCoverageModality)]}
              </span>
            </button>
          ))}
        </div>
      )}

      <div className="form-field" style={{ marginTop: 12 }}>
        <label>Leito *</label>
        <select required value={value} onChange={(e) => onChange(e.target.value)} disabled={loading || compatibleBeds.length === 0}>
          <option value="">Selecione o leito</option>
          {compatibleBeds.map((b) => (
            <option key={b.id} value={b.id}>
              [{wardModalityLabels[wardModalityValue(b.wardCoverageModality)]}] {b.wardName} ({wardCategoryLabels[wardCategoryValue(b.wardCategory)]}) — Leito {b.bedNumber}
            </option>
          ))}
        </select>
      </div>

      {!loading && compatibleBeds.length === 0 && (
        <p className="form-hint">
          Nenhum leito disponível para a modalidade {wardModalityLabels[patientModality]}.
        </p>
      )}
    </div>
  );
}
