import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FeegowInventoryLookupForm } from '../components/feegow/inventory/FeegowInventoryLookupForm';
import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';
import {
  FEEGOW_INVENTORY_LOOKUP_CONFIG,
  feegowInventoryLookupInsertPath,
  type FeegowInventoryLookupConfigId,
} from '../components/feegow/inventory/feegowInventoryNav';

type Props = {
  configId: FeegowInventoryLookupConfigId;
};

export function FeegowInventoryLookupInsertPage({ configId }: Props) {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const itemId = searchParams.get('id');
  const { hasPermission } = useAuth();
  const meta = FEEGOW_INVENTORY_LOOKUP_CONFIG[configId];
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(Boolean(itemId));

  useEffect(() => {
    if (!itemId) {
      setName('');
      setLoading(false);
      return;
    }

    setLoading(true);
    api.getInventoryLookupItems(meta.lookupType)
      .then((items) => {
        const item = items.find((entry) => entry.id === itemId);
        setName(item?.name ?? '');
        if (!item) setError('Registro não encontrado.');
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar registro.'))
      .finally(() => setLoading(false));
  }, [itemId, meta.lookupType]);

  if (!hasPermission('warehouse.manage')) {
    return (
      <FeegowInventoryScreenLayout>
        <div className="feegow-inventory-empty-panel">Você não tem permissão para alterar esta configuração.</div>
      </FeegowInventoryScreenLayout>
    );
  }

  if (loading) {
    return (
      <FeegowInventoryScreenLayout>
        <div className="feegow-inventory-empty-panel">Carregando...</div>
      </FeegowInventoryScreenLayout>
    );
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      const trimmed = name.trim();
      if (!trimmed) {
        setError(`Informe ${meta.fieldLabel.toLowerCase()}.`);
        return;
      }

      if (itemId) {
        await api.updateInventoryLookupItem(itemId, { name: trimmed });
        setSuccess('Registro atualizado com sucesso.');
      } else {
        const created = await api.createInventoryLookupItem(meta.lookupType, { name: trimmed });
        setSuccess('Registro cadastrado com sucesso.');
        navigate(feegowInventoryLookupInsertPath(configId, created.id), { replace: true });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar registro.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <FeegowInventoryScreenLayout error={error} success={success}>
      <FeegowInventoryLookupForm
        configId={configId}
        title={meta.title}
        fieldLabel={meta.fieldLabel}
        value={name}
        onChange={setName}
        onSubmit={handleSubmit}
        saving={saving}
      />
    </FeegowInventoryScreenLayout>
  );
}
