import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  api,
  paymentMethodLabels,
  paymentMethodValue,
  type MiscellaneousReceiptDto,
} from '../../api/client';
import { Modal } from '../Modal';
import { FilterBar } from '../FilterBar';
import { TablePagination } from '../feegow/TablePagination';
import { formatBrDate } from '../../utils/dateUtils';
import { printMiscellaneousReceipt } from '../../utils/printTemplates';

type Variant = 'hospital' | 'feegow';

const PAGE_SIZE = 50;

const emptyForm = {
  receiptDate: new Date().toISOString().slice(0, 10),
  payerName: '',
  receiverName: '',
  amount: '',
  description: '',
  paymentMethod: 2,
  reference: '',
};

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

type Props = {
  variant?: Variant;
};

export function MiscellaneousReceiptsPanel({ variant = 'hospital' }: Props) {
  const [receipts, setReceipts] = useState<MiscellaneousReceiptDto[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<MiscellaneousReceiptDto | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const load = useCallback(async (targetPage = 1) => {
    setLoading(true);
    setError('');
    try {
      const result = await api.getMiscellaneousReceipts(search, targetPage, PAGE_SIZE);
      setReceipts(result.items);
      setTotalCount(result.totalCount);
      setPage(result.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar recibos.');
    } finally {
      setLoading(false);
    }
  }, [search]);

  useEffect(() => {
    void load(1);
  }, [load]);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setShowModal(true);
    setError('');
    setSuccess('');
  }

  function openEdit(receipt: MiscellaneousReceiptDto) {
    setEditing(receipt);
    setForm({
      receiptDate: receipt.receiptDate.slice(0, 10),
      payerName: receipt.payerName,
      receiverName: receipt.receiverName,
      amount: String(receipt.amount),
      description: receipt.description,
      paymentMethod: paymentMethodValue(receipt.paymentMethod),
      reference: receipt.reference ?? '',
    });
    setShowModal(true);
    setError('');
    setSuccess('');
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const payload = {
        receiptDate: new Date(`${form.receiptDate}T12:00:00`).toISOString(),
        payerName: form.payerName.trim(),
        receiverName: form.receiverName.trim(),
        amount: Number(form.amount),
        description: form.description.trim(),
        paymentMethod: form.paymentMethod,
        reference: form.reference.trim() || undefined,
      };

      if (editing) {
        await api.updateMiscellaneousReceipt(editing.id, payload);
        setSuccess('Recibo atualizado com sucesso.');
      } else {
        await api.createMiscellaneousReceipt(payload);
        setSuccess('Recibo cadastrado com sucesso.');
      }

      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar recibo.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(receipt: MiscellaneousReceiptDto) {
    if (!window.confirm(`Excluir o recibo ${receipt.receiptNumber}?`)) return;
    setError('');
    setSuccess('');
    try {
      await api.deleteMiscellaneousReceipt(receipt.id);
      setSuccess('Recibo excluído.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir recibo.');
    }
  }

  const panelClass = variant === 'feegow' ? 'feegow-finance-page' : 'card-panel appt-panel';
  const headerClass = variant === 'feegow' ? 'feegow-finance-panel-head' : 'card-panel-header';

  return (
    <div className={panelClass} style={{ marginTop: variant === 'hospital' ? 16 : 0 }}>
      <div className={headerClass}>
        <div>
          <h3 style={{ margin: 0 }}>Recibos Diversos</h3>
          {variant === 'feegow' ? (
            <span className="feegow-finance-panel-sub">
              Emissão de recibos avulsos com impressão
            </span>
          ) : null}
        </div>
        <button className="btn" type="button" onClick={openCreate}>
          + Novo recibo
        </button>
      </div>

      {error ? <div className="alert alert-error">{error}</div> : null}
      {success ? <div className="alert alert-success">{success}</div> : null}

      <FilterBar>
        <div className="filter-field grow">
          <label htmlFor="receiptSearch">Buscar</label>
          <input
            id="receiptSearch"
            placeholder="Número, pagador, recebedor ou descrição..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="filter-field align-end">
          <button className="btn btn-secondary" type="button" onClick={() => { void load(1); }}>
            Buscar
          </button>
        </div>
      </FilterBar>

      <div className="card-panel-body" style={{ padding: 0 }}>
        {loading ? (
          <p style={{ padding: 16 }}>Carregando recibos...</p>
        ) : receipts.length === 0 ? (
          <p style={{ padding: 16 }}>Nenhum recibo diverso cadastrado.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Número</th>
                <th>Data</th>
                <th>Pagador</th>
                <th>Recebedor</th>
                <th>Valor</th>
                <th>Forma</th>
                <th>Descrição</th>
                <th>Referência</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {receipts.map((receipt) => (
                <tr key={receipt.id}>
                  <td><code>{receipt.receiptNumber}</code></td>
                  <td>{formatBrDate(receipt.receiptDate)}</td>
                  <td>{receipt.payerName}</td>
                  <td>{receipt.receiverName}</td>
                  <td>{formatCurrency(receipt.amount)}</td>
                  <td>{paymentMethodLabels[paymentMethodValue(receipt.paymentMethod)] ?? '—'}</td>
                  <td>{receipt.description}</td>
                  <td>{receipt.reference ?? '—'}</td>
                  <td>
                    <div className="table-actions">
                      <button
                        className="btn btn-sm btn-secondary"
                        type="button"
                        onClick={() => printMiscellaneousReceipt(receipt)}
                      >
                        Imprimir
                      </button>
                      <button
                        className="btn btn-sm btn-secondary"
                        type="button"
                        onClick={() => openEdit(receipt)}
                      >
                        Editar
                      </button>
                      <button
                        className="btn btn-sm btn-danger"
                        type="button"
                        onClick={() => { void handleDelete(receipt); }}
                      >
                        Excluir
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <TablePagination
          page={page}
          pageSize={PAGE_SIZE}
          totalCount={totalCount}
          onPageChange={(nextPage) => { void load(nextPage); }}
          loading={loading}
        />
      </div>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title={editing ? 'Editar recibo diverso' : 'Novo recibo diverso'}
        subtitle="Preencha os dados para emissão do recibo"
        width="lg"
      >
        <form className="form-grid" onSubmit={(e) => { void handleSubmit(e); }}>
          <div className="form-field">
            <label htmlFor="receiptDate">Data *</label>
            <input
              id="receiptDate"
              type="date"
              required
              value={form.receiptDate}
              onChange={(e) => setForm((prev) => ({ ...prev, receiptDate: e.target.value }))}
            />
          </div>
          <div className="form-field">
            <label htmlFor="paymentMethod">Forma de pagamento *</label>
            <select
              id="paymentMethod"
              required
              value={form.paymentMethod}
              onChange={(e) => setForm((prev) => ({ ...prev, paymentMethod: Number(e.target.value) }))}
            >
              {Object.entries(paymentMethodLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="payerName">Pagador *</label>
            <input
              id="payerName"
              required
              maxLength={200}
              value={form.payerName}
              onChange={(e) => setForm((prev) => ({ ...prev, payerName: e.target.value }))}
            />
          </div>
          <div className="form-field">
            <label htmlFor="receiverName">Recebedor *</label>
            <input
              id="receiverName"
              required
              maxLength={200}
              value={form.receiverName}
              onChange={(e) => setForm((prev) => ({ ...prev, receiverName: e.target.value }))}
            />
          </div>
          <div className="form-field">
            <label htmlFor="amount">Valor (R$) *</label>
            <input
              id="amount"
              type="number"
              required
              min="0.01"
              step="0.01"
              value={form.amount}
              onChange={(e) => setForm((prev) => ({ ...prev, amount: e.target.value }))}
            />
          </div>
          <div className="form-field">
            <label htmlFor="reference">Referência</label>
            <input
              id="reference"
              maxLength={120}
              value={form.reference}
              onChange={(e) => setForm((prev) => ({ ...prev, reference: e.target.value }))}
            />
          </div>
          <div className="form-field full-width">
            <label htmlFor="description">Descrição / histórico *</label>
            <textarea
              id="description"
              required
              rows={3}
              maxLength={500}
              value={form.description}
              onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
            />
          </div>
          <div className="modal-actions full-width">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>
              Cancelar
            </button>
            <button className="btn" type="submit" disabled={saving}>
              {saving ? 'Salvando...' : editing ? 'Salvar alterações' : 'Cadastrar recibo'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
