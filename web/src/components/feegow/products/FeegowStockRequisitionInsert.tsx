import { Link } from 'react-router-dom';
import { useMemo, type FormEvent } from 'react';
import type { ProductDto, UserListDto } from '../../../api/client';
import { stockRequisitionStatusLabels } from '../../../api/client';
import { FeegowRequisitionInteractions } from './FeegowRequisitionInteractions';
import {
  DEFAULT_DESTINATION_LOCATIONS,
  emptyRequisitionItemRow,
  FEEGOW_REQUISITION_PRIORITY_OPTIONS,
  formatCurrencyInput,
  itemLineTotal,
  type FeegowStockRequisitionFormState,
  type FeegowStockRequisitionItemForm,
} from './feegowStockRequisitionForm';

type Props = {
  form: FeegowStockRequisitionFormState;
  products: ProductDto[];
  users: UserListDto[];
  sequenceNumber?: number;
  createdAt?: string;
  createdBy?: string;
  readOnly?: boolean;
  canManageWarehouse?: boolean;
  onChange: (patch: Partial<FeegowStockRequisitionFormState>) => void;
  onSubmit: (event: FormEvent) => void;
  onApprove?: () => void;
  onFulfill?: () => void;
  onCancel?: () => void;
  onDeny?: () => void;
  saving?: boolean;
  acting?: boolean;
};

