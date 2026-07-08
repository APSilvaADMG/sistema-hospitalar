import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  api,
  bedStatusLabel,
  bedStatusLabels,
  bedStatusValue,
  hospitalizationRequestPriorityLabel,
  hospitalizationRequestPriorityLabels,
  hospitalizationRequestStatusLabel,
  hospitalizationRequestStatusLabels,
  hospitalizationRequestStatusValue,
  hospitalizationStatusLabel,
  isBedOccupied,
  isBedReserved,
  isHospitalizationActive,
  isSusHospitalization,
  susCharacterValue,
  susHospitalizationCharacterLabels,
  susHospitalizationModalityLabels,
  susModalityValue,
  resolvePatientModality,
  wardCategoryLabels,
  wardCategoryValue,
  wardModalityLabels,
  wardModalityValue,
  type ApiEnum,
  type BedDto,
  type BedTransferDto,
  type CreateBedRequest,
  type CreateHospitalizationRequestRequest,
  type CreateWardRequest,
  type HospitalizationDto,
  type HospitalizationRequestDto,
  type SigtapProcedureDto,
  type UpdateHospitalizationSusDataRequest,
  type WardDto,
  type PatientDetailDto,
  type PatientDto,
  type ProfessionalDto,
  transportLocationLabels,
  transportPriorityLabels,
  type CreateTransportRequestRequest,
  type HealthInsuranceDto,
} from '../api/client';
import { ClinicalGuideCaptureModal } from '../components/funi/ClinicalGuideCaptureModal';
import { PatientWorkspaceShell } from '../components/patient-workspace/PatientWorkspaceShell';
import { AvailableBedsPicker } from '../components/AvailableBedsPicker';
import { AdmissionTextField } from '../components/AdmissionTextField';
import { Cid10Picker } from '../components/Cid10Picker';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { hospitalizationTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrDate, formatBrDateTime } from '../utils/dateUtils';
import { printHospitalizationSummary } from '../utils/printTemplates';
import {
  applyTriageAdmissionSuggestion,
  fetchTriageAdmissionSuggestion,
  triageAdmissionHint,
} from '../utils/triageAdmissionUtils';
import { useAuth } from '../auth/AuthContext';

const emptyForm = { patientId: '', bedId: '', professionalId: '', reason: '', diagnosis: '', aiTriageLogId: '' };
const emptyWardForm = { name: '', code: '', floor: '', description: '', coverageModality: '3', category: '1' };
const emptyBedForm = { wardId: '', bedNumber: '' };
const emptyBlockForm = { reason: '', blockedUntil: '' };
const emptyReserveForm = { patientId: '', reason: '', until: '' };
const emptySusForm = {
  aihNumber: '',
  susCompetence: '',
  primaryCid10Code: '',
  secondaryCid10Code: '',
  primarySigtapProcedureCode: '',
  secondarySigtapProcedureCode: '',
  susCharacter: '1',
  susModality: '1',
  cnesCode: '2277185',
  susAuthorizationNumber: '',
};
const emptyRequestForm = {
  patientId: '',
  requestingProfessionalId: '',
  reason: '',
  diagnosis: '',
  cid10Code: '',
  notes: '',
  preferredWardId: '',
  preferredWardCategory: '',
  priority: '1',
  aiTriageLogId: '',
};

const emptyTransportForm = {
  destinationType: 'ImagingTomography',
  destinationDetail: '',
  priority: 'Normal',
  notes: '',
};

function initials(name: string) {
  return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('');
}

function ModalityBadge({ modality }: { modality: ApiEnum<1 | 2 | 3 | 4> }) {
  const value = wardModalityValue(modality);
  return <span className={`ward-badge ward-modality-${value}`}>{wardModalityLabels[value] ?? '—'}</span>;
}

