import { useEffect, useState, type FormEvent } from 'react';
import { api, type HealthInsuranceDto, type PatientDto } from '../../api/client';
import {
  computeBodySurfaceM2,
  emptyFuni55Form,
  funi55ChemoTypeOptions,
  funi55EcogOptions,
  funi55PurposeOptions,
  funi55StagingOptions,
  FUNI55_MEDICATION_ROWS,
  type Funi55ChemotherapyForm,
  type FuniChemotherapyMedicationRow,
  validateFuni55Form,
} from '../../data/funiGuides/funi55Quimioterapia';
import { FUNI_GUIDE_CATALOG } from '../../data/funiGuides/catalog';
import { mapFuni55ToTissGuide } from '../../utils/funiGuideMapper';
import {
  type ClinicalGuideContext,
  findClinicalSource,
  generateGuideFromClinicalData,
  parseClinicalFormData,
  saveClinicalSource,
} from '../../utils/clinicalGuideWorkflow';
import { printFuniGuide } from '../../utils/printFuniGuide';
import { FuniGuidePrintHeader } from './FuniGuidePrintHeader';
import { FuniCharInput } from './FuniCharInput';
import { FuniDateInput } from './FuniDateInput';
import { FuniDecimalInput } from './FuniDecimalInput';
import { FuniGuideShell } from './FuniGuideShell';
import { FuniPhoneInput } from './FuniPhoneInput';
import { FuniSignatureField } from './FuniSignatureField';
import './funiGuide.css';

const guideDef = FUNI_GUIDE_CATALOG.find((g) => g.slug === 'quimioterapia')!;

type Props = {
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  onSaved?: (guideId: string) => void;
  workflow?: 'direct' | 'clinical';
  clinicalContext?: ClinicalGuideContext;
  lockedPatientId?: string;
  initialSourceId?: string;
  onClinicalSaved?: () => void;
};

