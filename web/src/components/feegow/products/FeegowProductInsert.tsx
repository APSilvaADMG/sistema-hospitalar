import { useState, type FormEvent } from 'react';
import { FeegowPatientPhotoCapture } from '../patients/FeegowPatientPhotoCapture';
import {
  DEFAULT_LOCATIONS,
  DEFAULT_MANUFACTURERS,
  DEFAULT_PRESENTATIONS,
  DEFAULT_PRODUCT_CATEGORIES,
  DEFAULT_UNITS,
  FEEGOW_PRODUCT_TYPE_OPTIONS,
  type FeegowProductFormState,
} from './feegowProductForm';

type Props = {
  form: FeegowProductFormState;
  onChange: (patch: Partial<FeegowProductFormState>) => void;
  onSubmit: (event: FormEvent) => void;
  saving?: boolean;
  editing?: boolean;
  /** Layout integrado ao workspace de estoque (sem cabeçalho de paciente). */
  workspace?: boolean;
};

function ComboField({
  label,
  required,
  value,
  options,
  onChange,
  placeholder,
}: {
  label: string;
  required?: boolean;
  value: string;
  options: string[];
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  const listId = `combo-${label.replace(/\s+/g, '-').toLowerCase()}`;
  return (
    <label className="feegow-field">
      <span>
        {label}
        {required ? <span className="feegow-req">*</span> : null}
      </span>
      <input
        list={listId}
        required={required}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
      />
      <datalist id={listId}>
        {options.map((option) => (
          <option key={option} value={option} />
        ))}
      </datalist>
    </label>
  );
}

export function FeegowProductInsert({
  form,
  onChange,
  onSubmit,
  saving,
  editing,
  workspace,
}: Props) {
  const [activeToggle, setActiveToggle] = useState(true);

  function patch(partial: Partial<FeegowProductFormState>) {
    onChange(partial);
  }

  function toggleEntryLocation(location: string) {
    const selected = new Set(form.entryLocations);
    if (selected.has(location)) selected.delete(location);
    else selected.add(location);
    patch({ entryLocations: [...selected] });
  }

  const formBody = (
    <div className={workspace ? 'feegow-product-form-card' : 'feegow-patient-form-body'}>
        <section className="feegow-patient-section">
          <h2 className="feegow-patient-section-title">Dados do Produto</h2>

          <div className="feegow-patient-form-layout">
            <FeegowPatientPhotoCapture
              name={form.name || 'Produto'}
              photoData={form.photoData}
              onChange={(photoData) => patch({ photoData: photoData ?? undefined })}
            />

            <div className="feegow-patient-fields-stack">
              <div className="feegow-form-grid feegow-product-grid">
                <label className="feegow-field">
                  <span>
                    Tipo
                    <span className="feegow-req">*</span>
                  </span>
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

                <label className="feegow-field feegow-field-grow2">
                  <span>
                    Nome
                    <span className="feegow-req">*</span>
                  </span>
                  <input
                    required
                    value={form.name}
                    onChange={(e) => patch({ name: e.target.value })}
                  />
                </label>

                <ComboField
                  label="Apresentação"
                  required
                  value={form.presentation}
                  options={DEFAULT_PRESENTATIONS}
                  onChange={(presentation) => patch({ presentation })}
                  placeholder="Ex.: Caixa"
                />

                <label className="feegow-field">
                  <span>
                    Contendo
                    <span className="feegow-req">*</span>
                  </span>
                  <input
                    required
                    inputMode="decimal"
                    value={form.contentQuantity}
                    onChange={(e) => patch({ contentQuantity: e.target.value })}
                    placeholder="Ex.: 10"
                  />
                </label>

                <label className="feegow-field">
                  <span>
                    Unidade
                    <span className="feegow-req">*</span>
                  </span>
                  <input
                    required
                    list="feegow-product-units"
                    value={form.unit}
                    onChange={(e) => patch({ unit: e.target.value.toUpperCase() })}
                  />
                  <datalist id="feegow-product-units">
                    {DEFAULT_UNITS.map((unit) => (
                      <option key={unit} value={unit} />
                    ))}
                  </datalist>
                </label>

                <label className="feegow-field">
                  <span>Código</span>
                  <input
                    value={form.barcode}
                    onChange={(e) => patch({ barcode: e.target.value })}
                    placeholder="Código de barras"
                  />
                </label>

                <ComboField
                  label="Categoria"
                  value={form.category}
                  options={DEFAULT_PRODUCT_CATEGORIES}
                  onChange={(category) => patch({ category })}
                />

                <ComboField
                  label="Fabricante"
                  value={form.manufacturer}
                  options={DEFAULT_MANUFACTURERS}
                  onChange={(manufacturer) => patch({ manufacturer })}
                />

                <ComboField
                  label="Localização Padrão"
                  value={form.defaultLocation}
                  options={DEFAULT_LOCATIONS}
                  onChange={(defaultLocation) => patch({ defaultLocation })}
                />

                <label className="feegow-field">
                  <span>CD (TUSS)</span>
                  <input
                    value={form.tussCode}
                    onChange={(e) => patch({ tussCode: e.target.value })}
                    placeholder="Código TUSS"
                  />
                </label>
              </div>
            </div>
          </div>
        </section>

        <section className="feegow-patient-section">
          <h2 className="feegow-patient-section-title">Controle de Estoque</h2>
          <div className="feegow-form-grid feegow-product-grid">
            <label className="feegow-field">
              <span>
                Estoque Mínimo
                <span className="feegow-req">*</span>
              </span>
              <input
                required
                type="number"
                min={0}
                step="0.001"
                value={form.minimumStock}
                onChange={(e) => patch({ minimumStock: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Estoque Máximo</span>
              <input
                type="number"
                min={0}
                step="0.001"
                value={form.maximumStock}
                onChange={(e) => patch({ maximumStock: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Aviso de Validade (dias)</span>
              <input
                type="number"
                min={0}
                value={form.expiryWarningDays}
                onChange={(e) => patch({ expiryWarningDays: e.target.value })}
              />
            </label>

            <label className="feegow-field feegow-field-notes feegow-field-span-full">
              <span>Observações</span>
              <textarea
                rows={3}
                value={form.description}
                onChange={(e) => patch({ description: e.target.value })}
                placeholder="Informações complementares sobre o produto"
              />
            </label>
          </div>
        </section>

        <section className="feegow-patient-section">
          <h2 className="feegow-patient-section-title">Preços</h2>
          <div className="feegow-form-grid feegow-product-grid">
            <label className="feegow-field">
              <span>Preço Médio — Compra</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={form.averagePurchasePrice}
                onChange={(e) => patch({ averagePurchasePrice: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>
                Preço Médio — Venda
                <span className="feegow-req">*</span>
              </span>
              <input
                required
                type="number"
                min={0}
                step="0.01"
                value={form.averageSalePrice}
                onChange={(e) => patch({ averageSalePrice: e.target.value })}
              />
            </label>
          </div>
        </section>

        <section className="feegow-patient-section">
          <h2 className="feegow-patient-section-title">Opções</h2>
          <div className="feegow-form-grid feegow-product-grid">
            <div className="feegow-field feegow-field-check">
              <span>Permitir saída pelo cadastro</span>
              <label className="feegow-check-pill">
                <input
                  type="checkbox"
                  checked={form.allowOutboundFromRegister}
                  onChange={(e) => patch({ allowOutboundFromRegister: e.target.checked })}
                />
                <span>{form.allowOutboundFromRegister ? 'SIM' : 'NÃO'}</span>
              </label>
            </div>

            <div className="feegow-field feegow-field-span-full">
              <span>Locais de possíveis entradas</span>
              <div className="feegow-product-location-chips">
                {DEFAULT_LOCATIONS.map((location) => {
                  const selected = form.entryLocations.includes(location);
                  return (
                    <button
                      key={location}
                      type="button"
                      className={`feegow-product-location-chip${selected ? ' is-selected' : ''}`}
                      onClick={() => toggleEntryLocation(location)}
                    >
                      {location}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        </section>

        {editing && !workspace ? (
          <section className="feegow-patient-section feegow-product-movements-hint">
            <h2 className="feegow-patient-section-title">Movimentações</h2>
            <p>
              Após salvar, utilize a aba de movimentações na listagem para registrar entradas e saídas do produto.
            </p>
          </section>
        ) : null}
    </div>
  );

  if (workspace) {
    return (
      <form onSubmit={onSubmit}>
        {formBody}
      </form>
    );
  }

  return (
    <form className="feegow-patient-card feegow-product-card" onSubmit={onSubmit}>
      <header className="feegow-patient-card-head">
        <div className="feegow-patient-breadcrumb">
          <span className="feegow-patient-crumb-icon" aria-hidden>📦</span>
          <span className="feegow-patient-crumb-sep">/</span>
          <span className="feegow-patient-crumb-label">
            {editing ? 'Editar Produto' : 'Inserir Produto'}
          </span>
        </div>

        <div className="feegow-patient-toolbar">
          <label className="feegow-patient-toggle" title="Produto ativo">
            <input
              type="checkbox"
              checked={activeToggle}
              onChange={(e) => setActiveToggle(e.target.checked)}
            />
            <span className="feegow-patient-toggle-track" />
          </label>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Imprimir">🖨</button>
          <button type="submit" className="feegow-patient-save-btn" disabled={saving}>
            💾 SALVAR
          </button>
        </div>
      </header>
      {formBody}
    </form>
  );
}
