import { Link } from 'react-router-dom';
import {
  genderLabels,
  resolvePatientModality,
  wardModalityLabels,
  type PatientDetailDto,
} from '../../api/client';
import { PersonAvatar } from '../PersonAvatar';
import { calcAge, formatBirthDate, formatPhone } from '../../utils/pepUtils';

type Props = {
  patient: PatientDetailDto;
  onClear?: () => void;
  extra?: React.ReactNode;
};

export function PatientContextHeader({ patient, onClear, extra }: Props) {
  const modality = resolvePatientModality(patient.insurances?.[0]?.healthInsuranceName);
  const primaryInsurance = patient.insurances?.find((i) => i.isPrimary) ?? patient.insurances?.[0];

  return (
    <div className="patient-context-header">
      <PersonAvatar name={patient.fullName} photoData={patient.photoData} size={48} />
      <div className="patient-context-header-main">
        <h3>{patient.fullName}</h3>
        {patient.socialName && (
          <p className="form-hint" style={{ margin: '0 0 4px' }}>Nome social: {patient.socialName}</p>
        )}
        <div className="patient-context-meta">
          <span>{calcAge(patient.birthDate)} anos</span>
          <span>Nasc. {formatBirthDate(patient.birthDate)}</span>
          <span>{genderLabels[patient.gender] ?? '—'}</span>
          {patient.cpf && <span>CPF {patient.cpf}</span>}
          {patient.phone && <span>{formatPhone(patient.phone)}</span>}
          <span className={`ward-badge ward-modality-${modality}`}>{wardModalityLabels[modality]}</span>
          {primaryInsurance && (
            <span>{primaryInsurance.healthInsuranceName} · {primaryInsurance.cardNumber}</span>
          )}
        </div>
      </div>
      <div className="patient-context-actions">
        {extra}
        <Link className="btn btn-sm" to={`/pacientes/${patient.id}/prontuario`}>
          Prontuário completo
        </Link>
        {onClear && (
          <button type="button" className="btn btn-secondary btn-sm" onClick={onClear}>
            Trocar paciente
          </button>
        )}
      </div>
    </div>
  );
}
