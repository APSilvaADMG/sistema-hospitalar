import { useEffect, useMemo, useState } from 'react';

import { useNavigate } from 'react-router-dom';

import { api, type ProductKitDto } from '../api/client';

import { useAuth } from '../auth/AuthContext';

import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';

import { FeegowProductKitList } from '../components/feegow/products/FeegowProductKitList';



const PAGE_SIZE = 50;



export function FeegowProductKitListPage() {

  const navigate = useNavigate();

  const { hasPermission } = useAuth();

  const [kits, setKits] = useState<ProductKitDto[]>([]);

  const [search, setSearch] = useState('');

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState('');

  const [page, setPage] = useState(1);



  async function load(nextSearch = search) {

    setLoading(true);

    setError('');

    try {

      const list = await api.getProductKits(nextSearch);

      setKits(list);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar kits.');

    } finally {

      setLoading(false);

    }

  }



  useEffect(() => {

    load().catch(console.error);

  }, []);



  useEffect(() => {

    setPage(1);

  }, [search, kits]);



  const pagedKits = useMemo(() => {

    const start = (page - 1) * PAGE_SIZE;

    return kits.slice(start, start + PAGE_SIZE);

  }, [kits, page]);



  async function handleDelete(id: string) {

    if (!window.confirm('Deseja excluir este kit de produtos?')) return;

    setError('');

    try {

      await api.deleteProductKit(id);

      await load();

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao excluir kit.');

    }

  }



  return (

    <FeegowInventoryScreenLayout error={error}>

      <FeegowProductKitList

        kits={pagedKits}

        search={search}

        onSearchChange={setSearch}

        onSearch={() => load(search)}

        onEdit={(id) => navigate(`/estoque/kits/inserir?id=${id}`)}

        onDelete={handleDelete}

        loading={loading}

        canManage={hasPermission('warehouse.manage')}

        page={page}

        pageSize={PAGE_SIZE}

        totalCount={kits.length}

        onPageChange={setPage}

      />

    </FeegowInventoryScreenLayout>

  );

}

