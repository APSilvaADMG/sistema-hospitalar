import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type PatientVaccinationDto,
  type VaccineCatalogDto,
} from '../../../api/client';
import { formatBrDate } from '../../../utils/dateUtils';

const SCHEDULE_TABS = [
  { value: undefined, label: 'Todas' },
  { value: 1, label: 'Criança' },
  { value: 2, label: 'Grávida' },
  { value: 3, label: 'Adulto' },
] as const;

type Props = {
  patientId?: string;
  patientName?: string;
  compact?: boolean;
};

export function FeegowVaccinationPanel({ patientId: fixedPatientId, patientName, compact }: Props) {
  const [catalog, setCatalog] = useState<VaccineCatalogDto[]>([]);
  const [records, setRecords] = useState<PatientVaccinationDto[]>([]);
  const [scheduleFilter, setScheduleFilter] = useState<number | undefined>(undefined);
  const [patientId, setPatientId] = useState(fixedPatientId ?? '');
  const [patientSearch, setPatientSearch] = useState('');
  const [patients, setPatients] = useState<{ id: string; fullName: string }[]>([]);
  const [form, setForm] = useState({
    vaccineCatalogId: '',
    administeredAt: new Date().toISOString().slice(0, 10),
    doseNumber: '1',
    batchNumber: '',
    notes: '',
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadCatalog = useCallback(async () => {
    try {
      const items = await api.getVaccineCatalog(scheduleFilter);
      setCatalog(items);
    } catch {
      setCatalog([]);
    }
  }, [scheduleFilter]);

  const loadRecords = useCallback(async () => {
    if (!patientId) {
      setRecords([]);
      return;
    }
    try {
      const items = await api.getPatientVaccinations(patientId);
      setRecords(items);
    } catch {
      setRecords([]);
    }
  }, [patientId]);

  useEffect(() => {
    void loadCatalog();
  }, [loadCatalog]);

  useEffect(() => {
    setLoading(true);
    void loadRecords().finally(() => setLoading(false));
  }, [loadRecords]);

  useEffect(() => {
    if (fixedPatientId) {
      setPatientId(fixedPatientId);
      return;
    }
    const timer = window.setTimeout(() => {
      api.getPatients(patientSearch, 1)
        .then((r) => setPatients(r.items.map((p) => ({ id: p.id, fullName: p.fullName }))))
        .catch(() => setPatients([]));
    }, 300);
    return () => window.clearTimeout(timer);
  }, [fixedPatientId, patientSearch]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!patientId) {
      setError('Selecione o paciente.');
      return;
    }
    if (!form.vaccineCatalogId) {
      setError('Selecione a vacina.');
      return;
    }
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await api.createPatientVaccination({
        patientId,
        vaccineCatalogId: form.vaccineCatalogId,
        administeredAt: new Date(`${form.administeredAt}T12:00:00`).toISOString(),
        doseNumber: Number(form.doseNumber) || 1,
        batchNumber: form.batchNumber.trim() || undefined,
        notes: form.notes.trim() || undefined,
      });
      setSuccess('Vacinação registrada.');
      setForm((prev) => ({ ...prev, batchNumber: '', notes: '' }));
      await loadRecords();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar vacinação.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className={compact ? 'feegow-vaccination-panel' : 'feegow-finance-page'}>
      {!compact ? (
        <header className="feegow-inventory-page-head">
          <div className="feegow-inventory-breadcrumb">
            <span>Pacientes</span>
            <span className="feegow-inventory-crumb-sep">/</span>
            <span>💉 Vacinação</span>
          </div>
        </header>
      ) : null}

      {error ? <div className="alert alert-error feegow-agenda-alert">{error}</div> : null}
      {success ? <div className="alert alert-success feegow-agenda-alert">{success}</div> : null}

      <div className="feegow-vaccination-schedule-tabs">
        {SCHEDULE_TABS.map((tab) => (
          <button
            key={tab.label}
            type="button"
            className={`feegow-vaccination-tab${scheduleFilter === tab.value ? ' is-active' : ''}`}
            onClick={() => setScheduleFilter(tab.value)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <section className="feegow-finance-panel">
        <header className="feegow-finance-panel-head">
          <h3>Registrar vacinação</h3>
          {patientName ? <span className="feegow-finance-panel-sub">{patientName}</span> : null}
        </header>
        <form className="feegow-finance-form-grid" onSubmit={(e) => { void handleSubmit(e); }}>
          {!fixedPatientId ? (
            <label className="feegow-finance-field-wide">
              Paciente
              <input
                type="search"
                value={patientSearch}
                onChange={(e) => setPatientSearch(e.target.value)}
                placeholder="Buscar paciente..."
              />
              <select value={patientId} onChange={(e) => setPatientId(e.target.value)} required>
                <option value="">Selecione</option>
                {patients.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            </label>
          ) : null}
          <label className="feegow-finance-field-wide">
            Vacina
            <select
              required
              value={form.vaccineCatalogId}
              onChange={(e) => setForm({ ...form, vaccineCatalogId: e.target.value })}
            >
              <option value="">Selecione</option>
              {catalog.map((v) => (
                <option key={v.id} value={v.id}>{v.name}</option>
              ))}
            </select>
          </label>
          <label>
            Data
            <input
              type="date"
              required
              value={form.administeredAt}
              onChange={(e) => setForm({ ...form, administeredAt: e.target.value })}
            />
          </label>
          <label>
            Dose nº
            <input
              type="number"
              min={1}
              value={form.doseNumber}
              onChange={(e) => setForm({ ...form, doseNumber: e.target.value })}
            />
          </label>
          <label>
            Lote
            <input
              type="text"
              value={form.batchNumber}
              onChange={(e) => setForm({ ...form, batchNumber: e.target.value })}
            />
          </label>
          <label className="feegow-finance-field-wide">
            Observações
            <textarea
              rows={2}
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
            />
          </label>
          <div className="feegow-finance-field-wide">
            <button type="submit" className="feegow-finance-filter-btn" disabled={saving}>
              Registrar
            </button>
          </div>
        </form>
      </section>

      <section className="feegow-finance-panel feegow-finance-table-card">
        <header className="feegow-finance-panel-head">
          <h3>Histórico{patientName ? ` — ${patientName}` : ''}</h3>
        </header>
        <div className="feegow-finance-table-wrap">
          <table className="feegow-finance-table">
            <thead>
              <tr>
                <th>Data</th>
                {!fixedPatientId ? <th>Paciente</th> : null}
                <th>Vacina</th>
                <th>Dose</th>
                <th>Lote</th>
              </tr>
            </thead>
            <tbody>
              {records.map((row) => (
                <tr key={row.id}>
                  <td>{formatBrDate(row.administeredAt)}</td>
                  {!fixedPatientId ? <td>{row.patientName}</td> : null}
                  <td>{row.vaccineName}</td>
                  <td>{row.doseNumber}</td>
                  <td>{row.batchNumber ?? '—'}</td>
                </tr>
              ))}
              {!loading && records.length === 0 ? (
                <tr>
                  <td colSpan={fixedPatientId ? 4 : 5} className="feegow-finance-table-empty">
                    {patientId ? 'Nenhuma vacinação registrada.' : 'Selecione um paciente.'}
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
        {loading ? <p className="feegow-finance-loading">Carregando...</p> : null}
      </section>
    </div>
  );
}
