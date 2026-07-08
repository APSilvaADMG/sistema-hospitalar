import { useEffect, useMemo, useState } from 'react';
import {
  api,
  type CreateTpaAdministratorRequest,
  type CreateTpaClaimRequest,
  type HealthInsuranceDto,
  type PatientDto,
  type TpaAdministratorDto,
  type TpaClaimDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { PageHeader } from '../components/PageHeader';

const emptyAdmin: CreateTpaAdministratorRequest = {
  name: '',
  cnpj: '',
  contactName: '',
  contactEmail: '',
  commissionPercent: 5,
  discountPercent: 2,
};

export function TpaPage() {
  const [admins, setAdmins] = useState<TpaAdministratorDto[]>([]);
  const [claims, setClaims] = useState<TpaClaimDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [adminForm, setAdminForm] = useState<CreateTpaAdministratorRequest>(emptyAdmin);
  const [claimForm, setClaimForm] = useState<CreateTpaClaimRequest>({
    tpaAdministratorId: '',
    patientId: '',
    healthInsuranceId: '',
    serviceDate: new Date().toISOString().slice(0, 10),
    grossAmount: 0,
    commissionPercent: undefined,
    discountPercent: undefined,
    notes: '',
  });
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  async function load() {
    const [adminList, claimList, patientPaged, insuranceList] = await Promise.all([
      api.getTpaAdministrators(),
      api.getTpaClaims(),
      api.getPatients('', 1),
      api.getHealthInsurances(),
    ]);
    setAdmins(adminList);
    setClaims(claimList);
    setPatients(patientPaged.items);
    setInsurances(insuranceList);
  }

  useEffect(() => {
    load().catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar TPA.'));
  }, []);

  const totals = useMemo(() => ({
    gross: claims.reduce((acc, c) => acc + c.grossAmount, 0),
    net: claims.reduce((acc, c) => acc + c.netAmount, 0),
    paid: claims.filter((c) => c.status === 5).length,
  }), [claims]);

  async function submitAdmin(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    await api.createTpaAdministrator(adminForm);
    setOk('Administradora TPA cadastrada.');
    setAdminForm(emptyAdmin);
    await load();
  }

  async function submitClaim(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    await api.createTpaClaim({
      ...claimForm,
      healthInsuranceId: claimForm.healthInsuranceId || undefined,
      notes: claimForm.notes || undefined,
    });
    setOk('Claim TPA criado.');
    setClaimForm({
      tpaAdministratorId: '',
      patientId: '',
      healthInsuranceId: '',
      serviceDate: new Date().toISOString().slice(0, 10),
      grossAmount: 0,
      commissionPercent: undefined,
      discountPercent: undefined,
      notes: '',
    });
    await load();
  }

  return (
    <>
      <PageHeader eyebrow="Convênios" title="TPA" subtitle="Administradoras, claims e relatório básico." />
      {error && <div className="alert alert-error">{error}</div>}
      {ok && <div className="alert alert-success">{ok}</div>}

      <div className="kpi-grid">
        <KpiCard label="Administradoras" value={admins.length} variant="primary" />
        <KpiCard label="Claims" value={claims.length} variant="info" />
        <KpiCard label="Bruto total" value={totals.gross.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} variant="warning" />
        <KpiCard label="Líquido total" value={totals.net.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} variant="success" />
        <KpiCard label="Claims pagos" value={totals.paid} variant="neutral" />
      </div>

      <div className="grid-2" style={{ marginTop: 16 }}>
        <form className="card form-grid" onSubmit={submitAdmin}>
          <h3 style={{ marginTop: 0 }}>Nova administradora TPA</h3>
          <div className="form-field"><label>Nome *</label><input required value={adminForm.name} onChange={(e) => setAdminForm({ ...adminForm, name: e.target.value })} /></div>
          <div className="form-field"><label>CNPJ</label><input value={adminForm.cnpj} onChange={(e) => setAdminForm({ ...adminForm, cnpj: e.target.value })} /></div>
          <div className="form-field"><label>Contato</label><input value={adminForm.contactName} onChange={(e) => setAdminForm({ ...adminForm, contactName: e.target.value })} /></div>
          <div className="form-field"><label>Email</label><input type="email" value={adminForm.contactEmail} onChange={(e) => setAdminForm({ ...adminForm, contactEmail: e.target.value })} /></div>
          <div className="form-field"><label>Comissão %</label><input type="number" step="0.01" value={adminForm.commissionPercent} onChange={(e) => setAdminForm({ ...adminForm, commissionPercent: Number(e.target.value) })} /></div>
          <div className="form-field"><label>Desconto %</label><input type="number" step="0.01" value={adminForm.discountPercent} onChange={(e) => setAdminForm({ ...adminForm, discountPercent: Number(e.target.value) })} /></div>
          <div className="form-actions"><button className="btn" type="submit">Salvar administradora</button></div>
        </form>

        <form className="card form-grid" onSubmit={submitClaim}>
          <h3 style={{ marginTop: 0 }}>Novo claim</h3>
          <div className="form-field"><label>Administradora *</label><select required value={claimForm.tpaAdministratorId} onChange={(e) => setClaimForm({ ...claimForm, tpaAdministratorId: e.target.value })}><option value="">Selecione</option>{admins.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}</select></div>
          <div className="form-field"><label>Paciente *</label><select required value={claimForm.patientId} onChange={(e) => setClaimForm({ ...claimForm, patientId: e.target.value })}><option value="">Selecione</option>{patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}</select></div>
          <div className="form-field"><label>Convênio</label><select value={claimForm.healthInsuranceId} onChange={(e) => setClaimForm({ ...claimForm, healthInsuranceId: e.target.value })}><option value="">Particular</option>{insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}</select></div>
          <div className="form-field"><label>Data *</label><input type="date" required value={claimForm.serviceDate} onChange={(e) => setClaimForm({ ...claimForm, serviceDate: e.target.value })} /></div>
          <div className="form-field"><label>Valor bruto *</label><input type="number" step="0.01" required value={claimForm.grossAmount} onChange={(e) => setClaimForm({ ...claimForm, grossAmount: Number(e.target.value) })} /></div>
          <div className="form-field full"><label>Observações</label><input value={claimForm.notes} onChange={(e) => setClaimForm({ ...claimForm, notes: e.target.value })} /></div>
          <div className="form-actions"><button className="btn" type="submit">Criar claim</button></div>
        </form>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">Claims TPA</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Data</th><th>Administradora</th><th>Paciente</th><th>Bruto</th><th>Líquido</th><th>Status</th></tr></thead>
            <tbody>
              {claims.map((c) => (
                <tr key={c.id}>
                  <td>{c.serviceDate}</td>
                  <td>{c.tpaAdministratorName}</td>
                  <td>{c.patientName}</td>
                  <td>{c.grossAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</td>
                  <td>{c.netAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</td>
                  <td>{c.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

