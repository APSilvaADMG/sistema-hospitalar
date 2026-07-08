import { useState, type FormEvent } from 'react';
import type { StockInboundRequest } from '../../../api/client';

type InboundForm = Omit<StockInboundRequest, 'productId'>;

type Props = {
  open: boolean;
  locationOptions: string[];
  defaultLocation?: string;
  userName?: string;
  saving?: boolean;
  onClose: () => void;
  onSubmit: (payload: InboundForm) => Promise<void>;
};

const emptyInbound = (): InboundForm => ({
  quantity: 1,
  reason: 'Entrada de estoque',
  patientOrSupplier: '',
  responsibleName: '',
  userName: '',
  batchNumber: '',
  individualCode: '',
  location: '',
  expiryDate: '',
  invoiceNumber: '',
  unitPrice: 0,
  account: '',
});

export function FeegowProductMovementEntryModal({
  open,
  locationOptions,
  defaultLocation,
  userName,
  saving,
  onClose,
  onSubmit,
}: Props) {
  const [form, setForm] = useState<InboundForm>(() => ({
    ...emptyInbound(),
    location: defaultLocation ?? '',
    userName: userName ?? '',
  }));
  const [error, setError] = useState('');

  if (!open) return null;

  function handleClose() {
    setForm({ ...emptyInbound(), location: defaultLocation ?? '', userName: userName ?? '' });
    setError('');
    onClose();
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      if (form.quantity <= 0) {
        setError('Informe uma quantidade válida.');
        return;
      }
      await onSubmit({
        ...form,
        reason: form.reason.trim() || 'Entrada de estoque',
        expiryDate: form.expiryDate || undefined,
        unitPrice: form.unitPrice && form.unitPrice > 0 ? form.unitPrice : undefined,
      });
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar entrada.');
    }
  }

  return (
    <div className="feegow-inventory-modal-backdrop" role="presentation" onClick={handleClose}>
      <div
        className="feegow-inventory-modal"
        role="dialog"
        aria-labelledby="feegow-movement-modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="feegow-inventory-modal-head">
          <h3 id="feegow-movement-modal-title">Registrar entrada</h3>
          <button type="button" className="feegow-inventory-modal-close" onClick={handleClose}>×</button>
        </header>
        <form className="feegow-inventory-modal-body" onSubmit={handleSubmit}>
          {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
          <div className="feegow-inventory-modal-grid">
            <label className="feegow-inventory-field">
              <span>Quantidade *</span>
              <input
                type="number"
                min={0.001}
                step="0.001"
                required
                value={form.quantity}
                onChange={(e) => setForm((prev) => ({ ...prev, quantity: Number(e.target.value) }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Paciente/Fornecedor</span>
              <input
                value={form.patientOrSupplier ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, patientOrSupplier: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Responsável</span>
              <input
                value={form.responsibleName ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, responsibleName: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Usuário</span>
              <input
                value={form.userName ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, userName: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Lote</span>
              <input
                value={form.batchNumber ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, batchNumber: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Código individual</span>
              <input
                value={form.individualCode ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, individualCode: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Localização</span>
              <select
                value={form.location ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, location: e.target.value }))}
              >
                <option value="">Selecione</option>
                {locationOptions.map((location) => (
                  <option key={location} value={location}>{location}</option>
                ))}
              </select>
            </label>
            <label className="feegow-inventory-field">
              <span>Validade</span>
              <input
                type="date"
                value={form.expiryDate ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, expiryDate: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>NF</span>
              <input
                value={form.invoiceNumber ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, invoiceNumber: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Valor unitário</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={form.unitPrice ?? 0}
                onChange={(e) => setForm((prev) => ({ ...prev, unitPrice: Number(e.target.value) }))}
              />
            </label>
            <label className="feegow-inventory-field">
              <span>Conta</span>
              <input
                value={form.account ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, account: e.target.value }))}
              />
            </label>
            <label className="feegow-inventory-field feegow-inventory-field-span2">
              <span>Motivo</span>
              <input
                value={form.reason}
                onChange={(e) => setForm((prev) => ({ ...prev, reason: e.target.value }))}
              />
            </label>
          </div>
          <footer className="feegow-inventory-modal-foot">
            <button type="button" className="feegow-inventory-modal-cancel" onClick={handleClose}>
              Cancelar
            </button>
            <button type="submit" className="feegow-inventory-modal-save-btn" disabled={saving}>
              💾 Salvar
            </button>
          </footer>
        </form>
      </div>
    </div>
  );
}
