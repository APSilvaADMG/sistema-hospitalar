import { useEffect, useMemo, useState } from 'react';
import {
  api,
  type CreatePharmacyBillingEntryRequest,
  type HealthInsuranceDto,
  type PharmacyBillingEntryDto,
  type PharmacyDispensingDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { pharmacyTabs } from '../navigation/moduleSections';

const emptyForm: CreatePharmacyBillingEntryRequest = {
  dispensingId: '',
  payerType: 1,
  healthInsuranceId: '',
  unitPrice: 0,
  paid: false,
  notes: '',
  createFinancialAccountWhenPaid: true,
};

export function PharmacyBillingPage() {
  const [entries, setEntries] = useState<PharmacyBillingEntryDto[]>([]);
  const [dispensings, setDispensings] = useState<PharmacyDispensingDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [form, setForm] = useState<CreatePharmacyBillingEntryRequest>(emptyForm);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  async function load() {
    const [entryList, dispensingList, insuranceList] = await Promise.all([
      api.getPharmacyBillingEntries(),
      api.getDispensings(),
      api.getHealthInsurances(),
    ]);
    setEntries(entryList);
    setDispensings(dispensingList);
    setInsurances(insuranceList);
  }

  useEffect(() => {
    load().catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar faturamento.'));
  }, []);

  const totals = useMemo(() => ({
    total: entries.reduce((acc, i) => acc + i.totalAmount, 0),
    paid: entries.filter((i) => i.paid).reduce((acc, i) => acc + i.totalAmount, 0),
    pending: entries.filter((i) => !i.paid).reduce((acc, i) => acc + i.totalAmount, 0),
  }), [entries]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    await api.createPharmacyBillingEntry({
      ...form,
      healthInsuranceId: form.healthInsuranceId || undefined,
      notes: form.notes || undefined,
    });
    setOk('Faturamento registrado.');
    setForm(emptyForm);
    await load();
  }

  return (
    <>
      <PageHeader eyebrow="Farmácia" title="Faturamento / venda" subtitle="Particular ou convênio com vínculo financeiro quando pago." />
      <ModuleNav basePath="/farmacia" tabs={pharmacyTabs} contextId="pharmacy" />
      {error && <div className="alert alert-error">{error}</div>}
      {ok && <div className="alert alert-success">{ok}</div>}

      <div className="kpi-grid">
        <KpiCard label="Total faturado" value={totals.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} variant="primary" />
        <KpiCard label="Pago" value={totals.paid.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} variant="success" />
        <KpiCard label="Pendente" value={totals.pending.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} variant="warning" />
      </div>

      <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={submit}>
        <h3 style={{ marginTop: 0 }}>Novo faturamento de medicamento</h3>
        <div className="form-field"><label>Dispensação *</label><select required value={form.dispensingId} onChange={(e) => setForm({ ...form, dispensingId: e.target.value })}><option value="">Selecione</option>{dispensings.map((d) => <option key={d.id} value={d.id}>{d.patientName} - {d.productName} ({d.quantity})</option>)}</select></div>
        <div className="form-field"><label>Tipo *</label><select required value={form.payerType} onChange={(e) => setForm({ ...form, payerType: Number(e.target.value) })}><option value={1}>Particular</option><option value={2}>Convênio</option></select></div>
        <div className="form-field"><label>Convênio</label><select value={form.healthInsuranceId} onChange={(e) => setForm({ ...form, healthInsuranceId: e.target.value })}><option value="">—</option>{insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}</select></div>
        <div className="form-field"><label>Preço unitário *</label><input type="number" step="0.01" required value={form.unitPrice} onChange={(e) => setForm({ ...form, unitPrice: Number(e.target.value) })} /></div>
        <div className="form-field checkbox"><label><input type="checkbox" checked={form.paid} onChange={(e) => setForm({ ...form, paid: e.target.checked })} /> Pago</label></div>
        <div className="form-field checkbox"><label><input type="checkbox" checked={!!form.createFinancialAccountWhenPaid} onChange={(e) => setForm({ ...form, createFinancialAccountWhenPaid: e.target.checked })} /> Vincular financeiro quando pago</label></div>
        <div className="form-field full"><label>Observações</label><input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></div>
        <div className="form-actions"><button className="btn" type="submit">Registrar faturamento</button></div>
      </form>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">Lançamentos</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Data</th><th>Paciente</th><th>Produto</th><th>Tipo</th><th>Total</th><th>Pago</th><th>Financeiro</th></tr></thead>
            <tbody>
              {entries.map((e) => (
                <tr key={e.id}>
                  <td>{e.dispensedAt}</td>
                  <td>{e.patientName}</td>
                  <td>{e.productName}</td>
                  <td>{e.payerType === 2 ? `Convênio ${e.healthInsuranceName ?? ''}` : 'Particular'}</td>
                  <td>{e.totalAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</td>
                  <td>{e.paid ? 'Sim' : 'Não'}</td>
                  <td>{e.financialAccountId ? 'Vinculado' : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

