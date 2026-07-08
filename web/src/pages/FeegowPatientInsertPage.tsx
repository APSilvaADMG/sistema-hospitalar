import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FeegowPatientInsert } from '../components/feegow/patients/FeegowPatientInsert';
import { FeegowPatientScreenLayout } from '../components/feegow/patients/FeegowPatientScreenLayout';
import { FeegowPatientSidebar } from '../components/feegow/patients/FeegowPatientSidebar';
import {
  emptyFeegowPatientForm,
  feegowFormToCreatePayload,
  type FeegowPatientFormState,
} from '../components/feegow/patients/feegowPatientForm';
import { feegowPatientRecordPath } from '../components/feegow/patients/feegowPatientNav';
import { isValidCpf, isValidCns, onlyDigits } from '../utils/inputMasks';

export function FeegowPatientInsertPage() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [form, setForm] = useState<FeegowPatientFormState>(emptyFeegowPatientForm);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    api.getPatients('', 1, 1)
      .then((result) => {
        setForm((prev) => (
          prev.chartNumber
            ? prev
            : { ...prev, chartNumber: String(result.totalCount + 1) }
        ));
      })
      .catch(console.error);
  }, []);

  if (!hasPermission('patients.create')) {
    return (
      <FeegowPatientScreenLayout
        error="Você não tem permissão para cadastrar pacientes."
        sidebar={<FeegowPatientSidebar mode="insert" activeSection="dados-principais" />}
      >
        <div className="feegow-patient-card feegow-patient-card-empty">
          <p>Acesso negado.</p>
        </div>
      </FeegowPatientScreenLayout>
    );
  }

  function handleChange(patch: Partial<FeegowPatientFormState>) {
    setForm((prev) => ({ ...prev, ...patch }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      if (!form.noCpf && !form.cpf.trim()) {
        setError('Informe o CPF do paciente ou marque "Sem CPF" e cadastre o responsável legal.');
        return;
      }
      if (form.noCpf && !form.legalResponsible.name.trim()) {
        setError('Informe os dados do responsável legal para pacientes sem CPF.');
        return;
      }
      if (!form.chartNumber.trim()) {
        setError('Informe o número do prontuário.');
        return;
      }

      if (!form.noCpf) {
        if (!isValidCpf(form.cpf)) {
          setError('CPF inválido.');
          return;
        }

        const cpfCheck = await api.checkPatientCpf(onlyDigits(form.cpf));
        if (!cpfCheck.available) {
          setError(cpfCheck.message ?? 'Já existe um prontuário cadastrado com este CPF.');
          return;
        }
      } else if (!isValidCpf(form.legalResponsible.cpf)) {
        setError('CPF do responsável legal inválido.');
        return;
      }

      if (form.cns.trim() && !isValidCns(form.cns)) {
        setError('CNS inválido — verifique os 15 dígitos.');
        return;
      }

      const payload = feegowFormToCreatePayload(form);
      const result = await api.createPatient(payload);
      setSuccess('Paciente cadastrado com sucesso.');
      navigate(feegowPatientRecordPath(result.patient.id, 'dados-principais'));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar paciente.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <FeegowPatientScreenLayout
      error={error}
      success={success}
      sidebar={<FeegowPatientSidebar mode="insert" activeSection="dados-principais" />}
    >
      <FeegowPatientInsert
        form={form}
        onChange={handleChange}
        onSubmit={handleSubmit}
        saving={saving}
      />
    </FeegowPatientScreenLayout>
  );
}
