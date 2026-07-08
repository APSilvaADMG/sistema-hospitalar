import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type HealthInsuranceDto,
  type PatientDto,
  type TissGuidePrefillDto,
  type TussSearchResultDto,
} from '../../api/client';
import {
  accidentIndicatorOptions,
  consultationTypeOptions,
  emptyFuni21Form,
  type Funi21ConsultationForm,
  validateFuni21Form,
} from '../../data/funiGuides/funi21Consultation';
import { FUNI_GUIDE_CATALOG } from '../../data/funiGuides/catalog';
import { mapFuni21ToTissGuide } from '../../utils/funiGuideMapper';
import { buildFuni21FormFromPatient, pickBeneficiaryCns, resolvePrefillInsuranceId } from '../../utils/funiPrefill';
import {
  type ClinicalGuideContext,
  findClinicalSource,
  generateGuideFromClinicalData,
  parseClinicalFormData,
  saveClinicalSource,
} from '../../utils/clinicalGuideWorkflow';
import { printFuniGuide } from '../../utils/printFuniGuide';
import { FuniCharInput } from './FuniCharInput';
import { FuniGuidePrintHeader } from './FuniGuidePrintHeader';
import { FuniGuideShell } from './FuniGuideShell';
import { FuniSignatureField } from './FuniSignatureField';
import { InsuranceLogo } from '../InsuranceLogo';
import './funiGuide.css';

const guideDef = FUNI_GUIDE_CATALOG.find((g) => g.slug === 'consulta')!;

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

