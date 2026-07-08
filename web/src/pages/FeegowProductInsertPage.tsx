import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import {
  api,
  type CreateProductBillingRuleRequest,
  type ProductBillingRuleDto,
  type StockInboundRequest,
  type StockMovementDto,
  type UpdateProductBillingRuleRequest,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FeegowProductCadastroForm } from '../components/feegow/inventory/FeegowProductCadastroForm';
import { useInventoryLookupOptions } from '../components/feegow/inventory/useInventoryLookupOptions';
import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { FeegowProductSubSidebar } from '../components/feegow/inventory/FeegowProductSubSidebar';
import { FeegowProductMovements } from '../components/feegow/inventory/FeegowProductMovements';
import { FeegowProductBilling } from '../components/feegow/inventory/FeegowProductBilling';
import { FeegowProductMovementEntryModal } from '../components/feegow/inventory/FeegowProductMovementEntryModal';
import {
  feegowInventoryInsertPath,
  feegowInventoryListPath,
  inventoryTypeLabel,
  inventoryInsertProductType,
  parseInventoryTipo,
  parseProductTab,
} from '../components/feegow/inventory/feegowInventoryNav';
import {
  detailToFeegowForm,
  emptyFeegowProductForm,
  feegowFormToCreatePayload,
  type FeegowProductFormState,
} from '../components/feegow/products/feegowProductForm';