export function FuniChemotherapyGuideForm({
  patients,
  insurances,
  onSaved,
  workflow = 'direct',
  clinicalContext,
  lockedPatientId,
  initialSourceId,
  onClinicalSaved,
}: Props) {
  const [patientId, setPatientId] = useState('');
  const [healthInsuranceId, setHealthInsuranceId] = useState('');
  const [form, setForm] = useState<Funi55ChemotherapyForm>(() => emptyFuni55Form());
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [clinicalSourceId, setClinicalSourceId] = useState<string | undefined>(initialSourceId);

  useEffect(() => {
    if (lockedPatientId) setPatientId(lockedPatientId);
  }, [lockedPatientId]);

  function setField<K extends keyof Funi55ChemotherapyForm>(key: K, value: Funi55ChemotherapyForm[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  function setMedication(index: number, patch: Partial<FuniChemotherapyMedicationRow>) {
    setForm((f) => ({
      ...f,
      medications: f.medications.map((row, i) => (i === index ? { ...row, ...patch } : row)),
    }));
  }

  useEffect(() => {
    if (!patientId) return;

    let cancelled = false;

    (async () => {
      if (initialSourceId) {
        const source = await api.getClinicalSource(initialSourceId);
        if (cancelled) return;
        setClinicalSourceId(source.id);
        if (source.healthInsuranceId) setHealthInsuranceId(source.healthInsuranceId);
        const parsed = parseClinicalFormData<Funi55ChemotherapyForm>(source.formDataJson);
        if (parsed) {
          setForm(parsed);
          return;
        }
      }

      if (workflow === 'clinical' && clinicalContext) {
        const existing = await findClinicalSource(patientId, 17, clinicalContext);
        if (cancelled) return;
        if (existing) {
          setClinicalSourceId(existing.id);
          if (existing.healthInsuranceId) setHealthInsuranceId(existing.healthInsuranceId);
          const parsed = parseClinicalFormData<Funi55ChemotherapyForm>(existing.formDataJson);
          if (parsed) {
            setForm(parsed);
            return;
          }
        }
      }

      const patient = patients.find((p) => p.id === patientId);
      if (!patient) return;

      const age = patient.birthDate
        ? String(Math.floor((Date.now() - new Date(patient.birthDate).getTime()) / (365.25 * 24 * 3600 * 1000)))
        : '';

      setForm((f) => ({
        ...f,
        beneficiaryName: patient.fullName,
        age,
        sex: patient.gender === 2 ? 'F' : patient.gender === 1 ? 'M' : f.sex,
        providerGuideNumber: f.providerGuideNumber || `QTX${Date.now().toString().slice(-8)}`,
        phone: patient.phone ?? f.phone,
        email: patient.email ?? f.email,
      }));

      const [prefill, sessions] = await Promise.all([
        api.getTissGuidePrefill({
          patientId,
          guideType: 17,
          chemotherapySessionId: clinicalContext?.chemotherapySessionId,
        }),
        clinicalContext?.chemotherapySessionId
          ? api.getChemotherapySessions().catch(() => [])
          : Promise.resolve([]),
      ]);
      if (cancelled) return;

      setHealthInsuranceId(prefill.healthInsuranceId ?? '');
      const session = sessions.find((s) => s.id === clinicalContext?.chemotherapySessionId);
      setForm((f) => ({
        ...f,
        beneficiaryCardNumber: prefill.beneficiaryCardNumber ?? f.beneficiaryCardNumber,
        requestingProfessionalName: session?.professionalName ?? prefill.requestingProfessionalName ?? f.requestingProfessionalName,
        cid10Primary: prefill.cid10Code ?? f.cid10Primary,
        therapeuticPlan: session?.protocolName ?? f.therapeuticPlan,
        observation: session?.notes ?? f.observation,
        cyclesPlanned: session ? String(session.totalCycles) : f.cyclesPlanned,
        currentCycle: session ? String(session.cycleNumber) : f.currentCycle,
        operatorGuideNumber: prefill.authorizationPassword ?? f.operatorGuideNumber,
        password: prefill.authorizationPassword ?? f.password,
        medications: session?.drugRegimen
          ? f.medications.map((row, i) => (i === 0
            ? { ...row, description: session.drugRegimen }
            : row))
          : f.medications,
      }));
    })().catch(console.error);

    return () => {
      cancelled = true;
    };
  }, [patientId, patients, workflow, clinicalContext, initialSourceId]);

  useEffect(() => {
    const ins = insurances.find((i) => i.id === healthInsuranceId);
    if (ins?.ansRegistration) {
      setForm((f) => ({ ...f, ansRegistration: ins.ansRegistration!.replace(/\D/g, '').slice(0, 6) }));
    }
  }, [healthInsuranceId, insurances]);

  useEffect(() => {
    const bsa = computeBodySurfaceM2(form.weightKg, form.heightCm);
    if (bsa && bsa !== form.bodySurfaceM2) {
      setForm((f) => ({ ...f, bodySurfaceM2: bsa }));
    }
  }, [form.weightKg, form.heightCm, form.bodySurfaceM2]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    if (!patientId || !healthInsuranceId) {
      setError('Selecione paciente e convênio.');
      return;
    }
    const validationErrors = validateFuni55Form(form);
    if (validationErrors.length) {
      setError(validationErrors.join(' · '));
      return;
    }
    setSaving(true);
    try {
      if (workflow === 'clinical') {
        const source = await saveClinicalSource(
          patientId,
          17,
          healthInsuranceId,
          clinicalContext ?? { label: `Quimio — ${form.beneficiaryName}` },
          form,
        );
        setClinicalSourceId(source.id);
        setSuccess('Dados clínicos salvos no sistema. Gere a guia FUNI no faturamento quando necessário.');
        onClinicalSaved?.();
        return;
      }
      const payload = mapFuni55ToTissGuide(form, patientId, healthInsuranceId);
      const created = await api.createTissGuide(payload);
      setSuccess(`Guia FUNI 55 salva como TISS #${created.guideNumber}.`);
      onSaved?.(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar guia.');
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerateGuide() {
    if (!patientId || !healthInsuranceId) {
      setError('Selecione paciente e convênio.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const { guide } = await generateGuideFromClinicalData(
        patientId,
        17,
        healthInsuranceId,
        clinicalContext ?? { label: `Quimio — ${form.beneficiaryName}` },
        form,
        clinicalSourceId,
      );
      setSuccess(`Guia ${guide.guideNumber} gerada automaticamente.`);
      onSaved?.(guide.id);
      setTimeout(() => printFuniGuide('FUNI 55 — Quimioterapia'), 400);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar guia.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <FuniGuideShell
      guide={guideDef}
      patients={patients}
      insurances={insurances}
      patientId={patientId}
      healthInsuranceId={healthInsuranceId}
      onPatientChange={setPatientId}
      onInsuranceChange={setHealthInsuranceId}
      error={error}
      success={success}
      saving={saving}
      workflow={workflow}
      compact={workflow === 'clinical'}
      lockPatient={Boolean(lockedPatientId)}
      submitLabel={workflow === 'clinical' ? 'Salvar dados no sistema' : 'Salvar guia TISS (FUNI 55)'}
      onSubmit={handleSubmit}
      secondaryAction={workflow === 'clinical' ? (
        <button type="button" className="btn btn-secondary" disabled={saving} onClick={handleGenerateGuide}>
          Gerar guia agora
        </button>
      ) : undefined}
    >
      <article className="funi-guide-sheet funi-guide-print-target">
        <FuniGuidePrintHeader
          title="ANEXO DE SOLICITAÇÃO DE QUIMIOTERAPIA"
          subtitle="FUNI 55 — Rev. 00 · Padrão TISS"
          meta={(
            <>
              <div><strong>1</strong> Registro ANS</div>
              <FuniCharInput value={form.ansRegistration} maxLength={6} onChange={(v) => setField('ansRegistration', v.replace(/\D/g, ''))} />
            </>
          )}
        />

        <div className="funi-section">
          <div className="funi-section-title">Identificação da guia</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-4">
              <label>2 — Nº Guia no Prestador</label>
              <FuniCharInput value={form.providerGuideNumber} maxLength={20} onChange={(v) => setField('providerGuideNumber', v)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>3 — Número da Guia Referenciada</label>
              <FuniCharInput value={form.referencedGuideNumber} maxLength={20} onChange={(v) => setField('referencedGuideNumber', v)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>6 — Número da Guia Atribuído pela Operadora</label>
              <FuniCharInput value={form.operatorGuideNumber} maxLength={20} onChange={(v) => setField('operatorGuideNumber', v)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>4 — Senha</label>
              <FuniCharInput value={form.password} maxLength={20} onChange={(v) => setField('password', v)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>5 — Data da Autorização</label>
              <FuniDateInput value={form.authorizationDate} onChange={(v) => setField('authorizationDate', v)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>49 — Data da Solicitação</label>
              <FuniDateInput value={form.requestDate} onChange={(v) => setField('requestDate', v)} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Beneficiário</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6">
              <label>7 — Número da Carteira</label>
              <FuniCharInput value={form.beneficiaryCardNumber} maxLength={20} onChange={(v) => setField('beneficiaryCardNumber', v)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>8 — Nome</label>
              <input value={form.beneficiaryName} onChange={(e) => setField('beneficiaryName', e.target.value)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>12 — Idade</label>
              <FuniCharInput value={form.age} maxLength={3} onChange={(v) => setField('age', v.replace(/\D/g, ''))} />
            </div>
            <div className="funi-field funi-span-2">
              <label>13 — Sexo</label>
              <select value={form.sex} onChange={(e) => setField('sex', e.target.value as Funi55ChemotherapyForm['sex'])}>
                <option value="">—</option>
                <option value="M">M</option>
                <option value="F">F</option>
              </select>
            </div>
            <div className="funi-field funi-span-3">
              <label>9 — Peso (Kg)</label>
              <FuniDecimalInput value={form.weightKg} integerDigits={3} decimalDigits={2} onChange={(v) => setField('weightKg', v)} />
            </div>
            <div className="funi-field funi-span-3">
              <label>10 — Altura (Cm)</label>
              <FuniDecimalInput value={form.heightCm} integerDigits={3} decimalDigits={2} onChange={(v) => setField('heightCm', v)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>11 — Superfície Corporal (m²)</label>
              <input value={form.bodySurfaceM2} readOnly title="Calculado automaticamente (Mosteller)" />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Profissional Solicitante</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6">
              <label>14 — Nome do Profissional Solicitante</label>
              <input value={form.requestingProfessionalName} onChange={(e) => setField('requestingProfessionalName', e.target.value)} />
            </div>
            <div className="funi-field funi-span-3">
              <label>15 — Telefone</label>
              <FuniPhoneInput value={form.phone} onChange={(v) => setField('phone', v)} />
            </div>
            <div className="funi-field funi-span-3">
              <label>16 — E-mail</label>
              <input type="email" value={form.email} onChange={(e) => setField('email', e.target.value)} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Diagnóstico Oncológico</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-3">
              <label>17 — Data do diagnóstico</label>
              <FuniDateInput value={form.diagnosisDate} onChange={(v) => setField('diagnosisDate', v)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>18 — CID10 Principal</label>
              <FuniCharInput value={form.cid10Primary} maxLength={4} onChange={(v) => setField('cid10Primary', v.toUpperCase())} />
            </div>
            <div className="funi-field funi-span-2">
              <label>19 — CID10 (2)</label>
              <FuniCharInput value={form.cid10Secondary} maxLength={4} onChange={(v) => setField('cid10Secondary', v.toUpperCase())} />
            </div>
            <div className="funi-field funi-span-2">
              <label>20 — CID10 (3)</label>
              <FuniCharInput value={form.cid10Tertiary} maxLength={4} onChange={(v) => setField('cid10Tertiary', v.toUpperCase())} />
            </div>
            <div className="funi-field funi-span-3">
              <label>21 — CID10 (4)</label>
              <FuniCharInput value={form.cid10Quaternary} maxLength={4} onChange={(v) => setField('cid10Quaternary', v.toUpperCase())} />
            </div>
            <div className="funi-field funi-span-3">
              <label>22 — Estadiamento</label>
              <select value={form.staging} onChange={(e) => setField('staging', e.target.value)}>
                <option value="">—</option>
                {funi55StagingOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div className="funi-field funi-span-3">
              <label>23 — Tipo de Quimioterapia</label>
              <select value={form.chemotherapyType} onChange={(e) => setField('chemotherapyType', e.target.value)}>
                <option value="">—</option>
                {funi55ChemoTypeOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div className="funi-field funi-span-2">
              <label>24 — Finalidade</label>
              <select value={form.purpose} onChange={(e) => setField('purpose', e.target.value)}>
                <option value="">—</option>
                {funi55PurposeOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div className="funi-field funi-span-2">
              <label>25 — ECOG</label>
              <select value={form.ecog} onChange={(e) => setField('ecog', e.target.value)}>
                <option value="">—</option>
                {funi55EcogOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div className="funi-field funi-span-2">
              <label>26 — Tumor</label>
              <FuniCharInput value={form.tumor} maxLength={1} onChange={(v) => setField('tumor', v)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>27 — Nódulo</label>
              <FuniCharInput value={form.nodule} maxLength={1} onChange={(v) => setField('nodule', v)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>28 — Metástase</label>
              <FuniCharInput value={form.metastasis} maxLength={1} onChange={(v) => setField('metastasis', v)} />
            </div>
            <div className="funi-field funi-span-12">
              <label>29 — Plano Terapêutico</label>
              <textarea value={form.therapeuticPlan} onChange={(e) => setField('therapeuticPlan', e.target.value)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>30 — Diagnóstico Cito/Histopatológico</label>
              <textarea value={form.cytHistopathology} onChange={(e) => setField('cytHistopathology', e.target.value)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>31 — Informações relevantes</label>
              <textarea value={form.relevantInfo} onChange={(e) => setField('relevantInfo', e.target.value)} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Tratamentos Anteriores</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6">
              <label>40 — Cirurgia</label>
              <input value={form.priorSurgery} onChange={(e) => setField('priorSurgery', e.target.value)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>41 — Data da Realização</label>
              <FuniDateInput value={form.priorSurgeryDate} onChange={(v) => setField('priorSurgeryDate', v)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>42 — Área Irradiada</label>
              <input value={form.priorRadiotherapyArea} onChange={(e) => setField('priorRadiotherapyArea', e.target.value)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>43 — Data da Aplicação</label>
              <FuniDateInput value={form.priorRadiotherapyDate} onChange={(v) => setField('priorRadiotherapyDate', v)} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Ciclos</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-3">
              <label>45 — Número de Ciclos Previstos</label>
              <FuniCharInput value={form.cyclesPlanned} maxLength={2} onChange={(v) => setField('cyclesPlanned', v.replace(/\D/g, ''))} />
            </div>
            <div className="funi-field funi-span-3">
              <label>46 — Ciclo Atual</label>
              <FuniCharInput value={form.currentCycle} maxLength={2} onChange={(v) => setField('currentCycle', v.replace(/\D/g, ''))} />
            </div>
            <div className="funi-field funi-span-3">
              <label>47 — Nº de dias do Ciclo Atual</label>
              <FuniCharInput value={form.daysInCurrentCycle} maxLength={3} onChange={(v) => setField('daysInCurrentCycle', v.replace(/\D/g, ''))} />
            </div>
            <div className="funi-field funi-span-3">
              <label>48 — Intervalo entre Ciclos (dias)</label>
              <FuniCharInput value={form.intervalBetweenCyclesDays} maxLength={3} onChange={(v) => setField('intervalBetweenCyclesDays', v.replace(/\D/g, ''))} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Medicamentos e Drogas solicitadas</div>
          <div className="funi-med-table-wrap">
            <table className="funi-med-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>32 — Data prevista início</th>
                  <th>33 — Tabela</th>
                  <th>34 — Código</th>
                  <th>35 — Descrição</th>
                  <th>36 — Dosagem total</th>
                  <th>37 — Unid.</th>
                  <th>38 — Via</th>
                  <th>39 — Freq.</th>
                </tr>
              </thead>
              <tbody>
                {form.medications.slice(0, FUNI55_MEDICATION_ROWS).map((row, i) => (
                  <tr key={i}>
                    <td>{i + 1}</td>
                    <td><FuniDateInput value={row.plannedStartDate} onChange={(v) => setMedication(i, { plannedStartDate: v })} /></td>
                    <td><FuniCharInput value={row.tableCode} maxLength={2} onChange={(v) => setMedication(i, { tableCode: v })} /></td>
                    <td><FuniCharInput value={row.medicationCode} maxLength={10} onChange={(v) => setMedication(i, { medicationCode: v })} /></td>
                    <td><input value={row.description} onChange={(e) => setMedication(i, { description: e.target.value })} /></td>
                    <td><FuniDecimalInput value={row.totalDosage} integerDigits={5} decimalDigits={2} onChange={(v) => setMedication(i, { totalDosage: v })} /></td>
                    <td><FuniCharInput value={row.dosageUnit} maxLength={3} onChange={(v) => setMedication(i, { dosageUnit: v })} /></td>
                    <td><FuniCharInput value={row.administrationRoute} maxLength={2} onChange={(v) => setMedication(i, { administrationRoute: v })} /></td>
                    <td><FuniCharInput value={row.frequency} maxLength={2} onChange={(v) => setMedication(i, { frequency: v })} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-body">
            <div className="funi-field funi-span-12">
              <label>44 — Observação / Justificativa</label>
              <textarea value={form.observation} onChange={(e) => setField('observation', e.target.value)} rows={4} />
            </div>
          </div>
        </div>

        <div className="funi-signatures">
          <FuniSignatureField
            fieldNumber="50"
            label="Assinatura do Profissional Solicitante"
            value={form.requestingProfessionalSignature}
            onChange={(v) => setField('requestingProfessionalSignature', v)}
            layoutKey={`chemo-req-${patientId}`}
          />
          <FuniSignatureField
            fieldNumber="51"
            label="Assinatura do Responsável pela Autorização"
            value={form.authorizationResponsibleSignature}
            onChange={(v) => setField('authorizationResponsibleSignature', v)}
            layoutKey={`chemo-auth-${patientId}`}
          />
        </div>
      </article>
    </FuniGuideShell>
  );
}
