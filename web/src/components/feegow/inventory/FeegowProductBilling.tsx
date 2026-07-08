import { useState, type FormEvent } from 'react';
import { formatBrDate } from '../../../utils/dateUtils';
import type {
  CreateProductBillingRuleRequest,
  ProductBillingRuleDto,
  UpdateProductBillingRuleRequest,
} from '../../../api/client';

type Props = {
  productId?: string;
  itemName?: string;
  rules: ProductBillingRuleDto[];
  priceTableOptions: string[];
  tableFilter: string;
  statusFilter: '' | 'ativo' | 'inativo';
  loading?: boolean;
  onFilterChange: (patch: { tableFilter?: string; statusFilter?: '' | 'ativo' | 'inativo' }) => void;
  onCreate: (payload: CreateProductBillingRuleRequest) => Promise<void>;
  onUpdate: (ruleId: string, payload: UpdateProductBillingRuleRequest) => Promise<void>;
  onDelete: (ruleId: string) => Promise<void>;
};

const emptyRule = (): CreateProductBillingRuleRequest => ({
  priceTable: '',
  referenceTable: '',
  code: '',
  pricePfb: 0,
  pmc: 0,
  edition: '',
  validFrom: '',
  validTo: '',
  isActive: true,
});

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function FeegowProductBilling({
  productId,
  itemName,
  rules,
  priceTableOptions,
  tableFilter,
  statusFilter,
  loading,
  onFilterChange,
  onCreate,
  onUpdate,
  onDelete,
}: Props) {
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateProductBillingRuleRequest>(emptyRule());
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  if (!productId) {
    return (
      <section className="feegow-inventory-panel feegow-inventory-billing-panel feegow-inventory-empty-panel">
        <p className="feegow-inventory-billing-item">
          Item: <strong>—</strong>
        </p>
        <p>Salve o produto na aba Cadastro para configurar regras de faturamento.</p>
      </section>
    );
  }

  function openCreateModal() {
    setEditingId(null);
    setForm({ ...emptyRule(), priceTable: tableFilter });
    setError('');
    setShowModal(true);
  }

  function openEditModal(rule: ProductBillingRuleDto) {
    setEditingId(rule.id);
    setForm({
      priceTable: rule.priceTable,
      referenceTable: rule.referenceTable ?? '',
      code: rule.code ?? '',
      pricePfb: rule.pricePfb,
      pmc: rule.pmc,
      edition: rule.edition ?? '',
      validFrom: rule.validFrom ?? '',
      validTo: rule.validTo ?? '',
      isActive: rule.isActive,
    });
    setError('');
    setShowModal(true);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!form.priceTable.trim()) {
      setError('Selecione a tabela de preço.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      if (editingId) {
        await onUpdate(editingId, {
          ...form,
          isActive: form.isActive ?? true,
        });
      } else {
        await onCreate(form);
      }
      setShowModal(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar regra.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(ruleId: string) {
    if (!window.confirm('Deseja inativar esta regra de faturamento?')) return;
    try {
      await onDelete(ruleId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir regra.');
    }
  }

  return (
    <>
      <section className="feegow-inventory-panel feegow-inventory-billing-panel">
        <p className="feegow-inventory-billing-item">
          Item: <strong>{itemName?.trim() || '—'}</strong>
        </p>

        <div className="feegow-inventory-billing-filters">
          <label className="feegow-inventory-field">
            <span>Tabela</span>
            <select
              value={tableFilter}
              onChange={(e) => onFilterChange({ tableFilter: e.target.value })}
            >
              <option value="">Selecione</option>
              {priceTableOptions.map((table) => (
                <option key={table} value={table}>{table}</option>
              ))}
            </select>
          </label>
          <label className="feegow-inventory-field">
            <span>Status</span>
            <select
              value={statusFilter || 'ativo'}
              onChange={(e) => onFilterChange({
                statusFilter: e.target.value === '' ? '' : e.target.value as 'ativo' | 'inativo',
              })}
            >
              <option value="ativo">Ativo</option>
              <option value="inativo">Inativo</option>
              <option value="">Todos</option>
            </select>
          </label>
          <button type="button" className="feegow-inventory-add-rule-btn" onClick={openCreateModal}>
            + Adicionar regra
          </button>
        </div>

        {error && !showModal ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}

        <div className="feegow-inventory-table-wrap">
          <table className="feegow-inventory-table feegow-inventory-billing-table">
            <thead>
              <tr>
                <th>Tabela</th>
                <th>Tabela referência</th>
                <th>Código</th>
                <th>Preço (PFB)</th>
                <th>PMC</th>
                <th>Edição</th>
                <th>Início da Vigência</th>
                <th>Fim da Vigência</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {rules.map((rule) => (
                <tr
                  key={rule.id}
                  className="feegow-inventory-billing-row"
                  onClick={() => openEditModal(rule)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') openEditModal(rule);
                  }}
                  tabIndex={0}
                  title="Clique para editar"
                >
                  <td>{rule.priceTable}</td>
                  <td>{rule.referenceTable || '—'}</td>
                  <td>{rule.code || '—'}</td>
                  <td>{formatCurrency(rule.pricePfb)}</td>
                  <td>{formatCurrency(rule.pmc)}</td>
                  <td>{rule.edition || '—'}</td>
                  <td>{formatBrDate(rule.validFrom)}</td>
                  <td>{formatBrDate(rule.validTo)}</td>
                  <td>{rule.isActive ? 'Ativo' : 'Inativo'}</td>
                </tr>
              ))}
              {!loading && rules.length === 0 ? (
                <tr>
                  <td colSpan={9} className="feegow-inventory-table-empty">
                    Nenhuma regra encontrada.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>

      {showModal ? (
        <div className="feegow-inventory-modal-backdrop" role="presentation" onClick={() => setShowModal(false)}>
          <div
            className="feegow-inventory-modal"
            role="dialog"
            aria-labelledby="feegow-billing-modal-title"
            onClick={(e) => e.stopPropagation()}
          >
            <header className="feegow-inventory-modal-head">
              <h3 id="feegow-billing-modal-title">{editingId ? 'Editar regra' : 'Adicionar regra'}</h3>
              <button type="button" className="feegow-inventory-modal-close" onClick={() => setShowModal(false)}>×</button>
            </header>
            <form className="feegow-inventory-modal-body" onSubmit={handleSubmit}>
              {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
              <div className="feegow-inventory-modal-grid">
                <label className="feegow-inventory-field">
                  <span>Tabela *</span>
                  <select
                    required
                    value={form.priceTable}
                    onChange={(e) => setForm((prev) => ({ ...prev, priceTable: e.target.value }))}
                  >
                    <option value="">Selecione</option>
                    {priceTableOptions.map((table) => (
                      <option key={table} value={table}>{table}</option>
                    ))}
                  </select>
                </label>
                <label className="feegow-inventory-field">
                  <span>Tabela referência</span>
                  <input
                    value={form.referenceTable ?? ''}
                    onChange={(e) => setForm((prev) => ({ ...prev, referenceTable: e.target.value }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>Código</span>
                  <input
                    value={form.code ?? ''}
                    onChange={(e) => setForm((prev) => ({ ...prev, code: e.target.value }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>Preço (PFB)</span>
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    value={form.pricePfb}
                    onChange={(e) => setForm((prev) => ({ ...prev, pricePfb: Number(e.target.value) }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>PMC</span>
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    value={form.pmc}
                    onChange={(e) => setForm((prev) => ({ ...prev, pmc: Number(e.target.value) }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>Edição</span>
                  <input
                    value={form.edition ?? ''}
                    onChange={(e) => setForm((prev) => ({ ...prev, edition: e.target.value }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>Início da vigência</span>
                  <input
                    type="date"
                    value={form.validFrom ?? ''}
                    onChange={(e) => setForm((prev) => ({ ...prev, validFrom: e.target.value }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>Fim da vigência</span>
                  <input
                    type="date"
                    value={form.validTo ?? ''}
                    onChange={(e) => setForm((prev) => ({ ...prev, validTo: e.target.value }))}
                  />
                </label>
                <label className="feegow-inventory-field">
                  <span>Status</span>
                  <select
                    value={form.isActive === false ? 'inativo' : 'ativo'}
                    onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.value === 'ativo' }))}
                  >
                    <option value="ativo">Ativo</option>
                    <option value="inativo">Inativo</option>
                  </select>
                </label>
              </div>
              <footer className="feegow-inventory-modal-foot feegow-inventory-modal-foot-split">
                {editingId ? (
                  <button
                    type="button"
                    className="feegow-inventory-modal-danger"
                    onClick={() => handleDelete(editingId)}
                  >
                    Excluir
                  </button>
                ) : <span />}
                <div className="feegow-inventory-modal-foot-actions">
                  <button type="button" className="feegow-inventory-modal-cancel" onClick={() => setShowModal(false)}>
                    Cancelar
                  </button>
                  <button type="submit" className="feegow-inventory-modal-save-btn" disabled={saving}>
                    💾 Salvar
                  </button>
                </div>
              </footer>
            </form>
          </div>
        </div>
      ) : null}
    </>
  );
}