export function FeegowProductInsertPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const productId = searchParams.get('id');
  const tipo = parseInventoryTipo(searchParams.get('tipo'));
  const aba = parseProductTab(searchParams.get('aba'));
  const tipoLabel = inventoryTypeLabel(tipo);
  const { hasPermission, user } = useAuth();
  const [form, setForm] = useState<FeegowProductFormState>(() => ({
    ...emptyFeegowProductForm(),
    type: inventoryInsertProductType(tipo),
  }));
  const [movements, setMovements] = useState<StockMovementDto[]>([]);
  const [billingRules, setBillingRules] = useState<ProductBillingRuleDto[]>([]);
  const [priceTableOptions, setPriceTableOptions] = useState<string[]>(['Particular']);
  const [billingTableFilter, setBillingTableFilter] = useState('');
  const [billingStatusFilter, setBillingStatusFilter] = useState<'' | 'ativo' | 'inativo'>('ativo');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(Boolean(productId));
  const [loadingMovements, setLoadingMovements] = useState(false);
  const [loadingBilling, setLoadingBilling] = useState(false);
  const [quantityOnHand, setQuantityOnHand] = useState(0);
  const [showEntryModal, setShowEntryModal] = useState(false);
  const [entrySaving, setEntrySaving] = useState(false);
  const lookupOptions = useInventoryLookupOptions({
    category: form.category,
    manufacturer: form.manufacturer,
    location: form.defaultLocation,
    entryLocations: form.entryLocations,
  });

  useEffect(() => {
    api.getHealthInsurances()
      .then((insurances) => {
        const tables = ['Particular', ...insurances.map((item) => item.name)];
        setPriceTableOptions([...new Set(tables)]);
      })
      .catch(console.error);
  }, []);

  useEffect(() => {
    if (!productId) {
      setForm({ ...emptyFeegowProductForm(), type: inventoryInsertProductType(tipo) });
      setQuantityOnHand(0);
      setMovements([]);
      return;
    }
    setLoading(true);
    api.getProduct(productId)
      .then((detail) => {
        setForm(detailToFeegowForm(detail));
        setQuantityOnHand(detail.quantityOnHand);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar produto.'))
      .finally(() => setLoading(false));
  }, [productId, tipo]);

  const loadMovements = useCallback(async () => {
    if (!productId) {
      setMovements([]);
      return;
    }
    setLoadingMovements(true);
    try {
      const list = await api.getStockMovements({ productId });
      setMovements(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar movimentações.');
    } finally {
      setLoadingMovements(false);
    }
  }, [productId]);

  const loadBillingRules = useCallback(async () => {
    if (!productId) {
      setBillingRules([]);
      return;
    }
    setLoadingBilling(true);
    try {
      const list = await api.getProductBillingRules(
        productId,
        billingTableFilter || undefined,
        billingStatusFilter === '' ? undefined : billingStatusFilter === 'ativo',
      );
      setBillingRules(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar regras de faturamento.');
    } finally {
      setLoadingBilling(false);
    }
  }, [productId, billingTableFilter, billingStatusFilter]);

  useEffect(() => {
    if (productId) {
      loadMovements().catch(console.error);
    }
  }, [productId, loadMovements]);

  useEffect(() => {
    if (aba === 'faturamento') {
      loadBillingRules().catch(console.error);
    }
  }, [aba, loadBillingRules]);

  const productSidebar = (
    <FeegowProductSubSidebar tipo={tipo} productId={productId ?? undefined} activeTab={aba} />
  );

  if (!hasPermission('warehouse.manage')) {
    return (
      <FeegowInventoryScreenLayout sidebar={productSidebar}>
        <div className="feegow-inventory-empty-panel">Você não tem permissão para cadastrar produtos.</div>
      </FeegowInventoryScreenLayout>
    );
  }

  if (loading) {
    return (
      <FeegowInventoryScreenLayout sidebar={productSidebar}>
        <div className="feegow-inventory-empty-panel">Carregando produto...</div>
      </FeegowInventoryScreenLayout>
    );
  }

  function handleChange(patch: Partial<FeegowProductFormState>) {
    setForm((prev) => ({ ...prev, ...patch }));
  }

  async function handleSubmit(event?: FormEvent) {
    event?.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      if (!form.name.trim()) {
        setError('Informe o nome do produto.');
        return;
      }
      if (!form.presentation.trim()) {
        setError('Informe a apresentação do produto.');
        return;
      }
      if (!form.contentQuantity.trim()) {
        setError('Informe a quantidade contida na apresentação.');
        return;
      }
      if (Number(form.averageSalePrice) <= 0) {
        setError('Informe o preço médio de venda.');
        return;
      }

      const payload = feegowFormToCreatePayload(form);

      if (productId) {
        await api.updateProduct(productId, {
          name: payload.name,
          type: payload.type,
          unit: payload.unit,
          minimumStock: payload.minimumStock,
          description: payload.description,
          presentation: payload.presentation,
          contentQuantity: payload.contentQuantity,
          barcode: payload.barcode,
          category: payload.category,
          manufacturer: payload.manufacturer,
          defaultLocation: payload.defaultLocation,
          tussCode: payload.tussCode,
          maximumStock: payload.maximumStock,
          expiryWarningDays: payload.expiryWarningDays,
          averagePurchasePrice: payload.averagePurchasePrice,
          averageSalePrice: payload.averageSalePrice,
          allowOutboundFromRegister: payload.allowOutboundFromRegister,
          entryLocations: payload.entryLocations,
          photoData: payload.photoData,
        });
        setSuccess('Produto atualizado com sucesso.');
      } else {
        const created = await api.createProduct(payload);
        setSuccess('Produto cadastrado com sucesso.');
        navigate(feegowInventoryInsertPath(tipo, { id: created.id, aba }), { replace: true });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar produto.');
    } finally {
      setSaving(false);
    }
  }

  async function handleRegisterInbound(payload: Omit<StockInboundRequest, 'productId'>) {
    if (!productId) return;
    setEntrySaving(true);
    try {
      await api.registerStockInbound({ ...payload, productId });
      setSuccess('Entrada registrada com sucesso.');
      const detail = await api.getProduct(productId);
      setQuantityOnHand(detail.quantityOnHand);
      await loadMovements();
      setShowEntryModal(false);
    } finally {
      setEntrySaving(false);
    }
  }

  async function handleCreateBillingRule(payload: CreateProductBillingRuleRequest) {
    if (!productId) return;
    await api.createProductBillingRule(productId, payload);
    setSuccess('Regra de faturamento adicionada.');
    await loadBillingRules();
  }

  async function handleUpdateBillingRule(ruleId: string, payload: UpdateProductBillingRuleRequest) {
    await api.updateProductBillingRule(ruleId, payload);
    setSuccess('Regra de faturamento atualizada.');
    await loadBillingRules();
  }

  async function handleDeleteBillingRule(ruleId: string) {
    await api.deleteProductBillingRule(ruleId);
    setSuccess('Regra de faturamento inativada.');
    await loadBillingRules();
  }

  return (
    <FeegowInventoryScreenLayout error={error} success={success} sidebar={productSidebar}>
      <div className="feegow-product-workspace-main feegow-product-workspace-main-full">
        <header className="feegow-product-workspace-head">
          <div className="feegow-inventory-breadcrumb">
            <span>Estoque</span>
            <span className="feegow-inventory-crumb-sep">/</span>
            <span className="feegow-inventory-crumb-icon" aria-hidden>📦</span>
            <span className="feegow-inventory-crumb-sep">/</span>
            <span>{tipoLabel}</span>
          </div>
          <div className="feegow-product-workspace-toolbar">
            <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Anterior">‹</button>
            <Link
              to={feegowInventoryListPath(tipo)}
              className="feegow-product-workspace-tool-btn"
              title="Listar"
              aria-label="Lista"
            >
              ☰
            </Link>
            <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Próximo">›</button>
            <button type="button" className="feegow-product-workspace-tool-btn" aria-label="Histórico">↺</button>
            <button
              type="button"
              className="feegow-product-workspace-save-btn"
              disabled={saving}
              onClick={() => handleSubmit()}
            >
              💾 SALVAR
            </button>
          </div>
        </header>

        {aba === 'cadastro' ? (
          <FeegowProductCadastroForm
            form={form}
            categoryOptions={lookupOptions.categories}
            manufacturerOptions={lookupOptions.manufacturers}
            locationOptions={lookupOptions.locations}
            movements={movements}
            quantityOnHand={quantityOnHand}
            onChange={handleChange}
            onSubmit={handleSubmit}
            onOpenEntry={() => setShowEntryModal(true)}
            editing={Boolean(productId)}
          />
        ) : null}
        {aba === 'movimentacao' ? (
          <FeegowProductMovements
            productId={productId ?? undefined}
            movements={movements}
            loading={loadingMovements}
          />
        ) : null}
        {aba === 'faturamento' ? (
          <FeegowProductBilling
            productId={productId ?? undefined}
            itemName={form.name}
            rules={billingRules}
            priceTableOptions={priceTableOptions}
            tableFilter={billingTableFilter}
            statusFilter={billingStatusFilter}
            loading={loadingBilling}
            onFilterChange={(patch) => {
              if (patch.tableFilter !== undefined) setBillingTableFilter(patch.tableFilter);
              if (patch.statusFilter !== undefined) setBillingStatusFilter(patch.statusFilter);
            }}
            onCreate={handleCreateBillingRule}
            onUpdate={handleUpdateBillingRule}
            onDelete={handleDeleteBillingRule}
          />
        ) : null}

        <FeegowProductMovementEntryModal
          open={showEntryModal}
          locationOptions={lookupOptions.locations}
          defaultLocation={form.defaultLocation}
          userName={user?.fullName}
          saving={entrySaving}
          onClose={() => setShowEntryModal(false)}
          onSubmit={handleRegisterInbound}
        />
      </div>
    </FeegowInventoryScreenLayout>
  );
}
