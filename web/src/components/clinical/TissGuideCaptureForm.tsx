import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api,
  type HealthInsuranceDto,
  type PatientDto,
  type TussSearchResultDto,
} from '../../api/client';
import { TissGuideClinicalFields } from '../TissGuideClinicalFields';
import {
  emptyTissGuideForm,
  tissGuideFormToCreateRequest,
  defaultTissGuideItem,
  type TissGuideFormState,
} from '../../utils/tissGuideFormTypes';
import {
  type ClinicalDocumentContext,
  findClinicalSource,
  generateGuideFromClinicalData,
  parseClinicalFormData,
  saveClinicalSource,
} from '../../utils/clinicalDocumentWorkflow';

type Props = {
  guideType: number;
  guideTitle?: string;
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  workflow?: 'direct' | 'clinical';
  clinicalContext?: ClinicalDocumentContext;
  lockedPatientId?: string;
  initialSourceId?: string;
  onClinicalSaved?: () => void;
  onSaved?: (guideId: string) => void;
};

export function TissGuideCaptureForm({
  guideType,
  guideTitle,
  patients,
  insurances,
  workflow = 'clinical',
  clinicalContext,
  lockedPatientId,
  initialSourceId,
  onClinicalSaved,
  onSaved,
}: Props) {
  const [form, setForm] = useState<TissGuideFormState>(() => ({ ...emptyTissGuideForm(guideType), guideType }));
  const [clinicalSourceId, setClinicalSourceId] = useState<string | undefined>(initialSourceId);
  const [tussResults, setTussResults] = useState<Record<number, TussSearchResultDto[]>>({});
  const [prefillCard, setPrefillCard] = useState('');
  const [prefillPlan, setPrefillPlan] = useState('');
  const [prefillAuth, setPrefillAuth] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (lockedPatientId) {
      setForm((f) => ({ ...f, patientId: lockedPatientId }));
    }
  }, [lockedPatientId]);

  useEffect(() => {
    setForm((f) => ({ ...f, guideType }));
  }, [guideType]);

  useEffect(() => {
    if (!form.patientId) return;
    let cancelled = false;

    (async () => {
      if (initialSourceId) {
        const source = await api.getClinicalSource(initialSourceId);
        if (cancelled) return;
        setClinicalSourceId(source.id);
        const parsed = parseClinicalFormData<TissGuideFormState>(source.formDataJson);
        if (parsed) {
          setForm({ ...parsed, guideType });
          return;
        }
      }

      if (workflow === 'clinical' && clinicalContext) {
        const existing = await findClinicalSource(form.patientId, guideType, clinicalContext);
        if (cancelled) return;
        if (existing) {
          setClinicalSourceId(existing.id);
          const parsed = parseClinicalFormData<TissGuideFormState>(existing.formDataJson);
          if (parsed) {
            setForm({ ...parsed, guideType });
            return;
          }
        }
      }

      const prefill = await api.getTissGuidePrefill({
        patientId: form.patientId,
        guideType,
        healthInsuranceId: form.healthInsuranceId || undefined,
        appointmentId: (clinicalContext?.appointmentId ?? form.appointmentId) || undefined,
        hospitalizationId: (clinicalContext?.hospitalizationId ?? form.hospitalizationId) || undefined,
        chemotherapySessionId: clinicalContext?.chemotherapySessionId,
        surgeryId: (clinicalContext?.surgeryId ?? form.surgeryId) || undefined,
        labOrderId: (clinicalContext?.labOrderId ?? form.labOrderId) || undefined,
        imagingStudyId: (clinicalContext?.imagingStudyId ?? form.imagingStudyId) || undefined,
      });
      if (cancelled) return;

      setPrefillCard(prefill.beneficiaryCardNumber ?? '');
      setPrefillPlan(prefill.beneficiaryPlanName ?? '');
      setPrefillAuth(prefill.authorizationPassword ?? '');
      setForm((f) => ({
        ...f,
        guideType,
        healthInsuranceId: prefill.healthInsuranceId ?? f.healthInsuranceId,
        appointmentId: clinicalContext?.appointmentId ?? prefill.appointmentId ?? f.appointmentId,
        hospitalizationId: clinicalContext?.hospitalizationId ?? prefill.hospitalizationId ?? f.hospitalizationId,
        surgeryId: clinicalContext?.surgeryId ?? prefill.surgeryId ?? f.surgeryId,
        labOrderId: clinicalContext?.labOrderId ?? f.labOrderId,
        imagingStudyId: clinicalContext?.imagingStudyId ?? f.imagingStudyId,
        clinical: {
          ...f.clinical,
          cid10Code: prefill.cid10Code ?? f.clinical.cid10Code,
          requestingProfessionalId: prefill.requestingProfessionalId,
          requestingProfessionalName: prefill.requestingProfessionalName,
          requestingProfessionalCrm: prefill.requestingProfessionalCrm,
          executingProfessionalId: prefill.executingProfessionalId,
          executingProfessionalName: prefill.executingProfessionalName,
          admissionDate: prefill.admissionDate,
          dischargeDate: prefill.dischargeDate,
          requestedBedType: prefill.requestedBedType,
          surgeryId: clinicalContext?.surgeryId ?? prefill.surgeryId,
        },
        items: prefill.suggestedItems.length > 0
          ? prefill.suggestedItems.map((i) => ({ ...i }))
          : f.items,
      }));
    })().catch(console.error);

    return () => {
      cancelled = true;
    };
  }, [form.patientId, form.healthInsuranceId, guideType, workflow, clinicalContext, initialSourceId]);

  const formTotal = useMemo(
    () => form.items.reduce((s, i) => s + i.quantity * i.unitPrice, 0),
    [form.items],
  );

  function updateItem(index: number, patch: Partial<TissGuideFormState['items'][number]>) {
    setForm((f) => ({
      ...f,
      items: f.items.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    }));
  }

  async function loadSuggestedItems() {
    if (!form.patientId) {
      setError('Selecione o paciente.');
      return;
    }
    const items = await api.getSuggestedTissItems({
      patientId: form.patientId,
      hospitalizationId: form.hospitalizationId || undefined,
      appointmentId: form.appointmentId || undefined,
      guideType: form.guideType,
      surgeryId: form.surgeryId || form.clinical.surgeryId || undefined,
    });
    if (items.length === 0) {
      setSuccess('Nenhum procedimento sugerido encontrado.');
      return;
    }
    setForm((f) => ({ ...f, items: items.map((i) => ({ ...i })) }));
    setSuccess(`${items.length} procedimento(s) importados.`);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    if (!form.patientId || !form.healthInsuranceId) {
      setError('Selecione paciente e convênio.');
      return;
    }
    setSaving(true);
    try {
      if (workflow === 'clinical') {
        const source = await saveClinicalSource(
          form.patientId,
          guideType,
          form.healthInsuranceId,
          clinicalContext ?? { label: guideTitle ?? `Guia TISS ${guideType}` },
          { ...form, guideType },
        );
        setClinicalSourceId(source.id);
        setSuccess('Dados salvos no sistema.');
        onClinicalSaved?.();
        return;
      }
      const created = await api.createTissGuide(tissGuideFormToCreateRequest(form));
      setSuccess(`Guia ${created.guideNumber} criada.`);
      onSaved?.(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.');
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerate() {
    if (!form.patientId || !form.healthInsuranceId) {
      setError('Selecione paciente e convênio.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const { guide } = await generateGuideFromClinicalData(
        form.patientId,
        guideType,
        form.healthInsuranceId,
        clinicalContext ?? { label: guideTitle ?? `Guia TISS ${guideType}` },
        { ...form, guideType },
        clinicalSourceId,
      );
      setSuccess(`Guia ${guide.guideNumber} gerada.`);
      onSaved?.(guide.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar guia.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="card-panel appt-panel" onSubmit={handleSubmit}>
      <div className="card-panel-header">{guideTitle ?? `Guia TISS (tipo ${guideType})`}</div>
      {workflow === 'clinical' && (
        <div className="alert alert-info" style={{ margin: '12px 12px 0' }}>
          Dados capturados no fluxo assistencial. A guia será gerada no faturamento com preenchimento automático.
        </div>
      )}
      {error && <div className="alert alert-error" style={{ margin: 12 }}>{error}</div>}
      {success && <div className="alert alert-success" style={{ margin: 12 }}>{success}</div>}

      <div className="form-grid" style={{ padding: 12 }}>
        <div className="form-field">
          <label>Paciente *</label>
          <select
            required
            value={form.patientId}
            disabled={Boolean(lockedPatientId)}
            onChange={(e) => setForm((f) => ({ ...f, patientId: e.target.value }))}
          >
            <option value="">Selecione</option>
            {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
          </select>
        </div>
        <div className="form-field">
          <label>Convênio *</label>
          <select
            required
            value={form.healthInsuranceId}
            onChange={(e) => setForm((f) => ({ ...f, healthInsuranceId: e.target.value }))}
          >
            <option value="">Selecione</option>
            {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
          </select>
        </div>
        <div className="form-field full">
          <TissGuideClinicalFields
            guideType={form.guideType}
            clinical={form.clinical}
            beneficiaryCard={prefillCard}
            beneficiaryPlan={prefillPlan}
            authorizationPassword={prefillAuth}
            onChange={(clinical) => setForm((f) => ({ ...f, clinical }))}
          />
        </div>
        <div className="form-field full">
          <label>Observações</label>
          <textarea
            rows={2}
            value={form.notes}
            onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
          />
        </div>
        <div className="form-field full" style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
          <strong>Procedimentos TUSS</strong>
          <button type="button" className="btn btn-secondary btn-sm" onClick={loadSuggestedItems}>
            Importar do prontuário
          </button>
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => setForm((f) => ({ ...f, items: [...f.items, defaultTissGuideItem()] }))}>
            + Item
          </button>
          <span className="form-hint">Total: {formTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
        </div>
        {form.items.map((item, index) => (
          <div key={index} className="form-field full" style={{ display: 'grid', gridTemplateColumns: '1fr 2fr 80px 100px auto', gap: 8, alignItems: 'end' }}>
            <input
              placeholder="TUSS"
              value={item.tussCode}
              onChange={(e) => {
                updateItem(index, { tussCode: e.target.value });
                if (e.target.value.length >= 2) {
                  api.searchTuss(e.target.value).then((r) => setTussResults((prev) => ({ ...prev, [index]: r }))).catch(() => undefined);
                }
              }}
            />
            <input placeholder="Descrição" value={item.description} onChange={(e) => updateItem(index, { description: e.target.value })} />
            <input type="number" min={1} value={item.quantity} onChange={(e) => updateItem(index, { quantity: Number(e.target.value) })} />
            <input type="number" min={0} step="0.01" value={item.unitPrice} onChange={(e) => updateItem(index, { unitPrice: Number(e.target.value) })} />
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => setForm((f) => ({ ...f, items: f.items.filter((_, i) => i !== index) }))}>×</button>
            {tussResults[index]?.length ? (
              <div style={{ gridColumn: '1 / -1' }}>
                {tussResults[index].slice(0, 4).map((t) => (
                  <button
                    key={`${t.tussCode}-${t.description}`}
                    type="button"
                    className="btn btn-secondary btn-sm"
                    style={{ marginRight: 4, marginBottom: 4 }}
                    onClick={() => updateItem(index, { tussCode: t.tussCode, description: t.description, unitPrice: t.suggestedPrice ?? item.unitPrice })}
                  >
                    {t.tussCode}
                  </button>
                ))}
              </div>
            ) : null}
          </div>
        ))}
        <div className="form-field full modal-actions">
          <button type="submit" className="btn" disabled={saving}>
            {saving ? 'Salvando…' : workflow === 'clinical' ? 'Salvar dados no sistema' : 'Salvar guia TISS'}
          </button>
          {workflow === 'clinical' && (
            <button type="button" className="btn btn-secondary" disabled={saving} onClick={handleGenerate}>
              Gerar guia agora
            </button>
          )}
        </div>
      </div>
    </form>
  );
}
