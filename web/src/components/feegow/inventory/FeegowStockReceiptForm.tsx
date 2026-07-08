import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, type ProductDto, type StockReceiptDto } from '../../../api/client';
import { parseNfeXml } from '../../../utils/nfeXmlParser';

type ItemRow = {
  key: string;
  productId: string;
  batchNumber: string;
  expiryDate: string;
  quantity: string;
  unitPrice: string;
  manufacturer: string;
  locationName: string;
  ncm: string;
  cfop: string;
};

function emptyRow(): ItemRow {
  return {
    key: crypto.randomUUID(),
    productId: '',
    batchNumber: '',
    expiryDate: '',
    quantity: '',
    unitPrice: '',
    manufacturer: '',
    locationName: '',
    ncm: '',
    cfop: '',
  };
}

function digitsOnly(value: string): string {
  return value.replace(/\D/g, '');
}

function formatCnpj(value: string): string {
  const d = digitsOnly(value).slice(0, 14);
  if (d.length <= 2) return d;
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`;
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`;
  if (d.length <= 12) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
}

function parseMoney(value: string): number {
  const normalized = value.replace(/\./g, '').replace(',', '.');
  const n = Number(normalized);
  return Number.isFinite(n) ? n : 0;
}

function formatMoney(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function FeegowStockReceiptForm() {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [supplierName, setSupplierName] = useState('');
  const [supplierCnpj, setSupplierCnpj] = useState('');
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [invoiceSeries, setInvoiceSeries] = useState('');
  const [invoiceIssueDate, setInvoiceIssueDate] = useState('');
  const [nfeAccessKey, setNfeAccessKey] = useState('');
  const [freightAmount, setFreightAmount] = useState('');
  const [discountAmount, setDiscountAmount] = useState('');
  const [paymentCondition, setPaymentCondition] = useState('');
  const [notes, setNotes] = useState('');
  const [receivedByUserName, setReceivedByUserName] = useState('');
  const [items, setItems] = useState<ItemRow[]>([emptyRow()]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [lastReceipt, setLastReceipt] = useState<StockReceiptDto | null>(null);
  const xmlInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    api.getProducts('', false).then(setProducts).catch(() => setProducts([]));
  }, []);

  const productsSubtotal = useMemo(() => {
    return items.reduce((sum, row) => {
      const qty = Number(row.quantity);
      const price = parseMoney(row.unitPrice);
      if (!row.productId || !Number.isFinite(qty) || qty <= 0) return sum;
      return sum + qty * price;
    }, 0);
  }, [items]);

  const nfTotal = useMemo(() => {
    return productsSubtotal + parseMoney(freightAmount) - parseMoney(discountAmount);
  }, [productsSubtotal, freightAmount, discountAmount]);

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

  function lineTotal(row: ItemRow): number {
    const qty = Number(row.quantity);
    const price = parseMoney(row.unitPrice);
    if (!Number.isFinite(qty) || qty <= 0) return 0;
    return qty * price;
  }

  async function handleNfeXmlImport(file: File) {
    setError('');
    setSuccess('');
    try {
      const xmlText = await file.text();
      const parsed = parseNfeXml(xmlText);

      setSupplierName(parsed.supplierName);
      setSupplierCnpj(parsed.supplierCnpj ? formatCnpj(parsed.supplierCnpj) : '');
      setInvoiceNumber(parsed.invoiceNumber);
      setInvoiceSeries(parsed.invoiceSeries);
      setInvoiceIssueDate(parsed.invoiceIssueDate);
      setNfeAccessKey(parsed.nfeAccessKey);
      setFreightAmount(formatMoney(parsed.freightAmount));
      setDiscountAmount(formatMoney(parsed.discountAmount));

      const importedRows: ItemRow[] = parsed.items.map((item) => {
        const normalizedName = item.description.toLowerCase();
        const matched = products.find((product) =>
          product.name.toLowerCase() === normalizedName
          || product.sku.toLowerCase() === normalizedName);
        return {
          key: crypto.randomUUID(),
          productId: matched?.id ?? '',
          batchNumber: `LOTE-${parsed.invoiceNumber || 'NF'}`,
          expiryDate: '',
          quantity: String(item.quantity),
          unitPrice: formatMoney(item.unitPrice),
          manufacturer: matched?.manufacturer ?? '',
          locationName: matched?.defaultLocation ?? '',
          ncm: item.ncm,
          cfop: item.cfop,
        };
      });

      setItems(importedRows.length > 0 ? importedRows : [emptyRow()]);
      setSuccess(`NF-e importada: ${parsed.items.length} item(ns)${parsed.supplierName ? ` — ${parsed.supplierName}` : ''}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível importar o XML da NF-e.');
    } finally {
      if (xmlInputRef.current) {
        xmlInputRef.current.value = '';
      }
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      if (!supplierName.trim()) {
        setError('Informe o fornecedor.');
        return;
      }

      const cnpjDigits = digitsOnly(supplierCnpj);
      if (cnpjDigits && (cnpjDigits.length < 11 || cnpjDigits.length > 14)) {
        setError('CNPJ deve ter entre 11 e 14 dígitos.');
        return;
      }

      const keyDigits = digitsOnly(nfeAccessKey);
      if (keyDigits && keyDigits.length !== 44) {
        setError('Chave NF-e deve ter 44 dígitos.');
        return;
      }

      const payloadItems = items
        .filter((row) => row.productId && row.batchNumber.trim())
        .map((row) => {
          const product = products.find((p) => p.id === row.productId);
          return {
            productId: row.productId,
            batchNumber: row.batchNumber.trim(),
            expiryDate: row.expiryDate || undefined,
            quantity: Number(row.quantity),
            unitPrice: parseMoney(row.unitPrice),
            manufacturer: row.manufacturer.trim() || product?.manufacturer,
            locationName: row.locationName.trim() || product?.defaultLocation,
            ncm: row.ncm.trim() || undefined,
            cfop: row.cfop.trim() || undefined,
          };
        });

      if (payloadItems.length === 0) {
        setError('Adicione ao menos um item com produto e lote.');
        return;
      }

      const receipt = await api.createWarehouseReceipt({
        supplierName: supplierName.trim(),
        supplierCnpj: cnpjDigits || undefined,
        invoiceNumber: invoiceNumber.trim() || undefined,
        invoiceSeries: invoiceSeries.trim() || undefined,
        invoiceIssueDate: invoiceIssueDate || undefined,
        nfeAccessKey: keyDigits || undefined,
        freightAmount: parseMoney(freightAmount),
        discountAmount: parseMoney(discountAmount),
        paymentCondition: paymentCondition.trim() || undefined,
        notes: notes.trim() || undefined,
        receivedByUserName: receivedByUserName.trim() || undefined,
        items: payloadItems,
      });

      setLastReceipt(receipt);
      setSuccess(`Entrada registrada — NF ${receipt.invoiceNumber ?? receipt.id.slice(0, 8)}.`);
      setItems([emptyRow()]);
      setInvoiceNumber('');
      setInvoiceSeries('');
      setInvoiceIssueDate('');
      setNfeAccessKey('');
      setFreightAmount('');
      setDiscountAmount('');
      setPaymentCondition('');
      setNotes('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar entrada.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="feegow-warehouse-page">
      <header className="feegow-warehouse-head">
        <div>
          <h1 className="feegow-warehouse-title">Entrada de estoque (NF)</h1>
          <p className="feegow-warehouse-subtitle">Registro de recebimento com rastreabilidade por lote</p>
        </div>
        <div className="feegow-warehouse-head-actions">
          <Link to="/estoque/dashboard" className="feegow-warehouse-btn feegow-warehouse-btn-ghost">Dashboard</Link>
        </div>
      </header>

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}

      <form className="feegow-warehouse-form" onSubmit={handleSubmit}>
        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head"><h2>Dados do fornecedor</h2></header>
          <div className="feegow-warehouse-form-grid">
            <label className="feegow-warehouse-field">
              <span>Fornecedor *</span>
              <input value={supplierName} onChange={(e) => setSupplierName(e.target.value)} required />
            </label>
            <label className="feegow-warehouse-field">
              <span>CNPJ</span>
              <input
                value={supplierCnpj}
                onChange={(e) => setSupplierCnpj(formatCnpj(e.target.value))}
                placeholder="00.000.000/0000-00"
                maxLength={18}
              />
            </label>
            <label className="feegow-warehouse-field">
              <span>Recebido por</span>
              <input value={receivedByUserName} onChange={(e) => setReceivedByUserName(e.target.value)} />
            </label>
          </div>
        </section>

        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head">
            <h2>Dados da NF-e</h2>
            <div className="feegow-warehouse-head-actions">
              <input
                ref={xmlInputRef}
                type="file"
                accept=".xml,text/xml,application/xml"
                hidden
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) handleNfeXmlImport(file).catch(console.error);
                }}
              />
              <button
                type="button"
                className="feegow-warehouse-btn feegow-warehouse-btn-ghost"
                onClick={() => xmlInputRef.current?.click()}
              >
                Importar XML NF-e
              </button>
            </div>
          </header>
          <div className="feegow-warehouse-form-grid">
            <label className="feegow-warehouse-field">
              <span>Número da nota</span>
              <input value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} />
            </label>
            <label className="feegow-warehouse-field">
              <span>Série</span>
              <input value={invoiceSeries} onChange={(e) => setInvoiceSeries(e.target.value)} maxLength={10} />
            </label>
            <label className="feegow-warehouse-field">
              <span>Data de emissão</span>
              <input type="date" value={invoiceIssueDate} onChange={(e) => setInvoiceIssueDate(e.target.value)} />
            </label>
            <label className="feegow-warehouse-field feegow-warehouse-field-wide">
              <span>Chave de acesso NF-e</span>
              <input
                value={nfeAccessKey}
                onChange={(e) => setNfeAccessKey(digitsOnly(e.target.value).slice(0, 44))}
                placeholder="44 dígitos"
                maxLength={44}
              />
            </label>
            <label className="feegow-warehouse-field feegow-warehouse-field-wide">
              <span>Observações</span>
              <input value={notes} onChange={(e) => setNotes(e.target.value)} />
            </label>
          </div>
        </section>

        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head"><h2>Valores</h2></header>
          <div className="feegow-warehouse-form-grid">
            <label className="feegow-warehouse-field">
              <span>Frete (R$)</span>
              <input value={freightAmount} onChange={(e) => setFreightAmount(e.target.value)} placeholder="0,00" />
            </label>
            <label className="feegow-warehouse-field">
              <span>Desconto (R$)</span>
              <input value={discountAmount} onChange={(e) => setDiscountAmount(e.target.value)} placeholder="0,00" />
            </label>
            <label className="feegow-warehouse-field">
              <span>Condição de pagamento</span>
              <input
                value={paymentCondition}
                onChange={(e) => setPaymentCondition(e.target.value)}
                placeholder="ex: 30/60/90"
              />
            </label>
          </div>
          <div className="feegow-warehouse-nf-totals">
            <div><span>Subtotal produtos</span><strong>R$ {formatMoney(productsSubtotal)}</strong></div>
            <div><span>Frete</span><strong>R$ {formatMoney(parseMoney(freightAmount))}</strong></div>
            <div><span>Desconto</span><strong>− R$ {formatMoney(parseMoney(discountAmount))}</strong></div>
            <div className="feegow-warehouse-nf-total-final">
              <span>Total NF</span><strong>R$ {formatMoney(nfTotal)}</strong>
            </div>
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
            {items.map((row) => (
              <article key={row.key} className="feegow-warehouse-item-row">
                <label className="feegow-warehouse-field">
                  <span>Produto</span>
                  <select
                    value={row.productId}
                    onChange={(e) => {
                      const product = products.find((p) => p.id === e.target.value);
                      updateItem(row.key, {
                        productId: e.target.value,
                        manufacturer: product?.manufacturer ?? '',
                        locationName: product?.defaultLocation ?? '',
                      });
                    }}
                  >
                    <option value="">Selecione</option>
                    {products.map((p) => (
                      <option key={p.id} value={p.id}>{p.name} ({p.sku})</option>
                    ))}
                  </select>
                </label>
                <label className="feegow-warehouse-field">
                  <span>Lote *</span>
                  <input value={row.batchNumber} onChange={(e) => updateItem(row.key, { batchNumber: e.target.value })} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>Validade</span>
                  <input type="date" value={row.expiryDate} onChange={(e) => updateItem(row.key, { expiryDate: e.target.value })} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>Quantidade</span>
                  <input value={row.quantity} onChange={(e) => updateItem(row.key, { quantity: e.target.value })} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>Preço unit.</span>
                  <input value={row.unitPrice} onChange={(e) => updateItem(row.key, { unitPrice: e.target.value })} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>NCM</span>
                  <input value={row.ncm} onChange={(e) => updateItem(row.key, { ncm: e.target.value })} maxLength={10} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>CFOP</span>
                  <input value={row.cfop} onChange={(e) => updateItem(row.key, { cfop: e.target.value })} maxLength={10} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>Fabricante</span>
                  <input value={row.manufacturer} onChange={(e) => updateItem(row.key, { manufacturer: e.target.value })} />
                </label>
                <label className="feegow-warehouse-field">
                  <span>Localização</span>
                  <input value={row.locationName} onChange={(e) => updateItem(row.key, { locationName: e.target.value })} />
                </label>
                <div className="feegow-warehouse-line-total">
                  <span>Total linha</span>
                  <strong>R$ {formatMoney(lineTotal(row))}</strong>
                </div>
                <button type="button" className="feegow-warehouse-remove-btn" onClick={() => removeRow(row.key)} title="Remover">
                  🗑
                </button>
              </article>
            ))}
          </div>
        </section>

        <footer className="feegow-warehouse-form-foot">
          <button type="submit" className="feegow-warehouse-btn feegow-warehouse-btn-primary" disabled={saving}>
            {saving ? 'Salvando…' : 'Registrar entrada'}
          </button>
        </footer>
      </form>

      {lastReceipt ? (
        <section className="feegow-warehouse-card">
          <header className="feegow-warehouse-card-head"><h2>Última entrada</h2></header>
          <div className="feegow-warehouse-last-receipt">
            <p><strong>{lastReceipt.supplierName}</strong>{lastReceipt.supplierCnpj ? ` — CNPJ ${formatCnpj(lastReceipt.supplierCnpj)}` : ''}</p>
            <p>
              NF {lastReceipt.invoiceNumber ?? '—'}
              {lastReceipt.invoiceSeries ? ` / Série ${lastReceipt.invoiceSeries}` : ''}
              {lastReceipt.invoiceIssueDate ? ` — Emissão ${lastReceipt.invoiceIssueDate}` : ''}
            </p>
            {lastReceipt.nfeAccessKey ? <p className="feegow-warehouse-nfe-key">Chave: {lastReceipt.nfeAccessKey}</p> : null}
            <p>{lastReceipt.items.length} item(ns) — Total R$ {formatMoney(lastReceipt.totalAmount)}</p>
            {lastReceipt.paymentCondition ? <p>Pagamento: {lastReceipt.paymentCondition}</p> : null}
          </div>
        </section>
      ) : null}
    </div>
  );
}
