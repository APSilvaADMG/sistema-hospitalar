import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, type ProductDto, type ProductLotDto, type StockIssueDto } from '../../../api/client';

const ISSUE_TYPE_OPTIONS = [
  { value: 1, label: 'Consumo' },
  { value: 2, label: 'Perda' },
  { value: 3, label: 'Transferência' },
  { value: 4, label: 'Paciente' },
];

const SECTOR_OPTIONS = [
  'Enfermaria',
  'Centro Cirúrgico',
  'UTI',
  'Emergência',
  'Administração',
  'Farmácia',
  'Almoxarifado Central',
];

type ItemRow = {
  key: string;
  productId: string;
  quantity: string;
  productLotId: string;
};

function emptyRow(): ItemRow {
  return { key: crypto.randomUUID(), productId: '', quantity: '', productLotId: '' };
}

export function FeegowStockIssueForm() {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [lots, setLots] = useState<ProductLotDto[]>([]);
  const [sectorName, setSectorName] = useState('');
  const [responsibleName, setResponsibleName] = useState('');
  const [issueType, setIssueType] = useState(1);
  const [notes, setNotes] = useState('');
  const [items, setItems] = useState<ItemRow[]>([emptyRow()]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [lastIssue, setLastIssue] = useState<StockIssueDto | null>(null);

  useEffect(() => {
    Promise.all([
      api.getProducts('', false),
      api.getWarehouseLots(undefined, undefined),
    ])
      .then(([productList, lotList]) => {
        setProducts(productList);
        setLots(lotList);
      })
      .catch(() => {
        setProducts([]);
        setLots([]);
      });
  }, []);

  function updateItem(key: string, patch: Partial<ItemRow>) {
    setItems((prev) => prev.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  }

  function addRow() {
    setItems((prev) => [...prev, emptyRow()]);
  }

  function removeRow(key: string) {
    setItems((prev) => {
      const next = prev.filter((row) => row.key !== key);
      return next.length > 0 ? next : [emptyRow()];
    });
  }

  function lotsForProduct(productId: string) {
    return lots.filter((lot) => lot.productId === productId && lot.quantityOnHand > 0);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      if (!sectorName.trim()) {
        setError('Informe o setor.');
        return;
      }
      if (!responsibleName.trim()) {
        setError('Informe o responsável.');
        return;
      }

      const payloadItems = items
        .filter((row) => row.productId && row.quantity)
        .map((row) => ({
          productId: row.productId,
          quantity: Number(row.quantity),
          productLotId: row.productLotId || undefined,
        }));

      if (payloadItems.length === 0) {
        setError('Adicione ao menos um item.');
        return;
      }

      const issue = await api.createWarehouseIssue({
        sectorName: sectorName.trim(),
        responsibleName: responsibleName.trim(),
        issueType,
        notes: notes.trim() || undefined,
        items: payloadItems,
      });

      setLastIssue(issue);
      setSuccess('Saída registrada com sucesso.');
      setItems([emptyRow()]);
      setNotes('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar saída.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="feegow-warehouse-page">
      <header className="feegow-warehouse-head">
        <div>
          <h1 className="feegow-warehouse-title">Saída de estoque</h1>
          <p className="feegow-warehouse-subtitle">Baixa por consumo, transferência ou perda (FEFO)</p>
        </div>
        <div className="feegow-warehouse-head-actions">
          <Link to="/estoque/dashboard" className="feegow-warehouse-btn feegow-warehouse-btn-ghost">Dashboard</Link>
        </div>
      </header>

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}

      <form className="feegow-warehouse-form" onSubmit={handleSubmit}>
        <section className="feegow-warehouse-card">
          <div className="feegow-warehouse-form-grid">
            <label className="feegow-warehouse-field">
              <span>Setor *</span>
              <select value={sectorName} onChange={(e) => setSectorName(e.target.value)} required>
                <option value="">Selecione</option>
                {SECTOR_OPTIONS.map((sector) => (
                  <option key={sector} value={sector}>{sector}</option>
                ))}
              </select>
            </label>
            <label className="feegow-warehouse-field">
              <span>Responsável *</span>
              <input value={responsibleName} onChange={(e) => setResponsibleName(e.target.value)} required />
            </label>
            <label className="feegow-warehouse-field">
              <span>Tipo</span>
              <select value={issueType} onChange={(e) => setIssueType(Number(e.target.value))}>
                {ISSUE_TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </label>
            <label className="feegow-warehouse-field feegow-warehouse-field-wide">
              <span>Observações</span>
              <input value={notes} onChange={(e) => setNotes(e.target.value)} />
            </label>
          </div>
        </section>

        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head">
            <h2>Itens</h2>
            <button type="button" className="feegow-warehouse-btn feegow-warehouse-btn-ghost" onClick={addRow}>
              + Item
            </button>
          </header>

          <div className="feegow-warehouse-items">
            {items.map((row) => {
              const product = products.find((p) => p.id === row.productId);
              const productLots = lotsForProduct(row.productId);
              const isMedication = product?.type === 1;

              return (
                <article key={row.key} className="feegow-warehouse-item-row">
                  <label className="feegow-warehouse-field">
                    <span>Produto</span>
                    <select
                      value={row.productId}
                      onChange={(e) => updateItem(row.key, { productId: e.target.value, productLotId: '' })}
                    >
                      <option value="">Selecione</option>
                      {products.map((p) => (
                        <option key={p.id} value={p.id}>{p.name} — saldo {p.quantityOnHand}</option>
                      ))}
                    </select>
                  </label>
                  <label className="feegow-warehouse-field">
                    <span>Quantidade</span>
                    <input value={row.quantity} onChange={(e) => updateItem(row.key, { quantity: e.target.value })} />
                  </label>
                  {isMedication && productLots.length > 0 ? (
                    <label className="feegow-warehouse-field">
                      <span>Lote {isMedication ? '*' : ''}</span>
                      <select
                        value={row.productLotId}
                        onChange={(e) => updateItem(row.key, { productLotId: e.target.value })}
                      >
                        <option value="">FEFO automático</option>
                        {productLots.map((lot) => (
                          <option key={lot.id} value={lot.id}>
                            {lot.batchNumber} — {lot.quantityOnHand} (val. {lot.expiryDate ?? '—'})
                          </option>
                        ))}
                      </select>
                    </label>
                  ) : null}
                  <button type="button" className="feegow-warehouse-remove-btn" onClick={() => removeRow(row.key)} title="Remover">
                    🗑
                  </button>
                </article>
              );
            })}
          </div>
        </section>

        <footer className="feegow-warehouse-form-foot">
          <button type="submit" className="feegow-warehouse-btn feegow-warehouse-btn-primary" disabled={saving}>
            {saving ? 'Salvando…' : 'Registrar saída'}
          </button>
        </footer>
      </form>

      {lastIssue ? (
        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head"><h2>Última saída</h2></header>
          <p>{lastIssue.sectorName} — {lastIssue.items.length} item(ns)</p>
        </section>
      ) : null}
    </div>
  );
}
