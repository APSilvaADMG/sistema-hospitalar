import { useState, type FormEvent } from 'react';
import type {
  HealthInsuranceDto,
  MedicationInsuranceMappingDto,
  ProductDto,
} from '../../../api/client';
import { TablePagination } from '../TablePagination';

export type MedicationInsuranceFormState = {
  prescribedProductId: string;
  referenceProductId: string;
  healthInsuranceId: string;
};

export const emptyMedicationInsuranceForm = (): MedicationInsuranceFormState => ({
  prescribedProductId: '',
  referenceProductId: '',
  healthInsuranceId: '',
});

type Props = {
  mappings: MedicationInsuranceMappingDto[];
  products: ProductDto[];
  insurances: HealthInsuranceDto[];
  loading?: boolean;
  saving?: boolean;
  canManage?: boolean;
  onCreate: (payload: MedicationInsuranceFormState) => Promise<void>;
  onUpdate: (id: string, payload: MedicationInsuranceFormState) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
};

export function FeegowMedicationInsuranceList({
  mappings,
  products,
  insurances,
  loading,
  saving,
  canManage,
  onCreate,
  onUpdate,
  onDelete,
  page,
  pageSize,
  totalCount,
  onPageChange,
}: Props) {
  const showPagination = page != null && pageSize != null && totalCount != null && onPageChange != null;
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<MedicationInsuranceFormState>(emptyMedicationInsuranceForm());
  const [error, setError] = useState('');

  const medications = products.filter((p) => p.type === 1);

  function openCreate() {
    setEditingId(null);
    setForm(emptyMedicationInsuranceForm());
    setError('');
    setShowModal(true);
  }

  function openEdit(mapping: MedicationInsuranceMappingDto) {
    setEditingId(mapping.id);
    setForm({
      prescribedProductId: mapping.prescribedProductId,
      referenceProductId: mapping.referenceProductId,
      healthInsuranceId: mapping.healthInsuranceId,
    });
    setError('');
    setShowModal(true);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      if (editingId) {
        await onUpdate(editingId, form);
      } else {
        await onCreate(form);
      }
      setShowModal(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar cadastro.');
    }
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Deseja excluir este cadastro?')) return;
    try {
      await onDelete(id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir cadastro.');
    }
  }

  return (
    <div className="feegow-inventory-page">
      <header className="feegow-inventory-page-head">
        <div className="feegow-inventory-breadcrumb">
          <span>Medicamento Por Convênio</span>
          <span className="feegow-inventory-crumb-sep">/</span>
        </div>
      </header>

      <section className="feegow-inventory-panel feegow-inventory-table-card">
        <div className="feegow-inventory-panel-head">
          <h2 className="feegow-inventory-panel-title">Cadastro de Medicamento por Convênio</h2>
          {canManage ? (
            <button type="button" className="feegow-inventory-insert-btn-blue" onClick={openCreate}>
              + Inserir
            </button>
          ) : null}
        </div>
        <div className="feegow-inventory-table-wrap">
          <table className="feegow-inventory-table">
            <thead>
              <tr>
                <th>Convênios</th>
                <th>Medicamento prescrito</th>
                <th>Medicamento Referência</th>
                <th className="feegow-inventory-table-actions-col">Ações</th>
              </tr>
            </thead>
            <tbody>
              {mappings.map((mapping) => (
                <tr key={mapping.id} className="feegow-inventory-table-row-static">
                  <td>{mapping.healthInsuranceName}</td>
                  <td>{mapping.prescribedProductName}</td>
                  <td>{mapping.referenceProductName}</td>
                  <td className="feegow-inventory-table-actions">
                    <button type="button" onClick={() => openEdit(mapping)}>Editar</button>
                    {canManage ? (
                      <button type="button" onClick={() => handleDelete(mapping.id)}>Excluir</button>
                    ) : null}
                  </td>
                </tr>
              ))}
              {!loading && mappings.length === 0 ? (
                <tr>
                  <td colSpan={4} className="feegow-inventory-table-empty">
                    Nenhum cadastro encontrado.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {showPagination ? (
          <TablePagination
            page={page}
            pageSize={pageSize}
            totalCount={totalCount}
            onPageChange={onPageChange}
            loading={loading}
          />
        ) : null}
      </section>

      {showModal ? (
        <div className="feegow-inventory-modal-backdrop" role="presentation" onClick={() => setShowModal(false)}>
          <div
            className="feegow-inventory-modal"
            role="dialog"
            aria-labelledby="feegow-medication-mapping-title"
            onClick={(e) => e.stopPropagation()}
          >
            <header className="feegow-inventory-modal-head">
              <h3 id="feegow-medication-mapping-title">
                {editingId ? 'Editar cadastro' : 'Criar cadastro'}
              </h3>
              <button type="button" className="feegow-inventory-modal-close" onClick={() => setShowModal(false)}>
                ×
              </button>
            </header>
            <form className="feegow-inventory-modal-body" onSubmit={handleSubmit}>
              {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
              <div className="feegow-inventory-modal-grid">
                <label className="feegow-inventory-field">
                  <span>Medicamento Prescrito</span>
                  <select
                    required
                    value={form.prescribedProductId}
                    onChange={(e) => setForm((prev) => ({ ...prev, prescribedProductId: e.target.value }))}
                  >
                    <option value="">Selecione o medicamento prescrito</option>
                    {medications.map((product) => (
                      <option key={product.id} value={product.id}>{product.name}</option>
                    ))}
                  </select>
                </label>
                <label className="feegow-inventory-field">
                  <span>Medicamento Referência (Convênio)</span>
                  <select
                    required
                    value={form.referenceProductId}
                    onChange={(e) => setForm((prev) => ({ ...prev, referenceProductId: e.target.value }))}
                  >
                    <option value="">Selecione o medicamento referência</option>
                    {medications.map((product) => (
                      <option key={product.id} value={product.id}>{product.name}</option>
                    ))}
                  </select>
                </label>
                <label className="feegow-inventory-field feegow-inventory-field-span2">
                  <span>Convênios</span>
                  <select
                    required
                    value={form.healthInsuranceId}
                    onChange={(e) => setForm((prev) => ({ ...prev, healthInsuranceId: e.target.value }))}
                  >
                    <option value="">Selecione</option>
                    {insurances.map((insurance) => (
                      <option key={insurance.id} value={insurance.id}>{insurance.name}</option>
                    ))}
                  </select>
                </label>
              </div>
              <footer className="feegow-inventory-modal-foot">
                <button type="submit" className="feegow-inventory-modal-save-btn" disabled={saving}>
                  💾 Salvar
                </button>
              </footer>
            </form>
          </div>
        </div>
      ) : null}
    </div>
  );
}
