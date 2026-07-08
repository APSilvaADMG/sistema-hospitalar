import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api,
  tissGuideTypeLabels,
  type CreateTissGuideRequest,
  type HealthInsuranceDto,
  type PatientDto,
  type ServiceUnitDto,
  type TissGuideClinicalRequest,
  type TissGuideDto,
  type TissGuideItemRequest,
  type TissGuideTypeCatalogDto,
  type TussSearchResultDto,
  type UpdateTissGuideItemRequest,
  type UpdateTissGuideRequest,
} from '../../api/client';
import { Modal } from '../Modal';
import { TissGuideClinicalFields } from '../TissGuideClinicalFields';
import { getGuideCatalogEntry } from '../../data/tissGuideCatalog';

const defaultItem = (): TissGuideItemRequest => ({
  tussCode: '',
  description: '',
  quantity: 1,
  unitPrice: 0,
});

type GuideForm = {
  patientId: string;
  healthInsuranceId: string;
  serviceUnitId: string;
  guideType: number;
  appointmentId: string;
  hospitalizationId: string;
  surgeryId: string;
  notes: string;
  clinical: TissGuideClinicalRequest;
  items: UpdateTissGuideItemRequest[];
};

function emptyClinical(): TissGuideClinicalRequest {
  return { serviceCharacter: 1, accidentIndicator: 0 };
}

function guideToForm(g: TissGuideDto): GuideForm {
  return {
    patientId: g.patientId,
    healthInsuranceId: g.healthInsuranceId,
    serviceUnitId: g.serviceUnitId ?? '',
    guideType: g.guideType,
    appointmentId: g.appointmentId ?? '',
    hospitalizationId: g.hospitalizationId ?? '',
    surgeryId: g.clinical.surgeryId ?? '',
    notes: g.notes ?? '',
    clinical: { ...g.clinical },
    items: g.items.map((i) => ({
      id: i.id,
      tussCode: i.tussCode,
      description: i.description,
      quantity: i.quantity,
      unitPrice: i.unitPrice,
      priceTableSource: i.priceTableSource,
      cid10Code: i.cid10Code,
      relatedTussCode: i.relatedTussCode,
    })),
  };
}

function emptyGuideForm(guideType = 1): GuideForm {
  return {
    patientId: '',
    healthInsuranceId: '',
    serviceUnitId: '',
    guideType,
    appointmentId: '',
    hospitalizationId: '',
    surgeryId: '',
    notes: '',
    clinical: emptyClinical(),
    items: [{ ...defaultItem() }],
  };
}

