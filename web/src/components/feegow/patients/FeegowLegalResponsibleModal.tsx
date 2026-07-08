import { useEffect, useState, type FormEvent } from 'react';
import { Modal } from '../../Modal';
import { formatCpfInput, formatRgInput, isValidCpf, onlyDigits } from '../../../utils/inputMasks';
import { calculateAgeYears } from '../../../utils/dateUtils';
import {
  emptyLegalResponsible,
  type LegalResponsibleFormState,
} from './feegowPatientForm';

export type { LegalResponsibleFormState };
export { emptyLegalResponsible };

const RELATIONSHIP_OPTIONS = [
  { value: 0, label: 'Selecione' },
  { value: 1, label: 'Pai' },
  { value: 2, label: 'Mãe' },
  { value: 3, label: 'Cônjuge' },
  { value: 4, label: 'Outro (com documentação formal)' },
];

const AUTHORIZATION_OPTIONS = [
  { value: 0, label: 'Selecione' },
  { value: 1, label: 'Termo de Curatela' },
  { value: 2, label: 'Termo de Guarda' },
  { value: 3, label: 'Procuração Pública' },
];

type Props = {
  open: boolean;
  patientName: string;
  initialValue?: LegalResponsibleFormState;
  onClose: () => void;
  onConfirm: (value: LegalResponsibleFormState) => void;
};

export function FeegowLegalResponsibleModal({
  open,
  patientName,
  initialValue,
  onClose,
  onConfirm,
}: Props) {
  const [form, setForm] = useState<LegalResponsibleFormState>(emptyLegalResponsible());
  const [error, setError] = useState('');

  useEffect(() => {
    if (open) {
      setForm(initialValue ?? emptyLegalResponsible());
      setError('');
    }
  }, [open, initialValue]);

  function patch(partial: Partial<LegalResponsibleFormState>) {
    setForm((prev) => ({ ...prev, ...partial }));
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');

    if (!form.name.trim()) {
      setError('Informe o nome do responsável legal.');
      return;
    }
    if (!isValidCpf(form.cpf)) {
      setError('CPF do responsável inválido.');
      return;
    }
    if (!form.birthDate) {
      setError('Informe a data de nascimento do responsável.');
      return;
    }
    const age = calculateAgeYears(form.birthDate);
    if (age === null || age < 18) {
      setError('O responsável legal deve ser maior de 18 anos.');
      return;
    }
    if (!form.relationship) {
      setError('Selecione o parentesco do responsável.');
      return;
    }
    if (!form.rg.trim()) {
      setError('Informe o RG ou documento de identificação do responsável.');
      return;
    }
    if (form.relationship === 4) {
      if (!form.authorizationDocumentType) {
        setError('Para parentesco "Outro", selecione o documento formal de autorização.');
        return;
      }
      if (!form.authorizationDocumentReference.trim()) {
        setError('Informe a referência/número do documento formal de autorização.');
        return;
      }
    }

    onConfirm({
      ...form,
      name: form.name.trim(),
      cpf: onlyDigits(form.cpf),
      rg: form.rg.trim(),
      authorizationDocumentReference: form.authorizationDocumentReference.trim(),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Responsável legal"
      subtitle={
        patientName.trim()
          ? `Paciente sem CPF: ${patientName.trim()}`
          : 'Paciente sem CPF — informe o responsável legal'
      }
      width="lg"
    >
      <form className="feegow-form-grid feegow-legal-responsible-form" onSubmit={handleSubmit}>
        <p className="feegow-field-span-full feegow-legal-responsible-note">
          O prontuário será vinculado ao CPF do responsável legal. Parentesco permitido: pai, mãe, cônjuge
          ou outro maior de 18 anos com Termo de Curatela, Termo de Guarda ou Procuração Pública.
        </p>

        <label className="feegow-field feegow-field-span-full">
          <span>Nome do responsável<span className="feegow-req">*</span></span>
          <input
            value={form.name}
            onChange={(e) => patch({ name: e.target.value })}
            autoFocus
          />
        </label>

        <label className="feegow-field">
          <span>CPF do responsável<span className="feegow-req">*</span></span>
          <input
            value={form.cpf}
            onChange={(e) => patch({ cpf: formatCpfInput(e.target.value) })}
            placeholder="000.000.000-00"
            inputMode="numeric"
            autoComplete="off"
          />
        </label>

        <label className="feegow-field">
          <span>Nascimento do responsável<span className="feegow-req">*</span></span>
          <input
            type="date"
            value={form.birthDate}
            onChange={(e) => patch({ birthDate: e.target.value })}
          />
        </label>

        <label className="feegow-field">
          <span>Parentesco<span className="feegow-req">*</span></span>
          <select
            value={form.relationship}
            onChange={(e) => patch({ relationship: Number(e.target.value) })}
          >
            {RELATIONSHIP_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>

        <label className="feegow-field">
          <span>RG / Documento<span className="feegow-req">*</span></span>
          <input
            value={form.rg}
            onChange={(e) => patch({ rg: formatRgInput(e.target.value) })}
            placeholder="00.000.000-0"
          />
        </label>

        {form.relationship === 4 ? (
          <>
            <label className="feegow-field feegow-field-span-full">
              <span>Documento formal de autorização<span className="feegow-req">*</span></span>
              <select
                value={form.authorizationDocumentType}
                onChange={(e) => patch({ authorizationDocumentType: Number(e.target.value) })}
              >
                {AUTHORIZATION_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>

            <label className="feegow-field feegow-field-span-full">
              <span>Referência / número do documento<span className="feegow-req">*</span></span>
              <input
                value={form.authorizationDocumentReference}
                onChange={(e) => patch({ authorizationDocumentReference: e.target.value })}
                placeholder="Número ou identificação do termo/procuração"
              />
            </label>
          </>
        ) : null}

        {error ? <p className="feegow-field-span-full feegow-form-error">{error}</p> : null}

        <div className="feegow-form-actions feegow-field-span-full">
          <button type="button" className="feegow-form-btn-cancel" onClick={onClose}>
            Cancelar
          </button>
          <button type="submit" className="feegow-patient-save-btn">
            Confirmar responsável
          </button>
        </div>
      </form>
    </Modal>
  );
}

export function legalResponsibleRelationshipLabel(value: number): string {
  return RELATIONSHIP_OPTIONS.find((option) => option.value === value)?.label ?? '—';
}
