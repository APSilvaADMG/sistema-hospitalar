import { useEffect, useState } from 'react';
import { api, type ProfessionalDto } from '../../../api/client';
import { emptyFeegowSchedulingItem, type FeegowPatientSchedulingItem } from './feegowPatientForm';

const WEEKDAYS = ['Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado', 'Domingo'];

type Props = {
  value: FeegowPatientSchedulingItem[];
  onChange: (items: FeegowPatientSchedulingItem[]) => void;
};

export function FeegowPatientSchedulingTable({ value, onChange }: Props) {
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);

  useEffect(() => {
    api.getProfessionals().then(setProfessionals).catch(console.error);
  }, []);

  function updateItem(index: number, patch: Partial<FeegowPatientSchedulingItem>) {
    const next = value.map((item, i) => {
      if (i !== index) return item;
      const updated = { ...item, ...patch };
      if (patch.professionalId) {
        const pro = professionals.find((p) => p.id === patch.professionalId);
        if (pro?.specialtyName) {
          updated.specialtyName = pro.specialtyName;
        }
      }
      return updated;
    });
    onChange(next);
  }

  function addItem() {
    onChange([...value, emptyFeegowSchedulingItem()]);
  }

  function removeItem(index: number) {
    onChange(value.filter((_, i) => i !== index));
  }

  return (
    <section className="feegow-patient-subtable-section">
      <div className="feegow-patient-subtable-toolbar">
        <h2 className="feegow-patient-section-title">Programação de agendamento</h2>
        <button
          type="button"
          className="feegow-subtable-icon-btn feegow-subtable-icon-btn-add"
          onClick={addItem}
          aria-label="Adicionar programação"
          title="Adicionar programação"
        >
          +
        </button>
      </div>

      <div className="feegow-patient-insurance-table-wrap">
        <table className="feegow-patient-insurance-table feegow-patient-scheduling-table">
          <thead>
            <tr>
              <th className="feegow-subtable-actions-col" aria-label="Ações" />
              <th>Especialidade</th>
              <th>Profissional</th>
              <th>Procedimento</th>
              <th>Dia</th>
              <th>Horário</th>
              <th>Unidade / Local</th>
            </tr>
          </thead>
          <tbody>
            {value.length === 0 ? (
              <tr>
                <td colSpan={7} className="feegow-patient-insurance-empty">
                  Nenhuma programação cadastrada. Clique em + para incluir.
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
                      aria-label={`Remover programação ${index + 1}`}
                      title="Remover programação"
                    >
                      −
                    </button>
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.specialtyName}
                      onChange={(e) => updateItem(index, { specialtyName: e.target.value })}
                      placeholder="Especialidade"
                    />
                  </td>
                  <td>
                    <select
                      className="feegow-subtable-input"
                      value={item.professionalId}
                      onChange={(e) => updateItem(index, { professionalId: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {professionals.map((pro) => (
                        <option key={pro.id} value={pro.id}>{pro.fullName}</option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.procedureName}
                      onChange={(e) => updateItem(index, { procedureName: e.target.value })}
                      placeholder="Procedimento"
                    />
                  </td>
                  <td>
                    <select
                      className="feegow-subtable-input"
                      value={item.weekday}
                      onChange={(e) => updateItem(index, { weekday: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {WEEKDAYS.map((day) => (
                        <option key={day} value={day}>{day}</option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      type="time"
                      className="feegow-subtable-input"
                      value={item.time}
                      onChange={(e) => updateItem(index, { time: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      className="feegow-subtable-input"
                      value={item.unit}
                      onChange={(e) => updateItem(index, { unit: e.target.value })}
                      placeholder="Unidade"
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
