import { useEffect, useState } from 'react';
import { api, type BirthRegistrationDto, type CreateBirthRegistrationRequest, type PatientDto } from '../api/client';
import { PageHeader } from '../components/PageHeader';

const nowIso = new Date().toISOString().slice(0, 16);
const emptyForm: CreateBirthRegistrationRequest = {
  motherPatientId: '',
  babyName: '',
  birthAt: nowIso,
  weightKg: 3.1,
  heightCm: 49,
  notes: '',
};

export function BirthRegistrationPage() {
  const [items, setItems] = useState<BirthRegistrationDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [form, setForm] = useState<CreateBirthRegistrationRequest>(emptyForm);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  async function load() {
    const [records, patientPaged] = await Promise.all([
      api.getBirthRegistrations(),
      api.getPatients('', 1),
    ]);
    setItems(records);
    setPatients(patientPaged.items);
  }

  useEffect(() => {
    load().catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar registros.'));
  }, []);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    await api.createBirthRegistration({
      ...form,
      notes: form.notes || undefined,
    });
    setOk('Registro de nascimento salvo.');
    setForm(emptyForm);
    await load();
  }

  return (
    <>
      <PageHeader eyebrow="Recepção / Internação" title="Registro de nascimento" subtitle="Registro básico de recém-nascido vinculado à paciente mãe." />
      {error && <div className="alert alert-error">{error}</div>}
      {ok && <div className="alert alert-success">{ok}</div>}

      <form className="card form-grid" onSubmit={submit}>
        <div className="form-field"><label>Mãe *</label><select required value={form.motherPatientId} onChange={(e) => setForm({ ...form, motherPatientId: e.target.value })}><option value="">Selecione</option>{patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}</select></div>
        <div className="form-field"><label>Nome do bebê *</label><input required value={form.babyName} onChange={(e) => setForm({ ...form, babyName: e.target.value })} /></div>
        <div className="form-field"><label>Data/hora *</label><input type="datetime-local" required value={form.birthAt} onChange={(e) => setForm({ ...form, birthAt: e.target.value })} /></div>
        <div className="form-field"><label>Peso (kg) *</label><input type="number" step="0.001" required value={form.weightKg} onChange={(e) => setForm({ ...form, weightKg: Number(e.target.value) })} /></div>
        <div className="form-field"><label>Altura (cm) *</label><input type="number" step="0.01" required value={form.heightCm} onChange={(e) => setForm({ ...form, heightCm: Number(e.target.value) })} /></div>
        <div className="form-field full"><label>Observações</label><input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></div>
        <div className="form-actions"><button className="btn" type="submit">Salvar registro</button></div>
      </form>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">Registros recentes</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Data</th><th>Mãe</th><th>Bebê</th><th>Peso</th><th>Altura</th></tr></thead>
            <tbody>
              {items.map((r) => (
                <tr key={r.id}>
                  <td>{r.birthAt}</td>
                  <td>{r.motherName}</td>
                  <td>{r.babyName}</td>
                  <td>{r.weightKg} kg</td>
                  <td>{r.heightCm} cm</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

