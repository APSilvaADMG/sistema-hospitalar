import { useEffect, useState } from 'react';
import { api, type HealthInsuranceDto, type PatientInsuranceInput } from '../api/client';
import { formatCnsInput, isValidCns, onlyDigits } from '../utils/inputMasks';
import { InsuranceLogo } from './InsuranceLogo';

export const emptyInsurance = (): PatientInsuranceInput => ({
  healthInsuranceId: '',
  cardNumber: '',
  planName: '',
  cardHolderName: '',
  productCode: '',
  cnsNumber: '',
  accommodationType: '',
  validFrom: '',
  validUntil: '',
  isPrimary: false,
});

type Props = {
  value: PatientInsuranceInput[];
  onChange: (items: PatientInsuranceInput[]) => void;
};

export function PatientInsuranceFields({ value, onChange }: Props) {
  const [plans, setPlans] = useState<HealthInsuranceDto[]>([]);
  const [cnsHints, setCnsHints] = useState<Record<number, string>>({});

  useEffect(() => {
    api.getHealthInsurances().then(setPlans).catch(console.error);
  }, []);

  function updateItem(index: number, patch: Partial<PatientInsuranceInput>) {
    const next = value.map((item, i) => (i === index ? { ...item, ...patch } : item));
    if (patch.isPrimary) {
      onChange(next.map((item, i) => ({ ...item, isPrimary: i === index })));
      return;
    }
    onChange(next);
  }

  function addItem() {
    onChange([...value, { ...emptyInsurance(), isPrimary: value.length === 0 }]);
  }

  function removeItem(index: number) {
    const next = value.filter((_, i) => i !== index);
    if (next.length > 0 && !next.some((i) => i.isPrimary)) {
      next[0] = { ...next[0], isPrimary: true };
    }
    onChange(next);
  }

  return (
    <div className="insurance-fields">
      {value.length === 0 && (
        <p className="form-hint">Nenhum convênio cadastrado. Adicione para agilizar atendimento e faturamento TISS.</p>
      )}

      {value.map((item, index) => {
        const selectedPlan = plans.find((p) => p.id === item.healthInsuranceId);
        return (
        <div key={index} className="insurance-card">
          <div className="insurance-card-header">
            <strong>Convênio {index + 1}</strong>
            <div className="insurance-card-actions">
              <label className="insurance-primary">
                <input
                  type="radio"
                  name="primary-insurance"
                  checked={item.isPrimary}
                  onChange={() => updateItem(index, { isPrimary: true })}
                />
                Principal
              </label>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => removeItem(index)}>
                Remover
              </button>
            </div>
          </div>

          <div className="form-grid">
            <div className="form-field insurance-operator-field">
              <label>Operadora *</label>
              <div className="insurance-operator-select">
                {selectedPlan && (
                  <InsuranceLogo name={selectedPlan.name} logoUrl={selectedPlan.logoUrl} size={32} />
                )}
                <select
                  required
                  value={item.healthInsuranceId}
                  onChange={(e) => updateItem(index, { healthInsuranceId: e.target.value })}
                >
                  <option value="">Selecione...</option>
                  {plans.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="form-field">
              <label>Nº carteirinha *</label>
              <input
                required
                value={item.cardNumber}
                onChange={(e) => updateItem(index, { cardNumber: e.target.value })}
                placeholder="Número do cartão"
              />
            </div>
            <div className="form-field">
              <label>Plano / produto</label>
              <input
                value={item.planName ?? ''}
                onChange={(e) => updateItem(index, { planName: e.target.value })}
                placeholder="Ex.: Empresarial, Referência"
              />
            </div>
            <div className="form-field">
              <label>Código do produto</label>
              <input
                value={item.productCode ?? ''}
                onChange={(e) => updateItem(index, { productCode: e.target.value })}
              />
            </div>
            <div className="form-field">
              <label>Titular do cartão</label>
              <input
                value={item.cardHolderName ?? ''}
                onChange={(e) => updateItem(index, { cardHolderName: e.target.value })}
                placeholder="Se diferente do paciente"
              />
            </div>
            <div className="form-field">
              <label>CNS</label>
              <input
                value={item.cnsNumber ?? ''}
                onChange={(e) => {
                  updateItem(index, { cnsNumber: formatCnsInput(e.target.value) });
                  setCnsHints((prev) => {
                    const next = { ...prev };
                    delete next[index];
                    return next;
                  });
                }}
                onBlur={() => {
                  const digits = onlyDigits(item.cnsNumber ?? '', 15);
                  if (!digits) {
                    setCnsHints((prev) => {
                      const next = { ...prev };
                      delete next[index];
                      return next;
                    });
                    return;
                  }
                  setCnsHints((prev) => ({
                    ...prev,
                    [index]: isValidCns(digits) ? 'CNS válido.' : 'CNS inválido — verifique os dígitos.',
                  }));
                }}
                placeholder="000 0000 0000 0000"
                inputMode="numeric"
              />
              {cnsHints[index] ? (
                <p className={`form-hint${cnsHints[index].includes('inválido') ? ' form-hint-error' : ''}`}>
                  {cnsHints[index]}
                </p>
              ) : null}
            </div>
            <div className="form-field">
              <label>Acomodação</label>
              <select
                value={item.accommodationType ?? ''}
                onChange={(e) => updateItem(index, { accommodationType: e.target.value })}
              >
                <option value="">Não informado</option>
                <option value="Enfermaria">Enfermaria</option>
                <option value="Apartamento">Apartamento</option>
                <option value="UTI">UTI</option>
              </select>
            </div>
            <div className="form-field">
              <label>Válido de</label>
              <input
                type="date"
                value={item.validFrom ?? ''}
                onChange={(e) => updateItem(index, { validFrom: e.target.value })}
              />
            </div>
            <div className="form-field">
              <label>Válido até</label>
              <input
                type="date"
                value={item.validUntil ?? ''}
                onChange={(e) => updateItem(index, { validUntil: e.target.value })}
              />
            </div>
          </div>
        </div>
        );
      })}

      <button type="button" className="btn btn-secondary" onClick={addItem}>
        + Adicionar convênio
      </button>
    </div>
  );
}
