import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, type PatientDetailDto } from '../api/client';
import { FeegowPatientInsert } from '../components/feegow/patients/FeegowPatientInsert';
import { FeegowPatientScreenLayout } from '../components/feegow/patients/FeegowPatientScreenLayout';
import { FeegowPatientSectionContent } from '../components/feegow/patients/FeegowPatientSectionContent';
import { FeegowPatientSidebar } from '../components/feegow/patients/FeegowPatientSidebar';
import {
  birthLabelFromDate,
  emptyFeegowPatientForm,
  feegowFormToCreatePayload,
  formatFeegowPatientFormFields,
  parseFeegowStructuredNotes,
  parseSchedulingFromNotes,
  type FeegowPatientFormState,
} from '../components/feegow/patients/feegowPatientForm';
import { feegowPatientNavLabel } from '../components/feegow/patients/feegowPatientNav';
import { useAuth } from '../auth/AuthContext';
import { isValidCns } from '../utils/inputMasks';
import type { FormEvent } from 'react';

type Props = {
  patientId: string;
  section?: string;
};

function detailToFeegowForm(d: PatientDetailDto): FeegowPatientFormState {
  const primaryInsurance = d.insurances?.find((i) => i.isPrimary) ?? d.insurances?.[0];
  const parsedNotes = parseFeegowStructuredNotes(d.notes);
  const form: FeegowPatientFormState = {
    ...emptyFeegowPatientForm(),
    ...parsedNotes.fields,
    fullName: d.fullName,
    socialName: d.socialName ?? '',
    cpf: d.cpf,
    birthDate: d.birthDate,
    gender: d.gender,
    email: d.email ?? '',
    phone: d.phone ?? '',
    mobilePhone: d.mobilePhone ?? '',
    addressStreet: d.addressStreet ?? '',
    addressNumber: d.addressNumber ?? '',
    addressComplement: d.addressComplement ?? '',
    addressNeighborhood: d.addressNeighborhood ?? '',
    addressCity: d.addressCity ?? '',
    addressState: d.addressState ?? '',
    addressZipCode: d.addressZipCode ?? '',
    motherName: d.motherName ?? '',
    emergencyContactName: d.emergencyContactName ?? '',
    emergencyContactPhone: d.emergencyContactPhone ?? '',
    emergencyContactRelationship: d.emergencyContactRelationship ?? '',
    notes: parsedNotes.userNotes,
    schedulingPrograms: parseSchedulingFromNotes(d.notes),
    photoData: d.photoData,
    rg: d.rg ?? '',
    nationality: d.nationality ?? 'Brasileira',
    bloodType: d.bloodType ?? '',
    occupation: d.occupation ?? '',
    maritalStatus: d.maritalStatus ?? '',
    birthPlace: d.birthPlace ?? '',
    chartNumber: d.medicalRecordNumber ?? parsedNotes.fields.chartNumber ?? '',
    cns: primaryInsurance?.cnsNumber ?? '',
    insurances: d.insurances?.map((i) => ({
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
    })) ?? [],
  };

  return { ...form, ...formatFeegowPatientFormFields(form) };
}

export function FeegowPatientRecordPage({ patientId, section = 'dados-principais' }: Props) {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [patient, setPatient] = useState<PatientDetailDto | null>(null);
  const [form, setForm] = useState<FeegowPatientFormState>(emptyFeegowPatientForm);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    api.getPatient(patientId)
      .then((detail) => {
        setPatient(detail);
        if (section === 'dados-principais') {
          setForm(detailToFeegowForm(detail));
        }
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar paciente.'))
      .finally(() => setLoading(false));
  }, [patientId, section]);

  function handleChange(patch: Partial<FeegowPatientFormState>) {
    setForm((prev) => ({ ...prev, ...patch }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!patient || !hasPermission('patients.create')) return;
    setError('');
    setSuccess('');
    setSaving(true);
    try {
      if (form.cns.trim() && !isValidCns(form.cns)) {
        setError('CNS inválido — verifique os 15 dígitos.');
        return;
      }

      const payload = feegowFormToCreatePayload(form);
      await api.updatePatient(patientId, { ...payload, isActive: patient.isActive });
      setSuccess('Paciente atualizado com sucesso.');
      const refreshed = await api.getPatient(patientId);
      setPatient(refreshed);
      setForm(detailToFeegowForm(refreshed));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar paciente.');
    } finally {
      setSaving(false);
    }
  }

  const birthLabel = patient ? birthLabelFromDate(patient.birthDate) : 'Paciente';
  const sectionLabel = feegowPatientNavLabel(section);

  if (loading) {
    return (
      <FeegowPatientScreenLayout sidebar={<FeegowPatientSidebar mode="record" patientId={patientId} activeSection={section} />}>
        <div className="feegow-patient-card feegow-patient-card-empty"><p>Carregando paciente…</p></div>
      </FeegowPatientScreenLayout>
    );
  }

  if (!patient) {
    return (
      <FeegowPatientScreenLayout error={error} sidebar={<FeegowPatientSidebar mode="record" patientId={patientId} activeSection={section} />}>
        <div className="feegow-patient-card feegow-patient-card-empty"><p>Paciente não encontrado.</p></div>
      </FeegowPatientScreenLayout>
    );
  }

  if (section === 'dados-principais') {
    return (
      <FeegowPatientScreenLayout
        error={error}
        success={success}
        sidebar={<FeegowPatientSidebar mode="record" patientId={patientId} activeSection={section} />}
      >
        <FeegowPatientInsert
          form={form}
          onChange={handleChange}
          onSubmit={handleSubmit}
          saving={saving}
          birthLabel={birthLabel}
          patientId={patientId}
        />
      </FeegowPatientScreenLayout>
    );
  }

  return (
    <FeegowPatientScreenLayout
      error={error}
      success={success}
      sidebar={<FeegowPatientSidebar mode="record" patientId={patientId} activeSection={section} />}
    >
      <div className="feegow-patient-card">
        <header className="feegow-patient-card-head">
          <div className="feegow-patient-breadcrumb">
            <Link to={`/recepcao/pacientes/${patientId}/dados-principais`} className="feegow-patient-crumb-link">{patient.fullName}</Link>
            <span className="feegow-patient-crumb-sep">/</span>
            <span className="feegow-patient-crumb-label">{sectionLabel}</span>
          </div>
          <div className="feegow-patient-toolbar">
            <button type="button" className="feegow-patient-tool-btn" onClick={() => navigate(-1)} aria-label="Voltar">‹</button>
            <Link to={`/recepcao/pacientes/${patientId}/dados-principais`} className="feegow-patient-save-btn">EDITAR CADASTRO</Link>
          </div>
        </header>
        <div className="feegow-patient-form-body">
          <FeegowPatientSectionContent patientId={patientId} section={section} patient={patient} />
        </div>
      </div>
    </FeegowPatientScreenLayout>
  );
}
