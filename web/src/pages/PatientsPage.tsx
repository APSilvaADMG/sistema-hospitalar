import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  api,
  resolvePatientModality,
  wardModalityLabels,
  type BedDto,
  type CreatePatientRequest,
  type HealthInsuranceDto,
  type PatientDto,
  type PatientDetailDto,
  type ProfessionalDto,
  type UpdatePatientRequest,
} from '../api/client';
import { AdmissionTextField } from '../components/AdmissionTextField';
import { PatientConsentsPanel } from '../components/PatientConsentsPanel';
import { AddressFields } from '../components/AddressFields';
import { AvailableBedsPicker } from '../components/AvailableBedsPicker';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModulePageChrome } from '../components/ModulePageChrome';
import { patientTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { useLocation } from 'react-router-dom';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { PersonAvatar } from '../components/PersonAvatar';
import { PhotoCapture } from '../components/PhotoCapture';
import { PatientInsuranceFields } from '../components/PatientInsuranceFields';
import { PatientResponsibleModal } from '../components/PatientResponsibleModal';
import { useAuth } from '../auth/AuthContext';
import { formatBrDate } from '../utils/dateUtils';
import {
  calculateAgeYears,
  isMinorPatient,
  patientHasResponsible,
  responsibleStatusLabel,
  RESPONSIBLE_RELATIONSHIPS,
} from '../utils/patientResponsible';
import { usePersistedFilters } from '../hooks/usePersistedFilters';
import { printPatientLabel } from '../utils/printTemplates';
import { printPatientIdentityWristband } from '../utils/patientIdentityPrint';
import { PatientIdentityScanBar } from '../components/nursing/BedsideCarePanel';
import { TablePagination } from '../components/feegow/TablePagination';
import { isValidCpf, onlyDigits } from '../utils/inputMasks';
import { FILTER_STORAGE_KEYS } from '../utils/persistedFilters';
import type { PatientInsuranceInput } from '../api/client';

const emptyForm: CreatePatientRequest = {
  fullName: '',
  socialName: '',
  cpf: '',
  birthDate: '',
  gender: 0,
  email: '',
  phone: '',
  mobilePhone: '',
  addressStreet: '',
  addressNumber: '',
  addressComplement: '',
  addressNeighborhood: '',
  addressCity: '',
  addressState: '',
  addressZipCode: '',
  motherName: '',
  emergencyContactName: '',
  emergencyContactPhone: '',
  emergencyContactRelationship: '',
  notes: '',
  photoData: undefined,
  rg: '',
  nationality: 'Brasileira',
  bloodType: '',
  occupation: '',
  maritalStatus: '',
  birthPlace: '',
  insurances: [],
};

const emptyAdmit = { bedId: '', professionalId: '', reason: '', diagnosis: '' };
const PATIENTS_PAGE_SIZE = 50;
const RESPONSIBLE_PAGE_SIZE = 100;

function insurancesToInput(items: PatientDetailDto['insurances']): PatientInsuranceInput[] {
  return (items ?? []).map((i) => ({
    healthInsuranceId: i.healthInsuranceId,
    cardNumber: i.cardNumber,
    planName: i.planName ?? '',
    cardHolderName: i.cardHolderName ?? '',
    productCode: i.productCode ?? '',
    cnsNumber: i.cnsNumber ?? '',
    accommodationType: i.accommodationType ?? '',
    validFrom: i.validFrom ?? '',
    validUntil: i.validUntil ?? '',
    isPrimary: i.isPrimary,
  }));
}

function normalizeInsurances(items: PatientInsuranceInput[]): PatientInsuranceInput[] {
  return items
    .filter((i) => i.healthInsuranceId && i.cardNumber.trim())
    .map((i) => ({
      healthInsuranceId: i.healthInsuranceId,
      cardNumber: i.cardNumber.trim(),
      planName: i.planName?.trim() || undefined,
      cardHolderName: i.cardHolderName?.trim() || undefined,
      productCode: i.productCode?.trim() || undefined,
      cnsNumber: i.cnsNumber?.trim() || undefined,
      accommodationType: i.accommodationType?.trim() || undefined,
      validFrom: i.validFrom || undefined,
      validUntil: i.validUntil || undefined,
      isPrimary: i.isPrimary,
    }));
}

function detailToForm(d: PatientDetailDto): CreatePatientRequest & { isActive: boolean } {
  return {
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
    notes: d.notes ?? '',
    photoData: d.photoData,
    rg: d.rg ?? '',
    nationality: d.nationality ?? 'Brasileira',
    bloodType: d.bloodType ?? '',
    occupation: d.occupation ?? '',
    maritalStatus: d.maritalStatus ?? '',
    birthPlace: d.birthPlace ?? '',
    insurances: insurancesToInput(d.insurances),
    isActive: d.isActive,
  };
}

type PatientsPageProps = {
  embedded?: boolean;
  sectionBasePath?: string;
};

export function PatientsPage({ embedded = false, sectionBasePath }: PatientsPageProps = {}) {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const navBasePath = sectionBasePath ?? '/pacientes';
  const { section } = useModuleSection(navBasePath);
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [patientDetails, setPatientDetails] = useState<PatientDetailDto[]>([]);
  const [consentPatientId, setConsentPatientId] = useState('');
  const { filters, patch } = usePersistedFilters(FILTER_STORAGE_KEYS.patients, { search: '' });
  const search = filters.search;
  const [form, setForm] = useState(emptyForm);
  const [isActive, setIsActive] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();
  const [admitOnCreate, setAdmitOnCreate] = useState(false);
  const [admitForm, setAdmitForm] = useState(emptyAdmit);
  const [beds, setBeds] = useState<BedDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [healthInsurances, setHealthInsurances] = useState<HealthInsuranceDto[]>([]);
  const [reasonSnippets, setReasonSnippets] = useState<string[]>([]);
  const [diagnosisSnippets, setDiagnosisSnippets] = useState<string[]>([]);
  const [responsibleFilter, setResponsibleFilter] = useState<'all' | 'missing' | 'minors'>('all');
  const [responsibleSearch, setResponsibleSearch] = useState('');
  const [showResponsibleModal, setShowResponsibleModal] = useState(false);
  const [responsiblePatientId, setResponsiblePatientId] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [listLoading, setListLoading] = useState(false);

  const pageSize = activeSection === 'responsaveis' ? RESPONSIBLE_PAGE_SIZE : PATIENTS_PAGE_SIZE;

  const patientModality = useMemo(() => {
    const primary = form.insurances?.find((i) => i.isPrimary) ?? form.insurances?.[0];
    if (!primary?.healthInsuranceId) return resolvePatientModality(null);
    const insurance = healthInsurances.find((h) => h.id === primary.healthInsuranceId);
    return resolvePatientModality(insurance?.name);
  }, [form.insurances, healthInsurances]);

  const responsibleRows = useMemo(() => {
    const term = responsibleSearch.trim().toLowerCase();
    return patients.filter((patient) => {
      if (term) {
        const haystack = [
          patient.fullName,
          patient.cpf,
          patient.emergencyContactName,
          patient.motherName,
        ]
          .filter(Boolean)
          .join(' ')
          .toLowerCase();
        if (!haystack.includes(term)) return false;
      }
      if (responsibleFilter === 'missing') return !patientHasResponsible(patient);
      if (responsibleFilter === 'minors') {
        return isMinorPatient(patient.birthDate) && !patientHasResponsible(patient);
      }
      return true;
    });
  }, [patients, responsibleFilter, responsibleSearch]);

  const responsibleStats = useMemo(() => {
    const missing = patients.filter((p) => !patientHasResponsible(p)).length;
    const minorsMissing = patients.filter(
      (p) => isMinorPatient(p.birthDate) && !patientHasResponsible(p),
    ).length;
    return { missing, minorsMissing };
  }, [patients]);

  function openResponsibleCreate() {
    setResponsiblePatientId(undefined);
    setShowResponsibleModal(true);
  }

  function openResponsibleEdit(patientId: string) {
    setResponsiblePatientId(patientId);
    setShowResponsibleModal(true);
  }

  useEffect(() => {
    if (!admitOnCreate) return;
    api.getBeds({ modality: patientModality, status: 1 })
      .then(setBeds)
      .catch(console.error);
  }, [admitOnCreate, patientModality]);

  useEffect(() => {
    if (!showModal || !admitOnCreate) return;
    Promise.all([
      api.getHospitalizationSnippets('Reason'),
      api.getHospitalizationSnippets('Diagnosis'),
    ])
      .then(([reasons, diagnoses]) => {
        setReasonSnippets(reasons.map((s) => s.text));
        setDiagnosisSnippets(diagnoses.map((s) => s.text));
      })
      .catch(console.error);
  }, [showModal, admitOnCreate]);

  async function loadPatients(term?: string, targetPage = 1) {
    setListLoading(true);
    try {
      const result = await api.getPatients(term, targetPage, pageSize);
      setPatients(result.items);
      setTotalCount(result.totalCount);
      setPage(result.page);
    } finally {
      setListLoading(false);
    }
  }

  useEffect(() => {
    setPage(1);
    loadPatients(search, 1).catch(console.error);
  }, [activeSection]);

  useEffect(() => {
    const needsDetail = ['carteirinha-sus', 'documentos'].includes(activeSection);
    if (!needsDetail || patients.length === 0) return;
    Promise.all(patients.slice(0, 40).map((p) => api.getPatient(p.id)))
      .then(setPatientDetails)
      .catch(console.error);
  }, [activeSection, patients]);

  useEffect(() => {
    if (searchParams.get('novo') !== '1') return;
    void openCreate();
    const next = new URLSearchParams(searchParams);
    next.delete('novo');
    setSearchParams(next, { replace: true });
  }, [searchParams, setSearchParams]);

  async function openCreate() {
    setEditingId(null);
    setForm({ ...emptyForm, insurances: [] });
    setIsActive(true);
    setAdmitOnCreate(false);
    setAdmitForm(emptyAdmit);
    setShowModal(true);
    try {
      const [profList, insList] = await Promise.all([
        api.getProfessionals(),
        api.getHealthInsurances(),
      ]);
      setBeds([]);
      setProfessionals(profList);
      setHealthInsurances(insList);
    } catch (err) {
      console.error(err);
    }
  }

  async function openEdit(id: string) {
    setError('');
    try {
      const detail = await api.getPatient(id);
      const mapped = detailToForm(detail);
      setEditingId(id);
      setForm(mapped);
      setIsActive(mapped.isActive);
      setShowModal(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar paciente.');
    }
  }

  async function handleSearch(event: FormEvent) {
    event.preventDefault();
    setPage(1);
    await loadPatients(search, 1);
  }

  function handlePatientsPageChange(nextPage: number) {
    loadPatients(search, nextPage).catch(console.error);
  }

  async function handlePrintLabel(id: string) {
    try {
      const detail = await api.getPatient(id);
      printPatientLabel(detail);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao imprimir etiqueta.');
    }
  }

  async function handlePrintWristband(id: string) {
    try {
      const [detail, identity] = await Promise.all([
        api.getPatient(id),
        api.generatePatientBracelet(id, {}),
      ]);
      await printPatientIdentityWristband(detail, identity);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao imprimir pulseira.');
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');

    const payload = {
      ...form,
      photoData: form.photoData || undefined,
      socialName: form.socialName || undefined,
      email: form.email || undefined,
      phone: form.phone || undefined,
      mobilePhone: form.mobilePhone || undefined,
      addressStreet: form.addressStreet || undefined,
      addressNumber: form.addressNumber || undefined,
      addressComplement: form.addressComplement || undefined,
      addressNeighborhood: form.addressNeighborhood || undefined,
      addressCity: form.addressCity || undefined,
      addressState: form.addressState || undefined,
      addressZipCode: form.addressZipCode || undefined,
      motherName: form.motherName || undefined,
      emergencyContactName: form.emergencyContactName || undefined,
      emergencyContactPhone: form.emergencyContactPhone || undefined,
      emergencyContactRelationship: form.emergencyContactRelationship || undefined,
      notes: form.notes || undefined,
      rg: form.rg || undefined,
      nationality: form.nationality || undefined,
      bloodType: form.bloodType || undefined,
      occupation: form.occupation || undefined,
      maritalStatus: form.maritalStatus || undefined,
      birthPlace: form.birthPlace || undefined,
      insurances: normalizeInsurances(form.insurances ?? []),
    };

    try {
      if (editingId) {
        await api.updatePatient(editingId, { ...payload, isActive } as UpdatePatientRequest);
        setSuccess('Paciente atualizado com sucesso.');
      } else {
        if (!form.cpf.trim()) {
          setError('Informe o CPF do paciente.');
          return;
        }
        if (!isValidCpf(form.cpf)) {
          setError('CPF inválido.');
          return;
        }

        const cpfCheck = await api.checkPatientCpf(onlyDigits(form.cpf));
        if (!cpfCheck.available) {
          setError(cpfCheck.message ?? 'Já existe um prontuário cadastrado com este CPF.');
          return;
        }

        if (admitOnCreate) {
          if (!admitForm.bedId || !admitForm.professionalId || !admitForm.reason.trim()) {
            setError('Para internar no cadastro, selecione leito, médico e informe o motivo.');
            return;
          }
        }
        const result = await api.createPatient({
          ...payload,
          initialAdmission: admitOnCreate
            ? {
                bedId: admitForm.bedId,
                professionalId: admitForm.professionalId,
                reason: admitForm.reason.trim(),
                diagnosis: admitForm.diagnosis.trim() || undefined,
              }
            : undefined,
        });
        setSuccess(
          result.initialHospitalization
            ? `Paciente cadastrado e internado no leito ${result.initialHospitalization.bedNumber} (${result.initialHospitalization.wardName}).`
            : 'Paciente cadastrado com sucesso.',
        );
      }
      setShowModal(false);
      await loadPatients(search);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar paciente.');
    }
  }

  return (
    <>
      <ModulePageChrome
        embedded={embedded}
        eyebrow="Atendimento"
        title={activeSection === 'responsaveis' ? 'Responsáveis' : activeSection ? breadcrumb.title : 'Pacientes'}
        subtitle={
          activeSection === 'responsaveis'
            ? 'Cadastre e mantenha responsáveis legais e contatos de emergência dos pacientes.'
            : 'Cadastro completo com foto, documentos, endereço e contatos.'
        }
        basePath={navBasePath}
        tabs={patientTabs}
        contextId="reception"
        actions={
          <>
            {hasPermission('patients.create') && activeSection === 'responsaveis' && (
              <button className="btn" type="button" onClick={openResponsibleCreate}>
                + Cadastrar responsável
              </button>
            )}
            {hasPermission('patients.create') && activeSection !== 'responsaveis' && (
              <button className="btn" type="button" onClick={openCreate}>+ Novo paciente</button>
            )}
          </>
        }
      >
      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {activeSection !== 'responsaveis' && (
        <PatientIdentityScanBar
          onResolved={(r) => {
            void openEdit(r.patientId);
            setSuccess(`Paciente identificado: ${r.patientName} (${r.code})`);
          }}
        />
      )}

      <div className="kpi-grid">
        <KpiCard label="Pacientes listados" value={patients.length} variant="primary" />
        <KpiCard label="Com foto" value={patients.filter((p) => p.hasPhoto).length} variant="info" />
        <KpiCard label="Com e-mail" value={patients.filter((p) => p.email).length} variant="success" />
      </div>

      {activeSection === 'responsaveis' && (
        <>
          <div className="kpi-grid" style={{ marginTop: 16 }}>
            <KpiCard label="Pacientes" value={patients.length} variant="primary" />
            <KpiCard label="Sem responsável" value={responsibleStats.missing} variant="warning" />
            <KpiCard label="Menores pendentes" value={responsibleStats.minorsMissing} variant="danger" />
          </div>

          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Responsáveis e contatos de emergência</div>
            <FilterBar
              onSubmit={(e) => e.preventDefault()}
              actions={(
                <>
                  <select
                    value={responsibleFilter}
                    onChange={(e) => setResponsibleFilter(e.target.value as typeof responsibleFilter)}
                    aria-label="Filtrar responsáveis"
                  >
                    <option value="all">Todos</option>
                    <option value="missing">Sem responsável</option>
                    <option value="minors">Menores pendentes</option>
                  </select>
                </>
              )}
            >
              <div className="filter-field grow-lg">
                <label htmlFor="responsible-search">Buscar</label>
                <input
                  id="responsible-search"
                  placeholder="Paciente, CPF ou responsável..."
                  value={responsibleSearch}
                  onChange={(e) => setResponsibleSearch(e.target.value)}
                />
              </div>
            </FilterBar>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Paciente</th>
                    <th>Idade</th>
                    <th>Responsável</th>
                    <th>Parentesco</th>
                    <th>Telefone</th>
                    <th>Nome da mãe</th>
                    <th>Status</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {responsibleRows.map((patient) => (
                    <tr key={patient.id}>
                      <td>
                        <strong>{patient.fullName}</strong>
                        <div className="table-sub">{patient.cpf}</div>
                      </td>
                      <td>{calculateAgeYears(patient.birthDate)} anos</td>
                      <td>{patient.emergencyContactName || '—'}</td>
                      <td>{patient.emergencyContactRelationship || '—'}</td>
                      <td>{patient.emergencyContactPhone || '—'}</td>
                      <td>{patient.motherName || '—'}</td>
                      <td>
                        <span className={`badge ${patientHasResponsible(patient) ? 'badge-success' : 'badge-warning'}`}>
                          {responsibleStatusLabel(patient)}
                        </span>
                      </td>
                      <td>
                        {hasPermission('patients.create') && (
                          <button
                            className="btn btn-secondary btn-sm"
                            type="button"
                            onClick={() => openResponsibleEdit(patient.id)}
                          >
                            {patientHasResponsible(patient) ? 'Editar' : 'Cadastrar'}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                  {responsibleRows.length === 0 && (
                    <tr>
                      <td colSpan={8} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>
                        Nenhum paciente encontrado para o filtro atual.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <TablePagination
              page={page}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={handlePatientsPageChange}
              loading={listLoading}
            />
          </div>
        </>
      )}

      {activeSection === 'historico' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Histórico clínico (PEP)</div>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>CPF</th><th /></tr></thead>
            <tbody>
              {patients.map((p) => (
                <tr key={p.id}>
                  <td>{p.fullName}</td>
                  <td>{p.cpf}</td>
                  <td>
                    <Link to={`/pacientes/${p.id}/prontuario`} className="btn btn-secondary btn-sm">Abrir PEP</Link>
                    <Link to={`/pep/evolucao-medica?patient=${p.id}`} className="btn btn-secondary btn-sm" style={{ marginLeft: 6 }}>Evolução</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <TablePagination
            page={page}
            pageSize={pageSize}
            totalCount={totalCount}
            onPageChange={handlePatientsPageChange}
            loading={listLoading}
          />
        </div>
      )}

      {activeSection === 'documentos' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Documentos do paciente</div>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>RG</th><th>CPF</th><th>Status</th></tr></thead>
            <tbody>
              {patientDetails.map((d) => (
                <tr key={d.id}>
                  <td>{d.fullName}</td>
                  <td>{d.rg || 'Pendente'}</td>
                  <td>{d.cpf}</td>
                  <td>{d.rg ? 'Documentação básica OK' : 'Completar cadastro'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeSection === 'consentimentos' && (
        <div style={{ marginTop: 16 }}>
          <PatientConsentsPanel
            patientId={consentPatientId || undefined}
            patients={patients}
            onPatientChange={setConsentPatientId}
            onSuccess={(msg) => { setSuccess(msg); setError(''); }}
            onError={(msg) => { setError(msg); setSuccess(''); }}
          />
        </div>
      )}

      {activeSection === 'anexos' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Anexos clínicos</h3>
          <p>Exames, laudos e documentos digitalizados ficam vinculados ao PEP de cada paciente.</p>
          <Link to="/pep/anexos" className="btn btn-secondary">Gerenciar no PEP</Link>
        </div>
      )}

      {activeSection === 'carteirinha-sus' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Carteirinha SUS / CNS</div>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>CNS</th><th>Convênio principal</th></tr></thead>
            <tbody>
              {patientDetails.map((d) => (
                <tr key={d.id}>
                  <td>{d.fullName}</td>
                  <td>{d.cns || d.insurances?.find((i) => i.cnsNumber)?.cnsNumber || '—'}</td>
                  <td>{d.insurances?.find((i) => i.isPrimary)?.healthInsuranceName ?? 'SUS / Particular'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {!activeSection && (
      <div className="card-panel appt-panel">
        <FilterBar onSubmit={handleSearch} actions={<button className="btn" type="submit">Buscar</button>}>
          <div className="filter-field grow-lg">
            <label htmlFor="search">Pesquisar</label>
            <input
              id="search"
              placeholder="Nome, CPF, e-mail ou nome social..."
              value={search}
              onChange={(e) => patch({ search: e.target.value })}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Foto</th><th>Nome</th><th>CPF</th><th>Convênio</th><th>Nascimento</th><th>Cidade</th><th>Contato</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {patients.map((patient) => (
                <tr key={patient.id}>
                  <td><PersonAvatar name={patient.fullName} size={36} /></td>
                  <td>
                    <strong>{patient.fullName}</strong>
                    {patient.socialName && <div className="table-sub">{patient.socialName}</div>}
                  </td>
                  <td>{patient.cpf}</td>
                  <td>{patient.primaryInsuranceName ?? '—'}</td>
                  <td>{formatBrDate(patient.birthDate)}</td>
                  <td>{patient.addressCity ?? '—'}</td>
                  <td>{patient.mobilePhone ?? patient.phone ?? patient.email ?? '—'}</td>
                  <td>
                    <div className="table-actions">
                      {hasPermission('patients.create') && (
                        <>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => openEdit(patient.id)}>Editar</button>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => handlePrintLabel(patient.id)}>Etiqueta</button>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => handlePrintWristband(patient.id)}>Pulseira</button>
                        </>
                      )}
                      <Link to={`/pacientes/${patient.id}/prontuario`} className="btn btn-secondary btn-sm">PEP Digital</Link>
                    </div>
                  </td>
                </tr>
              ))}
              {patients.length === 0 && (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum paciente encontrado.</td></tr>
              )}
            </tbody>
          </table>
        </div>
        <TablePagination
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
          onPageChange={handlePatientsPageChange}
          loading={listLoading}
        />
      </div>
      )}

      </ModulePageChrome>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title={editingId ? 'Editar paciente' : 'Novo paciente'}
        subtitle="Preencha o máximo de informações para um cadastro completo."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field full">
            <div className="form-section-title">Foto do paciente</div>
            <PhotoCapture name={form.fullName || 'Paciente'} value={form.photoData} onChange={(photoData) => setForm({ ...form, photoData: photoData ?? undefined })} />
          </div>

          <div className="form-field full"><div className="form-section-title">Identificação</div></div>
          <div className="form-field"><label>Nome completo *</label><input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></div>
          <div className="form-field"><label>Nome social</label><input value={form.socialName} onChange={(e) => setForm({ ...form, socialName: e.target.value })} /></div>
          <div className="form-field"><label>CPF *</label><input required disabled={!!editingId} value={form.cpf} onChange={(e) => setForm({ ...form, cpf: e.target.value })} /></div>
          <div className="form-field"><label>RG</label><input value={form.rg} onChange={(e) => setForm({ ...form, rg: e.target.value })} /></div>
          <div className="form-field"><label>Data de nascimento *</label><input type="date" required value={form.birthDate} onChange={(e) => setForm({ ...form, birthDate: e.target.value })} /></div>
          <div className="form-field"><label>Naturalidade</label><input value={form.birthPlace} onChange={(e) => setForm({ ...form, birthPlace: e.target.value })} /></div>
          <div className="form-field">
            <label>Sexo</label>
            <select value={form.gender} onChange={(e) => setForm({ ...form, gender: Number(e.target.value) })}>
              <option value={0}>Não informado</option>
              <option value={1}>Masculino</option>
              <option value={2}>Feminino</option>
              <option value={3}>Outro</option>
            </select>
          </div>
          <div className="form-field">
            <label>Estado civil</label>
            <select value={form.maritalStatus} onChange={(e) => setForm({ ...form, maritalStatus: e.target.value })}>
              <option value="">Não informado</option>
              <option value="Solteiro(a)">Solteiro(a)</option>
              <option value="Casado(a)">Casado(a)</option>
              <option value="Divorciado(a)">Divorciado(a)</option>
              <option value="Viúvo(a)">Viúvo(a)</option>
              <option value="União estável">União estável</option>
            </select>
          </div>
          <div className="form-field"><label>Nacionalidade</label><input value={form.nationality} onChange={(e) => setForm({ ...form, nationality: e.target.value })} /></div>
          <div className="form-field"><label>Profissão</label><input value={form.occupation} onChange={(e) => setForm({ ...form, occupation: e.target.value })} /></div>
          <div className="form-field">
            <label>Tipo sanguíneo</label>
            <select value={form.bloodType} onChange={(e) => setForm({ ...form, bloodType: e.target.value })}>
              <option value="">Não informado</option>
              {['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'].map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </div>
          <div className="form-field"><label>Nome da mãe</label><input value={form.motherName} onChange={(e) => setForm({ ...form, motherName: e.target.value })} /></div>

          <div className="form-field full"><div className="form-section-title">Contato</div></div>
          <div className="form-field"><label>E-mail</label><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></div>
          <div className="form-field"><label>Telefone fixo</label><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
          <div className="form-field"><label>Celular</label><input value={form.mobilePhone} onChange={(e) => setForm({ ...form, mobilePhone: e.target.value })} /></div>

          <div className="form-field full"><div className="form-section-title">Endereço</div></div>
          <AddressFields values={form} onChange={(patch) => setForm({ ...form, ...patch })} prefix="patient-" />

          <div className="form-field full"><div className="form-section-title">Convênios / plano de saúde</div></div>
          <div className="form-field full">
            <PatientInsuranceFields
              value={form.insurances ?? []}
              onChange={(insurances) => setForm({ ...form, insurances })}
            />
          </div>

          <div className="form-field full"><div className="form-section-title">Contato de emergência</div></div>
          <div className="form-field"><label>Nome do responsável</label><input value={form.emergencyContactName} onChange={(e) => setForm({ ...form, emergencyContactName: e.target.value })} /></div>
          <div className="form-field"><label>Telefone</label><input value={form.emergencyContactPhone} onChange={(e) => setForm({ ...form, emergencyContactPhone: e.target.value })} /></div>
          <div className="form-field">
            <label>Parentesco</label>
            <select value={form.emergencyContactRelationship} onChange={(e) => setForm({ ...form, emergencyContactRelationship: e.target.value })}>
              <option value="">Selecione</option>
              {RESPONSIBLE_RELATIONSHIPS.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </div>

          <div className="form-field full"><label>Observações</label><textarea rows={3} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></div>

          {!editingId && hasPermission('pep.read') && (
            <>
              <div className="form-field full">
                <div className="form-section-title">Internação (opcional)</div>
                <label className="pep-checkbox-label">
                  <input
                    type="checkbox"
                    checked={admitOnCreate}
                    onChange={(e) => {
                      setAdmitOnCreate(e.target.checked);
                      if (!e.target.checked) setAdmitForm(emptyAdmit);
                    }}
                  />
                  Internar paciente ao concluir o cadastro (reservar leito)
                </label>
                <p className="form-hint">
                  Cobertura detectada: <strong>{wardModalityLabels[patientModality]}</strong>
                  {form.insurances?.length === 0 && ' — cadastre o convênio acima para filtrar alas compatíveis.'}
                </p>
              </div>
              {admitOnCreate && (
                <>
                  <div className="form-field full">
                    <AvailableBedsPicker
                      beds={beds}
                      value={admitForm.bedId}
                      onChange={(bedId) => setAdmitForm({ ...admitForm, bedId })}
                      patientModality={patientModality}
                      planName={form.insurances?.find((i) => i.isPrimary)?.planName
                        ?? form.insurances?.[0]?.planName}
                    />
                  </div>
                  <div className="form-field">
                    <label>Médico responsável *</label>
                    <select
                      required
                      value={admitForm.professionalId}
                      onChange={(e) => setAdmitForm({ ...admitForm, professionalId: e.target.value })}
                    >
                      <option value="">Selecione</option>
                      {professionals.map((p) => (
                        <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>
                      ))}
                    </select>
                  </div>
                  <AdmissionTextField
                    id="patient-admit-reason"
                    label="Motivo da internação"
                    required
                    value={admitForm.reason}
                    onChange={(reason) => setAdmitForm({ ...admitForm, reason })}
                    snippets={reasonSnippets}
                    placeholder="Ex: Observação pós-cirúrgica"
                  />
                  <AdmissionTextField
                    id="patient-admit-diagnosis"
                    label="Diagnóstico / CID"
                    value={admitForm.diagnosis}
                    onChange={(diagnosis) => setAdmitForm({ ...admitForm, diagnosis })}
                    snippets={diagnosisSnippets}
                    placeholder="Ex: Pneumonia bacteriana"
                  />
                </>
              )}
            </>
          )}

          {editingId && (
            <div className="form-field">
              <label>
                <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} style={{ marginRight: 8 }} />
                Cadastro ativo
              </label>
            </div>
          )}

          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">{editingId ? 'Salvar alterações' : 'Cadastrar paciente'}</button>
          </div>
        </form>
      </Modal>

      <PatientResponsibleModal
        open={showResponsibleModal}
        onClose={() => setShowResponsibleModal(false)}
        patients={patients.map((p) => ({
          id: p.id,
          fullName: p.fullName,
          birthDate: p.birthDate,
          cpf: p.cpf,
        }))}
        initialPatientId={responsiblePatientId}
        onSaved={() => {
          setSuccess('Responsável salvo com sucesso.');
          loadPatients(search).catch(console.error);
        }}
      />
    </>
  );
}
