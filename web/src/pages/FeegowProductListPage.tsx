import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api, type ProductDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import {
  FeegowProductList,
  emptyProductListFilters,
  type FeegowProductListFilters,
} from '../components/feegow/products/FeegowProductList';
import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { useInventoryLookupOptions } from '../components/feegow/inventory/useInventoryLookupOptions';
import {
  feegowInventoryInsertPath,
  inventoryListTypeFilter,
  parseInventoryTipo,
} from '../components/feegow/inventory/feegowInventoryNav';

const PAGE_SIZE = 50;

function applyClientFilters(products: ProductDto[], filters: FeegowProductListFilters): ProductDto[] {
  let result = [...products];

  if (filters.productName.trim()) {
    const term = filters.productName.trim().toLowerCase();
    result = result.filter((product) => product.name.toLowerCase().includes(term));
  }
  if (filters.code.trim()) {
    const term = filters.code.trim().toLowerCase();
    result = result.filter((product) =>
      (product.barcode ?? '').toLowerCase().includes(term)
      || product.sku.toLowerCase().includes(term));
  }
  if (filters.category) {
    result = result.filter((product) => product.category === filters.category);
  }
  if (filters.manufacturer) {
    result = result.filter((product) => product.manufacturer === filters.manufacturer);
  }
  if (filters.location) {
    result = result.filter((product) => product.defaultLocation === filters.location);
  }
  if (filters.individualCode.trim()) {
    const term = filters.individualCode.trim().toLowerCase();
    result = result.filter((product) =>
      (product.barcode ?? '').toLowerCase().includes(term)
      || product.sku.toLowerCase().includes(term));
  }
  if (filters.lowStockOnly === 'sim') {
    result = result.filter((product) => product.isLowStock);
  }
  if (filters.lowStockOnly === 'nao') {
    result = result.filter((product) => !product.isLowStock);
  }

  result.sort((a, b) => {
    if (filters.sortBy === 'codigo') {
      return (a.barcode || a.sku).localeCompare(b.barcode || b.sku, 'pt-BR');
    }
    if (filters.sortBy === 'categoria') {
      return (a.category ?? '').localeCompare(b.category ?? '', 'pt-BR');
    }
    return a.name.localeCompare(b.name, 'pt-BR');
  });

  return result;
}

export function FeegowProductListPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const tipo = parseInventoryTipo(searchParams.get('tipo'));
  const { hasPermission } = useAuth();
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [filters, setFilters] = useState<FeegowProductListFilters>(() => emptyProductListFilters());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const typeFilter = inventoryListTypeFilter(tipo);
      const list = await api.getProducts(undefined, undefined, typeFilter);
      setProducts(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar produtos.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    setFilters(emptyProductListFilters());
    setPage(1);
    load().catch(console.error);
  }, [tipo]);

  const filteredProducts = useMemo(() => applyClientFilters(products, filters), [products, filters]);

  useEffect(() => {
    setPage(1);
  }, [filters]);

  const pagedProducts = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filteredProducts.slice(start, start + PAGE_SIZE);
  }, [filteredProducts, page]);
  const productCategories = useMemo(
    () => products.map((product) => product.category).filter((value): value is string => Boolean(value)),
    [products],
  );
  const productManufacturers = useMemo(
    () => products.map((product) => product.manufacturer).filter((value): value is string => Boolean(value)),
    [products],
  );
  const productLocations = useMemo(
    () => products.map((product) => product.defaultLocation).filter((value): value is string => Boolean(value)),
    [products],
  );
  const lookupOptions = useInventoryLookupOptions({
    category: filters.category,
    manufacturer: filters.manufacturer,
    location: filters.location,
    categories: productCategories,
    manufacturers: productManufacturers,
    locations: productLocations,
  });

  return (
    <FeegowInventoryScreenLayout error={error}>
      <FeegowProductList
        products={pagedProducts}
        tipo={tipo}
        filters={filters}
        categoryOptions={lookupOptions.categories}
        manufacturerOptions={lookupOptions.manufacturers}
        locationOptions={lookupOptions.locations}
        onFiltersChange={(patch) => setFilters((prev) => ({ ...prev, ...patch }))}
        onSearch={() => load().catch(console.error)}
        onOpen={(id) => navigate(feegowInventoryInsertPath(tipo, { id }))}
        loading={loading}
        canInsert={hasPermission('warehouse.manage')}
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={filteredProducts.length}
        onPageChange={setPage}
      />
    </FeegowInventoryScreenLayout>
  );
}
