import { useEffect, useMemo, useState } from 'react';

import { useNavigate } from 'react-router-dom';

import { api, type InventoryLookupItemDto } from '../api/client';

import { useAuth } from '../auth/AuthContext';

import { FeegowInventoryLookupList } from '../components/feegow/inventory/FeegowInventoryLookupList';

import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';

import {

  FEEGOW_INVENTORY_LOOKUP_CONFIG,

  feegowInventoryLookupInsertPath,

  type FeegowInventoryLookupConfigId,

} from '../components/feegow/inventory/feegowInventoryNav';



const PAGE_SIZE = 50;



type Props = {

  configId: FeegowInventoryLookupConfigId;

};



export function FeegowInventoryLookupListPage({ configId }: Props) {

  const navigate = useNavigate();

  const { hasPermission } = useAuth();

  const meta = FEEGOW_INVENTORY_LOOKUP_CONFIG[configId];

  const [items, setItems] = useState<InventoryLookupItemDto[]>([]);

  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState('');

  const [page, setPage] = useState(1);



  async function load() {

    setLoading(true);

    setError('');

    try {

      const list = await api.getInventoryLookupItems(meta.lookupType);

      setItems(list);

      setSelectedIds(new Set());

      setPage(1);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar registros.');

    } finally {

      setLoading(false);

    }

  }



  useEffect(() => {

    load().catch(console.error);

  }, [configId]);



  const pagedItems = useMemo(() => {

    const start = (page - 1) * PAGE_SIZE;

    return items.slice(start, start + PAGE_SIZE);

  }, [items, page]);



  function toggle(id: string) {

    setSelectedIds((prev) => {

      const next = new Set(prev);

      if (next.has(id)) next.delete(id);

      else next.add(id);

      return next;

    });

  }



  function toggleAll() {

    if (pagedItems.length > 0 && pagedItems.every((item) => selectedIds.has(item.id))) {

      setSelectedIds(new Set());

      return;

    }

    setSelectedIds(new Set(pagedItems.map((item) => item.id)));

  }



  return (

    <FeegowInventoryScreenLayout error={error}>

      <FeegowInventoryLookupList

        configId={configId}

        title={meta.title}

        fieldLabel={meta.fieldLabel}

        items={pagedItems}

        selectedIds={selectedIds}

        onToggle={toggle}

        onToggleAll={toggleAll}

        onOpen={(id) => navigate(feegowInventoryLookupInsertPath(configId, id))}

        loading={loading}

        canManage={hasPermission('warehouse.manage')}

        page={page}

        pageSize={PAGE_SIZE}

        totalCount={items.length}

        onPageChange={setPage}

      />

    </FeegowInventoryScreenLayout>

  );

}

