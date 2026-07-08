import { Link } from 'react-router-dom';
import type { ReactNode } from 'react';

type Props = {
  title: string;
  icon?: string;
  toolbar?: ReactNode;
  listPath?: string;
  insertPath?: string;
  onSave?: () => void;
  saving?: boolean;
};

export function FeegowFinancePageHead({
  title,
  icon = '💰',
  toolbar,
  listPath,
  insertPath,
  onSave,
  saving,
}: Props) {
  return (
    <header className="feegow-finance-page-head">
      <div className="feegow-inventory-breadcrumb feegow-finance-breadcrumb">
        <span>Financeiro</span>
        <span className="feegow-inventory-crumb-sep">/</span>
        <span className="feegow-inventory-crumb-icon" aria-hidden>{icon}</span>
        <span className="feegow-inventory-crumb-sep">/</span>
        <span>{title}</span>
      </div>
      {toolbar ?? (
        <div className="feegow-product-workspace-toolbar">
          {listPath ? (
            <Link to={listPath} className="feegow-product-workspace-tool-btn" title="Listar" aria-label="Listar">
              ☰
            </Link>
          ) : (
            <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Listar">☰</button>
          )}
          {insertPath ? (
            <Link to={insertPath} className="feegow-product-workspace-tool-btn" title="Novo" aria-label="Novo">
              +
            </Link>
          ) : (
            <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Novo">+</button>
          )}
          {onSave ? (
            <button
              type="button"
              className="feegow-product-workspace-save-btn"
              disabled={saving}
              onClick={onSave}
            >
              💾 SALVAR
            </button>
          ) : null}
          <button type="button" className="feegow-finance-print-btn" aria-label="Imprimir">🖨</button>
        </div>
      )}
    </header>
  );
}
