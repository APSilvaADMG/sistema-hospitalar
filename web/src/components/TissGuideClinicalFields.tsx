import type { TissGuideClinicalRequest } from '../api/client';

const serviceCharacterOptions = [
  { value: 1, label: 'Eletivo' },
  { value: 2, label: 'Urgência' },
  { value: 3, label: 'Emergência' },
];

const accidentOptions = [
  { value: 0, label: 'Não se aplica' },
  { value: 1, label: 'Acidente de trabalho' },
  { value: 2, label: 'Acidente de trânsito' },
  { value: 3, label: 'Outros acidentes' },
];

const roleOptions = [
  { value: 1, label: 'Cirurgião' },
  { value: 2, label: '1º auxiliar' },
  { value: 3, label: '2º auxiliar' },
  { value: 4, label: 'Anestesista' },
  { value: 5, label: 'Instrumentador' },
];

type Props = {
  guideType: number;
  clinical: TissGuideClinicalRequest;
  disabled?: boolean;
  onChange: (clinical: TissGuideClinicalRequest) => void;
  beneficiaryCard?: string;
  beneficiaryPlan?: string;
  authorizationPassword?: string;
};

function showForType(guideType: number, types: number[]) {
  return types.includes(guideType);
}

export function TissGuideClinicalFields({
  guideType,
  clinical,
  disabled,
  onChange,
  beneficiaryCard,
  beneficiaryPlan,
  authorizationPassword,
}: Props) {
  function set<K extends keyof TissGuideClinicalRequest>(key: K, value: TissGuideClinicalRequest[K]) {
    onChange({ ...clinical, [key]: value });
  }

  const isConsultation = showForType(guideType, [1]);
  const isSpSadt = showForType(guideType, [2]);
  const isHospRequest = showForType(guideType, [6]);
  const isDischarge = showForType(guideType, [4, 3]);
  const isFees = showForType(guideType, [5]);
  const isOther = showForType(guideType, [7]);

  return (
    <div className="tiss-clinical-fields">
      <div className="form-section-title">Dados clínicos e beneficiário (TISS)</div>

      {(beneficiaryCard || beneficiaryPlan || authorizationPassword) && (
        <div className="tiss-beneficiary-snapshot">
          {beneficiaryCard && <span><strong>Carteirinha:</strong> {beneficiaryCard}</span>}
          {beneficiaryPlan && <span><strong>Plano:</strong> {beneficiaryPlan}</span>}
          {authorizationPassword && <span><strong>Senha:</strong> {authorizationPassword}</span>}
        </div>
      )}

      <div className="form-grid">
        <div className="form-field">
          <label>CID-10 principal</label>
          <input
            disabled={disabled}
            value={clinical.cid10Code ?? ''}
            onChange={(e) => set('cid10Code', e.target.value)}
            placeholder="Ex.: I10, J06.9"
          />
        </div>
        <div className="form-field">
          <label>CID-10 secundário</label>
          <input
            disabled={disabled}
            value={clinical.cid10Secondary ?? ''}
            onChange={(e) => set('cid10Secondary', e.target.value)}
          />
        </div>

        {(isConsultation || isSpSadt || isHospRequest) && (
          <div className="form-field">
            <label>Caráter do atendimento</label>
            <select
              disabled={disabled}
              value={clinical.serviceCharacter ?? 1}
              onChange={(e) => set('serviceCharacter', Number(e.target.value))}
            >
              {serviceCharacterOptions.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
        )}

        {(isConsultation || isSpSadt) && (
          <div className="form-field">
            <label>Indicador de acidente</label>
            <select
              disabled={disabled}
              value={clinical.accidentIndicator ?? 0}
              onChange={(e) => set('accidentIndicator', Number(e.target.value))}
            >
              {accidentOptions.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
        )}

        <div className="form-field">
          <label>Médico solicitante</label>
          <input
            disabled={disabled}
            value={clinical.requestingProfessionalName ?? ''}
            onChange={(e) => set('requestingProfessionalName', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label>CRM solicitante</label>
          <input
            disabled={disabled}
            value={clinical.requestingProfessionalCrm ?? ''}
            onChange={(e) => set('requestingProfessionalCrm', e.target.value)}
          />
        </div>

        {(isSpSadt || isDischarge || isFees) && (
          <>
            <div className="form-field">
              <label>Profissional executante</label>
              <input
                disabled={disabled}
                value={clinical.executingProfessionalName ?? ''}
                onChange={(e) => set('executingProfessionalName', e.target.value)}
              />
            </div>
            <div className="form-field">
              <label>CRM executante</label>
              <input
                disabled={disabled}
                value={clinical.executingProfessionalCrm ?? ''}
                onChange={(e) => set('executingProfessionalCrm', e.target.value)}
              />
            </div>
          </>
        )}

        {(isHospRequest || isDischarge) && (
          <>
            <div className="form-field">
              <label>Data admissão</label>
              <input
                type="datetime-local"
                disabled={disabled}
                value={clinical.admissionDate?.slice(0, 16) ?? ''}
                onChange={(e) => set('admissionDate', e.target.value ? new Date(e.target.value).toISOString() : undefined)}
              />
            </div>
            {isDischarge && (
              <div className="form-field">
                <label>Data alta</label>
                <input
                  type="datetime-local"
                  disabled={disabled}
                  value={clinical.dischargeDate?.slice(0, 16) ?? ''}
                  onChange={(e) => set('dischargeDate', e.target.value ? new Date(e.target.value).toISOString() : undefined)}
                />
              </div>
            )}
            <div className="form-field">
              <label>Leito / acomodação solicitada</label>
              <input
                disabled={disabled}
                value={clinical.requestedBedType ?? ''}
                onChange={(e) => set('requestedBedType', e.target.value)}
              />
            </div>
          </>
        )}

        {isFees && (
          <>
            <div className="form-field">
              <label>Papel na equipe</label>
              <select
                disabled={disabled}
                value={clinical.professionalRole ?? 1}
                onChange={(e) => set('professionalRole', Number(e.target.value))}
              >
                {roleOptions.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Participação (%)</label>
              <input
                type="number"
                min={0}
                max={100}
                step="0.01"
                disabled={disabled}
                value={clinical.participationPercent ?? ''}
                onChange={(e) => set('participationPercent', e.target.value ? Number(e.target.value) : undefined)}
              />
            </div>
          </>
        )}

        {(isSpSadt || isHospRequest || isOther) && (
          <div className="form-field full">
            <label>Justificativa clínica</label>
            <textarea
              rows={2}
              disabled={disabled}
              value={clinical.clinicalJustification ?? ''}
              onChange={(e) => set('clinicalJustification', e.target.value)}
            />
          </div>
        )}
      </div>
    </div>
  );
}
