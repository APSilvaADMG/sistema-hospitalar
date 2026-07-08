import { Link } from 'react-router-dom';
import type { ProductDto } from '../../../api/client';
import { FEEGOW_PRODUCT_TYPE_OPTIONS } from './feegowProductForm';
import { TablePagination } from '../TablePagination';
import {
  feegowInventoryInsertPath,
  inventoryTypeLabel,
  type FeegowInventoryItemType,
} from '../inventory/feegowInventoryNav';

export type FeegowProductListFilters = {
  productName: string;
  code: string;
  category: string;
  manufacturer: string;
  location: string;
  individualCode: string;
  lowStockOnly: '' | 'sim' | 'nao';
  sortBy: 'nome' | 'codigo' | 'categoria';
};

export const emptyProductListFilters = (): FeegowProductListFilters => ({
  productName: '',
  code: '',
  category: '',
  manufacturer: '',
  location: '',
  individualCode: '',
  lowStockOnly: '',
  sortBy: 'nome',
});

type Props = {
  products: ProductDto[];
  tipo: FeegowInventoryItemType;
  filters: FeegowProductListFilters;
  categoryOptions: string[];
  manufacturerOptions: string[];
  locationOptions: string[];
  onFiltersChange: (patch: Partial<FeegowProductListFilters>) => void;
  onSearch: () => void;
  onOpen: (id: string) => void;
  loading?: boolean;
  canInsert?: boolean;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
};

export function FeegowProductList({
  products,
  tipo,
  filters,
  categoryOptions,
  manufacturerOptions,
  locationOptions,
  onFiltersChange,
  onSearch,
  onOpen,
  loading,
  canInsert,
  page,
  pageSize,
  totalCount,
  onPageChange,
}: Props) {
  const tipoLabel = inventoryTypeLabel(tipo);
  const showPagination = page != null && pageSize != null && totalCount != null && onPageChange != null;

  return (
    <div className="feegow-inventory-page">
      <header className="feegow-inventory-page-head">
        <div className="feegow-inventory-breadcrumb">
          <span>Estoque</span>
          <span className="feegow-inventory-crumb-sep">/</span>
          <span className="feegow-inventory-crumb-icon" aria-hidden>📦</span>
          <span className="feegow-inventory-crumb-sep">/</span>
          <span>Lista</span>
        </div>
        {canInsert ? (
          <Link to={feegowInventoryInsertPath(tipo)} className="feegow-inventory-insert-btn">
            + INSERIR
          </Link>
        ) : null}
      </header>

      <section className="feegow-inventory-filter-card">
        <div className="feegow-inventory-filter-grid">
          <label className="feegow-inventory-field">
            <span>Produto</span>
            <input
              value={filters.productName}
              onChange={(e) => onFiltersChange({ productName: e.target.value })}
              placeholder="Selecione"
              list="feegow-product-name-options"
            />
            <datalist id="feegow-product-name-options">
              {products.map((product) => (
                <option key={product.id} value={product.name} />
              ))}
            </datalist>
          </label>

          <label className="feegow-inventory-field">
            <span>Tipo Produto</span>
            <select value={tipoLabel} disabled>
              {FEEGOW_PRODUCT_TYPE_OPTIONS.map((option) => (
                <option key={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label className="feegow-inventory-field">
            <span>Código</span>
            <input
              value={filters.code}
              onChange={(e) => onFiltersChange({ code: e.target.value })}
            />
          </label>

          <label className="feegow-inventory-field">
            <span>Categoria</span>
            <select
              value={filters.category}
              onChange={(e) => onFiltersChange({ category: e.target.value })}
            >
              <option value="">Selecione</option>
              {categoryOptions.map((category) => (
                <option key={category} value={category}>{category}</option>
              ))}
            </select>
          </label>

          <label className="feegow-inventory-field">
            <span>Fabricante</span>
            <select
              value={filters.manufacturer}
              onChange={(e) => onFiltersChange({ manufacturer: e.target.value })}
            >
              <option value="">Selecione</option>
              {manufacturerOptions.map((manufacturer) => (
                <option key={manufacturer} value={manufacturer}>{manufacturer}</option>
              ))}
            </select>
          </label>

          <label className="feegow-inventory-field">
            <span>Localização</span>
            <select
              value={filters.location}
              onChange={(e) => onFiltersChange({ location: e.target.value })}
            >
              <option value="">Selecione</option>
              {locationOptions.map((location) => (
                <option key={location} value={location}>{location}</option>
              ))}
            </select>
          </label>
        </div>

        <div className="feegow-inventory-filter-row2">
          <label className="feegow-inventory-field">
            <span>Código Individual</span>
            <input
              value={filters.individualCode}
              onChange={(e) => onFiltersChange({ individualCode: e.target.value })}
            />
          </label>

          <label className="feegow-inventory-field">
            <span>Abaixo do Mínimo</span>
            <select
              value={filters.lowStockOnly}
              onChange={(e) => onFiltersChange({ lowStockOnly: e.target.value as FeegowProductListFilters['lowStockOnly'] })}
            >
              <option value="">Selecione</option>
              <option value="sim">Sim</option>
              <option value="nao">Não</option>
            </select>
          </label>

          <label className="feegow-inventory-field">
            <span>Ordenar Por</span>
            <select
              value={filters.sortBy}
              onChange={(e) => onFiltersChange({ sortBy: e.target.value as FeegowProductListFilters['sortBy'] })}
            >
              <option value="nome">Nome</option>
              <option value="codigo">Código</option>
              <option value="categoria">Categoria</option>
            </select>
          </label>

          <div className="feegow-inventory-search-actions">
            <button type="button" className="feegow-inventory-search-btn" onClick={onSearch}>
              🔍 Buscar
            </button>
            <button type="button" className="feegow-inventory-icon-btn" title="Imprimir" aria-label="Imprimir">
              🖨
            </button>
            <button type="button" className="feegow-inventory-icon-btn" title="Exportar" aria-label="Exportar">
              ⊞
            </button>
          </div>
        </div>
      </section>

      <section className="feegow-inventory-table-card">
        <div className="feegow-inventory-legend">
          <span className="feegow-inventory-legend-expired">Fora da validade</span>
          <span className="feegow-inventory-legend-warning">Próximo do vencimento</span>
          <span className="feegow-inventory-legend-ok">Dentro do prazo</span>
        </div>
        <div className="feegow-inventory-table-wrap">
          <table className="feegow-inventory-table">
            <thead>
              <tr>
                <th>Produto</th>
                <th>Código</th>
                <th>Saldo</th>
                <th>Mínimo</th>
                <th>Categoria</th>
                <th>Fabricante</th>
                <th>Localização</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id} onClick={() => onOpen(product.id)}>
                  <td>{product.name}</td>
                  <td>{product.barcode || product.sku}</td>
                  <td className={product.isLowStock ? 'feegow-inventory-low-stock' : ''}>
                    {product.quantityOnHand} {product.unit}
                  </td>
                  <td>{product.minimumStock}</td>
                  <td>{product.category || '—'}</td>
                  <td>{product.manufacturer || '—'}</td>
                  <td>{product.defaultLocation || '—'}</td>
                </tr>
              ))}
              {!loading && products.length === 0 ? (
                <tr>
                  <td colSpan={7} className="feegow-inventory-table-empty">
                    Nenhum produto encontrado.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {showPagination ? (
          <TablePagination
            page={page}
            pageSize={pageSize}
            totalCount={totalCount}
            onPageChange={onPageChange}
            loading={loading}
          />
        ) : null}
      </section>
    </div>
  );
}
