import { useMemo, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import type { ProductDto } from '../../../api/client';
import {
  DEFAULT_PRICE_TABLES,
  emptyKitItemRow,
  kitFormTotalPrice,
  type FeegowProductKitFormState,
  type FeegowProductKitItemForm,
} from './feegowProductKitForm';

type Props = {
  form: FeegowProductKitFormState;
  products: ProductDto[];
  priceTableOptions?: string[];
  onChange: (patch: Partial<FeegowProductKitFormState>) => void;
  onSubmit: (event: FormEvent) => void;
  saving?: boolean;
  editing?: boolean;
};

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function FeegowProductKitInsert({
  form,
  products,
  priceTableOptions = [],
  onChange,
  onSubmit,
  saving,
}: Props) {
  const tableOptions = useMemo(
    () => [...new Set([...DEFAULT_PRICE_TABLES, ...priceTableOptions.filter(Boolean)])],
    [priceTableOptions],
  );
  const totalPrice = kitFormTotalPrice(form);

  function patch(partial: Partial<FeegowProductKitFormState>) {
    onChange(partial);
  }

  function updateItem(key: string, partial: Partial<FeegowProductKitItemForm>) {
    patch({
      items: form.items.map((item) => (item.key === key ? { ...item, ...partial } : item)),
    });
  }

  function handleProductSelect(key: string, productId: string) {
    const product = products.find((p) => p.id === productId);
    if (!product) {
      updateItem(key, { productId: '', productName: '', productSku: '' });
      return;
    }
    const code = product.barcode ?? product.sku;
    updateItem(key, {
      productId: product.id,
      productName: product.name,
      productSku: product.sku,
      insuranceCode: code,
      unitPrice: String(product.averageSalePrice ?? 0),
    });
  }

  function addRow() {
    patch({ items: [...form.items, emptyKitItemRow()] });
  }

  function removeRow(key: string) {
    const next = form.items.filter((item) => item.key !== key);
    patch({ items: next.length > 0 ? next : [emptyKitItemRow()] });
  }

  return (
    <form className="feegow-inventory-page feegow-product-kit-form" onSubmit={onSubmit}>
      <header className="feegow-product-workspace-head">
        <div className="feegow-inventory-breadcrumb">
          <span>Kits de Produtos</span>
          <span className="feegow-inventory-crumb-sep">/</span>
        </div>
        <div className="feegow-product-workspace-toolbar">
          <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Anterior">‹</button>
          <Link to="/estoque/kits" className="feegow-product-workspace-tool-btn" title="Listar" aria-label="Listar">
            ☰
          </Link>
          <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Próximo">›</button>
          <Link to="/estoque/kits/inserir" className="feegow-product-workspace-tool-btn" title="Novo" aria-label="Novo">
            +
          </Link>
          <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Histórico">↺</button>
          <button type="submit" className="feegow-product-workspace-save-btn" disabled={saving}>
            💾 SALVAR
          </button>
        </div>
      </header>

      <section className="feegow-inventory-panel feegow-product-form-card feegow-product-kit-section">
        <h2 className="feegow-inventory-panel-title">Dados do Kit</h2>
        <div className="feegow-inventory-config-form-body feegow-product-kit-grid">
          <label className="feegow-inventory-field feegow-inventory-field-grow2">
            <span>Nome *</span>
            <input
              required
              value={form.name}
              onChange={(e) => patch({ name: e.target.value })}
              placeholder="Ex.: Kit consulta ginecológica"
            />
          </label>
          <label className="feegow-inventory-field">
            <span>Tabela</span>
            <input
              list="feegow-kit-price-tables"
              value={form.priceTable}
              onChange={(e) => patch({ priceTable: e.target.value })}
              placeholder="Convênio ou tabela de preço"
            />
            <datalist id="feegow-kit-price-tables">
              {tableOptions.map((option) => (
                <option key={option} value={option} />
              ))}
            </datalist>
          </label>
        </div>
      </section>

      <section className="feegow-inventory-panel feegow-product-form-card feegow-product-kit-section">
        <div className="feegow-inventory-panel-head">
          <h2 className="feegow-inventory-panel-title">Produtos do kit</h2>
          <button type="button" className="feegow-inventory-add-rule-btn" onClick={addRow}>
            + Adicionar produto
          </button>
        </div>

        <div className="feegow-inventory-table-wrap feegow-product-kit-items-wrap">
          <table className="feegow-inventory-table feegow-product-kit-items-table">
              <thead>
                <tr>
                  <th>Quantidade</th>
                  <th>Produto</th>
                  <th>Código</th>
                  <th>Valor unitário</th>
                  <th>Valor variável</th>
                  <th className="feegow-inventory-table-actions-col" aria-label="Ações" />
                </tr>
              </thead>
              <tbody>
                {form.items.map((item) => (
                  <tr key={item.key}>
                    <td>
                      <input
                        className="feegow-product-kit-cell-input"
                        required={Boolean(item.productId)}
                        inputMode="decimal"
                        value={item.quantity}
                        onChange={(e) => updateItem(item.key, { quantity: e.target.value })}
                      />
                    </td>
                    <td>
                      <select
                        className="feegow-product-kit-cell-select"
                        required
                        value={item.productId}
                        onChange={(e) => handleProductSelect(item.key, e.target.value)}
                      >
                        <option value="">Selecione</option>
                        {products.map((product) => (
                          <option key={product.id} value={product.id}>
                            {product.name}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        className="feegow-product-kit-cell-input"
                        value={item.insuranceCode}
                        onChange={(e) => updateItem(item.key, { insuranceCode: e.target.value })}
                        placeholder="Código"
                      />
                    </td>
                    <td>
                      <input
                        className="feegow-product-kit-cell-input"
                        required={Boolean(item.productId)}
                        inputMode="decimal"
                        value={item.unitPrice}
                        onChange={(e) => updateItem(item.key, { unitPrice: e.target.value })}
                      />
                    </td>
                    <td className="feegow-product-kit-variable-cell">
                      <label className="feegow-check-pill">
                        <input
                          type="checkbox"
                          checked={item.variablePrice}
                          onChange={(e) => updateItem(item.key, { variablePrice: e.target.checked })}
                        />
                        <span>{item.variablePrice ? 'SIM' : 'NÃO'}</span>
                      </label>
                    </td>
                    <td className="feegow-inventory-table-actions">
                      <button type="button" onClick={() => removeRow(item.key)} title="Remover linha">
                        Excluir
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={3} className="feegow-product-kit-total-label">
                    Valor total estimado
                  </td>
                  <td colSpan={3} className="feegow-product-kit-total-value">
                    {formatCurrency(totalPrice)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
      </section>
    </form>
  );
}
