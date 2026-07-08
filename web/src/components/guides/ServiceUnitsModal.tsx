import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type CreateServiceUnitRequest,
  type ServiceUnitDto,
  type UpdateServiceUnitRequest,
} from '../../api/client';
import { Modal } from '../Modal';

type UnitForm = {
  name: string;
  code: string;
  cnes: string;
  address: string;
  isDefault: boolean;
  isActive: boolean;
};

const emptyForm = (): UnitForm => ({
  name: '',
  code: '',
  cnes: '',
  address: '',
  isDefault: false,
  isActive: true,
});

function unitToForm(u: ServiceUnitDto): UnitForm {
  return {
    name: u.name,
    code: u.code,
    cnes: u.cnes ?? '',
    address: u.address ?? '',
    isDefault: u.isDefault,
    isActive: u.isActive,
  };
}

type Props = {
  open: boolean;
  onClose: () => void;
  onChanged: () => void;
  onError: (message: string) => void;
  onSuccess: (message: string) => void;
};

export function ServiceUnitsModal({ open, onClose, onChanged, onError, onSuccess }: Props) {
  const [units, setUnits] = useState<ServiceUnitDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [editing, setEditing] = useState<ServiceUnitDto | null>(null);
  const [form, setForm] = useState<UnitForm>(emptyForm());
  const [saving, setSaving] = useState(false);

  async function loadUnits() {
    setLoading(true);
    try {
      const list = await api.getServiceUnits();
      setUnits(list);
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Erro ao carregar unidades.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (open) {
      setEditing(null);
      setForm(emptyForm());
      loadUnits().catch(console.error);
    }
  }, [open]);

  function startCreate() {
    setEditing(null);
    setForm(emptyForm());
  }

  function startEdit(unit: ServiceUnitDto) {
    setEditing(unit);
    setForm(unitToForm(unit));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      if (editing) {
        const payload: UpdateServiceUnitRequest = {
          name: form.name.trim(),
          code: form.code.trim(),
          cnes: form.cnes.trim() || undefined,
          address: form.address.trim() || undefined,
          isDefault: form.isDefault,
          isActive: form.isActive,
        };
        await api.updateServiceUnit(editing.id, payload);
        onSuccess('Unidade atualizada.');
      } else {
        const payload: CreateServiceUnitRequest = {
          name: form.name.trim(),
          code: form.code.trim(),
          cnes: form.cnes.trim() || undefined,
          address: form.address.trim() || undefined,
          isDefault: form.isDefault,
        };
        await api.createServiceUnit(payload);
        onSuccess('Unidade criada.');
      }
      setEditing(null);
      setForm(emptyForm());
      await loadUnits();
      onChanged();
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Erro ao salvar unidade.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Unidades de atendimento" subtitle="Cadastro usado em guias TISS e SUS." width="lg">
      <div className="guides-service-units">
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12 }}>
          <strong>{units.length} unidade(s)</strong>
          <button type="button" className="btn btn-secondary btn-sm" onClick={startCreate}>Nova unidade</button>
        </div>

        {loading && <p>Carregando…</p>}
        {!loading && (
          <table className="data-table" style={{ marginBottom: 16 }}>
            <thead>
              <tr>
                <th>Nome</th>
                <th>Código</th>
                <th>CNES</th>
                <th>Padrão</th>
                <th>Ativa</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {units.map((u) => (
                <tr key={u.id}>
                  <td>{u.name}</td>
                  <td>{u.code}</td>
                  <td>{u.cnes ?? '—'}</td>
                  <td>{u.isDefault ? 'Sim' : '—'}</td>
                  <td>{u.isActive ? 'Sim' : 'Não'}</td>
                  <td>
                    <button type="button" className="btn btn-secondary btn-xs" onClick={() => startEdit(u)}>Editar</button>
                  </td>
                </tr>
              ))}
              {units.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign: 'center', color: 'var(--muted)' }}>Nenhuma unidade cadastrada.</td></tr>
              )}
            </tbody>
          </table>
        )}

        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field">
            <label>Nome *</label>
            <input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div className="form-field">
            <label>Código *</label>
            <input required value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
          </div>
          <div className="form-field">
            <label>CNES</label>
            <input value={form.cnes} onChange={(e) => setForm({ ...form, cnes: e.target.value })} />
          </div>
          <div className="form-field">
            <label>Endereço</label>
            <input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
          </div>
          <div className="form-field">
            <label>
              <input
                type="checkbox"
                checked={form.isDefault}
                onChange={(e) => setForm({ ...form, isDefault: e.target.checked })}
              />
              {' '}Unidade padrão
            </label>
          </div>
          {editing && (
            <div className="form-field">
              <label>
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                />
                {' '}Ativa
              </label>
            </div>
          )}
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Fechar</button>
            <button type="submit" className="btn" disabled={saving}>
              {saving ? 'Salvando…' : editing ? 'Salvar unidade' : 'Criar unidade'}
            </button>
          </div>
        </form>
      </div>
    </Modal>
  );
}
