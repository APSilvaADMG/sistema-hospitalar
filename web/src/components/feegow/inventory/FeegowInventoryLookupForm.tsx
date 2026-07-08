import { Link } from 'react-router-dom';
import type { FormEvent } from 'react';
import {
  feegowInventoryLookupInsertPath,
  feegowInventoryLookupListPath,
  type FeegowInventoryLookupConfigId,
} from './feegowInventoryNav';

type Props = {
  configId: FeegowInventoryLookupConfigId;
  title: string;
  fieldLabel: string;
  value: string;
  onChange: (value: string) => void;
  onSubmit: (event: FormEvent) => void;
  saving?: boolean;
};

export function FeegowInventoryLookupForm({
  configId,
  title,
  fieldLabel,
  value,
  onChange,
  onSubmit,
  saving,
}: Props) {
  return (
    <form className="feegow-inventory-page" onSubmit={onSubmit}>
      <header className="feegow-product-workspace-head">
        <div className="feegow-inventory-breadcrumb">
          <span>{title}</span>
          <span className="feegow-inventory-crumb-sep">/</span>
        </div>
        <div className="feegow-product-workspace-toolbar">
          <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Anterior">‹</button>
          <Link
            to={feegowInventoryLookupListPath(configId)}
            className="feegow-product-workspace-tool-btn"
            title="Listar"
            aria-label="Listar"
          >
            ☰
          </Link>
          <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Próximo">›</button>
          <Link
            to={feegowInventoryLookupInsertPath(configId)}
            className="feegow-product-workspace-tool-btn"
            title="Novo"
            aria-label="Novo"
          >
            +
          </Link>
          <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Histórico">↺</button>
          <button type="submit" className="feegow-product-workspace-save-btn" disabled={saving}>
            💾 SALVAR
          </button>
        </div>
      </header>

      <section className="feegow-inventory-config-form-card">
        <label className="feegow-inventory-field">
          <span>{fieldLabel}</span>
          <input
            required
            autoFocus
            value={value}
            onChange={(e) => onChange(e.target.value)}
          />
        </label>
      </section>
    </form>
  );
}
