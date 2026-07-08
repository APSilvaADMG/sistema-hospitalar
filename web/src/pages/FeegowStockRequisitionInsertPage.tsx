import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api, type ProductDto, type UserListDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FeegowStockRequisitionInsert } from '../components/feegow/products/FeegowStockRequisitionInsert';
import {
  detailToFeegowRequisitionForm,
  emptyFeegowStockRequisitionForm,
  feegowRequisitionFormToPayload,
  type FeegowStockRequisitionFormState,
} from '../components/feegow/products/feegowStockRequisitionForm';
import { formatBrDateTime } from '../utils/dateUtils';

export function FeegowStockRequisitionInsertPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const requisitionId = searchParams.get('id');
  const { hasPermission, user } = useAuth();
  const [form, setForm] = useState<FeegowStockRequisitionFormState>(
    emptyFeegowStockRequisitionForm(user?.fullName ?? ''),
  );
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [users, setUsers] = useState<UserListDto[]>([]);
  const [sequenceNumber, setSequenceNumber] = useState<number | undefined>();
  const [createdAt, setCreatedAt] = useState<string | undefined>();
  const [createdBy, setCreatedBy] = useState<string | undefined>();
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [acting, setActing] = useState(false);
  const [loading, setLoading] = useState(true);

  const canCreate = hasPermission('warehouse.manage', 'pharmacy.dispense');
  const canManageWarehouse = hasPermission('warehouse.manage');
  const readOnly = Boolean(requisitionId && form.status !== 1);

  useEffect(() => {
    Promise.all([
      api.getProducts('', false),
      api.getUsers().catch(() => [] as UserListDto[]),
      requisitionId ? api.getStockRequisition(requisitionId) : Promise.resolve(null),
    ])
      .then(([productList, userList, detail]) => {
        setProducts(productList);
        setUsers(userList.filter((item) => item.isActive));
        if (detail) {
          setForm(detailToFeegowRequisitionForm(detail));
          setSequenceNumber(detail.sequenceNumber);
          setCreatedBy(detail.requestedBy);
          setCreatedAt(formatBrDateTime(detail.requestedAt));
        }
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar dados.'))
      .finally(() => setLoading(false));
  }, [requisitionId]);

  if (!canCreate) {
    return (
      <div className="feegow-requisition-page">
        <div className="feegow-requisition-empty-card">
          <p>Você não tem permissão para solicitar materiais do estoque.</p>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="feegow-requisition-page">
        <div className="feegow-requisition-empty-card">
          <p>Carregando requisição...</p>
        </div>
      </div>
    );
  }

  function handleChange(patch: Partial<FeegowStockRequisitionFormState>) {
    setForm((prev) => ({ ...prev, ...patch }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (readOnly) return;

    setError('');
    setSuccess('');
    setSaving(true);

    try {
      if (!form.requestedBy.trim()) {
        setError('Informe o solicitante.');
        return;
      }
      if (!form.recipientName.trim()) {
        setError('Informe o destinatário.');
        return;
      }

      const payload = feegowRequisitionFormToPayload(form);
      if (payload.items.length === 0) {
        setError('Adicione ao menos um produto à requisição.');
        return;
      }

      if (requisitionId) {
        const updated = await api.updateStockRequisition(requisitionId, payload);
        setForm(detailToFeegowRequisitionForm(updated));
        setSequenceNumber(updated.sequenceNumber);
        setCreatedBy(updated.requestedBy);
        setCreatedAt(formatBrDateTime(updated.requestedAt));
        setSuccess('Requisição atualizada com sucesso.');
      } else {
        const created = await api.createStockRequisition(payload);
        setSuccess('Requisição registrada com sucesso.');
        navigate(`/estoque/requisicoes/inserir?id=${created.id}`, { replace: true });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar requisição.');
    } finally {
      setSaving(false);
    }
  }

  async function runAction(action: () => Promise<void>, message: string) {
    setError('');
    setSuccess('');
    setActing(true);
    try {
      await action();
      if (requisitionId) {
        const detail = await api.getStockRequisition(requisitionId);
        setForm(detailToFeegowRequisitionForm(detail));
        setSequenceNumber(detail.sequenceNumber);
      }
      setSuccess(message);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na operação.');
    } finally {
      setActing(false);
    }
  }

  return (
    <>
      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}
      <FeegowStockRequisitionInsert
        form={form}
        products={products}
        users={users}
        sequenceNumber={sequenceNumber}
        createdAt={createdAt}
        createdBy={createdBy}
        readOnly={readOnly}
        canManageWarehouse={canManageWarehouse}
        onChange={handleChange}
        onSubmit={handleSubmit}
        onApprove={
          requisitionId
            ? () => runAction(() => api.approveStockRequisition(requisitionId).then(() => undefined), 'Requisição aprovada.')
            : undefined
        }
        onFulfill={
          requisitionId
            ? () => runAction(() => api.fulfillStockRequisition(requisitionId).then(() => undefined), 'Requisição atendida e estoque atualizado.')
            : undefined
        }
        onCancel={
          requisitionId
            ? () => runAction(() => api.cancelStockRequisition(requisitionId).then(() => undefined), 'Requisição cancelada.')
            : undefined
        }
        onDeny={
          requisitionId && canManageWarehouse
            ? () => {
                const reason = window.prompt('Informe o motivo da negativa:');
                if (!reason?.trim()) return;
                runAction(
                  () => api.denyStockRequisition(requisitionId, { reason: reason.trim() }).then(() => undefined),
                  'Requisição negada.',
                );
              }
            : undefined
        }
        saving={saving}
        acting={acting}
      />
    </>
  );
}