function formatMoney(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

type Props = {
  open: boolean;
  editingGuide: TissGuideDto | null;
  initialGuideType?: number;
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  serviceUnits: ServiceUnitDto[];
  guideCatalog: TissGuideTypeCatalogDto[];
  onClose: () => void;
  onSaved: (message: string) => void;
  onError: (message: string) => void;
};

export function TissGuideEditModal({
  open,
  editingGuide,
  initialGuideType = 1,
  patients,
  insurances,
  serviceUnits,
  guideCatalog,
  onClose,
  onSaved,
  onError,
}: Props) {
  const [form, setForm] = useState<GuideForm>(emptyGuideForm(initialGuideType));
  const [tussResults, setTussResults] = useState<Record<number, TussSearchResultDto[]>>({});
  const [prefillCard, setPrefillCard] = useState('');
  const [prefillPlan, setPrefillPlan] = useState('');
  const [prefillAuth, setPrefillAuth] = useState('');
  const [saving, setSaving] = useState(false);

  const isDraftForm = !editingGuide || editingGuide.status === 1;

  const selectedGuideTypeInfo = useMemo(
    () => getGuideCatalogEntry(form.guideType, guideCatalog),
    [form.guideType, guideCatalog],
  );

  const formTotal = useMemo(
    () => form.items.reduce((s, i) => s + i.quantity * i.unitPrice, 0),
    [form.items],
  );

  useEffect(() => {
    if (!open) return;
    if (editingGuide) {
      setForm(guideToForm(editingGuide));
    } else {
      const defaultUnit = serviceUnits.find((u) => u.isDefault)?.id ?? '';
      setForm({ ...emptyGuideForm(initialGuideType), serviceUnitId: defaultUnit });
    }
    setTussResults({});
    setPrefillCard('');
    setPrefillPlan('');
    setPrefillAuth('');
  }, [open, editingGuide, initialGuideType, serviceUnits]);

  useEffect(() => {
    if (!open || editingGuide || !form.patientId) return;
    api.getTissGuidePrefill({
      patientId: form.patientId,
      guideType: form.guideType,
      healthInsuranceId: form.healthInsuranceId || undefined,
    }).then((prefill) => {
      setPrefillCard(prefill.beneficiaryCardNumber ?? '');
      setPrefillPlan(prefill.beneficiaryPlanName ?? '');
      setPrefillAuth(prefill.authorizationPassword ?? '');
      setForm((f) => ({
        ...f,
        healthInsuranceId: prefill.healthInsuranceId ?? f.healthInsuranceId,
        appointmentId: prefill.appointmentId ?? f.appointmentId,
        hospitalizationId: prefill.hospitalizationId ?? f.hospitalizationId,
        surgeryId: prefill.surgeryId ?? f.surgeryId,
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
          surgeryId: prefill.surgeryId,
        },
        items: prefill.suggestedItems.length > 0
          ? prefill.suggestedItems.map((i) => ({ ...i }))
          : f.items,
      }));
    }).catch(console.error);
  }, [open, editingGuide, form.patientId, form.guideType, form.healthInsuranceId]);

  function updateItem(index: number, patch: Partial<UpdateTissGuideItemRequest>) {
    setForm((f) => ({
      ...f,
      items: f.items.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    }));
  }

  async function searchTussForItem(index: number, query: string) {
    if (query.trim().length < 2) {
      setTussResults((prev) => ({ ...prev, [index]: [] }));
      return;
    }
    const results = await api.searchTuss(query);
    setTussResults((prev) => ({ ...prev, [index]: results }));
  }

  async function loadSuggestedItems() {
    if (!form.patientId) {
      onError('Selecione o paciente antes de importar procedimentos.');
      return;
    }
    try {
      const items = await api.getSuggestedTissItems({
        patientId: form.patientId,
        hospitalizationId: form.hospitalizationId || undefined,
        appointmentId: form.appointmentId || undefined,
        guideType: form.guideType,
        surgeryId: form.surgeryId || form.clinical.surgeryId || undefined,
      });
      if (items.length === 0) {
        onSaved('Nenhum procedimento pendente encontrado para importar.');
        return;
      }
      setForm((f) => ({ ...f, items: items.map((i) => ({ ...i })) }));
      onSaved(`${items.length} procedimento(s) importado(s) do prontuário.`);
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Erro ao importar procedimentos.');
    }
  }

  function addItem() {
    setForm((f) => ({ ...f, items: [...f.items, { ...defaultItem() }] }));
  }

  function removeItem(index: number) {
    setForm((f) => ({ ...f, items: f.items.filter((_, i) => i !== index) }));
  }

  async function handleSaveGuide(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      const clinical = {
        ...form.clinical,
        surgeryId: form.surgeryId || form.clinical.surgeryId,
      };
      if (editingGuide) {
        const payload: UpdateTissGuideRequest = {
          healthInsuranceId: form.healthInsuranceId,
          serviceUnitId: form.serviceUnitId || undefined,
          appointmentId: form.appointmentId || undefined,
          hospitalizationId: form.hospitalizationId || undefined,
          guideType: form.guideType,
          notes: form.notes || undefined,
          clinical,
          items: form.items,
        };
        await api.updateTissGuide(editingGuide.id, payload);
        onSaved('Guia TISS atualizada.');
      } else {
        const payload: CreateTissGuideRequest = {
          patientId: form.patientId,
          healthInsuranceId: form.healthInsuranceId,
          serviceUnitId: form.serviceUnitId || undefined,
          appointmentId: form.appointmentId || undefined,
          hospitalizationId: form.hospitalizationId || undefined,
          guideType: form.guideType,
          notes: form.notes || undefined,
          clinical,
          items: form.items,
        };
        await api.createTissGuide(payload);
        onSaved('Guia TISS criada em rascunho.');
      }
      onClose();
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Erro ao salvar guia.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={editingGuide ? `Editar ${editingGuide.guideNumber}` : 'Nova guia TISS'}
      subtitle={isDraftForm ? 'Rascunho — todos os campos podem ser alterados.' : 'Somente leitura.'}
      width="lg"
    >
      <form className="form-grid" onSubmit={handleSaveGuide}>
        {!editingGuide && (
          <div className="form-field">
            <label>Paciente *</label>
            <select required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
              <option value="">Selecione</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
        )}
        {editingGuide && (
          <div className="form-field">
            <label>Paciente</label>
            <input disabled value={editingGuide.patientName} />
          </div>
        )}
        <div className="form-field">
          <label>Convênio *</label>
          <select
            required
            disabled={!isDraftForm}
            value={form.healthInsuranceId}
            onChange={(e) => setForm({ ...form, healthInsuranceId: e.target.value })}
          >
            <option value="">Selecione</option>
            {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
          </select>
        </div>
        <div className="form-field">
          <label>Unidade de atendimento</label>
          <select
            disabled={!isDraftForm}
            value={form.serviceUnitId}
            onChange={(e) => setForm({ ...form, serviceUnitId: e.target.value })}
          >
            <option value="">Padrão do sistema</option>
            {serviceUnits.filter((u) => u.isActive).map((u) => (
              <option key={u.id} value={u.id}>{u.name}</option>
            ))}
          </select>
        </div>
        <div className="form-field">
          <label>Tipo de guia</label>
          <select
            disabled={!isDraftForm}
            value={form.guideType}
            onChange={(e) => setForm({ ...form, guideType: Number(e.target.value) })}
          >
            {Object.entries(tissGuideTypeLabels)
              .filter(([code]) => {
                const n = Number(code);
                return n <= 7 || n === 12 || guideCatalog.some((g) => g.code === n && g.isCreatable);
              })
              .map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
          {selectedGuideTypeInfo && (
            <p className="form-hint tiss-guide-type-hint">
              <strong>{selectedGuideTypeInfo.name}</strong> — {selectedGuideTypeInfo.whenToUse}
            </p>
          )}
        </div>
        <div className="form-field">
          <label>ID agendamento (opcional)</label>
          <input
            disabled={!isDraftForm}
            value={form.appointmentId}
            onChange={(e) => setForm({ ...form, appointmentId: e.target.value })}
            placeholder="UUID do agendamento"
          />
        </div>
        <div className="form-field">
          <label>ID internação (opcional)</label>
          <input
            disabled={!isDraftForm}
            value={form.hospitalizationId}
            onChange={(e) => setForm({ ...form, hospitalizationId: e.target.value })}
            placeholder="UUID da internação"
          />
        </div>
        <div className="form-field full">
          <label>Observações</label>
          <textarea
            rows={2}
            disabled={!isDraftForm}
            value={form.notes}
            onChange={(e) => setForm({ ...form, notes: e.target.value })}
          />
        </div>

        <div className="form-field full">
          <TissGuideClinicalFields
            guideType={form.guideType}
            clinical={form.clinical}
            disabled={!isDraftForm}
            beneficiaryCard={prefillCard || editingGuide?.beneficiaryCardNumber}
            beneficiaryPlan={prefillPlan || editingGuide?.beneficiaryPlanName}
            authorizationPassword={prefillAuth || editingGuide?.authorizationPassword}
            onChange={(clinical) => setForm((f) => ({ ...f, clinical }))}
          />
        </div>

        <div className="form-field full">
          <div className="form-section-title">
            Procedimentos TUSS
            <span style={{ float: 'right', fontWeight: 600 }}>Total: {formatMoney(formTotal)}</span>
          </div>
          <table className="data-table tiss-editable-table">
            <thead>
              <tr><th>TUSS</th><th>Descrição</th><th>Qtd</th><th>Valor unit.</th><th>Total</th>{isDraftForm && <th />}</tr>
            </thead>
            <tbody>
              {form.items.map((item, idx) => (
                <tr key={item.id ?? `new-${idx}`}>
                  <td>
                    <input
                      required
                      disabled={!isDraftForm}
                      value={item.tussCode}
                      onChange={(e) => {
                        updateItem(idx, { tussCode: e.target.value });
                        searchTussForItem(idx, e.target.value).catch(console.error);
                      }}
                    />
                    {(tussResults[idx]?.length ?? 0) > 0 && isDraftForm && (
                      <div className="card" style={{ marginTop: 4, padding: 4, maxHeight: 100, overflow: 'auto' }}>
                        {tussResults[idx]?.map((t) => (
                          <button
                            key={`${t.tussCode}-${t.description}`}
                            type="button"
                            className="btn btn-secondary btn-sm"
                            style={{ display: 'block', width: '100%', marginBottom: 2, textAlign: 'left', fontSize: 11 }}
                            onClick={() => {
                              updateItem(idx, { tussCode: t.tussCode, description: t.description, unitPrice: t.suggestedPrice ?? item.unitPrice });
                              setTussResults((prev) => ({ ...prev, [idx]: [] }));
                            }}
                          >
                            {t.tussCode} — {t.description} ({t.source})
                          </button>
                        ))}
                      </div>
                    )}
                  </td>
                  <td>
                    <input
                      required
                      disabled={!isDraftForm}
                      value={item.description}
                      onChange={(e) => updateItem(idx, { description: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min={1}
                      required
                      disabled={!isDraftForm}
                      value={item.quantity}
                      onChange={(e) => updateItem(idx, { quantity: Number(e.target.value) })}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      step="0.01"
                      min={0}
                      required
                      disabled={!isDraftForm}
                      value={item.unitPrice}
                      onChange={(e) => updateItem(idx, { unitPrice: Number(e.target.value) })}
                    />
                  </td>
                  <td>{formatMoney(item.quantity * item.unitPrice)}</td>
                  {isDraftForm && (
                    <td>
                      <button type="button" className="btn btn-secondary btn-sm" onClick={() => removeItem(idx)} disabled={form.items.length <= 1}>
                        Remover
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          {isDraftForm && (
            <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
              <button type="button" className="btn btn-secondary btn-sm" onClick={addItem}>+ Procedimento</button>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => loadSuggestedItems().catch(console.error)}>
                Importar do prontuário
              </button>
            </div>
          )}
        </div>

        {isDraftForm && (
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn" disabled={saving}>
              {saving ? 'Salvando…' : editingGuide ? 'Salvar alterações' : 'Criar guia'}
            </button>
          </div>
        )}
      </form>
    </Modal>
  );
}
