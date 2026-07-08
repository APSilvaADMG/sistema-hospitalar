import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api, type ProductDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import { FeegowProductKitInsert } from '../components/feegow/products/FeegowProductKitInsert';
import {
  detailToFeegowKitForm,
  emptyFeegowProductKitForm,
  feegowKitFormToPayload,
  type FeegowProductKitFormState,
} from '../components/feegow/products/feegowProductKitForm';

export function FeegowProductKitInsertPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const kitId = searchParams.get('id');
  const { hasPermission } = useAuth();
  const [form, setForm] = useState<FeegowProductKitFormState>(emptyFeegowProductKitForm());
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [priceTableOptions, setPriceTableOptions] = useState<string[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(Boolean(kitId));

  useEffect(() => {
    Promise.all([
      api.getProducts('', false),
      api.getHealthInsurances(),
      kitId ? api.getProductKit(kitId) : Promise.resolve(null),
    ])
      .then(([productList, insurances, detail]) => {
        setProducts(productList);
        setPriceTableOptions(insurances.map((item) => item.name));
        if (detail) setForm(detailToFeegowKitForm(detail));
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar dados.'))
      .finally(() => setLoading(false));
  }, [kitId]);

  if (!hasPermission('warehouse.manage')) {
    return (
      <FeegowInventoryScreenLayout>
        <div className="feegow-inventory-empty-panel">Você não tem permissão para cadastrar kits de produtos.</div>
      </FeegowInventoryScreenLayout>
    );
  }

  if (loading) {
    return (
      <FeegowInventoryScreenLayout>
        <div className="feegow-inventory-empty-panel">Carregando kit...</div>
      </FeegowInventoryScreenLayout>
    );
  }

  function handleChange(patch: Partial<FeegowProductKitFormState>) {
    setForm((prev) => ({ ...prev, ...patch }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      if (!form.name.trim()) {
        setError('Informe o nome do kit.');
        return;
      }

      const payload = feegowKitFormToPayload(form);
      if (payload.items.length === 0) {
        setError('Adicione ao menos um produto ao kit.');
        return;
      }

      if (kitId) {
        await api.updateProductKit(kitId, payload);
        setSuccess('Kit atualizado com sucesso.');
      } else {
        const created = await api.createProductKit(payload);
        setSuccess('Kit cadastrado com sucesso.');
        navigate(`/estoque/kits/inserir?id=${created.id}`, { replace: true });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar kit.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <FeegowInventoryScreenLayout error={error} success={success}>
      <FeegowProductKitInsert
        form={form}
        products={products}
        priceTableOptions={priceTableOptions}
        onChange={handleChange}
        onSubmit={handleSubmit}
        saving={saving}
        editing={Boolean(kitId)}
      />
    </FeegowInventoryScreenLayout>
  );
}
