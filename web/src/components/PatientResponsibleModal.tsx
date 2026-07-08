import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api, type PatientDetailDto } from '../api/client';
import { Modal } from './Modal';
import { isMinorPatient, RESPONSIBLE_RELATIONSHIPS } from '../utils/patientResponsible';

export type ResponsibleFormState = {
  patientId: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  emergencyContactRelationship: string;
  motherName: string;
};

const emptyForm: ResponsibleFormState = {
  patientId: '',
  emergencyContactName: '',
  emergencyContactPhone: '',
  emergencyContactRelationship: '',
  motherName: '',
};

type PatientOption = {
  id: string;
  fullName: string;
  birthDate: string;
  cpf: string;
};

type Props = {
  open: boolean;
  onClose: () => void;
  patients: PatientOption[];
  initialPatientId?: string;
  onSaved: () => void;
};

function detailToResponsibleForm(detail: PatientDetailDto): ResponsibleFormState {
  return {
    patientId: detail.id,
    emergencyContactName: detail.emergencyContactName ?? '',
    emergencyContactPhone: detail.emergencyContactPhone ?? '',
    emergencyContactRelationship: detail.emergencyContactRelationship ?? '',
    motherName: detail.motherName ?? '',
  };
}

export function PatientResponsibleModal({
  open,
  onClose,
  patients,
  initialPatientId,
  onSaved,
}: Props) {
  const [form, setForm] = useState<ResponsibleFormState>(emptyForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const selectedPatient = useMemo(
    () => patients.find((p) => p.id === form.patientId),
    [patients, form.patientId],
  );

  const minor = selectedPatient ? isMinorPatient(selectedPatient.birthDate) : false;

  useEffect(() => {
    if (!open) return;

    setError('');
    if (!initialPatientId) {
      setForm(emptyForm);
      return;
    }

    setLoading(true);
    api.getPatient(initialPatientId)
      .then((detail) => setForm(detailToResponsibleForm(detail)))
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar paciente.'))
      .finally(() => setLoading(false));
  }, [open, initialPatientId]);

  async function handlePatientChange(patientId: string) {
    setForm({ ...emptyForm, patientId });
    if (!patientId) return;

    setLoading(true);
    setError('');
    try {
      const detail = await api.getPatient(patientId);
      setForm(detailToResponsibleForm(detail));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar paciente.');
    } finally {
      setLoading(false);
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!form.patientId) {
      setError('Selecione o paciente.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      const detail = await api.getPatient(form.patientId);
      await api.updatePatient(form.patientId, {
        fullName: detail.fullName,
        socialName: detail.socialName,
        birthDate: detail.birthDate,
        gender: detail.gender,
        email: detail.email,
        phone: detail.phone,
        mobilePhone: detail.mobilePhone,
        addressStreet: detail.addressStreet,
        addressNumber: detail.addressNumber,
        addressComplement: detail.addressComplement,
        addressNeighborhood: detail.addressNeighborhood,
        addressCity: detail.addressCity,
        addressState: detail.addressState,
        addressZipCode: detail.addressZipCode,
        motherName: form.motherName.trim() || undefined,
        emergencyContactName: form.emergencyContactName.trim() || undefined,
        emergencyContactPhone: form.emergencyContactPhone.trim() || undefined,
        emergencyContactRelationship: form.emergencyContactRelationship || undefined,
        notes: detail.notes,
        photoData: detail.photoData,
        rg: detail.rg,
        nationality: detail.nationality,
        bloodType: detail.bloodType,
        occupation: detail.occupation,
        maritalStatus: detail.maritalStatus,
        birthPlace: detail.birthPlace,
        isActive: detail.isActive,
        insurances: detail.insurances?.map((i) => ({
          healthInsuranceId: i.healthInsuranceId,
          cardNumber: i.cardNumber,
          planName: i.planName,
          cardHolderName: i.cardHolderName,
          productCode: i.productCode,
          cnsNumber: i.cnsNumber,
          accommodationType: i.accommodationType,
          validFrom: i.validFrom,
          validUntil: i.validUntil,
          isPrimary: i.isPrimary,
        })),
      });
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar responsável.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={initialPatientId ? 'Editar responsável' : 'Cadastrar responsável'}
      subtitle="Vincule o responsável legal ou contato de emergência ao paciente."
      width="md"
    >
      <form className="form-grid" onSubmit={handleSubmit}>
        {error && <div className="alert alert-error full">{error}</div>}

        <div className="form-field full">
          <label htmlFor="responsible-patient">Paciente *</label>
          <select
            id="responsible-patient"
            required
            disabled={!!initialPatientId || loading}
            value={form.patientId}
            onChange={(e) => handlePatientChange(e.target.value)}
          >
            <option value="">Selecione o paciente</option>
            {patients.map((p) => (
              <option key={p.id} value={p.id}>
                {p.fullName} — {p.cpf}
              </option>
            ))}
          </select>
        </div>

        {selectedPatient && minor && (
          <div className="alert alert-warning full">
            Paciente menor de idade: informe responsável (nome e telefone) ou nome da mãe.
          </div>
        )}

        <div className="form-field full">
          <div className="form-section-title">Responsável / contato de emergência</div>
        </div>

        <div className="form-field">
          <label htmlFor="responsible-name">Nome do responsável *</label>
          <input
            id="responsible-name"
            required
            value={form.emergencyContactName}
            onChange={(e) => setForm({ ...form, emergencyContactName: e.target.value })}
            placeholder="Nome completo"
          />
        </div>

        <div className="form-field">
          <label htmlFor="responsible-phone">Telefone *</label>
          <input
            id="responsible-phone"
            required
            value={form.emergencyContactPhone}
            onChange={(e) => setForm({ ...form, emergencyContactPhone: e.target.value })}
            placeholder="(00) 00000-0000"
          />
        </div>

        <div className="form-field">
          <label htmlFor="responsible-relationship">Parentesco</label>
          <select
            id="responsible-relationship"
            value={form.emergencyContactRelationship}
            onChange={(e) => setForm({ ...form, emergencyContactRelationship: e.target.value })}
          >
            <option value="">Selecione</option>
            {RESPONSIBLE_RELATIONSHIPS.map((item) => (
              <option key={item} value={item}>{item}</option>
            ))}
          </select>
        </div>

        <div className="form-field">
          <label htmlFor="responsible-mother">Nome da mãe {minor ? '*' : ''}</label>
          <input
            id="responsible-mother"
            required={minor && !form.emergencyContactName.trim()}
            value={form.motherName}
            onChange={(e) => setForm({ ...form, motherName: e.target.value })}
          />
        </div>

        <div className="form-actions full">
          <button className="btn btn-secondary" type="button" onClick={onClose} disabled={loading}>
            Cancelar
          </button>
          <button className="btn" type="submit" disabled={loading}>
            {loading ? 'Salvando...' : 'Salvar responsável'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
