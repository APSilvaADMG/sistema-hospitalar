import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  api,
  financialCategoryLabel,
  financialCategoryLabels,
  financialCategoryValue,
  type PatientDto,
  type PayableCategoryPresetDto,
  type SupplierDto,
} from '../../../api/client';
import { addDaysIso } from '../../../utils/dateUtils';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';
import { feegowFinanceInsertPath, feegowFinanceListPath } from './feegowFinanceNav';

type Props = {
  direction: 1 | 2;
};

type ItemRow = {
  id: string;
  description: string;
  amount: string;
};

function newItem(): ItemRow {
  return { id: crypto.randomUUID(), description: '', amount: '' };
}

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function FeegowFinanceAccountInsert({ direction }: Props) {
  const navigate = useNavigate();
  const kind = direction === 2 ? 'pagar' : 'receber';
  const title = direction === 2 ? 'Contas a Pagar' : 'Contas a Receber';
  const isPayable = direction === 2;

  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [payablePresets, setPayablePresets] = useState<PayableCategoryPresetDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [supplierId, setSupplierId] = useState('');
  const [counterpartyName, setCounterpartyName] = useState('');
  const [patientId, setPatientId] = useState('');
  const [patientSearch, setPatientSearch] = useState('');
  const [dueDate, setDueDate] = useState(() => addDaysIso(7));
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [notes, setNotes] = useState('');
  const [category, setCategory] = useState(isPayable ? 7 : 1);
  const [items, setItems] = useState<ItemRow[]>([newItem()]);
  const [installmentCount, setInstallmentCount] = useState(1);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (isPayable) {
      Promise.all([api.getSuppliers(), api.getPayableCategoryPresets()])
        .then(([supplierList, presets]) => {
          setSuppliers(supplierList);
          setPayablePresets(presets);
          if (presets[0]) {
            setCategory(financialCategoryValue(presets[0].category));
          }
        })
        .catch(() => {});
    }
  }, [isPayable]);

  const searchPatients = useCallback(async (term: string) => {
    try {
      const result = await api.getPatients(term, 1);
      setPatients(result.items);
    } catch {
      setPatients([]);
    }
  }, []);

  useEffect(() => {
    if (!isPayable) {
      const timer = window.setTimeout(() => {
        void searchPatients(patientSearch);
      }, 300);
      return () => window.clearTimeout(timer);
    }
    return undefined;
  }, [isPayable, patientSearch, searchPatients]);

  const itemsTotal = useMemo(
    () => items.reduce((sum, item) => sum + (Number(item.amount) || 0), 0),
    [items],
  );

  const installmentValue = installmentCount > 0 ? itemsTotal / installmentCount : itemsTotal;

  function updateItem(id: string, patch: Partial<ItemRow>) {
    setItems((rows) => rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }

  function addItem() {
    setItems((rows) => [...rows, newItem()]);
  }

  function removeItem(id: string) {
    setItems((rows) => (rows.length <= 1 ? rows : rows.filter((row) => row.id !== id)));
  }

  async function handleSave() {
    const description = items.map((i) => i.description.trim()).filter(Boolean).join('; ')
      || (isPayable ? 'Despesa' : 'Receita');
    const amount = itemsTotal;

    if (isPayable && !supplierId && !counterpartyName.trim()) {
      setError('Informe o fornecedor ou favorecido.');
      return;
    }
    if (!isPayable && !patientId) {
      setError('Selecione o paciente.');
      return;
    }
    if (!amount || amount <= 0) {
      setError('Informe ao menos um item com valor válido.');
      return;
    }

    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const lineItemPayload = items
        .filter((item) => item.description.trim() && Number(item.amount) > 0)
        .map((item) => ({
          description: item.description.trim(),
          quantity: 1,
          unitAmount: Number(item.amount),
        }));

      await api.createFinancialAccount({
        direction,
        patientId: !isPayable ? patientId : undefined,
        supplierId: isPayable ? (supplierId || undefined) : undefined,
        counterpartyName: isPayable ? (counterpartyName.trim() || undefined) : undefined,
        category,
        description,
        amount,
        dueDate: dueDate ? new Date(`${dueDate}T12:00:00`).toISOString() : undefined,
        notes: notes.trim() || undefined,
        invoiceNumber: invoiceNumber.trim() || undefined,
        installmentCount: installmentCount > 1 ? installmentCount : undefined,
        lineItems: lineItemPayload.length > 0 ? lineItemPayload : undefined,
        expectedPaymentMethod: isPayable ? 5 : 2,
      });

      setSuccess(isPayable ? 'Conta a pagar cadastrada.' : 'Conta a receber cadastrada.');
      window.setTimeout(() => navigate(feegowFinanceListPath(kind)), 600);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar conta.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead
        title={title}
        listPath={feegowFinanceListPath(kind)}
        insertPath={feegowFinanceInsertPath(kind)}
        onSave={() => void handleSave()}
        saving={saving}
      />

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}

      <form className="feegow-finance-insert-form" onSubmit={(e) => { e.preventDefault(); void handleSave(); }}>
        <section className="feegow-finance-panel">
          <div className="feegow-finance-form-grid">
            {isPayable ? (
              <>
                <label>
                  Pagar a
                  <select value={supplierId} onChange={(e) => setSupplierId(e.target.value)}>
                    <option value="">Selecione fornecedor</option>
                    {suppliers.map((s) => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>
                </label>
                <label>
                  Favorecido
                  <input
                    type="text"
                    value={counterpartyName}
                    onChange={(e) => setCounterpartyName(e.target.value)}
                    placeholder="Nome do favorecido"
                  />
                </label>
              </>
            ) : (
              <label className="feegow-finance-field-wide">
                Paciente
                <input
                  type="search"
                  value={patientSearch}
                  onChange={(e) => setPatientSearch(e.target.value)}
                  placeholder="Buscar paciente..."
                />
                <select value={patientId} onChange={(e) => setPatientId(e.target.value)} required>
                  <option value="">Selecione</option>
                  {patients.map((p) => (
                    <option key={p.id} value={p.id}>{p.fullName}</option>
                  ))}
                </select>
              </label>
            )}
            <label>
              Data
              <input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} required />
            </label>
            <label>
              N. Fiscal
              <input
                type="text"
                value={invoiceNumber}
                onChange={(e) => setInvoiceNumber(e.target.value)}
                placeholder="Número da nota"
              />
            </label>
            <label>
              Categoria
              <select
                value={category}
                onChange={(e) => setCategory(Number(e.target.value))}
              >
                {isPayable
                  ? payablePresets.map((preset) => {
                      const value = financialCategoryValue(preset.category);
                      return (
                        <option key={value} value={value}>
                          {financialCategoryLabel(preset.category)}
                        </option>
                      );
                    })
                  : [1, 2, 3, 4, 5, 6].map((value) => (
                      <option key={value} value={value}>
                        {financialCategoryLabels[value]}
                      </option>
                    ))}
              </select>
            </label>
            <label className="feegow-finance-field-wide">
              Observações
              <textarea
                rows={2}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Observações adicionais"
              />
            </label>
          </div>
        </section>

        <section className="feegow-finance-panel">
          <header className="feegow-finance-panel-head">
            <h3>Itens</h3>
            <button type="button" className="feegow-finance-link-btn" onClick={addItem}>
              + Adicionar item
            </button>
          </header>
          <div className="feegow-finance-items-table-wrap">
            <table className="feegow-finance-items-table">
              <thead>
                <tr>
                  <th>Descrição</th>
                  <th>Valor</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <input
                        type="text"
                        value={item.description}
                        onChange={(e) => updateItem(item.id, { description: e.target.value })}
                        placeholder="Descrição do item"
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={item.amount}
                        onChange={(e) => updateItem(item.id, { amount: e.target.value })}
                        placeholder="0,00"
                      />
                    </td>
                    <td>
                      <button type="button" className="feegow-finance-link-btn" onClick={() => removeItem(item.id)}>
                        Remover
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td><strong>Total</strong></td>
                  <td colSpan={2}><strong>{formatCurrency(itemsTotal)}</strong></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </section>

        <section className="feegow-finance-panel feegow-finance-installments">
          <header className="feegow-finance-panel-head">
            <h3>Parcelas</h3>
          </header>
          <div className="feegow-finance-installments-row">
            <label>
              Quantidade
              <input
                type="number"
                min={1}
                max={48}
                value={installmentCount}
                onChange={(e) => setInstallmentCount(Math.max(1, Number(e.target.value) || 1))}
              />
            </label>
            <p>
              Valor por parcela: <strong>{formatCurrency(installmentValue)}</strong>
            </p>
          </div>
        </section>
      </form>
    </div>
  );
}