export function HospitalizationPage() {
  const { user, hasPermission } = useAuth();
  const { section } = useModuleSection('/internacao');
  const canManage = hasPermission('hospitalization.manage');
  const canManageBeds = hasPermission('patients.create', 'reports.read');
  const canReviewRequests = hasPermission('patients.create', 'reports.read');
  const manageLeitos = canManageBeds && section === 'leitos';
  const showAdmissionWorkflow = section === 'admissao';

  const [wards, setWards] = useState<WardDto[]>([]);
  const [beds, setBeds] = useState<BedDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [selectedWard, setSelectedWard] = useState('');
  const [wardFilter, setWardFilter] = useState('');
  const [modalityFilter, setModalityFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [search, setSearch] = useState('');
  const [bedStatusFilter, setBedStatusFilter] = useState('');
  const [form, setForm] = useState(emptyForm);
  const [selectedPatient, setSelectedPatient] = useState<PatientDetailDto | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [searchParams, setSearchParams] = useSearchParams();
  const [showModal, setShowModal] = useState(false);
  const [admitBeds, setAdmitBeds] = useState<BedDto[]>([]);
  const [loadingAdmitBeds, setLoadingAdmitBeds] = useState(false);
  const [triageHint, setTriageHint] = useState('');
  const [transfers, setTransfers] = useState<BedTransferDto[]>([]);
  const [showTransferModal, setShowTransferModal] = useState(false);
  const [transferTarget, setTransferTarget] = useState<HospitalizationDto | null>(null);
  const [transferBeds, setTransferBeds] = useState<BedDto[]>([]);
  const [transferForm, setTransferForm] = useState({ targetBedId: '', professionalId: '', reason: '' });
  const [showWardModal, setShowWardModal] = useState(false);
  const [editingWard, setEditingWard] = useState<WardDto | null>(null);
  const [wardForm, setWardForm] = useState(emptyWardForm);
  const [showBedModal, setShowBedModal] = useState(false);
  const [editingBed, setEditingBed] = useState<BedDto | null>(null);
  const [bedForm, setBedForm] = useState(emptyBedForm);
  const [showBlockModal, setShowBlockModal] = useState(false);
  const [blockTarget, setBlockTarget] = useState<BedDto | null>(null);
  const [blockForm, setBlockForm] = useState(emptyBlockForm);
  const [showReserveModal, setShowReserveModal] = useState(false);
  const [reserveTarget, setReserveTarget] = useState<BedDto | null>(null);
  const [reserveForm, setReserveForm] = useState(emptyReserveForm);
  const [requests, setRequests] = useState<HospitalizationRequestDto[]>([]);
  const [requestStatusFilter, setRequestStatusFilter] = useState('');
  const [showRequestModal, setShowRequestModal] = useState(false);
  const [requestForm, setRequestForm] = useState(emptyRequestForm);
  const [showReviewModal, setShowReviewModal] = useState(false);
  const [reviewingRequest, setReviewingRequest] = useState<HospitalizationRequestDto | null>(null);
  const [reviewForm, setReviewForm] = useState({ reviewedByProfessionalId: '', reviewNotes: '', approve: true });
  const [linkedRequestId, setLinkedRequestId] = useState<string | null>(null);
  const [showSusModal, setShowSusModal] = useState(false);
  const [susTarget, setSusTarget] = useState<HospitalizationDto | null>(null);
  const [susForm, setSusForm] = useState(emptySusForm);
  const [sigtapSearch, setSigtapSearch] = useState('');
  const [sigtapResults, setSigtapResults] = useState<SigtapProcedureDto[]>([]);
  const [reasonSnippets, setReasonSnippets] = useState<string[]>([]);
  const [diagnosisSnippets, setDiagnosisSnippets] = useState<string[]>([]);
  const [showTransportModal, setShowTransportModal] = useState(false);
  const [transportTarget, setTransportTarget] = useState<HospitalizationDto | null>(null);
  const [transportForm, setTransportForm] = useState(emptyTransportForm);
  const [showDeathModal, setShowDeathModal] = useState(false);
  const [deathTarget, setDeathTarget] = useState<HospitalizationDto | null>(null);
  const [deathForm, setDeathForm] = useState({ notes: '', primaryCid10Code: '' });
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [clinicalCapture, setClinicalCapture] = useState<{
    patientId: string;
    guideType: number;
    hospitalizationId?: string;
    label: string;
  } | null>(null);

  async function refreshSnippets() {
    const [reasons, diagnoses] = await Promise.all([
      api.getHospitalizationSnippets('Reason'),
      api.getHospitalizationSnippets('Diagnosis'),
    ]);
    setReasonSnippets(reasons.map((s) => s.text));
    setDiagnosisSnippets(diagnoses.map((s) => s.text));
  }

  async function load() {
    const modality = modalityFilter ? Number(modalityFilter) : undefined;
    const category = categoryFilter ? Number(categoryFilter) : undefined;
    const [wardList, bedList, hospList, patientList, profList, insuranceList] = await Promise.all([
      api.getWards(modality, category),
      api.getBeds({ wardId: selectedWard || undefined, modality, category }),
      api.getHospitalizations(
        undefined,
        section === 'obitos' ? 'deceased' : section === 'altas' ? 'discharged' : 'active',
      ),
      api.getPatients(undefined, 1),
      api.getProfessionals(),
      api.getHealthInsurances(),
    ]);
    setWards(wardList);
    setBeds(bedList);
    setHospitalizations(hospList);
    setPatients(patientList.items);
    setProfessionals(profList);
    setInsurances(Array.isArray(insuranceList) ? insuranceList : []);
    if (section === 'transferencias') {
      api.getBedTransfers(50).then(setTransfers).catch(console.error);
    }
    if (section === 'admissao') {
      const status = requestStatusFilter ? Number(requestStatusFilter) : undefined;
      api.getHospitalizationRequests({ status }).then(setRequests).catch(console.error);
    }
  }

  useEffect(() => {
    load().catch(console.error);
  }, [selectedWard, modalityFilter, categoryFilter, section, requestStatusFilter]);

  useEffect(() => {
    if (!form.patientId) {
      setSelectedPatient(null);
      return;
    }
    api.getPatient(form.patientId).then(setSelectedPatient).catch(console.error);
  }, [form.patientId]);

  const patientModality = useMemo(() => {
    const primary = selectedPatient?.insurances?.find((i) => i.isPrimary)
      ?? selectedPatient?.insurances?.[0];
    return resolvePatientModality(primary?.healthInsuranceName);
  }, [selectedPatient]);

  const stats = useMemo(() => {
    const totalBeds = wards.reduce((s, w) => s + w.totalBeds, 0);
    const availableBeds = wards.reduce((s, w) => s + w.availableBeds, 0);
    const byModality = (mod: number) => wards.filter((w) => wardModalityValue(w.coverageModality) === mod);
    return {
      active: hospitalizations.filter((h) => isHospitalizationActive(h.status)).length,
      availableBeds,
      occupiedBeds: totalBeds - availableBeds,
      wards: wards.length,
      susWards: byModality(3).length,
      convenioWards: byModality(2).length,
      particularWards: byModality(1).length,
      blockedBeds: wards.reduce((s, w) => s + w.blockedBeds, 0),
    };
  }, [wards, hospitalizations]);

  const filteredHospitalizations = useMemo(() => {
    return hospitalizations
      .filter((h) => {
        if (section === 'altas') return !isHospitalizationActive(h.status);
        if (section === 'obitos') return h.patientIsDeceased;
        if (section === 'leitos' || section === 'transferencias') return isHospitalizationActive(h.status);
        return true;
      })
      .filter((h) => !wardFilter || h.wardName === wards.find((w) => w.id === wardFilter)?.name)
      .filter((h) => !modalityFilter || wardModalityValue(h.wardCoverageModality) === Number(modalityFilter) || wardModalityValue(h.wardCoverageModality) === 4)
      .filter((h) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          h.patientName.toLowerCase().includes(term)
          || h.wardName.toLowerCase().includes(term)
          || h.bedNumber.toLowerCase().includes(term)
          || h.professionalName.toLowerCase().includes(term)
          || h.reason.toLowerCase().includes(term)
        );
      });
  }, [hospitalizations, wardFilter, modalityFilter, search, wards, section]);

  const filteredBeds = useMemo(() => {
    return beds.filter((b) => !bedStatusFilter || bedStatusValue(b.status) === Number(bedStatusFilter));
  }, [beds, bedStatusFilter]);

  const occupancyByBedId = useMemo(() => {
    const map = new Map<string, HospitalizationDto>();
    for (const h of hospitalizations) {
      if (isHospitalizationActive(h.status)) {
        map.set(h.bedId.toLowerCase(), h);
      }
    }
    return map;
  }, [hospitalizations]);

  function resolveOccupant(bed: BedDto) {
    if (bed.occupantPatientId && bed.occupantPatientName) {
      return {
        patientId: bed.occupantPatientId,
        patientName: bed.occupantPatientName,
        professionalName: bed.occupantProfessionalName ?? '—',
        admittedAt: bed.occupantAdmittedAt ?? new Date().toISOString(),
      };
    }
    const fromList = occupancyByBedId.get(bed.id.toLowerCase());
    if (!fromList) return null;
    return {
      patientId: fromList.patientId,
      patientName: fromList.patientName,
      professionalName: fromList.professionalName,
      admittedAt: fromList.admittedAt,
    };
  }

  useEffect(() => {
    if (!showModal && !showRequestModal) return;
    refreshSnippets().catch(console.error);
  }, [showModal, showRequestModal]);

  useEffect(() => {
    if (!showModal || !form.patientId || !selectedPatient) {
      setAdmitBeds([]);
      return;
    }

    setForm((f) => (f.bedId ? { ...f, bedId: '' } : f));
    setLoadingAdmitBeds(true);
    api.getAvailableBedsForPatient(form.patientId)
      .then(setAdmitBeds)
      .catch(console.error)
      .finally(() => setLoadingAdmitBeds(false));
  }, [showModal, form.patientId, selectedPatient]);

  async function loadTriageSuggestion(patientId: string, currentForm: typeof emptyForm) {
    if (!patientId) {
      setTriageHint('');
      return currentForm;
    }

    try {
      const suggestion = await fetchTriageAdmissionSuggestion(patientId);
      if (!suggestion) {
        setTriageHint('');
        return { ...currentForm, aiTriageLogId: '' };
      }

      setTriageHint(triageAdmissionHint(suggestion));
      return applyTriageAdmissionSuggestion(currentForm, suggestion);
    } catch {
      setTriageHint('');
      return currentForm;
    }
  }

  function openAdmitModal() {
    setLinkedRequestId(null);
    setForm(emptyForm);
    setSelectedPatient(null);
    setAdmitBeds([]);
    setTriageHint('');
    setShowModal(true);
  }

  function openAdmitFromRequest(req: HospitalizationRequestDto) {
    setLinkedRequestId(req.id);
    setForm({
      patientId: req.patientId,
      bedId: '',
      professionalId: req.requestingProfessionalId,
      reason: req.reason,
      diagnosis: req.diagnosis ?? '',
      aiTriageLogId: '',
    });
    setTriageHint('');
    setShowModal(true);
  }

  function openRequestModal() {
    setRequestForm({
      ...emptyRequestForm,
      requestingProfessionalId: user?.professionalId ?? professionals[0]?.id ?? '',
    });
    setShowRequestModal(true);
  }

  async function handleRequestSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    const payload: CreateHospitalizationRequestRequest = {
      patientId: requestForm.patientId,
      requestingProfessionalId: requestForm.requestingProfessionalId,
      reason: requestForm.reason,
      diagnosis: requestForm.diagnosis || undefined,
      cid10Code: requestForm.cid10Code || undefined,
      notes: requestForm.notes || undefined,
      preferredWardId: requestForm.preferredWardId || undefined,
      preferredWardCategory: requestForm.preferredWardCategory ? Number(requestForm.preferredWardCategory) : undefined,
      priority: Number(requestForm.priority),
      aiTriageLogId: requestForm.aiTriageLogId || undefined,
    };
    try {
      await api.createHospitalizationRequest(payload);
      setSuccess('Solicitação de internação registrada.');
      setShowRequestModal(false);
      await Promise.all([load(), refreshSnippets()]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao solicitar internação.');
    }
  }

  function openReviewModal(req: HospitalizationRequestDto, approve: boolean) {
    setReviewingRequest(req);
    setReviewForm({
      reviewedByProfessionalId: user?.professionalId ?? professionals[0]?.id ?? '',
      reviewNotes: '',
      approve,
    });
    setShowReviewModal(true);
  }

  async function handleReviewSubmit(event: FormEvent) {
    event.preventDefault();
    if (!reviewingRequest) return;
    setError('');
    setSuccess('');
    try {
      await api.reviewHospitalizationRequest(reviewingRequest.id, {
        approve: reviewForm.approve,
        reviewedByProfessionalId: reviewForm.reviewedByProfessionalId,
        reviewNotes: reviewForm.reviewNotes || undefined,
      });
      setSuccess(reviewForm.approve ? 'Solicitação aprovada.' : 'Solicitação rejeitada.');
      setShowReviewModal(false);
      setReviewingRequest(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao analisar solicitação.');
    }
  }

  async function handleCancelRequest(id: string) {
    if (!window.confirm('Cancelar esta solicitação de internação?')) return;
    setError('');
    try {
      await api.cancelHospitalizationRequest(id);
      setSuccess('Solicitação cancelada.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao cancelar solicitação.');
    }
  }

  async function handleAdmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      if (linkedRequestId) {
        await api.admitFromHospitalizationRequest(linkedRequestId, {
          bedId: form.bedId,
          professionalId: form.professionalId,
        });
      } else {
        await api.admitPatient({
          ...form,
          aiTriageLogId: form.aiTriageLogId || undefined,
        });
      }
      setSuccess('Paciente internado com sucesso.');
      setForm(emptyForm);
      setLinkedRequestId(null);
      setSelectedPatient(null);
      setTriageHint('');
      setShowModal(false);
      await Promise.all([load(), refreshSnippets()]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao internar paciente.');
    }
  }

  async function handleDischarge(id: string) {
    setError('');
    setSuccess('');
    try {
      await api.dischargePatient(id);
      setSuccess('Alta registrada. Leito liberado.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao dar alta.');
    }
  }

  function openDeathModal(h: HospitalizationDto) {
    setDeathTarget(h);
    setDeathForm({
      notes: '',
      primaryCid10Code: h.susData?.primaryCid10Code ?? h.diagnosis ?? '',
    });
    setShowDeathModal(true);
  }

  async function handleRegisterDeath(e: FormEvent) {
    e.preventDefault();
    if (!deathTarget) return;
    setError('');
    setSuccess('');
    const confirmed = window.confirm(
      `Confirmar registro de óbito para ${deathTarget.patientName}? Esta ação bloqueia novos atendimentos (RN-047).`,
    );
    if (!confirmed) return;
    try {
      await api.registerPatientDeath(deathTarget.id, {
        notes: deathForm.notes.trim() || undefined,
        primaryCid10Code: deathForm.primaryCid10Code.trim() || undefined,
      });
      setSuccess('Óbito registrado. Paciente bloqueado para novos atendimentos.');
      setShowDeathModal(false);
      setDeathTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar óbito.');
    }
  }

  async function openTransferModal(h: HospitalizationDto) {
    setTransferTarget(h);
    setTransferForm({ targetBedId: '', professionalId: h.professionalId, reason: '' });
    setShowTransferModal(true);
    setLoadingAdmitBeds(true);
    try {
      const available = await api.getAvailableBedsForPatient(h.patientId);
      setTransferBeds(available.filter((b) => b.id !== h.bedId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar leitos.');
    } finally {
      setLoadingAdmitBeds(false);
    }
  }

  function openTransportModal(h: HospitalizationDto) {
    setTransportTarget(h);
    setTransportForm(emptyTransportForm);
    setShowTransportModal(true);
  }

  async function handleTransport(e: FormEvent) {
    e.preventDefault();
    if (!transportTarget) return;
    setError('');
    try {
      const payload: CreateTransportRequestRequest = {
        patientId: transportTarget.patientId,
        hospitalizationId: transportTarget.id,
        patientName: transportTarget.patientName,
        originType: 'Hospitalization',
        originDetail: `${transportTarget.wardName} — Leito ${transportTarget.bedNumber}`,
        destinationType: transportForm.destinationType,
        destinationDetail: transportForm.destinationDetail || undefined,
        priority: transportForm.priority,
        notes: transportForm.notes || undefined,
      };
      await api.createTransportRequest(payload);
      setShowTransportModal(false);
      setTransportTarget(null);
      setSuccess('Transporte solicitado. Acompanhe em Operacional → Central de Transportes.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao solicitar transporte.');
    }
  }

  function openWardModal(ward?: WardDto) {
    if (ward) {
      setEditingWard(ward);
      setWardForm({
        name: ward.name,
        code: ward.code ?? '',
        floor: ward.floor ?? '',
        description: ward.description ?? '',
        coverageModality: String(wardModalityValue(ward.coverageModality)),
        category: String(wardCategoryValue(ward.category)),
      });
    } else {
      setEditingWard(null);
      setWardForm(emptyWardForm);
    }
    setShowWardModal(true);
  }

  async function handleWardSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    const payload: CreateWardRequest = {
      name: wardForm.name,
      code: wardForm.code || undefined,
      floor: wardForm.floor || undefined,
      description: wardForm.description || undefined,
      coverageModality: Number(wardForm.coverageModality),
      category: Number(wardForm.category),
    };
    try {
      if (editingWard) {
        await api.updateWard(editingWard.id, payload);
        setSuccess('Ala atualizada.');
      } else {
        await api.createWard(payload);
        setSuccess('Ala cadastrada.');
      }
      setShowWardModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar ala.');
    }
  }

  async function handleDeactivateWard(ward: WardDto) {
    if (!window.confirm(`Desativar a ala "${ward.name}" e todos os seus leitos?`)) return;
    setError('');
    try {
      await api.deactivateWard(ward.id);
      setSuccess('Ala desativada.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao desativar ala.');
    }
  }

  function openBedModal(bed?: BedDto) {
    if (bed) {
      setEditingBed(bed);
      setBedForm({ wardId: bed.wardId, bedNumber: bed.bedNumber });
    } else {
      setEditingBed(null);
      setBedForm({ wardId: selectedWard || wards[0]?.id || '', bedNumber: '' });
    }
    setShowBedModal(true);
  }

  useEffect(() => {
    const clearParam = (key: string) => {
      const next = new URLSearchParams(searchParams);
      next.delete(key);
      setSearchParams(next, { replace: true });
    };

    const leitoId = searchParams.get('leito');
    if (leitoId && beds.length > 0) {
      const bed = beds.find((b) => b.id === leitoId);
      if (bed) openBedModal(bed);
      clearParam('leito');
      return;
    }

    const alaId = searchParams.get('ala');
    if (alaId && wards.length > 0) {
      const ward = wards.find((w) => w.id === alaId);
      if (ward) openWardModal(ward);
      clearParam('ala');
      return;
    }

    const internacaoId = searchParams.get('internacao');
    if (internacaoId && hospitalizations.length > 0) {
      const item = hospitalizations.find((h) => h.id === internacaoId);
      if (item) setSearch(item.patientName);
      clearParam('internacao');
      return;
    }

    const novo = searchParams.get('novo');
    if (novo === 'leito' && wards.length > 0) {
      openBedModal();
      clearParam('novo');
      return;
    }
    if (novo === 'ala') {
      openWardModal();
      clearParam('novo');
      return;
    }
    if (novo === '1') {
      openAdmitModal();
      clearParam('novo');
    }
  }, [beds, wards, hospitalizations, searchParams, setSearchParams]);

  async function handleBedSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      if (editingBed) {
        await api.updateBed(editingBed.id, { bedNumber: bedForm.bedNumber });
        setSuccess('Leito atualizado.');
      } else {
        const payload: CreateBedRequest = { wardId: bedForm.wardId, bedNumber: bedForm.bedNumber };
        await api.createBed(payload);
        setSuccess('Leito cadastrado.');
      }
      setShowBedModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar leito.');
    }
  }

  function openBlockModal(bed: BedDto) {
    setBlockTarget(bed);
    setBlockForm({
      reason: bed.statusReason ?? '',
      blockedUntil: bed.blockedUntil ? bed.blockedUntil.slice(0, 10) : '',
    });
    setShowBlockModal(true);
  }

  async function handleBlockSubmit(event: FormEvent) {
    event.preventDefault();
    if (!blockTarget) return;
    setError('');
    setSuccess('');
    const statusValue = bedStatusValue(blockTarget.status);
    const releasing = statusValue === 3 || statusValue === 5;
    try {
      if (releasing) {
        await api.releaseBed(blockTarget.id, { reason: blockForm.reason || undefined });
        setSuccess(statusValue === 5 ? 'Reserva liberada.' : 'Leito liberado.');
      } else {
        if (!blockForm.reason.trim()) {
          setError('Informe o motivo do bloqueio.');
          return;
        }
        await api.blockBed(blockTarget.id, {
          reason: blockForm.reason,
          until: blockForm.blockedUntil ? new Date(blockForm.blockedUntil).toISOString() : undefined,
        });
        setSuccess('Leito bloqueado.');
      }
      setShowBlockModal(false);
      setBlockTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao alterar status do leito.');
    }
  }

  function openReserveModal(bed: BedDto) {
    setReserveTarget(bed);
    setReserveForm(emptyReserveForm);
    setShowReserveModal(true);
  }

  async function handleReserveSubmit(event: FormEvent) {
    event.preventDefault();
    if (!reserveTarget) return;
    setError('');
    setSuccess('');
    try {
      await api.reserveBed(reserveTarget.id, {
        patientId: reserveForm.patientId,
        reason: reserveForm.reason || undefined,
        until: reserveForm.until ? new Date(reserveForm.until).toISOString() : undefined,
      });
      setSuccess('Leito reservado para o paciente.');
      setShowReserveModal(false);
      setReserveTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao reservar leito.');
    }
  }

  async function handleDeactivateBed(bed: BedDto) {
    if (!window.confirm(`Desativar o leito ${bed.bedNumber} (${bed.wardName})?`)) return;
    setError('');
    try {
      await api.deactivateBed(bed.id);
      setSuccess('Leito desativado.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao desativar leito.');
    }
  }

  function openSusModal(h: HospitalizationDto) {
    const sus = h.susData;
    setSusTarget(h);
    setSusForm({
      aihNumber: sus?.aihNumber ?? '',
      susCompetence: sus?.susCompetence ?? new Date().toISOString().slice(0, 7).replace('-', ''),
      primaryCid10Code: sus?.primaryCid10Code ?? '',
      secondaryCid10Code: sus?.secondaryCid10Code ?? '',
      primarySigtapProcedureCode: sus?.primarySigtapProcedureCode ?? '',
      secondarySigtapProcedureCode: sus?.secondarySigtapProcedureCode ?? '',
      susCharacter: String(susCharacterValue(sus?.susCharacter) || 1),
      susModality: String(susModalityValue(sus?.susModality) || 1),
      cnesCode: sus?.cnesCode ?? '2277185',
      susAuthorizationNumber: sus?.susAuthorizationNumber ?? '',
    });
    setSigtapSearch('');
    setSigtapResults([]);
    setShowSusModal(true);
  }

  async function searchSigtap(term: string) {
    setSigtapSearch(term);
    if (term.trim().length < 2) {
      setSigtapResults([]);
      return;
    }
    try {
      const result = await api.getSigtapProcedures(term, 1, 50);
      setSigtapResults(result.items);
    } catch {
      setSigtapResults([]);
    }
  }

  async function handleSusSubmit(event: FormEvent) {
    event.preventDefault();
    if (!susTarget) return;
    setError('');
    setSuccess('');
    const payload: UpdateHospitalizationSusDataRequest = {
      aihNumber: susForm.aihNumber || undefined,
      susCompetence: susForm.susCompetence || undefined,
      primaryCid10Code: susForm.primaryCid10Code || undefined,
      secondaryCid10Code: susForm.secondaryCid10Code || undefined,
      primarySigtapProcedureCode: susForm.primarySigtapProcedureCode || undefined,
      secondarySigtapProcedureCode: susForm.secondarySigtapProcedureCode || undefined,
      susCharacter: Number(susForm.susCharacter),
      susModality: Number(susForm.susModality),
      cnesCode: susForm.cnesCode || undefined,
      susAuthorizationNumber: susForm.susAuthorizationNumber || undefined,
    };
    try {
      await api.updateHospitalizationSusData(susTarget.id, payload);
      setSuccess('Dados SUS/AIH atualizados.');
      setShowSusModal(false);
      setSusTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar dados SUS.');
    }
  }

  async function handleTransfer(event: FormEvent) {
    event.preventDefault();
    if (!transferTarget) return;
    setError('');
    setSuccess('');
    try {
      await api.transferBed(transferTarget.id, {
        targetBedId: transferForm.targetBedId,
        professionalId: transferForm.professionalId || undefined,
        reason: transferForm.reason || undefined,
      });
      setSuccess('Transferência de leito registrada.');
      setShowTransferModal(false);
      setTransferTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na transferência.');
    }
  }

  async function handlePrintSummary() {
    const rangeFrom = new Date();
    rangeFrom.setDate(rangeFrom.getDate() - 30);
    const dateFrom = rangeFrom.toISOString().slice(0, 10);
    const dateTo = new Date().toISOString().slice(0, 10);
    try {
      const [dash, list] = await Promise.all([
        api.getHospitalizationHubDashboard(dateFrom, dateTo),
        api.getHospitalizationHubList({ dateFrom, dateTo, take: 50 }),
      ]);
      printHospitalizationSummary(dash, { dateFrom, dateTo, items: list.items });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar relatório de internação.');
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Internação"
        title={showAdmissionWorkflow ? 'Admissão hospitalar' : 'Gestão de Leitos'}
        subtitle={showAdmissionWorkflow
          ? 'Solicitações pré-admissão, autorização e efetivação da internação com leito.'
          : 'Alas por modalidade (SUS, Convênio, Particular), mapa de ocupação e internações ativas.'}
      >
        <button type="button" className="btn btn-secondary" onClick={() => void handlePrintSummary()}>
          Imprimir resumo
        </button>
        {canManage && showAdmissionWorkflow && (
          <>
            <button className="btn btn-secondary" type="button" onClick={openRequestModal}>+ Solicitar internação</button>
            <button className="btn" type="button" onClick={openAdmitModal}>+ Admissão direta</button>
          </>
        )}
        {canManage && !showAdmissionWorkflow && (
          <button className="btn" type="button" onClick={openAdmitModal}>+ Nova internação</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/internacao" tabs={hospitalizationTabs} contextId="hospitalization" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <PatientWorkspaceShell moduleId="hospitalization" patients={patients} hidePickerWhenSelected>

      {showAdmissionWorkflow && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>Solicitações de internação — {requests.length}</span>
            <div className="filter-field w-lg no-margin">
              <select
                aria-label="Filtrar status da solicitação"
                value={requestStatusFilter}
                onChange={(e) => setRequestStatusFilter(e.target.value)}
              >
                <option value="">Todos os status</option>
                {Object.entries(hospitalizationRequestStatusLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
          </div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Solicitado em</th>
                  <th>Paciente</th>
                  <th>Prioridade</th>
                  <th>Médico</th>
                  <th>Ala preferencial</th>
                  <th>Motivo</th>
                  <th>Status</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {requests.map((r) => {
                  const statusValue = hospitalizationRequestStatusValue(r.status);
                  return (
                    <tr key={r.id}>
                      <td>{formatBrDateTime(r.requestedAt)}</td>
                      <td>{r.patientName}</td>
                      <td>{hospitalizationRequestPriorityLabel(r.priority)}</td>
                      <td>{r.requestingProfessionalName}</td>
                      <td>{r.preferredWardName ?? (r.preferredWardCategory ? wardCategoryLabels[wardCategoryValue(r.preferredWardCategory)] : '—')}</td>
                      <td>{r.reason}</td>
                      <td><span className="badge">{hospitalizationRequestStatusLabel(r.status)}</span></td>
                      <td>
                        <div className="table-actions">
                          {statusValue === 1 && canReviewRequests && (
                            <>
                              <button type="button" className="btn btn-secondary btn-sm" onClick={() => openReviewModal(r, true)}>Aprovar</button>
                              <button type="button" className="btn btn-secondary btn-sm" onClick={() => openReviewModal(r, false)}>Rejeitar</button>
                            </>
                          )}
                          {statusValue === 2 && canManage && (
                            <button type="button" className="btn btn-sm" onClick={() => openAdmitFromRequest(r)}>Efetivar admissão</button>
                          )}
                          {(statusValue === 1 || statusValue === 2) && canManage && (
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleCancelRequest(r.id)}>Cancelar</button>
                          )}
                          {statusValue === 4 && r.hospitalizationId && (
                            <Link to={`/pacientes/${r.patientId}/prontuario`} className="btn btn-secondary btn-sm">PEP</Link>
                          )}
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() => setClinicalCapture({
                              patientId: r.patientId,
                              guideType: 6,
                              label: `Solicitação internação — ${r.patientName}`,
                            })}
                          >
                            Dados TISS
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {requests.length === 0 && (
                  <tr>
                    <td colSpan={8} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                      Nenhuma solicitação encontrada.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="kpi-grid">
        <KpiCard label="Internações ativas" value={stats.active} variant="primary" />
        <KpiCard label="Leitos disponíveis" value={stats.availableBeds} variant="success" />
        <KpiCard label="Leitos ocupados" value={stats.occupiedBeds} variant="warning" />
        <KpiCard label="Alas SUS" value={stats.susWards} variant="info" />
        <KpiCard label="Alas Convênio" value={stats.convenioWards} variant="info" />
        <KpiCard label="Alas Particular" value={stats.particularWards} variant="info" />
        <KpiCard label="Leitos bloqueados" value={stats.blockedBeds} variant="warning" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Alas hospitalares — {wards.length} ala(s)</span>
          {manageLeitos && (
            <button type="button" className="btn btn-sm" onClick={() => openWardModal()}>+ Nova ala</button>
          )}
        </div>
        <div className="card-panel-body">
          <div className="ward-overview-grid">
            {wards.map((w) => (
              <div key={w.id} className="ward-overview-card">
                <div className="ward-overview-header">
                  <strong>{w.name}</strong>
                  <ModalityBadge modality={w.coverageModality} />
                </div>
                {w.code && <span className="ward-code">{w.code}</span>}
                <p className="ward-desc">{w.description ?? '—'}</p>
                <div className="ward-overview-meta">
                  <span>{wardCategoryLabels[wardCategoryValue(w.category)]}</span>
                  {w.floor && <span>{w.floor}º andar</span>}
                  <span>{w.availableBeds}/{w.totalBeds} livres</span>
                  {w.blockedBeds > 0 && <span>{w.blockedBeds} bloqueado(s)</span>}
                </div>
                {manageLeitos && (
                  <div className="table-actions" style={{ marginTop: 10 }}>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => openWardModal(w)}>Editar</button>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleDeactivateWard(w)}>Desativar</button>
                  </div>
                )}
              </div>
            ))}
            {wards.length === 0 && (
              <p className="bula-empty">Nenhuma ala encontrada com os filtros atuais.</p>
            )}
          </div>
        </div>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Mapa de leitos — {filteredBeds.length} leito(s)</span>
          {manageLeitos && (
            <button type="button" className="btn btn-sm" onClick={() => openBedModal()} disabled={wards.length === 0}>+ Novo leito</button>
          )}
        </div>
        <FilterBar>
          <div className="filter-field w-xs">
            <label htmlFor="modalityMap">Modalidade</label>
            <select id="modalityMap" value={modalityFilter} onChange={(e) => setModalityFilter(e.target.value)}>
              <option value="">Todas</option>
              {Object.entries(wardModalityLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-xs">
            <label htmlFor="categoryMap">Tipo</label>
            <select id="categoryMap" value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(wardCategoryLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="wardMap">Ala</label>
            <select id="wardMap" value={selectedWard} onChange={(e) => setSelectedWard(e.target.value)}>
              <option value="">Todas as alas</option>
              {wards.map((w) => (
                <option key={w.id} value={w.id}>{w.name} ({wardModalityLabels[wardModalityValue(w.coverageModality)]})</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-md">
            <label htmlFor="bedStatus">Status do leito</label>
            <select id="bedStatus" value={bedStatusFilter} onChange={(e) => setBedStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(bedStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
        </FilterBar>
        <div className="card-panel-body">
          {filteredBeds.length === 0 ? (
            <div className="appt-empty">
              <div className="appt-empty-icon">🛏️</div>
              <h3>Nenhum leito encontrado</h3>
              <p>Ajuste os filtros para visualizar o mapa de leitos.</p>
            </div>
          ) : (
            <>
              <div className="bed-map-legend">
                <span><i className="bed-legend-dot bed-legend-available" aria-hidden /> Disponível</span>
                <span><i className="bed-legend-dot bed-legend-occupied" aria-hidden /> Ocupado (com paciente)</span>
                <span><i className="bed-legend-dot bed-legend-maintenance" aria-hidden /> Manutenção / reservado</span>
              </div>
              <div className="bed-grid">
                {filteredBeds.map((bed) => {
                  const occupant = resolveOccupant(bed);
                  const isOccupied = isBedOccupied(bed.status) || !!occupant;
                  const isReserved = isBedReserved(bed.status);
                  const statusValue = isOccupied ? 2 : bedStatusValue(bed.status);

                  return (
                    <div
                      key={bed.id}
                      className={`bed-card bed-status-${statusValue}${isOccupied ? ' bed-card-occupied' : ''}`}
                      title={
                        isOccupied && occupant
                          ? `${occupant.patientName} — internado desde ${formatBrDateTime(occupant.admittedAt)}`
                          : bedStatusLabel(bed.status)
                      }
                    >
                      <div className="bed-card-badges">
                        <ModalityBadge modality={bed.wardCoverageModality} />
                        <span className="ward-badge ward-category">{wardCategoryLabels[wardCategoryValue(bed.wardCategory)]}</span>
                      </div>
                      <strong>{bed.wardName}</strong>
                      <span className="bed-card-number">
                        Leito {bed.wardCode ? `${bed.wardCode} · ` : ''}{bed.bedNumber}
                      </span>
                      <span className={`badge${isOccupied ? ' badge-danger' : statusValue === 1 ? ' badge-success' : ''}`}>
                        {isOccupied && occupant ? 'Ocupado' : bedStatusLabel(bed.status)}
                      </span>
                      {isOccupied && occupant ? (
                        <div className="bed-card-occupant">
                          <div className="bed-card-occupant-avatar">{initials(occupant.patientName)}</div>
                          <div className="bed-card-occupant-info">
                            <Link to={`/pacientes/${occupant.patientId}/prontuario`} className="bed-card-occupant-name">
                              {occupant.patientName}
                            </Link>
                            <span className="bed-card-occupant-meta">
                              desde {formatBrDate(occupant.admittedAt)}
                            </span>
                            <span className="bed-card-occupant-meta">{occupant.professionalName}</span>
                          </div>
                        </div>
                      ) : isOccupied ? (
                        <span className="bed-card-occupant-unknown">Paciente não identificado</span>
                      ) : null}
                      {(statusValue === 3 || isReserved) && bed.statusReason && (
                        <p className="bed-card-block-reason" title={bed.statusReason}>
                          {bed.statusReason}
                          {bed.blockedUntil && <> · até {formatBrDate(bed.blockedUntil)}</>}
                        </p>
                      )}
                      {manageLeitos && !isOccupied && (
                        <div className="table-actions bed-card-actions">
                          <button type="button" className="btn btn-secondary btn-sm" onClick={() => openBedModal(bed)}>Editar</button>
                          {statusValue === 1 && (
                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => openReserveModal(bed)}>
                              Reservar
                            </button>
                          )}
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() => openBlockModal(bed)}
                          >
                            {statusValue === 3 || isReserved ? 'Liberar' : 'Bloquear'}
                          </button>
                          <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleDeactivateBed(bed)}>Desativar</button>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </div>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">
          {section === 'obitos' && 'Registros de óbito'}
          {section === 'altas' && 'Altas hospitalares'}
          {section === 'transferencias' && 'Internações ativas (transferências)'}
          {!section && 'Internações ativas'}
          {' — '}{filteredHospitalizations.length} registro(s)
        </div>
        {section === 'obitos' && (
          <div className="card-panel-body" style={{ paddingBottom: 0 }}>
            <p className="form-hint" style={{ margin: '0 0 12px' }}>
              Use &quot;Registrar óbito&quot; em internações ativas. O paciente será bloqueado para novos atendimentos (RN-047).
            </p>
          </div>
        )}
        <FilterBar>
          <div className="filter-field w-xs">
            <label htmlFor="modalityHosp">Modalidade</label>
            <select id="modalityHosp" value={modalityFilter} onChange={(e) => setModalityFilter(e.target.value)}>
              <option value="">Todas</option>
              {Object.entries(wardModalityLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="wardHosp">Ala</label>
            <select id="wardHosp" value={wardFilter} onChange={(e) => setWardFilter(e.target.value)}>
              <option value="">Todas</option>
              {wards.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="hospSearch">Buscar</label>
            <input
              id="hospSearch"
              placeholder="Paciente, leito, médico ou motivo..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Ala / Leito</th>
                <th>Modalidade</th>
                <th>Médico</th>
                <th>Entrada</th>
                <th>Motivo</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filteredHospitalizations.map((h) => (
                <tr key={h.id}>
                  <td>
                    <div className="appt-card-patient">
                      <div className="appt-avatar">{initials(h.patientName)}</div>
                      <strong>{h.patientName}</strong>
                    </div>
                  </td>
                  <td>
                    {h.wardName} — {h.bedNumber}
                    <div className="table-sub">{wardCategoryLabels[wardCategoryValue(h.wardCategory)]}</div>
                  </td>
                  <td><ModalityBadge modality={h.wardCoverageModality} /></td>
                  <td>{h.professionalName}</td>
                  <td>{formatBrDateTime(h.admittedAt)}</td>
                  <td>
                    {h.reason}
                    {isSusHospitalization(h) && h.susData?.aihNumber && (
                      <div className="table-sub">AIH {h.susData.aihNumber}</div>
                    )}
                  </td>
                  <td><span className="badge">{hospitalizationStatusLabel(h.status)}</span></td>
                  <td>
                    <div className="table-actions">
                      <Link to={`/pacientes/${h.patientId}/prontuario`} className="btn btn-secondary btn-sm">PEP</Link>
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => setClinicalCapture({
                          patientId: h.patientId,
                          hospitalizationId: h.id,
                          guideType: 4,
                          label: `Internação — ${h.patientName}`,
                        })}
                      >
                        Dados TISS
                      </button>
                      {isHospitalizationActive(h.status) && isSusHospitalization(h) && canManage && (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => openSusModal(h)}>
                          SUS/AIH
                        </button>
                      )}
                      {isHospitalizationActive(h.status) && canManage && (
                        <>
                          {(section === 'transferencias' || section === 'leitos' || section === '') && (
                            <button className="btn btn-secondary btn-sm" type="button" onClick={() => openTransferModal(h)}>
                              Transferir
                            </button>
                          )}
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => openTransportModal(h)}>
                            Maqueiro
                          </button>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => handleDischarge(h.id)}>
                            Dar alta
                          </button>
                          <button
                            className="btn btn-danger btn-sm"
                            type="button"
                            onClick={() => openDeathModal(h)}
                            title="RN-047 — Registro de óbito"
                          >
                            Registrar óbito
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filteredHospitalizations.length === 0 && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    {section === 'obitos' ? 'Nenhum óbito registrado.' : section === 'altas' ? 'Nenhuma alta registrada.' : 'Nenhuma internação ativa.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {section === 'transferencias' && (
        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
          <div className="card-panel-header">Histórico de transferências — {transfers.length}</div>
          <table className="data-table">
            <thead>
              <tr><th>Paciente</th><th>Origem</th><th>Destino</th><th>Data</th><th>Motivo</th></tr>
            </thead>
            <tbody>
              {transfers.map((t) => (
                <tr key={t.id}>
                  <td>{t.patientName}</td>
                  <td>{t.fromWardName} — {t.fromBedNumber}</td>
                  <td>{t.toWardName} — {t.toBedNumber}</td>
                  <td>{formatBrDateTime(t.transferredAt)}</td>
                  <td>{t.reason ?? '—'}</td>
                </tr>
              ))}
              {transfers.length === 0 && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma transferência registrada.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Modal
        open={showWardModal}
        onClose={() => setShowWardModal(false)}
        title={editingWard ? 'Editar ala' : 'Nova ala'}
        subtitle="Cadastro de alas por modalidade e tipo de unidade."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleWardSubmit}>
          <div className="form-field">
            <label htmlFor="wardName">Nome *</label>
            <input id="wardName" required value={wardForm.name} onChange={(e) => setWardForm({ ...wardForm, name: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="wardCode">Código</label>
            <input id="wardCode" value={wardForm.code} onChange={(e) => setWardForm({ ...wardForm, code: e.target.value })} placeholder="Ex.: UTI-01" />
          </div>
          <div className="form-field">
            <label htmlFor="wardFloor">Andar</label>
            <input id="wardFloor" value={wardForm.floor} onChange={(e) => setWardForm({ ...wardForm, floor: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="wardModality">Modalidade *</label>
            <select id="wardModality" required value={wardForm.coverageModality} onChange={(e) => setWardForm({ ...wardForm, coverageModality: e.target.value })}>
              {Object.entries(wardModalityLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="wardCategory">Tipo *</label>
            <select id="wardCategory" required value={wardForm.category} onChange={(e) => setWardForm({ ...wardForm, category: e.target.value })}>
              {Object.entries(wardCategoryLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="wardDesc">Descrição</label>
            <textarea id="wardDesc" rows={2} value={wardForm.description} onChange={(e) => setWardForm({ ...wardForm, description: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowWardModal(false)}>Cancelar</button>
            <button type="submit" className="btn">{editingWard ? 'Salvar' : 'Cadastrar ala'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showBedModal}
        onClose={() => setShowBedModal(false)}
        title={editingBed ? 'Editar leito' : 'Novo leito'}
        subtitle="Número único dentro da ala selecionada."
      >
        <form className="form-grid" onSubmit={handleBedSubmit}>
          {!editingBed && (
            <div className="form-field full">
              <label htmlFor="bedWard">Ala *</label>
              <select id="bedWard" required value={bedForm.wardId} onChange={(e) => setBedForm({ ...bedForm, wardId: e.target.value })}>
                <option value="">Selecione</option>
                {wards.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
              </select>
            </div>
          )}
          <div className="form-field full">
            <label htmlFor="bedNumber">Número do leito *</label>
            <input id="bedNumber" required value={bedForm.bedNumber} onChange={(e) => setBedForm({ ...bedForm, bedNumber: e.target.value })} placeholder="Ex.: 101" />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowBedModal(false)}>Cancelar</button>
            <button type="submit" className="btn">{editingBed ? 'Salvar' : 'Cadastrar leito'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showBlockModal}
        onClose={() => { setShowBlockModal(false); setBlockTarget(null); }}
        title={
          blockTarget && (bedStatusValue(blockTarget.status) === 3 || isBedReserved(blockTarget.status))
            ? isBedReserved(blockTarget.status) ? 'Liberar reserva' : 'Liberar leito'
            : 'Bloquear leito'
        }
        subtitle={blockTarget ? `${blockTarget.wardName} — leito ${blockTarget.bedNumber}` : undefined}
      >
        {blockTarget && (bedStatusValue(blockTarget.status) === 3 || isBedReserved(blockTarget.status)) ? (
          <form className="form-grid" onSubmit={handleBlockSubmit}>
            <p className="form-hint">O leito voltará a ficar disponível para internação.</p>
            <div className="form-field full">
              <label htmlFor="releaseReason">Observação</label>
              <input id="releaseReason" value={blockForm.reason} onChange={(e) => setBlockForm({ ...blockForm, reason: e.target.value })} />
            </div>
            <div className="form-field full modal-actions">
              <button type="button" className="btn btn-secondary" onClick={() => setShowBlockModal(false)}>Cancelar</button>
              <button type="submit" className="btn">Confirmar liberação</button>
            </div>
          </form>
        ) : (
          <form className="form-grid" onSubmit={handleBlockSubmit}>
            <div className="form-field full">
              <label htmlFor="blockReason">Motivo *</label>
              <input id="blockReason" required value={blockForm.reason} onChange={(e) => setBlockForm({ ...blockForm, reason: e.target.value })} placeholder="Ex.: manutenção elétrica, isolamento temporário" />
            </div>
            <div className="form-field">
              <label htmlFor="blockUntil">Previsão de liberação</label>
              <input id="blockUntil" type="date" value={blockForm.blockedUntil} onChange={(e) => setBlockForm({ ...blockForm, blockedUntil: e.target.value })} />
            </div>
            <div className="form-field full modal-actions">
              <button type="button" className="btn btn-secondary" onClick={() => setShowBlockModal(false)}>Cancelar</button>
              <button type="submit" className="btn">Bloquear leito</button>
            </div>
          </form>
        )}
      </Modal>

      <Modal
        open={showReserveModal}
        onClose={() => { setShowReserveModal(false); setReserveTarget(null); }}
        title="Reservar leito"
        subtitle={reserveTarget ? `${reserveTarget.wardName} — leito ${reserveTarget.bedNumber}` : undefined}
      >
        <form className="form-grid" onSubmit={handleReserveSubmit}>
          <div className="form-field full">
            <label htmlFor="reservePatient">Paciente *</label>
            <select
              id="reservePatient"
              required
              value={reserveForm.patientId}
              onChange={(e) => setReserveForm({ ...reserveForm, patientId: e.target.value })}
            >
              <option value="">Selecione</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="reserveReason">Motivo</label>
            <input
              id="reserveReason"
              value={reserveForm.reason}
              onChange={(e) => setReserveForm({ ...reserveForm, reason: e.target.value })}
              placeholder="Ex.: admissão programada, transferência externa"
            />
          </div>
          <div className="form-field">
            <label htmlFor="reserveUntil">Reserva até</label>
            <input
              id="reserveUntil"
              type="date"
              value={reserveForm.until}
              onChange={(e) => setReserveForm({ ...reserveForm, until: e.target.value })}
            />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowReserveModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Confirmar reserva</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showTransferModal}
        onClose={() => setShowTransferModal(false)}
        title="Transferir paciente de leito"
        subtitle={transferTarget ? `${transferTarget.patientName} — ${transferTarget.wardName} / ${transferTarget.bedNumber}` : undefined}
        width="lg"
      >
        <form className="form-grid" onSubmit={handleTransfer}>
          <div className="form-field full">
            <AvailableBedsPicker
              beds={transferBeds}
              value={transferForm.targetBedId}
              onChange={(targetBedId) => setTransferForm({ ...transferForm, targetBedId })}
              patientModality={transferTarget ? wardModalityValue(transferTarget.wardCoverageModality) : 4}
              loading={loadingAdmitBeds}
              requirePatient
              hasPatient
              serverFiltered
            />
          </div>
          <div className="form-field">
            <label htmlFor="transferProf">Profissional responsável</label>
            <select id="transferProf" value={transferForm.professionalId} onChange={(e) => setTransferForm({ ...transferForm, professionalId: e.target.value })}>
              <option value="">Manter atual</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="transferReason">Motivo</label>
            <input id="transferReason" value={transferForm.reason} onChange={(e) => setTransferForm({ ...transferForm, reason: e.target.value })} placeholder="Ex.: necessidade clínica, isolamento" />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowTransferModal(false)}>Cancelar</button>
            <button className="btn" type="submit" disabled={!transferForm.targetBedId}>Confirmar transferência</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showRequestModal}
        onClose={() => setShowRequestModal(false)}
        title="Solicitar internação"
        subtitle="Pré-admissão para análise da recepção antes da alocação de leito."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleRequestSubmit}>
          <div className="form-field">
            <label htmlFor="reqPatientId">Paciente *</label>
            <select
              id="reqPatientId"
              required
              value={requestForm.patientId}
              onChange={(e) => setRequestForm({ ...requestForm, patientId: e.target.value })}
            >
              <option value="">Selecione</option>
              {patients.map((p) => (
                <option key={p.id} value={p.id}>{p.fullName}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="reqProfessionalId">Médico solicitante *</label>
            <select
              id="reqProfessionalId"
              required
              value={requestForm.requestingProfessionalId}
              onChange={(e) => setRequestForm({ ...requestForm, requestingProfessionalId: e.target.value })}
            >
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="reqPriority">Prioridade *</label>
            <select id="reqPriority" required value={requestForm.priority} onChange={(e) => setRequestForm({ ...requestForm, priority: e.target.value })}>
              {Object.entries(hospitalizationRequestPriorityLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="reqWard">Ala preferencial</label>
            <select id="reqWard" value={requestForm.preferredWardId} onChange={(e) => setRequestForm({ ...requestForm, preferredWardId: e.target.value })}>
              <option value="">Sem preferência</option>
              {wards.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="reqCategory">Tipo de unidade</label>
            <select id="reqCategory" value={requestForm.preferredWardCategory} onChange={(e) => setRequestForm({ ...requestForm, preferredWardCategory: e.target.value })}>
              <option value="">Sem preferência</option>
              {Object.entries(wardCategoryLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <AdmissionTextField
            id="reqReason"
            label="Motivo clínico"
            required
            value={requestForm.reason}
            onChange={(reason) => setRequestForm({ ...requestForm, reason })}
            snippets={reasonSnippets}
          />
          <AdmissionTextField
            id="reqDiagnosis"
            label="Diagnóstico"
            value={requestForm.diagnosis}
            onChange={(diagnosis) => setRequestForm({ ...requestForm, diagnosis })}
            snippets={diagnosisSnippets}
          />
          <div className="form-field full">
            <label>CID-10</label>
            <Cid10Picker
              value={requestForm.cid10Code}
              onChange={(code) => setRequestForm({ ...requestForm, cid10Code: code })}
            />
          </div>
          <div className="form-field full">
            <label htmlFor="reqNotes">Observações</label>
            <textarea id="reqNotes" rows={2} value={requestForm.notes} onChange={(e) => setRequestForm({ ...requestForm, notes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowRequestModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Enviar solicitação</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showReviewModal}
        onClose={() => { setShowReviewModal(false); setReviewingRequest(null); }}
        title={reviewForm.approve ? 'Aprovar solicitação' : 'Rejeitar solicitação'}
        subtitle={reviewingRequest ? reviewingRequest.patientName : undefined}
      >
        <form className="form-grid" onSubmit={handleReviewSubmit}>
          <div className="form-field full">
            <label htmlFor="reviewProf">Profissional responsável *</label>
            <select
              id="reviewProf"
              required
              value={reviewForm.reviewedByProfessionalId}
              onChange={(e) => setReviewForm({ ...reviewForm, reviewedByProfessionalId: e.target.value })}
            >
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="reviewNotes">Parecer</label>
            <textarea id="reviewNotes" rows={3} value={reviewForm.reviewNotes} onChange={(e) => setReviewForm({ ...reviewForm, reviewNotes: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowReviewModal(false)}>Cancelar</button>
            <button type="submit" className="btn">{reviewForm.approve ? 'Confirmar aprovação' : 'Confirmar rejeição'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showSusModal}
        onClose={() => { setShowSusModal(false); setSusTarget(null); }}
        title="Dados regulatórios SUS / AIH"
        subtitle={susTarget ? `${susTarget.patientName} — CNS ${susTarget.patientCns ?? 'não informado'}` : undefined}
        width="lg"
      >
        <form className="form-grid" onSubmit={handleSusSubmit}>
          <div className="form-field">
            <label htmlFor="susAih">Número AIH</label>
            <input id="susAih" value={susForm.aihNumber} onChange={(e) => setSusForm({ ...susForm, aihNumber: e.target.value })} placeholder="Gerado automaticamente se vazio" />
          </div>
          <div className="form-field">
            <label htmlFor="susComp">Competência (AAAAMM)</label>
            <input id="susComp" value={susForm.susCompetence} onChange={(e) => setSusForm({ ...susForm, susCompetence: e.target.value })} maxLength={6} />
          </div>
          <div className="form-field">
            <label htmlFor="susCnes">CNES</label>
            <input id="susCnes" value={susForm.cnesCode} onChange={(e) => setSusForm({ ...susForm, cnesCode: e.target.value })} maxLength={7} />
          </div>
          <div className="form-field">
            <label htmlFor="susAuth">Autorização AIH</label>
            <input id="susAuth" value={susForm.susAuthorizationNumber} onChange={(e) => setSusForm({ ...susForm, susAuthorizationNumber: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="susChar">Caráter da internação</label>
            <select id="susChar" value={susForm.susCharacter} onChange={(e) => setSusForm({ ...susForm, susCharacter: e.target.value })}>
              {Object.entries(susHospitalizationCharacterLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="susMod">Modalidade</label>
            <select id="susMod" value={susForm.susModality} onChange={(e) => setSusForm({ ...susForm, susModality: e.target.value })}>
              {Object.entries(susHospitalizationModalityLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field full">
            <label>CID-10 principal</label>
            <Cid10Picker
              value={susForm.primaryCid10Code}
              onChange={(code) => setSusForm({ ...susForm, primaryCid10Code: code })}
            />
          </div>
          <div className="form-field full">
            <label>CID-10 secundário</label>
            <Cid10Picker
              value={susForm.secondaryCid10Code}
              onChange={(code) => setSusForm({ ...susForm, secondaryCid10Code: code })}
            />
          </div>
          <div className="form-field">
            <label htmlFor="susProc1">Procedimento SIGTAP principal</label>
            <input id="susProc1" value={susForm.primarySigtapProcedureCode} onChange={(e) => setSusForm({ ...susForm, primarySigtapProcedureCode: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="susProc2">Procedimento SIGTAP secundário</label>
            <input id="susProc2" value={susForm.secondarySigtapProcedureCode} onChange={(e) => setSusForm({ ...susForm, secondarySigtapProcedureCode: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="sigtapSearch">Buscar no catálogo SIGTAP</label>
            <input id="sigtapSearch" value={sigtapSearch} onChange={(e) => void searchSigtap(e.target.value)} placeholder="Digite código ou descrição..." />
            {sigtapResults.length > 0 && (
              <ul className="sigtap-quick-list">
                {sigtapResults.slice(0, 6).map((p) => (
                  <li key={p.id}>
                    <button
                      type="button"
                      className="sigtap-quick-item"
                      onClick={() => setSusForm({ ...susForm, primarySigtapProcedureCode: p.code })}
                    >
                      <strong>{p.code}</strong> — {p.description}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowSusModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Salvar dados SUS</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showTransportModal}
        onClose={() => setShowTransportModal(false)}
        title="Solicitar transporte (maqueiro)"
        subtitle={transportTarget ? `${transportTarget.patientName} — ${transportTarget.wardName} / ${transportTarget.bedNumber}` : undefined}
        width="md"
      >
        <form className="form-grid" onSubmit={handleTransport}>
          <div className="form-field full">
            <div className="alert alert-info" style={{ margin: 0 }}>
              Origem: Internação — {transportTarget?.wardName} / Leito {transportTarget?.bedNumber}
            </div>
          </div>
          <div className="form-field">
            <label htmlFor="hospTrDest">Destino</label>
            <select
              id="hospTrDest"
              value={transportForm.destinationType}
              onChange={(e) => setTransportForm({ ...transportForm, destinationType: e.target.value })}
            >
              {Object.entries(transportLocationLabels).filter(([k]) => k !== 'Hospitalization').map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="hospTrPriority">Prioridade</label>
            <select
              id="hospTrPriority"
              value={transportForm.priority}
              onChange={(e) => setTransportForm({ ...transportForm, priority: e.target.value })}
            >
              {Object.entries(transportPriorityLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="form-field full">
            <label htmlFor="hospTrDestDetail">Detalhe do destino</label>
            <input
              id="hospTrDestDetail"
              placeholder="Ex: Tomografia — Subsolo"
              value={transportForm.destinationDetail}
              onChange={(e) => setTransportForm({ ...transportForm, destinationDetail: e.target.value })}
            />
          </div>
          <div className="form-field full">
            <label htmlFor="hospTrNotes">Observações</label>
            <textarea
              id="hospTrNotes"
              rows={2}
              value={transportForm.notes}
              onChange={(e) => setTransportForm({ ...transportForm, notes: e.target.value })}
            />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowTransportModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Enviar para fila</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showModal}
        onClose={() => { setShowModal(false); setLinkedRequestId(null); }}
        title={linkedRequestId ? 'Efetivar admissão' : 'Nova internação'}
        subtitle="O leito deve ser compatível com a cobertura do paciente (SUS, Convênio ou Particular)."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleAdmit}>
          <div className="form-field">
            <label htmlFor="patientId">Paciente *</label>
            <select
              id="patientId"
              required
              disabled={!!linkedRequestId}
              value={form.patientId}
              onChange={(e) => {
                const patientId = e.target.value;
                const nextForm = { ...form, patientId, bedId: '', aiTriageLogId: '' };
                setForm(nextForm);
                void loadTriageSuggestion(patientId, nextForm).then(setForm);
              }}
            >
              <option value="">Selecione</option>
              {patients.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName}{p.primaryInsuranceName ? ` (${p.primaryInsuranceName})` : ''}
                </option>
              ))}
            </select>
          </div>
          <div className="form-field full">
            <AvailableBedsPicker
              beds={admitBeds}
              value={form.bedId}
              onChange={(bedId) => setForm({ ...form, bedId })}
              patientModality={patientModality}
              planName={selectedPatient?.insurances?.find((i) => i.isPrimary)?.planName
                ?? selectedPatient?.insurances?.[0]?.planName}
              loading={loadingAdmitBeds}
              requirePatient
              hasPatient={!!selectedPatient}
              serverFiltered
            />
          </div>
          <div className="form-field">
            <label htmlFor="professionalId">Médico responsável *</label>
            <select id="professionalId" required value={form.professionalId} onChange={(e) => setForm({ ...form, professionalId: e.target.value })}>
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          {triageHint && (
            <div className="form-field full">
              <div className="alert alert-success" style={{ margin: 0 }}>{triageHint}</div>
            </div>
          )}
          {!linkedRequestId && (
            <>
              <AdmissionTextField
                id="reason"
                label="Motivo"
                required
                value={form.reason}
                onChange={(reason) => setForm({ ...form, reason })}
                snippets={reasonSnippets}
              />
              <AdmissionTextField
                id="diagnosis"
                label="Diagnóstico"
                value={form.diagnosis}
                onChange={(diagnosis) => setForm({ ...form, diagnosis })}
                snippets={diagnosisSnippets}
              />
            </>
          )}
          {linkedRequestId && (
            <div className="form-field full">
              <div className="alert alert-info" style={{ margin: 0 }}>
                Motivo: {form.reason}
                {form.diagnosis ? ` · Diagnóstico: ${form.diagnosis}` : ''}
              </div>
            </div>
          )}
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => { setShowModal(false); setLinkedRequestId(null); }}>Cancelar</button>
            <button className="btn" type="submit">{linkedRequestId ? 'Confirmar admissão' : 'Confirmar internação'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showDeathModal}
        onClose={() => setShowDeathModal(false)}
        title="Registrar óbito hospitalar"
        subtitle="RN-047 — O paciente será marcado como falecido e não poderá receber novos atendimentos."
        width="md"
      >
        {deathTarget && (
          <form className="form-grid" onSubmit={handleRegisterDeath}>
            <div className="form-field full">
              <div className="alert alert-warning" style={{ margin: 0 }}>
                Paciente: <strong>{deathTarget.patientName}</strong>
                <br />
                Leito: {deathTarget.wardName} — {deathTarget.bedNumber}
              </div>
            </div>
            <div className="form-field">
              <label htmlFor="deathCid">CID-10 principal</label>
              <input
                id="deathCid"
                value={deathForm.primaryCid10Code}
                onChange={(e) => setDeathForm({ ...deathForm, primaryCid10Code: e.target.value })}
                placeholder="Ex.: I21.9"
              />
            </div>
            <div className="form-field full">
              <label htmlFor="deathNotes">Observações</label>
              <textarea
                id="deathNotes"
                rows={3}
                value={deathForm.notes}
                onChange={(e) => setDeathForm({ ...deathForm, notes: e.target.value })}
                placeholder="Circunstâncias, horário confirmado, comunicação à família..."
              />
            </div>
            <div className="form-field full modal-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setShowDeathModal(false)}>
                Cancelar
              </button>
              <button className="btn btn-danger" type="submit">
                Confirmar óbito
              </button>
            </div>
          </form>
        )}
      </Modal>

      </PatientWorkspaceShell>

      {clinicalCapture && (
        <ClinicalGuideCaptureModal
          open
          onClose={() => setClinicalCapture(null)}
          guideType={clinicalCapture.guideType}
          patients={patients}
          insurances={insurances}
          patientId={clinicalCapture.patientId}
          clinicalContext={{
            hospitalizationId: clinicalCapture.hospitalizationId,
            label: clinicalCapture.label,
          }}
        />
      )}
    </>
  );
}
