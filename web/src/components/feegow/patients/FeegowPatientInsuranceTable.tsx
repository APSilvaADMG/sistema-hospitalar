import { useEffect, useState } from 'react';
import { api, type HealthInsuranceDto, type PatientInsuranceInput } from '../../../api/client';
import { emptyInsurance } from '../../PatientInsuranceFields';

type Props = {
  value: PatientInsuranceInput[];
  onChange: (items: PatientInsuranceInput[]) => void;
};

export function FeegowPatientInsuranceTable({ value, onChange }: Props) {
  const [plans, setPlans] = useState<HealthInsuranceDto[]>([]);

  useEffect(() => {
    api.getHealthInsurances().then(setPlans).catch(console.error);
  }, []);

  function updateItem(index: number, patch: Partial<PatientInsuranceInput>) {
    const next = value.map((item, i) => (i === index ? { ...item, ...patch } : item));
    onChange(next);
  }

  function addItem() {
    onChange([...value, { ...emptyInsurance(), isPrimary: value.length === 0 }]);
  }

  function removeItem(index: number) {
    const next = value.filter((_, i) => i !== index);
    if (next.length > 0 && !next.some((item) => item.isPrimary)) {
      next[0] = { ...next[0], isPrimary: true };
    }
    onChange(next);
  }

  return (
    <section className="feegow-patient-subtable-section">
      <div className="feegow-patient-subtable-toolbar">
        <h2 className="feegow-patient-section-title">Convênios do Paciente</h2>
        <button
          type="button"
          className="feegow-subtable-icon-btn feegow-subtable-icon-btn-add"
          onClick={addItem}
          aria-label="Adicionar convênio"
          title="Adicionar convênio"
        >
          +
        </button>
      </div>

      <div className="feegow-patient-insurance-table-wrap">
        <table className="feegow-patient-insurance-table">
          <thead>
            <tr>
              <th className="feegow-subtable-actions-col" aria-label="Ações" />
              <th>Convênio</th>
              <th>Plano</th>
              <th>Matrícula / Carteirinha</th>
              <th>Token Carteirinha</th>
              <th>Validade</th>
              <th>Titular</th>
            </tr>
          </thead>
          <tbody>
            {value.length === 0 ? (
              <tr>
                <td colSpan={7} className="feegow-patient-insurance-empty">
                  Nenhum convênio cadastrado. Clique em + para incluir.
                </td>
              </tr>
            ) : (
              value.map((item, index) => (
                <tr key={index}>
                  <td className="feegow-subtable-actions-col">
                    <button
                      type="button"
                      className="feegow-subtable-icon-btn feegow-subtable-icon-btn-remove"
                      onClick={() => removeItem(index)}
                      aria-label={`Remover convênio ${index + 1}`}
                      title="Remover convênio"
                    >
                      −
                    </button>
                  </td>
                  <td>
                    <select
                      className="feegow-subtable-input"
                      value={item.healthInsuranceId}
                      onChange={(e) => updateItem(index, { healthInsuranceId: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {plans.map((plan) => (
                        <option key={plan.id} value={plan.id}>{plan.name}</option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.planName ?? ''}
                      onChange={(e) => updateItem(index, { planName: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.cardNumber}
                      onChange={(e) => updateItem(index, { cardNumber: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.productCode ?? ''}
                      onChange={(e) => updateItem(index, { productCode: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      type="date"
                      className="feegow-subtable-input"
                      value={item.validUntil ?? ''}
                      onChange={(e) => updateItem(index, { validUntil: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.cardHolderName ?? ''}
                      onChange={(e) => updateItem(index, { cardHolderName: e.target.value })}
                    />
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
