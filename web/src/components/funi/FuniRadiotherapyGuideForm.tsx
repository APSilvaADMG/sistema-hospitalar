import { useEffect, useState, type FormEvent } from 'react';
import { api, type HealthInsuranceDto, type PatientDto } from '../../api/client';
import {
  emptyFuni56Form,
  funi56EcogOptions,
  funi56PurposeOptions,
  funi56StagingOptions,
  type Funi56RadiotherapyForm,
  validateFuni56Form,
} from '../../data/funiGuides/funi56Radioterapia';
import { FUNI_GUIDE_CATALOG } from '../../data/funiGuides/catalog';
import { mapFuni56ToTissGuide } from '../../utils/funiGuideMapper';
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

const guideDef = FUNI_GUIDE_CATALOG.find((g) => g.slug === 'radioterapia')!;

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

export function FuniRadiotherapyGuideForm({
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
  const [form, setForm] = useState<Funi56RadiotherapyForm>(() => emptyFuni56Form());
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [clinicalSourceId, setClinicalSourceId] = useState<string | undefined>(initialSourceId);

  useEffect(() => {
    if (lockedPatientId) setPatientId(lockedPatientId);
  }, [lockedPatientId]);

  function setField<K extends keyof Funi56RadiotherapyForm>(key: K, value: Funi56RadiotherapyForm[K]) {
    setForm((f) => ({ ...f, [key]: value }));
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
        const parsed = parseClinicalFormData<Funi56RadiotherapyForm>(source.formDataJson);
        if (parsed) {
          setForm(parsed);
          return;
        }
      }

      if (workflow === 'clinical' && clinicalContext) {
        const existing = await findClinicalSource(patientId, 18, clinicalContext);
        if (cancelled) return;
        if (existing) {
          setClinicalSourceId(existing.id);
          if (existing.healthInsuranceId) setHealthInsuranceId(existing.healthInsuranceId);
          const parsed = parseClinicalFormData<Funi56RadiotherapyForm>(existing.formDataJson);
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
        providerGuideNumber: f.providerGuideNumber || `RTX${Date.now().toString().slice(-8)}`,
        phone: patient.phone ?? f.phone,
        email: patient.email ?? f.email,
      }));
      const prefill = await api.getTissGuidePrefill({ patientId, guideType: 18 });
      if (cancelled) return;
      setHealthInsuranceId(prefill.healthInsuranceId ?? '');
      setForm((f) => ({
        ...f,
        beneficiaryCardNumber: prefill.beneficiaryCardNumber ?? f.beneficiaryCardNumber,
        requestingProfessionalName: prefill.requestingProfessionalName ?? f.requestingProfessionalName,
        cid10Primary: prefill.cid10Code ?? f.cid10Primary,
        operatorGuideNumber: prefill.authorizationPassword ?? f.operatorGuideNumber,
        password: prefill.authorizationPassword ?? f.password,
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

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    if (!patientId || !healthInsuranceId) {
      setError('Selecione paciente e convênio.');
      return;
    }
    const validationErrors = validateFuni56Form(form);
    if (validationErrors.length) {
      setError(validationErrors.join(' · '));
      return;
    }
    setSaving(true);
    try {
      if (workflow === 'clinical') {
        const source = await saveClinicalSource(
          patientId,
          18,
          healthInsuranceId,
          clinicalContext ?? { label: `Radioterapia — ${form.beneficiaryName}` },
          form,
        );
        setClinicalSourceId(source.id);
        setSuccess('Dados clínicos salvos no sistema. Gere a guia FUNI no faturamento quando necessário.');
        onClinicalSaved?.();
        return;
      }
      const created = await api.createTissGuide(mapFuni56ToTissGuide(form, patientId, healthInsuranceId));
      setSuccess(`Guia FUNI 56 salva como TISS #${created.guideNumber}.`);
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
        18,
        healthInsuranceId,
        clinicalContext ?? { label: `Radioterapia — ${form.beneficiaryName}` },
        form,
        clinicalSourceId,
      );
      setSuccess(`Guia ${guide.guideNumber} gerada automaticamente.`);
      onSaved?.(guide.id);
      setTimeout(() => printFuniGuide('FUNI 56 — Radioterapia'), 400);
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
      submitLabel={workflow === 'clinical' ? 'Salvar dados no sistema' : 'Salvar guia TISS (FUNI 56)'}
      onSubmit={handleSubmit}
      secondaryAction={workflow === 'clinical' ? (
        <button type="button" className="btn btn-secondary" disabled={saving} onClick={handleGenerateGuide}>
          Gerar guia agora
        </button>
      ) : undefined}
    >
      <article className="funi-guide-sheet funi-guide-print-target">
        <FuniGuidePrintHeader
          title="ANEXO DE SOLICITAÇÃO DE RADIOTERAPIA"
          subtitle="FUNI 56 — Rev. 00 · Padrão TISS"
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
            <div className="funi-field funi-span-4"><label>2 — Nº Guia no Prestador</label><FuniCharInput value={form.providerGuideNumber} maxLength={20} onChange={(v) => setField('providerGuideNumber', v)} /></div>
            <div className="funi-field funi-span-4"><label>3 — Número da Guia Referenciada</label><FuniCharInput value={form.referencedGuideNumber} maxLength={20} onChange={(v) => setField('referencedGuideNumber', v)} /></div>
            <div className="funi-field funi-span-4"><label>6 — Guia Operadora</label><FuniCharInput value={form.operatorGuideNumber} maxLength={20} onChange={(v) => setField('operatorGuideNumber', v)} /></div>
            <div className="funi-field funi-span-4"><label>4 — Senha</label><FuniCharInput value={form.password} maxLength={20} onChange={(v) => setField('password', v)} /></div>
            <div className="funi-field funi-span-4"><label>5 — Data da Autorização</label><FuniDateInput value={form.authorizationDate} onChange={(v) => setField('authorizationDate', v)} /></div>
            <div className="funi-field funi-span-4"><label>35 — Data da Solicitação</label><FuniDateInput value={form.requestDate} onChange={(v) => setField('requestDate', v)} /></div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Beneficiário</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6"><label>7 — Número da Carteira</label><FuniCharInput value={form.beneficiaryCardNumber} maxLength={20} onChange={(v) => setField('beneficiaryCardNumber', v)} /></div>
            <div className="funi-field funi-span-6"><label>8 — Nome</label><input value={form.beneficiaryName} onChange={(e) => setField('beneficiaryName', e.target.value)} /></div>
            <div className="funi-field funi-span-3"><label>9 — Idade</label><FuniCharInput value={form.age} maxLength={3} onChange={(v) => setField('age', v.replace(/\D/g, ''))} /></div>
            <div className="funi-field funi-span-3"><label>10 — Sexo</label>
              <select value={form.sex} onChange={(e) => setField('sex', e.target.value as Funi56RadiotherapyForm['sex'])}>
                <option value="">—</option><option value="M">M</option><option value="F">F</option>
              </select>
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Profissional Solicitante</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6"><label>11 — Nome do Profissional Solicitante</label><input value={form.requestingProfessionalName} onChange={(e) => setField('requestingProfessionalName', e.target.value)} /></div>
            <div className="funi-field funi-span-3"><label>12 — Telefone</label><FuniPhoneInput value={form.phone} onChange={(v) => setField('phone', v)} /></div>
            <div className="funi-field funi-span-3"><label>13 — E-mail</label><input type="email" value={form.email} onChange={(e) => setField('email', e.target.value)} /></div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Diagnóstico Oncológico</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-3"><label>14 — Data do diagnóstico</label><FuniDateInput value={form.diagnosisDate} onChange={(v) => setField('diagnosisDate', v)} /></div>
            <div className="funi-field funi-span-2"><label>15 — CID10 Principal</label><FuniCharInput value={form.cid10Primary} maxLength={4} onChange={(v) => setField('cid10Primary', v.toUpperCase())} /></div>
            <div className="funi-field funi-span-2"><label>16 — CID10 (2)</label><FuniCharInput value={form.cid10Secondary} maxLength={4} onChange={(v) => setField('cid10Secondary', v.toUpperCase())} /></div>
            <div className="funi-field funi-span-2"><label>17 — CID10 (3)</label><FuniCharInput value={form.cid10Tertiary} maxLength={4} onChange={(v) => setField('cid10Tertiary', v.toUpperCase())} /></div>
            <div className="funi-field funi-span-3"><label>18 — CID10 (4)</label><FuniCharInput value={form.cid10Quaternary} maxLength={4} onChange={(v) => setField('cid10Quaternary', v.toUpperCase())} /></div>
            <div className="funi-field funi-span-2"><label>19 — Diagnóstico por Imagem</label><FuniCharInput value={form.imageDiagnosis} maxLength={1} onChange={(v) => setField('imageDiagnosis', v)} /></div>
            <div className="funi-field funi-span-2"><label>20 — Estadiamento</label>
              <select value={form.staging} onChange={(e) => setField('staging', e.target.value)}><option value="">—</option>{funi56StagingOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</select>
            </div>
            <div className="funi-field funi-span-2"><label>21 — ECOG</label>
              <select value={form.ecog} onChange={(e) => setField('ecog', e.target.value)}><option value="">—</option>{funi56EcogOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</select>
            </div>
            <div className="funi-field funi-span-2"><label>22 — Finalidade</label>
              <select value={form.purpose} onChange={(e) => setField('purpose', e.target.value)}><option value="">—</option>{funi56PurposeOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</select>
            </div>
            <div className="funi-field funi-span-6"><label>23 — Diagnóstico Cito/Histopatológico</label><textarea value={form.cytHistopathology} onChange={(e) => setField('cytHistopathology', e.target.value)} /></div>
            <div className="funi-field funi-span-6"><label>24 — Informações relevantes</label><textarea value={form.relevantInfo} onChange={(e) => setField('relevantInfo', e.target.value)} /></div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Tratamentos Anteriores</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6"><label>25 — Cirurgia</label><input value={form.priorSurgery} onChange={(e) => setField('priorSurgery', e.target.value)} /></div>
            <div className="funi-field funi-span-6"><label>26 — Data da Realização</label><FuniDateInput value={form.priorSurgeryDate} onChange={(v) => setField('priorSurgeryDate', v)} /></div>
            <div className="funi-field funi-span-6"><label>27 — Quimioterapia</label><input value={form.priorChemotherapy} onChange={(e) => setField('priorChemotherapy', e.target.value)} /></div>
            <div className="funi-field funi-span-6"><label>28 — Data da Aplicação</label><FuniDateInput value={form.priorChemotherapyDate} onChange={(v) => setField('priorChemotherapyDate', v)} /></div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Plano de Radioterapia</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-3"><label>29 — Número de Campos</label><FuniCharInput value={form.fieldCount} maxLength={3} onChange={(v) => setField('fieldCount', v.replace(/\D/g, ''))} /></div>
            <div className="funi-field funi-span-3"><label>30 — Dose por dia (Gy)</label><FuniDecimalInput value={form.dosePerDayGy} integerDigits={4} decimalDigits={0} onChange={(v) => setField('dosePerDayGy', v)} /></div>
            <div className="funi-field funi-span-3"><label>31 — Dose Total (Gy)</label><FuniDecimalInput value={form.totalDoseGy} integerDigits={4} decimalDigits={0} onChange={(v) => setField('totalDoseGy', v)} /></div>
            <div className="funi-field funi-span-3"><label>32 — Número de Dias</label><FuniCharInput value={form.numberOfDays} maxLength={3} onChange={(v) => setField('numberOfDays', v.replace(/\D/g, ''))} /></div>
            <div className="funi-field funi-span-6"><label>33 — Data Prevista para Início</label><FuniDateInput value={form.plannedStartDate} onChange={(v) => setField('plannedStartDate', v)} /></div>
            <div className="funi-field funi-span-12"><label>34 — Observação / Justificativa</label><textarea value={form.observation} onChange={(e) => setField('observation', e.target.value)} rows={4} /></div>
          </div>
        </div>

        <div className="funi-signatures">
          <FuniSignatureField
            fieldNumber="36"
            label="Assinatura do Profissional Solicitante"
            value={form.requestingProfessionalSignature}
            onChange={(v) => setField('requestingProfessionalSignature', v)}
            layoutKey={`radio-req-${patientId}`}
          />
          <FuniSignatureField
            fieldNumber="37"
            label="Assinatura do Autorizador da Operadora"
            value={form.authorizationResponsibleSignature}
            onChange={(v) => setField('authorizationResponsibleSignature', v)}
            layoutKey={`radio-auth-${patientId}`}
          />
        </div>
      </article>
    </FuniGuideShell>
  );
}