export function FeegowStockRequisitionInsert({
  form,
  products,
  users,
  sequenceNumber,
  createdAt,
  createdBy,
  readOnly,
  canManageWarehouse,
  onChange,
  onSubmit,
  onApprove,
  onFulfill,
  onCancel,
  onDeny,
  saving,
  acting,
}: Props) {
  const destinationOptions = useMemo(
    () => [...new Set([...DEFAULT_DESTINATION_LOCATIONS, form.destinationLocation].filter(Boolean))],
    [form.destinationLocation],
  );
  const userNames = useMemo(
    () => [...new Set([...users.map((u) => u.fullName), form.requestedBy, form.recipientName].filter(Boolean))],
    [users, form.requestedBy, form.recipientName],
  );

  function patch(partial: Partial<FeegowStockRequisitionFormState>) {
    onChange(partial);
  }

  function updateItem(key: string, partial: Partial<FeegowStockRequisitionItemForm>) {
    patch({
      items: form.items.map((item) => (item.key === key ? { ...item, ...partial } : item)),
    });
  }

  function handleProductSelect(key: string, productId: string) {
    const product = products.find((p) => p.id === productId);
    if (!product) {
      updateItem(key, {
        productId: '',
        productName: '',
        productSku: '',
        productUnit: '',
        quantityOnHand: 0,
      });
      return;
    }
    updateItem(key, {
      productId: product.id,
      productName: product.name,
      productSku: product.sku,
      productUnit: product.unit,
      quantityOnHand: product.quantityOnHand,
      unitPrice: formatCurrencyInput(product.averageSalePrice ?? 0),
    });
  }

  function addRow() {
    patch({ items: [...form.items, emptyRequisitionItemRow()] });
  }

  function removeRow(key: string) {
    const next = form.items.filter((item) => item.key !== key);
    patch({ items: next.length > 0 ? next : [emptyRequisitionItemRow()] });
  }

  return (
    <div className="feegow-requisition-page feegow-requisition-insert-page">
      <header className="feegow-requisition-insert-head">
        <div className="feegow-requisition-insert-title-wrap">
          <h1 className="feegow-requisition-page-title">Requisição de estoque</h1>
          <span className="feegow-requisition-insert-slash">/</span>
          <div className="feegow-requisition-insert-toolbar-icons">
            <button type="button" className="feegow-requisition-icon-btn" title="Imprimir">🖨</button>
            <Link to="/estoque/requisicoes" className="feegow-requisition-icon-btn" title="Listar">☰</Link>
            {!readOnly ? (
              <button type="button" className="feegow-requisition-icon-btn" title="Adicionar produto" onClick={addRow}>+</button>
            ) : null}
          </div>
        </div>
      </header>

      <div className="feegow-requisition-insert-layout">
        <div className="feegow-requisition-insert-main">
          <form onSubmit={onSubmit}>
            <section className="feegow-requisition-panel">
              <header className="feegow-requisition-panel-head">
                <h2>Requisição #{sequenceNumber ?? '—'}</h2>
                <div className="feegow-requisition-panel-actions">
                  {canManageWarehouse && form.status === 1 && onApprove ? (
                    <button type="button" className="feegow-requisition-secondary-btn" disabled={acting} onClick={onApprove}>
                      Aprovar
                    </button>
                  ) : null}
                  {canManageWarehouse && form.status === 2 && onFulfill ? (
                    <button type="button" className="feegow-requisition-secondary-btn" disabled={acting} onClick={onFulfill}>
                      Atender
                    </button>
                  ) : null}
                  {canManageWarehouse && (form.status === 1 || form.status === 2) && onDeny ? (
                    <button type="button" className="feegow-requisition-secondary-btn feegow-warehouse-deny-btn" disabled={acting} onClick={onDeny}>
                      Negar
                    </button>
                  ) : null}
                  {(form.status === 1 || form.status === 2) && onCancel ? (
                    <button type="button" className="feegow-requisition-secondary-btn feegow-requisition-secondary-btn-danger" disabled={acting} onClick={onCancel}>
                      Cancelar
                    </button>
                  ) : null}
                  {!readOnly ? (
                    <button type="submit" className="feegow-requisition-save-btn" disabled={saving || acting}>
                      💾 SALVAR
                    </button>
                  ) : null}
                </div>
              </header>

              <div className="feegow-requisition-panel-body">
                <div className="feegow-requisition-form-grid feegow-requisition-form-grid-3">
                  <label className="feegow-requisition-field">
                    <span>Solicitante</span>
                    <select
                      required
                      disabled={readOnly}
                      value={form.requestedBy}
                      onChange={(e) => patch({ requestedBy: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {userNames.map((name) => (
                        <option key={name} value={name}>{name}</option>
                      ))}
                    </select>
                  </label>

                  <label className="feegow-requisition-field">
                    <span>Prioridade</span>
                    <select
                      disabled={readOnly}
                      value={form.priority}
                      onChange={(e) => patch({ priority: Number(e.target.value) })}
                    >
                      {FEEGOW_REQUISITION_PRIORITY_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </label>

                  <label className="feegow-requisition-field">
                    <span>Status</span>
                    <select disabled value={form.status}>
                      {Object.entries(stockRequisitionStatusLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </label>

                  <label className="feegow-requisition-field">
                    <span>Localização destino</span>
                    <select
                      disabled={readOnly}
                      value={form.destinationLocation}
                      onChange={(e) => patch({ destinationLocation: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {destinationOptions.map((option) => (
                        <option key={option} value={option}>{option}</option>
                      ))}
                    </select>
                  </label>

                  <label className="feegow-requisition-field">
                    <span>Data Prazo</span>
                    <div className="feegow-requisition-date-wrap">
                      <span className="feegow-requisition-date-icon" aria-hidden>📅</span>
                      <input
                        type="date"
                        disabled={readOnly}
                        value={form.dueDate}
                        onChange={(e) => patch({ dueDate: e.target.value })}
                      />
                    </div>
                  </label>

                  <label className="feegow-requisition-field">
                    <span>
                      Destinatário
                      <span className="feegow-req">*</span>
                    </span>
                    <select
                      required
                      disabled={readOnly}
                      value={form.recipientName}
                      onChange={(e) => patch({ recipientName: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {userNames.map((name) => (
                        <option key={`dest-${name}`} value={name}>{name}</option>
                      ))}
                    </select>
                  </label>
                </div>
              </div>
            </section>

            <section className="feegow-requisition-panel feegow-requisition-products-panel">
              <header className="feegow-requisition-panel-head">
                <h2>Produtos</h2>
                {!readOnly ? (
                  <button type="button" className="feegow-requisition-add-line-btn" onClick={addRow} title="Adicionar produto">
                    +
                  </button>
                ) : null}
              </header>

              <div className="feegow-requisition-products-stack">
                {form.items.map((item) => (
                  <article key={item.key} className="feegow-requisition-product-block">
                    <div className="feegow-requisition-form-grid feegow-requisition-form-grid-4">
                      <label className="feegow-requisition-field feegow-requisition-field-grow2">
                        <span>
                          Produto
                          <span className="feegow-req">*</span>
                        </span>
                        {readOnly ? (
                          <input readOnly value={item.productName} />
                        ) : (
                          <select
                            required
                            value={item.productId}
                            onChange={(e) => handleProductSelect(item.key, e.target.value)}
                          >
                            <option value="">Selecione</option>
                            {products.map((product) => (
                              <option key={product.id} value={product.id}>{product.name}</option>
                            ))}
                          </select>
                        )}
                      </label>

                      <label className="feegow-requisition-field">
                        <span>Quantidade</span>
                        <input
                          readOnly={readOnly}
                          value={item.quantity}
                          onChange={(e) => updateItem(item.key, { quantity: e.target.value })}
                        />
                      </label>

                      <label className="feegow-requisition-field">
                        <span>Unidade de Medida</span>
                        <input readOnly value={item.productUnit || '—'} />
                      </label>

                      <label className="feegow-requisition-field">
                        <span>Status</span>
                        <select
                          disabled={readOnly}
                          value={item.itemStatus}
                          onChange={(e) => updateItem(item.key, { itemStatus: Number(e.target.value) })}
                        >
                          {Object.entries(stockRequisitionStatusLabels).map(([value, label]) => (
                            <option key={value} value={value}>{label}</option>
                          ))}
                        </select>
                      </label>
                    </div>

                    <div className="feegow-requisition-form-grid feegow-requisition-form-grid-3">
                      <label className="feegow-requisition-field">
                        <span>Quantidade Transferida</span>
                        <input readOnly={readOnly} value={item.fulfilledQuantity} onChange={(e) => updateItem(item.key, { fulfilledQuantity: e.target.value })} />
                      </label>

                      <label className="feegow-requisition-field">
                        <span>Valor Unitário</span>
                        <div className="feegow-requisition-money-wrap">
                          <span>R$</span>
                          <input
                            readOnly={readOnly}
                            value={item.unitPrice}
                            onChange={(e) => updateItem(item.key, { unitPrice: e.target.value })}
                          />
                        </div>
                      </label>

                      <label className="feegow-requisition-field">
                        <span>Valor Total</span>
                        <div className="feegow-requisition-money-wrap">
                          <span>R$</span>
                          <input readOnly value={formatCurrencyInput(itemLineTotal(item))} />
                        </div>
                      </label>
                    </div>

                    <div className="feegow-requisition-notes-row">
                      <label className="feegow-requisition-field feegow-requisition-field-grow">
                        <span>Observações</span>
                        <textarea
                          readOnly={readOnly}
                          rows={2}
                          value={item.notes}
                          onChange={(e) => updateItem(item.key, { notes: e.target.value })}
                        />
                      </label>
                      {!readOnly ? (
                        <button
                          type="button"
                          className="feegow-requisition-delete-line-btn"
                          onClick={() => removeRow(item.key)}
                          title="Remover produto"
                        >
                          🗑
                        </button>
                      ) : null}
                    </div>
                  </article>
                ))}
              </div>
            </section>
          </form>

          {createdBy && createdAt ? (
            <p className="feegow-requisition-audit">
              Requisição criada por {createdBy} - {createdAt}
            </p>
          ) : null}
        </div>

        <FeegowRequisitionInteractions />
      </div>
    </div>
  );
}
