import { useEffect, useMemo, useState } from 'react';

import { useNavigate } from 'react-router-dom';

import { api, type StockRequisitionDto } from '../api/client';

import { useAuth } from '../auth/AuthContext';

import { FeegowStockRequisitionList } from '../components/feegow/products/FeegowStockRequisitionList';



const PAGE_SIZE = 50;



export function FeegowStockRequisitionListPage() {

  const navigate = useNavigate();

  const { hasPermission } = useAuth();

  const [requisitions, setRequisitions] = useState<StockRequisitionDto[]>([]);

  const [statusFilter, setStatusFilter] = useState<number | ''>('');

  const [priorityFilter, setPriorityFilter] = useState<number | ''>('');

  const [dueDateFilter, setDueDateFilter] = useState('');

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState('');

  const [page, setPage] = useState(1);



  async function load(

    nextStatus = statusFilter,

    nextPriority = priorityFilter,

    nextDueDate = dueDateFilter,

  ) {

    setLoading(true);

    setError('');

    try {

      const list = await api.getStockRequisitions(

        nextStatus === '' ? undefined : nextStatus,

        nextPriority === '' ? undefined : nextPriority,

        nextDueDate || undefined,

      );

      setRequisitions(list);

      setPage(1);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar requisições.');

    } finally {

      setLoading(false);

    }

  }



  useEffect(() => {

    load().catch(console.error);

  }, []);



  const pagedRequisitions = useMemo(() => {

    const start = (page - 1) * PAGE_SIZE;

    return requisitions.slice(start, start + PAGE_SIZE);

  }, [requisitions, page]);



  const canCreate = hasPermission('warehouse.manage', 'pharmacy.dispense');
  const canManageWarehouse = hasPermission('warehouse.manage');

  async function handleDeny(id: string) {
    const reason = window.prompt('Informe o motivo da negativa:');
    if (!reason?.trim()) return;

    setError('');
    try {
      await api.denyStockRequisition(id, { reason: reason.trim() });
      await load(statusFilter, priorityFilter, dueDateFilter);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao negar requisição.');
    }
  }



  return (

    <>

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}

      <FeegowStockRequisitionList

        requisitions={pagedRequisitions}

        statusFilter={statusFilter}

        priorityFilter={priorityFilter}

        dueDateFilter={dueDateFilter}

        onStatusFilterChange={setStatusFilter}

        onPriorityFilterChange={setPriorityFilter}

        onDueDateFilterChange={setDueDateFilter}

        onSearch={() => load(statusFilter, priorityFilter, dueDateFilter)}

        onOpen={(id) => navigate(`/estoque/requisicoes/inserir?id=${id}`)}

        onDeny={canManageWarehouse ? handleDeny : undefined}

        loading={loading}

        canCreate={canCreate}

        canManageWarehouse={canManageWarehouse}

        page={page}

        pageSize={PAGE_SIZE}

        totalCount={requisitions.length}

        onPageChange={setPage}

      />

    </>

  );

}

