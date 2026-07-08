import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  susGuideTypeLabels,
  type CreateSusGuideRequest,
  type PatientDto,
  type ProfessionalDto,
  type ServiceUnitDto,
  type SusGuideDto,
  type UpdateSusGuideRequest,
} from '../../api/client';
import { Modal } from '../Modal';

type SusForm = {
  patientId: string;
  guideType: number;
  professionalId: string;
  serviceUnitId: string;
  appointmentId: string;
  hospitalizationId: string;
  cid10Code: string;
  sigtapProcedureCode: string;
  procedureDescription: string;
  competence: string;
  notes: string;
  totalAmount: string;
};

function emptySusForm(guideType = 1): SusForm {
  return {
    patientId: '',
    guideType,
    professionalId: '',
    serviceUnitId: '',
    appointmentId: '',
    hospitalizationId: '',
    cid10Code: '',
    sigtapProcedureCode: '',
    procedureDescription: '',
    competence: '',
    notes: '',
    totalAmount: '',
  };
}

function guideToForm(g: SusGuideDto): SusForm {
  return {
    patientId: g.patientId,
    guideType: g.guideType,
    professionalId: g.professionalId ?? '',
    serviceUnitId: g.serviceUnitId ?? '',
    appointmentId: g.appointmentId ?? '',
    hospitalizationId: g.hospitalizationId ?? '',
    cid10Code: g.cid10Code ?? '',
    sigtapProcedureCode: g.sigtapProcedureCode ?? '',
    procedureDescription: g.procedureDescription ?? '',
    competence: g.competence ?? '',
    notes: g.notes ?? '',
    totalAmount: g.totalAmount != null ? String(g.totalAmount) : '',
  };
}

type Props = {
  open: boolean;
  editingGuide: SusGuideDto | null;
  initialGuideType?: number;
  patients: PatientDto[];
  professionals: ProfessionalDto[];
  serviceUnits: ServiceUnitDto[];
  onClose: () => void;
  onSaved: (message: string) => void;
  onError: (message: string) => void;
};

export function SusGuideEditModal({
  open,
  editingGuide,
  initialGuideType = 1,
  patients,
  professionals,
  serviceUnits,
  onClose,
  onSaved,
  onError,
}: Props) {
  const [form, setForm] = useState<SusForm>(emptySusForm(initialGuideType));
  const [saving, setSaving] = useState(false);

  const isDraftForm = !editingGuide || editingGuide.status === 1;

  useEffect(() => {
    if (!open) return;
    if (editingGuide) {
      setForm(guideToForm(editingGuide));
    } else {
      const defaultUnit = serviceUnits.find((u) => u.isDefault)?.id ?? '';
      setForm({ ...emptySusForm(initialGuideType), serviceUnitId: defaultUnit });
    }
  }, [open, editingGuide, initialGuideType, serviceUnits]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      const totalAmount = form.totalAmount ? Number(form.totalAmount) : undefined;
      if (editingGuide) {
        const payload: UpdateSusGuideRequest = {
          professionalId: form.professionalId || undefined,
          serviceUnitId: form.serviceUnitId || undefined,
          appointmentId: form.appointmentId || undefined,
          hospitalizationId: form.hospitalizationId || undefined,
          cid10Code: form.cid10Code || undefined,
          sigtapProcedureCode: form.sigtapProcedureCode || undefined,
          procedureDescription: form.procedureDescription || undefined,
          competence: form.competence || undefined,
          notes: form.notes || undefined,
          totalAmount,
        };
        await api.updateSusGuide(editingGuide.id, payload);
        onSaved(`Guia SUS ${susGuideTypeLabels[editingGuide.guideType] ?? ''} atualizada.`);
      } else {
        const payload: CreateSusGuideRequest = {
          patientId: form.patientId,
          guideType: form.guideType,
          professionalId: form.professionalId || undefined,
          serviceUnitId: form.serviceUnitId || undefined,
          appointmentId: form.appointmentId || undefined,
          hospitalizationId: form.hospitalizationId || undefined,
          cid10Code: form.cid10Code || undefined,
          sigtapProcedureCode: form.sigtapProcedureCode || undefined,
          procedureDescription: form.procedureDescription || undefined,
          competence: form.competence || undefined,
          notes: form.notes || undefined,
          totalAmount,
        };
        const created = await api.createSusGuide(payload);
        onSaved(`Guia SUS ${susGuideTypeLabels[created.guideType] ?? ''} criada: ${created.guideNumber}`);
      }
      onClose();
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Erro ao salvar guia SUS.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={editingGuide ? `Editar ${editingGuide.guideNumber}` : 'Nova guia SUS'}
      subtitle={isDraftForm ? 'Rascunho — campos editáveis.' : 'Somente leitura.'}
      width="lg"
    >
      <form className="form-grid" onSubmit={handleSubmit}>
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
          <label>Tipo SUS *</label>
          <select
            disabled={!!editingGuide}
            value={form.guideType}
            onChange={(e) => setForm({ ...form, guideType: Number(e.target.value) })}
          >
            {Object.entries(susGuideTypeLabels).map(([v, l]) => (
              <option key={v} value={v}>{l}</option>
            ))}
          </select>
        </div>
        <div className="form-field">
          <label>Médico responsável</label>
          <select
            disabled={!isDraftForm}
            value={form.professionalId}
            onChange={(e) => setForm({ ...form, professionalId: e.target.value })}
          >
            <option value="">Não informado</option>
            {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
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
          <label>Código SIGTAP</label>
          <input
            disabled={!isDraftForm}
            value={form.sigtapProcedureCode}
            onChange={(e) => setForm({ ...form, sigtapProcedureCode: e.target.value })}
            placeholder="Ex.: 0301010072"
          />
        </div>
        <div className="form-field">
          <label>CID-10</label>
          <input
            disabled={!isDraftForm}
            value={form.cid10Code}
            onChange={(e) => setForm({ ...form, cid10Code: e.target.value })}
          />
        </div>
        <div className="form-field full">
          <label>Descrição do procedimento</label>
          <input
            disabled={!isDraftForm}
            value={form.procedureDescription}
            onChange={(e) => setForm({ ...form, procedureDescription: e.target.value })}
          />
        </div>
        <div className="form-field">
          <label>Competência</label>
          <input
            disabled={!isDraftForm}
            value={form.competence}
            onChange={(e) => setForm({ ...form, competence: e.target.value })}
            placeholder="AAAA/MM"
          />
        </div>
        <div className="form-field">
          <label>Valor estimado (R$)</label>
          <input
            type="number"
            step="0.01"
            min={0}
            disabled={!isDraftForm}
            value={form.totalAmount}
            onChange={(e) => setForm({ ...form, totalAmount: e.target.value })}
          />
        </div>
        <div className="form-field">
          <label>ID agendamento</label>
          <input
            disabled={!isDraftForm}
            value={form.appointmentId}
            onChange={(e) => setForm({ ...form, appointmentId: e.target.value })}
          />
        </div>
        <div className="form-field">
          <label>ID internação</label>
          <input
            disabled={!isDraftForm}
            value={form.hospitalizationId}
            onChange={(e) => setForm({ ...form, hospitalizationId: e.target.value })}
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

        {isDraftForm && (
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn" disabled={saving}>
              {saving ? 'Salvando…' : editingGuide ? 'Salvar alterações' : 'Criar guia SUS'}
            </button>
          </div>
        )}
      </form>
    </Modal>
  );
}
