import type { AdministrationRouteDto } from '../api/client';
import { AdministrationRouteSelect } from './AdministrationRouteSelect';
import type { PrescriptionLineItem } from '../utils/prescriptionFormat';

type Props = {
  items: PrescriptionLineItem[];
  routes: AdministrationRouteDto[];
  routesLoading?: boolean;
  onChange: (items: PrescriptionLineItem[]) => void;
  disabled?: boolean;
  compact?: boolean;
};

function updateItem(
  items: PrescriptionLineItem[],
  index: number,
  patch: Partial<PrescriptionLineItem>,
): PrescriptionLineItem[] {
  return items.map((item, i) => (i === index ? { ...item, ...patch } : item));
}

export function PrescriptionItemsEditor({
  items,
  routes,
  routesLoading,
  onChange,
  disabled,
  compact,
}: Props) {
  if (items.length === 0) return null;

  const tableClass = compact
    ? 'feegow-patient-section-table prescription-items-table'
    : 'data-table prescription-items-table';

  return (
    <div className="prescription-items-editor">
      <h4 className={compact ? 'feegow-patient-section-panel-title' : 'pep-section-title'}>
        Itens da prescrição
      </h4>
      {routesLoading ? (
        <p className="form-hint">Carregando catálogo de vias de administração…</p>
      ) : null}
      <div className="prescription-items-table-wrap">
        <table className={tableClass}>
          <thead>
            <tr>
              <th>Medicamento</th>
              <th>Via</th>
              <th>Dose</th>
              <th>Frequência</th>
              <th>Obs.</th>
              {!disabled ? <th /> : null}
            </tr>
          </thead>
          <tbody>
            {items.map((item, index) => (
              <tr key={item.medicationId}>
                <td><strong>{item.medicationName}</strong></td>
                <td>
                  <AdministrationRouteSelect
                    routes={routes}
                    value={item.administrationRouteCode}
                    onChange={(code) => onChange(updateItem(items, index, { administrationRouteCode: code }))}
                    disabled={disabled}
                    required
                  />
                </td>
                <td>
                  <input
                    value={item.dosage}
                    onChange={(e) => onChange(updateItem(items, index, { dosage: e.target.value }))}
                    placeholder="Ex.: 500 mg"
                    disabled={disabled}
                  />
                </td>
                <td>
                  <input
                    value={item.frequency}
                    onChange={(e) => onChange(updateItem(items, index, { frequency: e.target.value }))}
                    placeholder="Ex.: 8/8h"
                    disabled={disabled}
                  />
                </td>
                <td>
                  <input
                    value={item.notes}
                    onChange={(e) => onChange(updateItem(items, index, { notes: e.target.value }))}
                    placeholder="Opcional"
                    disabled={disabled}
                  />
                </td>
                {!disabled ? (
                  <td>
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => onChange(items.filter((_, i) => i !== index))}
                    >
                      Remover
                    </button>
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
