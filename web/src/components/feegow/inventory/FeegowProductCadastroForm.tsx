import { useRef, type FormEvent, type ReactNode } from 'react';
import { formatBrDate } from '../../../utils/dateUtils';
import {
  DEFAULT_PRESENTATIONS,
  DEFAULT_UNITS,
  FEEGOW_PRODUCT_TYPE_OPTIONS,
  type FeegowProductFormState,
} from '../products/feegowProductForm';
import {
  buildStockPositionRows,
  stockPositionTotals,
  type FeegowStockPositionRow,
} from './feegowStockPosition';
import type { StockMovementDto } from '../../../api/client';

type Props = {
  form: FeegowProductFormState;
  categoryOptions: string[];
  manufacturerOptions: string[];
  locationOptions: string[];
  movements?: StockMovementDto[];
  quantityOnHand?: number;
  onChange: (patch: Partial<FeegowProductFormState>) => void;
  onSubmit: (event: FormEvent) => void;
  onOpenEntry?: () => void;
  editing?: boolean;
};

function formatCurrency(value?: number): string {
  if (value == null || value <= 0) return '—';
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function FieldLabel({ children, required }: { children: ReactNode; required?: boolean }) {
  return (
    <span className="feegow-product-cadastro-label">
      {children}
      {required ? <span className="feegow-req">*</span> : null}
    </span>
  );
}

export function FeegowProductCadastroForm({
  form,
  categoryOptions,
  manufacturerOptions,
  locationOptions,
  movements = [],
  quantityOnHand = 0,
  onChange,
  onSubmit,
  onOpenEntry,
  editing,
}: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
  const stockRows: FeegowStockPositionRow[] = buildStockPositionRows(movements);
  const contentQty = Number(form.contentQuantity) || 1;
  const totals = stockPositionTotals(quantityOnHand, contentQty);

  function patch(partial: Partial<FeegowProductFormState>) {
    onChange(partial);
  }

  function handlePhotoFile(file: File | undefined) {
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result;
      if (typeof result === 'string') patch({ photoData: result });
    };
    reader.readAsDataURL(file);
  }

  return (
    <form className="feegow-product-cadastro" onSubmit={onSubmit}>
      <div className="feegow-product-cadastro-top">
        <div className="feegow-product-cadastro-photo-col">
          <button
            type="button"
            className="feegow-product-cadastro-photo"
            onClick={() => fileRef.current?.click()}
          >
            {form.photoData ? (
              <img src={form.photoData} alt="" className="feegow-product-cadastro-photo-img" />
            ) : (
              <>
                <span className="feegow-product-cadastro-photo-icon" aria-hidden>☁</span>
                <span>Sem foto</span>
              </>
            )}
          </button>
          <input
            ref={fileRef}
            type="file"
            accept="image/*"
            hidden
            onChange={(e) => handlePhotoFile(e.target.files?.[0])}
          />

          <label className="feegow-product-cadastro-side-field">
            <FieldLabel>Locais de possíveis entradas</FieldLabel>
            <select
              value={form.entryLocations[0] ?? ''}
              onChange={(e) => patch({ entryLocations: e.target.value ? [e.target.value] : [] })}
            >
              <option value="">Selecione</option>
              {locationOptions.map((location) => (
                <option key={location} value={location}>{location}</option>
              ))}
            </select>
          </label>

          <label className="feegow-product-cadastro-side-field">
            <FieldLabel>Observações</FieldLabel>
            <textarea
              rows={4}
              value={form.description}
              onChange={(e) => patch({ description: e.target.value })}
            />
          </label>
        </div>

        <div className="feegow-product-cadastro-fields">
          <div className="feegow-product-cadastro-row feegow-product-cadastro-row-5">
            <label className="feegow-product-cadastro-field feegow-product-cadastro-field-grow2">
              <FieldLabel required>Nome</FieldLabel>
              <input
                required
                value={form.name}
                onChange={(e) => patch({ name: e.target.value })}
              />
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel required>Tipo</FieldLabel>
              <select
                required
                value={form.type}
                onChange={(e) => patch({ type: Number(e.target.value) })}
              >
                {FEEGOW_PRODUCT_TYPE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Código</FieldLabel>
              <input
                value={form.barcode}
                onChange={(e) => patch({ barcode: e.target.value })}
              />
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Categoria</FieldLabel>
              <select
                value={form.category}
                onChange={(e) => patch({ category: e.target.value })}
              >
                <option value="">Selecione</option>
                {categoryOptions.map((category) => (
                  <option key={category} value={category}>{category}</option>
                ))}
              </select>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Fabricante</FieldLabel>
              <select
                value={form.manufacturer}
                onChange={(e) => patch({ manufacturer: e.target.value })}
              >
                <option value="">Selecione</option>
                {manufacturerOptions.map((manufacturer) => (
                  <option key={manufacturer} value={manufacturer}>{manufacturer}</option>
                ))}
              </select>
            </label>
          </div>

          <div className="feegow-product-cadastro-row feegow-product-cadastro-row-5">
            <label className="feegow-product-cadastro-field">
              <FieldLabel required>Apresentação</FieldLabel>
              <input
                required
                list="feegow-cadastro-presentations"
                value={form.presentation}
                onChange={(e) => patch({ presentation: e.target.value })}
                placeholder="Ex: caixa, garrafa..."
              />
              <datalist id="feegow-cadastro-presentations">
                {DEFAULT_PRESENTATIONS.map((option) => (
                  <option key={option} value={option} />
                ))}
              </datalist>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel required>Contendo</FieldLabel>
              <input
                required
                inputMode="decimal"
                value={form.contentQuantity}
                onChange={(e) => patch({ contentQuantity: e.target.value })}
              />
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel required>Unidade</FieldLabel>
              <select
                required
                value={form.unit}
                onChange={(e) => patch({ unit: e.target.value })}
              >
                <option value="">Selecione</option>
                {DEFAULT_UNITS.map((unit) => (
                  <option key={unit} value={unit}>{unit}</option>
                ))}
              </select>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Localização Padrão</FieldLabel>
              <select
                value={form.defaultLocation}
                onChange={(e) => patch({ defaultLocation: e.target.value })}
              >
                <option value="">Selecione</option>
                {locationOptions.map((location) => (
                  <option key={location} value={location}>{location}</option>
                ))}
              </select>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel>CD</FieldLabel>
              <select
                value={form.tussCode}
                onChange={(e) => patch({ tussCode: e.target.value })}
              >
                <option value="">Selecione</option>
              </select>
            </label>
          </div>

          <div className="feegow-product-cadastro-row feegow-product-cadastro-row-stock">
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Estoque Mínimo</FieldLabel>
              <div className="feegow-product-cadastro-split-input">
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={form.minimumStock}
                  onChange={(e) => patch({ minimumStock: e.target.value })}
                />
                <select value={form.unit || 'UN'} disabled aria-label="Unidade mínimo">
                  <option>{form.unit || 'Unidade'}</option>
                </select>
              </div>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Estoque Máximo</FieldLabel>
              <div className="feegow-product-cadastro-split-input">
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={form.maximumStock}
                  onChange={(e) => patch({ maximumStock: e.target.value })}
                />
                <select value={form.unit || 'UN'} disabled aria-label="Unidade máximo">
                  <option>{form.unit || 'Unidade'}</option>
                </select>
              </div>
            </label>
            <label className="feegow-product-cadastro-field feegow-product-cadastro-field-days">
              <FieldLabel>Aviso de Validade</FieldLabel>
              <div className="feegow-product-cadastro-days-input">
                <input
                  type="number"
                  min={0}
                  value={form.expiryWarningDays}
                  onChange={(e) => patch({ expiryWarningDays: e.target.value })}
                />
                <span>dias</span>
              </div>
            </label>
          </div>

          <div className="feegow-product-cadastro-row feegow-product-cadastro-row-prices">
            <label className="feegow-product-cadastro-field">
              <FieldLabel>Preço Médio — Compra</FieldLabel>
              <div className="feegow-product-cadastro-price-row">
                <span className="feegow-product-cadastro-currency">R$</span>
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={form.averagePurchasePrice}
                  onChange={(e) => patch({ averagePurchasePrice: e.target.value })}
                />
                <div className="feegow-product-cadastro-radio-group">
                  <label><input type="radio" name="purchasePriceMode" defaultChecked /> por Conjunto</label>
                  <label><input type="radio" name="purchasePriceMode" /> por Unidade</label>
                </div>
              </div>
            </label>
            <label className="feegow-product-cadastro-field">
              <FieldLabel required>Preço Médio — Venda</FieldLabel>
              <div className="feegow-product-cadastro-price-row">
                <span className="feegow-product-cadastro-currency">R$</span>
                <input
                  required
                  type="number"
                  min={0}
                  step="0.01"
                  value={form.averageSalePrice}
                  onChange={(e) => patch({ averageSalePrice: e.target.value })}
                />
                <div className="feegow-product-cadastro-radio-group">
                  <label><input type="radio" name="salePriceMode" defaultChecked /> por Conjunto</label>
                  <label><input type="radio" name="salePriceMode" /> por Unidade</label>
                </div>
              </div>
            </label>
          </div>

          <label className="feegow-product-cadastro-check">
            <input
              type="checkbox"
              checked={form.allowOutboundFromRegister}
              onChange={(e) => patch({ allowOutboundFromRegister: e.target.checked })}
            />
            <span>Permitir saída pelo cadastro</span>
          </label>
        </div>
      </div>

      <section className="feegow-product-stock-position">
        <header className="feegow-product-stock-position-head">
          <h2>Posição de Estoque</h2>
          <div className="feegow-product-stock-position-actions">
            <button type="button" className="feegow-product-stock-position-icon" aria-label="Grade">⊞</button>
            <button type="button" className="feegow-product-stock-position-icon" aria-label="Atualizar">↻</button>
            <button
              type="button"
              className="feegow-product-stock-position-entry-btn"
              disabled={!editing}
              onClick={onOpenEntry}
            >
              ↓ Entrada
            </button>
          </div>
        </header>
        <div className="feegow-inventory-table-wrap">
          <table className="feegow-inventory-table feegow-product-stock-position-table">
            <thead>
              <tr>
                <th className="feegow-product-stock-check-col">
                  <input type="checkbox" aria-label="Selecionar todos" disabled={!editing} />
                </th>
                <th>Lote</th>
                <th>Validade</th>
                <th>Cód. Individual</th>
                <th>Localização</th>
                <th>Responsável</th>
                <th>Quantidade</th>
                <th>Valor Médio</th>
              </tr>
            </thead>
            <tbody>
              {stockRows.map((row) => (
                <tr key={row.id}>
                  <td className="feegow-product-stock-check-col">
                    <input type="checkbox" aria-label={`Selecionar lote ${row.batchNumber}`} />
                  </td>
                  <td>{row.batchNumber}</td>
                  <td>{formatBrDate(row.expiryDate)}</td>
                  <td>{row.individualCode}</td>
                  <td>{row.location}</td>
                  <td>{row.responsibleName}</td>
                  <td>{row.quantity}</td>
                  <td>{formatCurrency(row.unitPrice)}</td>
                </tr>
              ))}
              {stockRows.length === 0 ? (
                <tr>
                  <td colSpan={8} className="feegow-inventory-table-empty">
                    {editing ? 'Nenhum lote em estoque.' : 'Salve o produto para registrar entradas.'}
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        <footer className="feegow-product-stock-position-foot">
          <span>Conjunto: {totals.conjunto}</span>
          <span>Unidade: {totals.unidade}</span>
        </footer>
      </section>
    </form>
  );
}
