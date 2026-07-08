import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  type MedicalRecordEntryDto,
  type PatientIdentityResolveDto,
} from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';

const LABEL_TYPES = [
  { value: 2, label: 'Exame' },
  { value: 3, label: 'Medicamento' },
  { value: 4, label: 'Amostra' },
];

function parsePrescriptionLine(content: string): { name: string; dose: string; route: string } {
  const lines = content.split('\n').map((l) => l.trim()).filter(Boolean);
  const medLine = lines.find((l) => /medicamento|prescri/i.test(l)) ?? lines[0] ?? '';
  const doseLine = lines.find((l) => /^dose/i.test(l)) ?? '';
  const routeLine = lines.find((l) => /^via/i.test(l)) ?? '';
  return {
    name: medLine.replace(/^[^:]+:\s*/i, '').trim() || content.slice(0, 80),
    dose: doseLine.replace(/^[^:]+:\s*/i, '').trim() || '—',
    route: routeLine.replace(/^[^:]+:\s*/i, '').trim() || 'VO',
  };
}

export function BedsideCarePanel() {
  const [scanCode, setScanCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [context, setContext] = useState<PatientIdentityResolveDto | null>(null);
  const [prescriptions, setPrescriptions] = useState<MedicalRecordEntryDto[]>([]);
  const [vitals, setVitals] = useState({ pa: '', fc: '', fr: '', temp: '', spo2: '' });
  const [vitalsPassword, setVitalsPassword] = useState('');
  const [selectedRxId, setSelectedRxId] = useState<string | null>(null);
  const [medication, setMedication] = useState({ name: '', dose: '', via: '' });
  const [medPassword, setMedPassword] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  useEffect(() => {
    if (!context) {
      setPrescriptions([]);
      return;
    }
    api.getMedicalRecord(context.patientId)
      .then((record) => {
        const active = record.entries.filter(
          (e) => e.entryType === 3 && e.isSigned,
        );
        setPrescriptions(active);
      })
      .catch(() => setPrescriptions([]));
  }, [context]);

  async function resolveCode(code: string) {
    const trimmed = code.trim();
    if (!trimmed) return;
    setLoading(true);
    setError('');
    setSuccess('');
    setSelectedRxId(null);
    try {
      const result = await api.resolvePatientIdentity(trimmed);
      setContext(result);
      setScanCode('');
    } catch (err) {
      setContext(null);
      setError(err instanceof Error ? err.message : 'Identificador não encontrado.');
    } finally {
      setLoading(false);
    }
  }

  function handleScanSubmit(e: FormEvent) {
    e.preventDefault();
    resolveCode(scanCode);
  }

  function selectPrescription(rx: MedicalRecordEntryDto) {
    const parsed = parsePrescriptionLine(rx.content);
    setSelectedRxId(rx.id);
    setMedication({ name: parsed.name, dose: parsed.dose, via: parsed.route });
  }

  async function handleSaveVitals(e: FormEvent) {
    e.preventDefault();
    if (!context) return;
    if (!vitalsPassword) {
      setError('Informe sua senha para registrar sinais vitais.');
      return;
    }
    setError('');
    try {
      const result = await api.registerBedsideVitals(context.patientId, {
        identityCode: context.code,
        bloodPressure: vitals.pa || undefined,
        heartRate: vitals.fc || undefined,
        respiratoryRate: vitals.fr || undefined,
        temperature: vitals.temp || undefined,
        spO2: vitals.spo2 || undefined,
        password: vitalsPassword,
      });
      setSuccess(result.message);
      setVitals({ pa: '', fc: '', fr: '', temp: '', spo2: '' });
      setVitalsPassword('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar sinais vitais.');
    }
  }

  async function handleAdministerMedication(e: FormEvent) {
    e.preventDefault();
    if (!context) return;
    if (!medPassword) {
      setError('Informe sua senha para confirmar a administração.');
      return;
    }
    if (context.allergyWarnings && context.allergyWarnings.length > 0) {
      const ok = window.confirm(
        `ATENÇÃO: possíveis alergias registradas no prontuário.\n\n${context.allergyWarnings.join('\n')}\n\nConfirmar administração de ${medication.name}?`,
      );
      if (!ok) return;
    }
    setError('');
    try {
      const result = await api.administerBedsideMedication(context.patientId, {
        identityCode: context.code,
        prescriptionEntryId: selectedRxId ?? undefined,
        medicationName: medication.name,
        dose: medication.dose,
        route: medication.via,
        password: medPassword,
      });
      setSuccess(result.message);
      setMedication({ name: '', dose: '', via: '' });
      setMedPassword('');
      setSelectedRxId(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar medicação.');
    }
  }

  return (
    <div className="bedside-care" style={{ marginTop: 16 }}>
      <div className="card form-grid">
        <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Identificação no leito</h3>
        <p className="form-hint" style={{ gridColumn: '1 / -1', margin: 0 }}>
          Escaneie a pulseira (QR/código de barras) ou digite o código GTH. Compatível com leitor USB em modo teclado.
        </p>
        <form className="form-field full" onSubmit={handleScanSubmit} style={{ display: 'flex', gap: 8 }}>
          <input
            ref={inputRef}
            value={scanCode}
            onChange={(e) => setScanCode(e.target.value)}
            placeholder="Código GTH ou GTH:XXXXXXXX"
            autoComplete="off"
            style={{ flex: 1, fontSize: 18, padding: '10px 12px' }}
          />
          <button className="btn" type="submit" disabled={loading || !scanCode.trim()}>
            {loading ? 'Buscando…' : 'Identificar'}
          </button>
        </form>
      </div>

      {error && <div className="alert alert-error" style={{ marginTop: 12 }}>{error}</div>}
      {success && <div className="alert alert-success" style={{ marginTop: 12 }}>{success}</div>}

      {context && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>{context.patientName}</span>
            <span className="badge">{context.code}</span>
          </div>
          <div className="card-panel-body" style={{ padding: 16 }}>
            <div className="form-grid">
              <div className="form-field">
                <label>Prontuário</label>
                <div>{context.medicalRecordNumber ?? '—'}</div>
              </div>
              <div className="form-field">
                <label>Nascimento</label>
                <div>{context.birthDate}</div>
              </div>
              <div className="form-field">
                <label>Leito</label>
                <div>{context.bedNumber && context.wardName ? `${context.wardName} — ${context.bedNumber}` : '—'}</div>
              </div>
              <div className="form-field">
                <label>Tipo sanguíneo</label>
                <div>{context.bloodType ?? '—'}</div>
              </div>
            </div>

            {context.allergyWarnings && context.allergyWarnings.length > 0 && (
              <div className="alert alert-error" style={{ marginTop: 12 }}>
                <strong>Alergias / alertas clínicos</strong>
                <ul style={{ margin: '8px 0 0', paddingLeft: 18 }}>
                  {context.allergyWarnings.map((w) => <li key={w}>{w}</li>)}
                </ul>
              </div>
            )}

            <div style={{ display: 'flex', gap: 8, marginTop: 16, flexWrap: 'wrap' }}>
              <Link className="btn btn-secondary btn-sm" to={`/pep/evolucao-medica?paciente=${context.patientId}`}>Abrir PEP</Link>
              <Link className="btn btn-secondary btn-sm" to={`/enfermagem/sae/evolucao?paciente=${context.patientId}`}>Evolução Enfermagem</Link>
            </div>
          </div>
        </div>
      )}

      {context && prescriptions.length > 0 && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Prescrições assinadas (eMAR)</div>
          <ul className="bi-progress-list" style={{ padding: '12px 16px' }}>
            {prescriptions.map((rx) => (
              <li key={rx.id}>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => selectPrescription(rx)}
                  style={{ marginRight: 8 }}
                >
                  Usar
                </button>
                {parsePrescriptionLine(rx.content).name}
                <span style={{ color: 'var(--muted)', fontSize: 13 }}> — {formatBrDateTime(rx.createdAt)}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {context && (
        <>
          <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={handleSaveVitals}>
            <h4 style={{ gridColumn: '1 / -1', margin: 0 }}>Sinais vitais rápidos</h4>
            <div className="form-field"><label>PA</label><input value={vitals.pa} onChange={(e) => setVitals({ ...vitals, pa: e.target.value })} placeholder="120x80" /></div>
            <div className="form-field"><label>FC</label><input value={vitals.fc} onChange={(e) => setVitals({ ...vitals, fc: e.target.value })} /></div>
            <div className="form-field"><label>FR</label><input value={vitals.fr} onChange={(e) => setVitals({ ...vitals, fr: e.target.value })} /></div>
            <div className="form-field"><label>Temp °C</label><input value={vitals.temp} onChange={(e) => setVitals({ ...vitals, temp: e.target.value })} /></div>
            <div className="form-field"><label>SpO2 %</label><input value={vitals.spo2} onChange={(e) => setVitals({ ...vitals, spo2: e.target.value })} /></div>
            <div className="form-field full"><label>Senha (confirmação)</label><input type="password" value={vitalsPassword} onChange={(e) => setVitalsPassword(e.target.value)} autoComplete="current-password" /></div>
            <div className="form-actions"><button className="btn" type="submit">Registrar no PEP</button></div>
          </form>

          <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={handleAdministerMedication}>
            <h4 style={{ gridColumn: '1 / -1', margin: 0 }}>Administração de medicamento (eMAR)</h4>
            {selectedRxId && <p className="form-hint full" style={{ margin: 0 }}>Vinculado à prescrição assinada selecionada.</p>}
            <div className="form-field"><label>Medicamento</label><input value={medication.name} onChange={(e) => setMedication({ ...medication, name: e.target.value })} required /></div>
            <div className="form-field"><label>Dose</label><input value={medication.dose} onChange={(e) => setMedication({ ...medication, dose: e.target.value })} required /></div>
            <div className="form-field"><label>Via</label><input value={medication.via} onChange={(e) => setMedication({ ...medication, via: e.target.value })} placeholder="VO, IV…" /></div>
            <div className="form-field full"><label>Senha (assinatura operacional)</label><input type="password" value={medPassword} onChange={(e) => setMedPassword(e.target.value)} autoComplete="current-password" required /></div>
            <div className="form-actions"><button className="btn" type="submit">Validar pulseira e registrar</button></div>
          </form>
        </>
      )}
    </div>
  );
}

export function PatientIdentityTools({ patientId }: { patientId: string }) {
  const [labelType, setLabelType] = useState(2);
  const [labelContext, setLabelContext] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  async function handleBracelet() {
    setErr('');
    try {
      const identity = await api.generatePatientBracelet(patientId, {});
      const patient = await api.getPatient(patientId);
      const { printPatientIdentityWristband } = await import('../../utils/patientIdentityPrint');
      await printPatientIdentityWristband(patient, identity);
      setMsg(`Pulseira ${identity.code} gerada e enviada à impressão.`);
    } catch (e) {
      setErr(e instanceof Error ? e.message : 'Erro ao gerar pulseira.');
    }
  }

  async function handleLabel() {
    setErr('');
    try {
      const identity = await api.generatePatientLabel(patientId, {
        labelType,
        labelContext: labelContext || undefined,
      });
      const patient = await api.getPatient(patientId);
      const { printPatientIdentityLabel } = await import('../../utils/patientIdentityPrint');
      await printPatientIdentityLabel(patient, identity);
      setMsg(`Etiqueta ${identity.code} gerada.`);
    } catch (e) {
      setErr(e instanceof Error ? e.message : 'Erro ao gerar etiqueta.');
    }
  }

  return (
    <div className="card form-grid" style={{ marginTop: 16 }}>
      <h4 style={{ gridColumn: '1 / -1', margin: 0 }}>Pulseira e etiquetas (GTH)</h4>
      {msg && <div className="alert alert-success full">{msg}</div>}
      {err && <div className="alert alert-error full">{err}</div>}
      <div className="form-actions full">
        <button type="button" className="btn btn-secondary" onClick={handleBracelet}>Gerar pulseira com QR</button>
      </div>
      <div className="form-field">
        <label>Tipo de etiqueta</label>
        <select value={labelType} onChange={(e) => setLabelType(Number(e.target.value))}>
          {LABEL_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
        </select>
      </div>
      <div className="form-field">
        <label>Referência (exame/med/amostra)</label>
        <input value={labelContext} onChange={(e) => setLabelContext(e.target.value)} placeholder="Hemograma, Dipirona 500mg…" />
      </div>
      <div className="form-actions full">
        <button type="button" className="btn" onClick={handleLabel}>Gerar e imprimir etiqueta</button>
      </div>
    </div>
  );
}

/** Barra compacta de scan — recepção e busca rápida de paciente. */
export function PatientIdentityScanBar({ onResolved }: { onResolved?: (r: PatientIdentityResolveDto) => void }) {
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const trimmed = code.trim();
    if (!trimmed) return;
    setLoading(true);
    setError('');
    try {
      const result = await api.resolvePatientIdentity(trimmed);
      onResolved?.(result);
      setCode('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Código não encontrado.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="card form-grid" style={{ marginBottom: 16 }}>
      <h4 style={{ gridColumn: '1 / -1', margin: 0 }}>Identificar por pulseira / QR</h4>
      <form className="form-field full" onSubmit={handleSubmit} style={{ display: 'flex', gap: 8 }}>
        <input
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder="Escaneie ou digite GTH-…"
          autoComplete="off"
          style={{ flex: 1 }}
        />
        <button className="btn btn-secondary" type="submit" disabled={loading || !code.trim()}>
          {loading ? '…' : 'Buscar'}
        </button>
      </form>
      {error && <div className="alert alert-error full">{error}</div>}
    </div>
  );
}