export function FuniConsultationGuideForm({
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
  const [form, setForm] = useState<Funi21ConsultationForm>(() => emptyFuni21Form());
  const [tussHits, setTussHits] = useState<TussSearchResultDto[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [operatorPrefill, setOperatorPrefill] = useState<TissGuidePrefillDto | null>(null);
  const [refreshingOperator, setRefreshingOperator] = useState(false);
  const [clinicalSourceId, setClinicalSourceId] = useState<string | undefined>(initialSourceId);

  useEffect(() => {
    if (lockedPatientId) setPatientId(lockedPatientId);
  }, [lockedPatientId]);

  function setField<K extends keyof Funi21ConsultationForm>(key: K, value: Funi21ConsultationForm[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  useEffect(() => {
    if (!patientId) {
      setForm(emptyFuni21Form());
      setHealthInsuranceId('');
      setOperatorPrefill(null);
      return;
    }

    let cancelled = false;

    (async () => {
      try {
        if (initialSourceId && !clinicalSourceId) {
          const source = await api.getClinicalSource(initialSourceId);
          if (cancelled) return;
          setClinicalSourceId(source.id);
          if (source.healthInsuranceId) setHealthInsuranceId(source.healthInsuranceId);
          const parsed = parseClinicalFormData<Funi21ConsultationForm>(source.formDataJson);
          if (parsed) {
            setForm(parsed);
            return;
          }
        }

        if (workflow === 'clinical' && clinicalContext) {
          const existing = await findClinicalSource(patientId, 1, clinicalContext);
          if (cancelled) return;
          if (existing) {
            setClinicalSourceId(existing.id);
            if (existing.healthInsuranceId) setHealthInsuranceId(existing.healthInsuranceId);
            const parsed = parseClinicalFormData<Funi21ConsultationForm>(existing.formDataJson);
            if (parsed) {
              setForm(parsed);
              return;
            }
          }
        }

        const [patient, prefill] = await Promise.all([
          api.getPatient(patientId),
          api.getTissGuidePrefill({
            patientId,
            guideType: 1,
            healthInsuranceId: healthInsuranceId || undefined,
            appointmentId: clinicalContext?.appointmentId,
            hospitalizationId: clinicalContext?.hospitalizationId,
          }),
        ]);
        if (cancelled) return;

        const insuranceId = resolvePrefillInsuranceId(patient, prefill, healthInsuranceId);
        if (insuranceId !== healthInsuranceId) {
          setHealthInsuranceId(insuranceId);
          return;
        }
        setForm(buildFuni21FormFromPatient(patient, prefill, insuranceId));
        setOperatorPrefill(prefill.operatorDataSource ? prefill : null);
      } catch (err) {
        console.error(err);
        if (!cancelled) {
          const patient = patients.find((p) => p.id === patientId);
          if (patient) {
            setForm({
              ...emptyFuni21Form(),
              beneficiaryName: patient.fullName,
              beneficiaryCns: pickBeneficiaryCns(patient.cns),
              providerGuideNumber: `GC${Date.now().toString().slice(-8)}`,
            });
          }
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [patientId, healthInsuranceId, patients, workflow, clinicalContext, initialSourceId, clinicalSourceId]);

  async function handleRefreshOperator() {
    if (!patientId || !healthInsuranceId) return;
    setRefreshingOperator(true);
    setError('');
    try {
      const [patient, prefill] = await Promise.all([
        api.getPatient(patientId),
        api.getTissGuidePrefill({
          patientId,
          guideType: 1,
          healthInsuranceId,
          includeOperatorData: true,
          refreshOperatorData: true,
        }),
      ]);
      setForm(buildFuni21FormFromPatient(patient, prefill, healthInsuranceId));
      setOperatorPrefill(prefill.operatorDataSource ? prefill : null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha ao consultar operadora.');
    } finally {
      setRefreshingOperator(false);
    }
  }

  const operatorStatusLabel = operatorPrefill?.operatorEligibilityStatus === 1
    ? 'Elegível'
    : operatorPrefill?.operatorEligibilityStatus === 2
      ? 'Não elegível'
      : operatorPrefill?.operatorEligibilityStatus === 3
        ? 'Pendente'
        : operatorPrefill?.operatorEligibilityStatus === 4
          ? 'Erro'
          : '';

  useEffect(() => {
    const ins = insurances.find((i) => i.id === healthInsuranceId);
    if (ins?.ansRegistration) {
      setForm((f) => ({ ...f, ansRegistration: ins.ansRegistration!.replace(/\D/g, '').slice(0, 8) }));
    }
  }, [healthInsuranceId, insurances]);

  useEffect(() => {
    const code = typeof form.procedureCode === 'string' ? form.procedureCode : '';
    if (code.length < 3) {
      setTussHits([]);
      return;
    }
    const t = setTimeout(() => {
      api.searchTuss(code).then(setTussHits).catch(() => setTussHits([]));
    }, 300);
    return () => clearTimeout(t);
  }, [form.procedureCode]);

  const selectedInsurance = insurances.find((i) => i.id === healthInsuranceId);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    if (!patientId || !healthInsuranceId) {
      setError('Selecione paciente e convênio.');
      return;
    }
    const validationErrors = validateFuni21Form(form);
    if (validationErrors.length) {
      setError(validationErrors.slice(0, 4).join(' · '));
      return;
    }
    setSaving(true);
    try {
      if (workflow === 'clinical') {
        const source = await saveClinicalSource(
          patientId,
          1,
          healthInsuranceId,
          clinicalContext ?? { label: `Consulta — ${form.beneficiaryName}` },
          form,
        );
        setClinicalSourceId(source.id);
        setSuccess('Dados clínicos salvos no sistema. Gere a guia FUNI no faturamento quando necessário.');
        onClinicalSaved?.();
        return;
      }
      const payload = mapFuni21ToTissGuide(form, patientId, healthInsuranceId, clinicalContext?.appointmentId);
      const created = await api.createTissGuide(payload);
      setSuccess(`Guia FUNI 21 salva como TISS #${created.guideNumber}. Integrada ao faturamento.`);
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
        1,
        healthInsuranceId,
        clinicalContext ?? { label: `Consulta — ${form.beneficiaryName}` },
        form,
        clinicalSourceId,
      );
      setSuccess(`Guia ${guide.guideNumber} gerada automaticamente.`);
      onSaved?.(guide.id);
      setTimeout(() => printFuniGuide('FUNI 21 — Guia de Consulta'), 400);
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
      submitLabel={workflow === 'clinical' ? 'Salvar dados no sistema' : 'Salvar guia TISS (FUNI 21)'}
      onSubmit={handleSubmit}
      secondaryAction={workflow === 'clinical' ? (
        <button type="button" className="btn btn-secondary" disabled={saving} onClick={handleGenerateGuide}>
          Gerar guia agora
        </button>
      ) : undefined}
    >
      <article className="funi-guide-sheet funi-guide-print-target">
        <FuniGuidePrintHeader
          title="GUIA DE CONSULTA"
          subtitle="FUNI 21 — Rev. 01 · Padrão TISS"
          operator={selectedInsurance ? (
            <>
              <InsuranceLogo
                name={selectedInsurance.name}
                logoUrl={selectedInsurance.logoUrl}
                size={48}
                className="funi-guide-operator-logo"
              />
              <div className="funi-guide-operator-name">{selectedInsurance.name}</div>
              {selectedInsurance.ansRegistration && (
                <div className="funi-guide-operator-ans">ANS {selectedInsurance.ansRegistration}</div>
              )}
              <div style={{ marginTop: 6 }}>
                <strong>1</strong> Registro ANS
                <FuniCharInput value={form.ansRegistration} maxLength={8} onChange={(v) => setField('ansRegistration', v)} />
              </div>
            </>
          ) : (
            <>
              <div className="funi-guide-operator-placeholder">
                <span>Operadora</span>
                <small>Selecione o convênio</small>
              </div>
              <div style={{ marginTop: 6 }}>
                <strong>1</strong> Registro ANS
                <FuniCharInput value={form.ansRegistration} maxLength={8} onChange={(v) => setField('ansRegistration', v)} />
              </div>
            </>
          )}
        />

        {operatorPrefill?.operatorDataSource && (
          <div className={`funi-operator-banner funi-operator-banner--${operatorPrefill.operatorEligibilityStatus === 1 ? 'ok' : 'warn'}`}>
            <div>
              <strong>Dados confirmados pela operadora</strong>
              {operatorStatusLabel && ` · ${operatorStatusLabel}`}
              {operatorPrefill.operatorDataSource === 'live' ? ' · consulta em tempo real' : ' · consulta recente em cache'}
              {operatorPrefill.beneficiaryPlanName && (
                <div>Plano: {operatorPrefill.beneficiaryPlanName}</div>
              )}
              {operatorPrefill.cardValidUntil && (
                <div>Validade da carteira: {operatorPrefill.cardValidUntil.slice(0, 10)}</div>
              )}
              {operatorPrefill.authorizationPassword && (
                <div>Senha/autorização: {operatorPrefill.authorizationPassword}</div>
              )}
              {operatorPrefill.operatorMessage && (
                <div className="funi-operator-banner-message">{operatorPrefill.operatorMessage}</div>
              )}
            </div>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              disabled={refreshingOperator}
              onClick={handleRefreshOperator}
            >
              {refreshingOperator ? 'Consultando…' : 'Atualizar na operadora'}
            </button>
          </div>
        )}

        <div className="funi-section">
          <div className="funi-section-title">Identificação da guia</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6">
              <label>2 — Nº Guia no Prestador</label>
              <FuniCharInput value={form.providerGuideNumber} maxLength={20} onChange={(v) => setField('providerGuideNumber', v)} />
            </div>
            <div className="funi-field funi-span-6">
              <label>3 — Número da Guia Atribuído pela Operadora</label>
              <FuniCharInput value={form.operatorGuideNumber} maxLength={20} onChange={(v) => setField('operatorGuideNumber', v)} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Beneficiário</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-6">
              <label>4 — Número da Carteira</label>
              <FuniCharInput value={form.beneficiaryCardNumber} maxLength={20} onChange={(v) => setField('beneficiaryCardNumber', v)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>5 — Validade da Carteira</label>
              <input type="date" value={form.cardValidity} onChange={(e) => setField('cardValidity', e.target.value)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>6 — Atendimento a RN (S/N)</label>
              <select value={form.newbornCare} onChange={(e) => setField('newbornCare', e.target.value as Funi21ConsultationForm['newbornCare'])}>
                <option value="">—</option>
                <option value="S">S</option>
                <option value="N">N</option>
              </select>
            </div>
            <div className="funi-field funi-span-8">
              <label>7 — Nome</label>
              <input value={form.beneficiaryName} onChange={(e) => setField('beneficiaryName', e.target.value)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>8 — Cartão Nacional de Saúde</label>
              <FuniCharInput value={form.beneficiaryCns} maxLength={15} onChange={(v) => setField('beneficiaryCns', v.replace(/\D/g, ''))} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Contratado</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-4">
              <label>9 — Código na Operadora</label>
              <FuniCharInput value={form.providerOperatorCode} maxLength={14} onChange={(v) => setField('providerOperatorCode', v)} />
            </div>
            <div className="funi-field funi-span-8">
              <label>10 — Nome do Contratado</label>
              <input value={form.contractedName} onChange={(e) => setField('contractedName', e.target.value)} />
            </div>
            <div className="funi-field funi-span-3">
              <label>11 — Código CNES</label>
              <FuniCharInput value={form.cnesCode} maxLength={7} onChange={(v) => setField('cnesCode', v.replace(/\D/g, ''))} />
            </div>
            <div className="funi-field funi-span-9">
              <label>12 — Nome do Profissional Executante</label>
              <input value={form.executingProfessionalName} onChange={(e) => setField('executingProfessionalName', e.target.value)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>13 — Conselho</label>
              <input value={form.professionalCouncil} onChange={(e) => setField('professionalCouncil', e.target.value)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>14 — Número no Conselho</label>
              <FuniCharInput value={form.councilNumber} maxLength={15} onChange={(v) => setField('councilNumber', v)} />
            </div>
            <div className="funi-field funi-span-2">
              <label>15 — UF</label>
              <input maxLength={2} value={form.councilUf} onChange={(e) => setField('councilUf', e.target.value.toUpperCase())} />
            </div>
            <div className="funi-field funi-span-4">
              <label>16 — Código CBO</label>
              <FuniCharInput value={form.cboCode} maxLength={6} onChange={(v) => setField('cboCode', v.replace(/\D/g, ''))} />
            </div>
          </div>
        </div>

        <div className="funi-section">
          <div className="funi-section-title">Dados do Atendimento / Procedimento Realizado</div>
          <div className="funi-section-body">
            <div className="funi-field funi-span-3">
              <label>17 — Indicação de Acidente</label>
              <select value={form.accidentIndicator} onChange={(e) => setField('accidentIndicator', e.target.value)}>
                {accidentIndicatorOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div className="funi-field funi-span-3">
              <label>18 — Data do Atendimento</label>
              <input type="date" required value={form.attendanceDate} onChange={(e) => setField('attendanceDate', e.target.value)} />
            </div>
            <div className="funi-field funi-span-3">
              <label>19 — Tipo de Consulta</label>
              <select value={form.consultationType} onChange={(e) => setField('consultationType', e.target.value)}>
                {consultationTypeOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
            <div className="funi-field funi-span-1">
              <label>20 — Tabela</label>
              <input maxLength={2} value={form.procedureTable} onChange={(e) => setField('procedureTable', e.target.value)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>21 — Código do Procedimento (TUSS)</label>
              <input value={form.procedureCode} onChange={(e) => setField('procedureCode', e.target.value)} />
              {tussHits.length > 0 && (
                <div className="card" style={{ marginTop: 4, padding: 4, maxHeight: 120, overflow: 'auto' }}>
                  {tussHits.slice(0, 6).map((t) => (
                    <button
                      key={`${t.tussCode}-${t.description}`}
                      type="button"
                      className="btn btn-secondary btn-sm"
                      style={{ display: 'block', width: '100%', marginBottom: 2, textAlign: 'left', fontSize: 10 }}
                      onClick={() => setForm((f) => ({
                        ...f,
                        procedureCode: t.tussCode,
                        procedureDescription: t.description,
                        procedureValue: String(t.suggestedPrice ?? f.procedureValue),
                      }))}
                    >
                      {t.tussCode} — {t.description}
                    </button>
                  ))}
                </div>
              )}
            </div>
            <div className="funi-field funi-span-4">
              <label>Descrição</label>
              <input value={form.procedureDescription} onChange={(e) => setField('procedureDescription', e.target.value)} />
            </div>
            <div className="funi-field funi-span-4">
              <label>22 — Valor do Procedimento (R$)</label>
              <input
                inputMode="decimal"
                value={form.procedureValue}
                onChange={(e) => setField('procedureValue', e.target.value)}
              />
            </div>
            <div className="funi-field funi-span-12">
              <label>23 — Observação / Justificativa</label>
              <textarea value={form.observation} onChange={(e) => setField('observation', e.target.value)} />
            </div>
          </div>
        </div>

        <div className="funi-signatures">
          <FuniSignatureField
            fieldNumber="24"
            label="Assinatura do Profissional Executante"
            value={form.executingProfessionalSignature}
            onChange={(v) => setField('executingProfessionalSignature', v)}
            layoutKey={`consult-exec-${patientId}`}
          />
          <FuniSignatureField
            fieldNumber="25"
            label="Assinatura do Beneficiário ou Responsável"
            value={form.beneficiarySignature}
            onChange={(v) => setField('beneficiarySignature', v)}
            layoutKey={`consult-ben-${patientId}`}
          />
        </div>
      </article>
    </FuniGuideShell>
  );
}
