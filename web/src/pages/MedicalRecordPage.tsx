import { useCallback, useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  api,
  appointmentStatusLabel,
  isAppointmentStatus,
  bloodTypeLabels,
  entryTypeLabels,
  entryTypeToNumber,
  formatEntryTypeLabel,
  financialStatusLabel,
  genderLabels,
  hospitalizationStatusLabel,
  imagingModalityLabels,
  imagingStatusLabels,
  labOrderStatusLabels,
  tissGuideStatusLabels,
  tissGuideTypeLabels,
  type AppointmentDto,
  type BedDto,
  type DigitalRecordSummaryDto,
  type FinancialAccountDto,
  type HealthInsuranceDto,
  type ImagingStudyDto,
  type LabOrderDto,
  type MedicalRecordEntryDto,
  type PatientDetailDto,
  type PharmacyDispensingDto,
  type ProfessionalDto,
  type SpecialtyClinicalCatalogDto,
  type TissGuideItemRequest,
  resolvePatientModality,
} from '../api/client';
import { AdmissionTextField } from '../components/AdmissionTextField';
import { AvailableBedsPicker } from '../components/AvailableBedsPicker';
import { ClinicalEntryForm, type ClinicalEntryPayload } from '../components/ClinicalEntryForm';
import { DigitalSignaturePad } from '../components/DigitalSignaturePad';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PepOfflineBanner } from '../components/PepOfflineBanner';
import { PersonAvatar } from '../components/PersonAvatar';
import { useAuth } from '../auth/AuthContext';
import {
  createTissGuideAction,
  queueTissSendAfterCreate,
  saveMedicalEntry,
  sendTissGuideAction,
  signMedicalEntry,
} from '../offline/pepActions';
import { savePepSnapshot, getPepSnapshot } from '../offline/pepOfflineDb';
import { usePepOffline } from '../offline/usePepOffline';
import { formatBrDate, formatBrDateTime, formatBrLongDate, formatBrTime } from '../utils/dateUtils';
import {
  printDischargeSummary,
  printMedicalRecordReport,
  printPatientLabel,
  printPatientWristband,
} from '../utils/printTemplates';
import {
  calcAge,
  entryTypeIcons,
  formatAddress,
  formatBirthDate,
  formatPhone,
  loadPatientAppointments,
  phoneHref,
} from '../utils/pepUtils';
import {
  applyTriageAdmissionSuggestion,
  fetchTriageAdmissionSuggestion,
  triageAdmissionHint,
} from '../utils/triageAdmissionUtils';
import {
  medicalRecordTabFromSlug,
  medicalRecordSlugFromTab,
  type MedicalRecordTab,
} from '../navigation/medicalRecordTabs';
import { PatientCommunicationsPanel } from '../components/connect/PatientCommunicationsPanel';

type Tab = MedicalRecordTab;

const emptyAdmit = { bedId: '', professionalId: '', reason: '', diagnosis: '', aiTriageLogId: '' };
const defaultTissItems: TissGuideItemRequest[] = [
  { tussCode: '10101039', description: 'Diária hospitalar', quantity: 1, unitPrice: 800 },
];

export function MedicalRecordPage() {
  const { patientId, section: sectionSlug } = useParams<{ patientId: string; section?: string }>();
  const navigate = useNavigate();
  const { user, hasPermission } = useAuth();

  const [patient, setPatient] = useState<PatientDetailDto | null>(null);
  const [digital, setDigital] = useState<DigitalRecordSummaryDto | null>(null);
  const [catalog, setCatalog] = useState<SpecialtyClinicalCatalogDto | null>(null);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [dispensings, setDispensings] = useState<PharmacyDispensingDto[]>([]);
  const [labOrders, setLabOrders] = useState<LabOrderDto[]>([]);
  const [imagingStudies, setImagingStudies] = useState<ImagingStudyDto[]>([]);
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [financialAccounts, setFinancialAccounts] = useState<FinancialAccountDto[]>([]);

  const tab = medicalRecordTabFromSlug(sectionSlug);

  function setTab(next: Tab) {
    if (!patientId) return;
    const slug = medicalRecordSlugFromTab(next);
    navigate(`/pacientes/${patientId}/prontuario/${slug}`, { replace: true });
  }

  useEffect(() => {
    if (!patientId) return;
    if (!sectionSlug) {
      navigate(`/pacientes/${patientId}/prontuario/resumo`, { replace: true });
      return;
    }
    const canonical = medicalRecordSlugFromTab(medicalRecordTabFromSlug(sectionSlug));
    if (sectionSlug !== canonical) {
      navigate(`/pacientes/${patientId}/prontuario/${canonical}`, { replace: true });
    }
  }, [patientId, sectionSlug, navigate]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [showEntryModal, setShowEntryModal] = useState(false);
  const [showAdmitModal, setShowAdmitModal] = useState(false);
  const [showTissModal, setShowTissModal] = useState(false);
  const [showDischargeModal, setShowDischargeModal] = useState(false);
  const [showSignModal, setShowSignModal] = useState(false);
  const [signingEntryId, setSigningEntryId] = useState<string | null>(null);
  const [signPassword, setSignPassword] = useState('');
  const [signatureImage, setSignatureImage] = useState<string | null>(null);
  const [signingProfessionalId, setSigningProfessionalId] = useState('');
  const [signOnSave, setSignOnSave] = useState(true);
  const [tissSendOnSave, setTissSendOnSave] = useState(false);

  const [entryFormKey, setEntryFormKey] = useState(0);

  const [admitForm, setAdmitForm] = useState(emptyAdmit);
  const [triageHint, setTriageHint] = useState('');
  const [admitBeds, setAdmitBeds] = useState<BedDto[]>([]);
  const [loadingAdmitBeds, setLoadingAdmitBeds] = useState(false);
  const [reasonSnippets, setReasonSnippets] = useState<string[]>([]);
  const [diagnosisSnippets, setDiagnosisSnippets] = useState<string[]>([]);
  const [dischargeNotes, setDischargeNotes] = useState('');
  const [tissForm, setTissForm] = useState({ healthInsuranceId: '', guideType: 3, notes: '' });
  const [tissItems, setTissItems] = useState<TissGuideItemRequest[]>(defaultTissItems);

  const canWriteClinical = hasPermission('pep.write');
  const canManageHosp = hasPermission('hospitalization.manage');
  const canManageTiss = hasPermission('billing.read', 'billing.write');
  const canSeeFinancial = hasPermission('billing.read');
  const canPrintLabel = hasPermission('patients.create');
  const canPrintClinical = hasPermission('pep.read');
  const canSendTiss = hasPermission('billing.write');

  const { online, pendingCount, syncing, syncNow, refresh: refreshOffline } = usePepOffline(patientId);

  const activeHosp = digital?.activeHospitalization;
  const entries = digital?.record.entries ?? [];
  const primaryInsurance = patient?.insurances?.find((i) => i.isPrimary) ?? patient?.insurances?.[0];

  const load = useCallback(async () => {
    if (!patientId) return;
    const [
      patientData,
      digitalData,
      profList,
      insList,
      dispensingList,
      allLabOrders,
      allImaging,
      appointmentList,
      financialList,
    ] = await Promise.all([
      api.getPatient(patientId),
      api.getDigitalRecord(patientId),
      api.getProfessionals(),
      api.getHealthInsurances(),
      api.getDispensings(patientId),
      api.getLabOrders(),
      api.getImagingStudies(),
      loadPatientAppointments(patientId),
      canSeeFinancial
        ? api.getFinancialAccountsByPatient(patientId).catch(() => [] as FinancialAccountDto[])
        : Promise.resolve([] as FinancialAccountDto[]),
    ]);

    setPatient(patientData);
    setDigital(digitalData);
    setProfessionals(profList);
    if (profList.length > 0) {
      setSigningProfessionalId((prev) => prev || user?.professionalId || profList[0].id);
    }
    setInsurances(insList.filter((i) => i.name !== 'Particular' && i.name !== 'SUS'));
    setDispensings(dispensingList);
    setLabOrders(allLabOrders.filter((o) => o.patientId === patientId));
    setImagingStudies(allImaging.filter((s) => s.patientId === patientId));
    setAppointments(appointmentList);
    setFinancialAccounts(financialList);

    await savePepSnapshot({
      patientId,
      savedAt: new Date().toISOString(),
      patient: patientData,
      digital: digitalData,
    });
  }, [patientId, canSeeFinancial]);

  const loadWithOfflineFallback = useCallback(async () => {
    if (!patientId) return;
    try {
      await load();
      setError('');
    } catch (err) {
      const snapshot = await getPepSnapshot(patientId);
      if (snapshot) {
        setPatient(snapshot.patient as PatientDetailDto);
        setDigital(snapshot.digital as DigitalRecordSummaryDto);
        setError('Modo offline — exibindo última versão salva deste prontuário.');
      } else {
        setError(err instanceof Error ? err.message : 'Erro ao carregar prontuário');
      }
    }
  }, [load, patientId]);

  function openTissModal() {
    if (primaryInsurance) {
      const cardInfo = `Carteira: ${primaryInsurance.cardNumber}${primaryInsurance.planName ? ` · Plano: ${primaryInsurance.planName}` : ''}`;
      setTissForm((f) => ({
        ...f,
        healthInsuranceId: primaryInsurance.healthInsuranceId,
        notes: cardInfo,
      }));
    }
    setShowTissModal(true);
  }

  useEffect(() => {
    loadWithOfflineFallback().catch(console.error);
  }, [loadWithOfflineFallback]);

  async function handleSync() {
    const result = await syncNow();
    await loadWithOfflineFallback();
    await refreshOffline();
    if (result.synced > 0) {
      setSuccess(`${result.synced} item(ns) sincronizado(s) com sucesso.`);
    }
    if (result.failed > 0) {
      setError(`${result.failed} item(ns) falharam na sincronização. Tente novamente.`);
    }
  }

  useEffect(() => {
    if (user?.professionalId) {
      api.getClinicalCatalogByProfessional(user.professionalId).then(setCatalog).catch(console.error);
    }
  }, [user?.professionalId]);

  useEffect(() => {
    if (user?.professionalId && !admitForm.professionalId) {
      setAdmitForm((f) => ({ ...f, professionalId: user.professionalId! }));
    }
  }, [user?.professionalId]);

  useEffect(() => {
    if (user?.professionalId) {
      setSigningProfessionalId(user.professionalId);
    }
  }, [user?.professionalId]);

  const patientModality = resolvePatientModality(primaryInsurance?.healthInsuranceName);

  useEffect(() => {
    if (!showAdmitModal || !patientId) return;
    setLoadingAdmitBeds(true);
    api.getAvailableBedsForPatient(patientId)
      .then(setAdmitBeds)
      .catch(console.error)
      .finally(() => setLoadingAdmitBeds(false));
  }, [showAdmitModal, patientId]);

  useEffect(() => {
    if (!showAdmitModal || !patientId) {
      setTriageHint('');
      return;
    }

    let cancelled = false;
    fetchTriageAdmissionSuggestion(patientId)
      .then((suggestion) => {
        if (cancelled) return;
        if (!suggestion) {
          setTriageHint('');
          return;
        }

        setTriageHint(triageAdmissionHint(suggestion));
        setAdmitForm((prev) => applyTriageAdmissionSuggestion(prev, suggestion));
      })
      .catch(() => {
        if (!cancelled) setTriageHint('');
      });

    return () => {
      cancelled = true;
    };
  }, [showAdmitModal, patientId]);

  useEffect(() => {
    if (!showAdmitModal) return;
    Promise.all([
      api.getHospitalizationSnippets('Reason'),
      api.getHospitalizationSnippets('Diagnosis'),
    ])
      .then(([reasons, diagnoses]) => {
        setReasonSnippets(reasons.map((s) => s.text));
        setDiagnosisSnippets(diagnoses.map((s) => s.text));
      })
      .catch(console.error);
  }, [showAdmitModal]);

  const careStats = useMemo(() => {
    const pendingLabs = labOrders.filter((o) => o.status === 1 || o.status === 2).length;
    const pendingImaging = imagingStudies.filter((s) => s.status === 1 || s.status === 2).length;
    const openBalance = financialAccounts
      .filter((a) => a.status === 1 || a.status === 2)
      .reduce((sum, a) => sum + a.balance, 0);
    const upcomingAppointments = appointments.filter(
      (a) => !isAppointmentStatus(a.status, 5, 6) && new Date(a.scheduledAt) >= new Date(),
    ).length;
    const lastEntry = entries[0];
    return { pendingLabs, pendingImaging, openBalance, upcomingAppointments, lastEntry };
  }, [labOrders, imagingStudies, financialAccounts, appointments, entries]);

  async function handleEntryPayload(payload: ClinicalEntryPayload) {
    if (!patientId) return;
    setError('');
    if (!payload.content.trim()) {
      setError('Informe o conteúdo ou selecione itens do catálogo.');
      return;
    }
    if (signOnSave && !signatureImage) {
      setError('Desenhe sua assinatura digital para concluir o registro.');
      return;
    }
    const signingProfessional = user?.professionalId ?? signingProfessionalId;
    if (signOnSave && !signingProfessional) {
      setError('Selecione o profissional responsável pela assinatura.');
      return;
    }
    try {
      const result = await saveMedicalEntry(patientId, {
        entryType: payload.entryType,
        content: payload.content,
        cid10Code: payload.cid10Code || undefined,
        professionalId: signingProfessional,
        hospitalizationId: activeHosp?.id,
        signatureImage: signOnSave ? signatureImage ?? undefined : undefined,
      });
      setSuccess(result.queued
        ? 'Registro salvo localmente (offline). Será sincronizado quando a rede voltar.'
        : signOnSave
          ? 'Registro salvo e assinado digitalmente.'
          : 'Registro salvo no prontuário.');
      setShowEntryModal(false);
      setEntryFormKey((k) => k + 1);
      setSignatureImage(null);
      await refreshOffline();
      if (online) await loadWithOfflineFallback();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar');
    }
  }

  async function handleSignExistingEntry(e: FormEvent) {
    e.preventDefault();
    if (!patientId || !signingEntryId) return;

    const signingProfessional = user?.professionalId ?? signingProfessionalId;
    if (!signingProfessional) {
      setError('Selecione o profissional responsável pela assinatura.');
      return;
    }
    if (!signatureImage) {
      setError('Desenhe sua assinatura digital.');
      return;
    }
    if (!signPassword) {
      setError('Informe sua senha para confirmar a assinatura.');
      return;
    }
    setError('');
    try {
      const result = await signMedicalEntry(
        patientId,
        signingEntryId,
        signingProfessional,
        signatureImage,
        signPassword,
      );
      setSuccess(result.queued
        ? 'Assinatura salva localmente. Sincronizará quando houver conexão.'
        : 'Registro assinado digitalmente.');
      setShowSignModal(false);
      setSigningEntryId(null);
      setSignatureImage(null);
      setSignPassword('');
      await refreshOffline();
      if (online) await loadWithOfflineFallback();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao assinar');
    }
  }

  async function handleAdmit(e: FormEvent) {
    e.preventDefault();
    if (!patientId) return;
    setError('');
    try {
      await api.admitPatient({ patientId, ...admitForm });
      setSuccess('Paciente internado. Registro automático no prontuário.');
      setShowAdmitModal(false);
      setAdmitForm({ ...emptyAdmit, professionalId: user?.professionalId ?? '' });
      setTab('hospitalization');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao internar');
    }
  }

  async function handleDischarge(e: FormEvent) {
    e.preventDefault();
    if (!activeHosp) return;
    setError('');
    try {
      await api.dischargePatient(activeHosp.id, dischargeNotes || undefined);
      setSuccess('Alta registrada. Comprovante no histórico clínico.');
      setShowDischargeModal(false);
      setDischargeNotes('');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro na alta');
    }
  }

  async function handleTissSubmit(e: FormEvent) {
    e.preventDefault();
    if (!patientId) return;
    setError('');
    try {
      const createResult = await createTissGuideAction(patientId, {
        patientId,
        healthInsuranceId: tissForm.healthInsuranceId,
        guideType: tissForm.guideType,
        hospitalizationId: activeHosp?.id,
        items: tissItems,
        notes: tissForm.notes || undefined,
      });

      if (tissSendOnSave && canSendTiss) {
        if (createResult.queued) {
          await queueTissSendAfterCreate(patientId, createResult.clientRequestId);
        } else if (createResult.guide) {
          const sendResult = await sendTissGuideAction(patientId, createResult.guide.id);
          if (sendResult.queued) {
            setSuccess('Guia criada e envio enfileirado (offline).');
          } else {
            setSuccess('Guia TISS criada e enviada ao convênio.');
          }
          setShowTissModal(false);
          setTissItems(defaultTissItems);
          setTab('tiss');
          await refreshOffline();
          if (online) await loadWithOfflineFallback();
          return;
        }
      }

      setSuccess(createResult.queued
        ? 'Guia salva localmente (offline). Sincronizará automaticamente.'
        : tissSendOnSave
          ? 'Guia criada. Envio pendente de conexão.'
          : 'Guia TISS criada em rascunho.');
      setShowTissModal(false);
      setTissItems(defaultTissItems);
      setTab('tiss');
      await refreshOffline();
      if (online) await loadWithOfflineFallback();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar guia');
    }
  }

  async function handleSendTissGuide(guideId: string) {
    if (!patientId) return;
    setError('');
    try {
      const result = await sendTissGuideAction(patientId, guideId);
      setSuccess(result.queued
        ? 'Envio da guia enfileirado. Sincronizará quando houver rede.'
        : 'Guia TISS enviada ao convênio.');
      await refreshOffline();
      if (online) await loadWithOfflineFallback();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enviar guia');
    }
  }

  function openFab() {
    if ((tab === 'summary' || tab === 'clinical') && canWriteClinical) setShowEntryModal(true);
    else if (tab === 'hospitalization' && canManageHosp) {
      if (activeHosp) setShowDischargeModal(true);
      else setShowAdmitModal(true);
    } else if (tab === 'tiss' && canManageTiss) openTissModal();
  }

  const fabLabel = tab === 'summary' || tab === 'clinical' ? 'Novo registro'
    : tab === 'hospitalization' ? (activeHosp ? 'Dar alta' : 'Internar')
    : tab === 'tiss' ? 'Nova guia'
    : '';

  const showFab = ((tab === 'summary' || tab === 'clinical') && canWriteClinical)
    || (tab === 'hospitalization' && canManageHosp)
    || (tab === 'tiss' && canManageTiss);

  if (!patient || !digital) {
    return <div className="card pep-loading">Carregando prontuário digital...</div>;
  }

  const age = calcAge(patient.birthDate);
  const displayPhone = formatPhone(patient.mobilePhone ?? patient.phone);
  const telLink = phoneHref(patient.mobilePhone ?? patient.phone);
  const bloodLabel = patient.bloodType ? bloodTypeLabels[patient.bloodType] ?? patient.bloodType : '—';

  return (
    <div className="pep-root">
      <div className="pep-banner">
        <div className="pep-banner-top">
          <Link to="/pacientes" className="back-link">← Pacientes</Link>
          <div className="pep-banner-actions">
            {canPrintClinical && (
              <button
                type="button"
                className="pep-action-btn"
                onClick={() => printMedicalRecordReport(patient, digital.record)}
              >
                Imprimir / PDF
              </button>
            )}
            {canPrintLabel && (
              <>
                <button type="button" className="pep-action-btn" onClick={() => printPatientLabel(patient)}>
                  Etiqueta
                </button>
                <button type="button" className="pep-action-btn" onClick={() => printPatientWristband(patient)}>
                  Pulseira
                </button>
              </>
            )}
            {canWriteClinical && (
              <button type="button" className="pep-action-btn pep-action-primary" onClick={() => setShowEntryModal(true)}>
                + Registro
              </button>
            )}
          </div>
        </div>
        <div className="pep-banner-main">
          <PersonAvatar name={patient.fullName} photoData={patient.photoData} size={52} />
          <div className="pep-banner-text">
            <h1>{patient.socialName ? `${patient.fullName} (${patient.socialName})` : patient.fullName}</h1>
            <p className="pep-banner-meta">
              PEP {digital.record.recordNumber} · CPF {patient.cpf}
              {patient.rg ? ` · RG ${patient.rg}` : ''}
            </p>
            <p className="pep-banner-meta">
              {age} anos · {genderLabels[patient.gender] ?? '—'} · Tipo sanguíneo {bloodLabel}
            </p>
            {displayPhone && telLink && (
              <p className="pep-banner-meta">
                <a href={telLink} className="pep-phone-link">{displayPhone}</a>
                {patient.email && <> · {patient.email}</>}
              </p>
            )}
            {primaryInsurance && (
              <p className="pep-insurance">
                {primaryInsurance.healthInsuranceName}
                {primaryInsurance.planName ? ` · ${primaryInsurance.planName}` : ''}
                {' · Carteira '}{primaryInsurance.cardNumber}
              </p>
            )}
            {activeHosp && (
              <span className="pep-badge-active">
                Internado — {activeHosp.wardName} Leito {activeHosp.bedNumber}
              </span>
            )}
          </div>
        </div>
        <div className="pep-kpi-row">
          <KpiCard label="Registros" value={entries.length} variant="info" />
          <KpiCard label="Exames pendentes" value={careStats.pendingLabs + careStats.pendingImaging} variant={careStats.pendingLabs + careStats.pendingImaging > 0 ? 'warning' : 'neutral'} />
          <KpiCard label="Consultas" value={careStats.upcomingAppointments} variant="primary" />
          {canSeeFinancial && (
            <KpiCard
              label="Saldo em aberto"
              value={careStats.openBalance.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              variant={careStats.openBalance > 0 ? 'danger' : 'success'}
            />
          )}
        </div>
      </div>

      <PepOfflineBanner
        online={online}
        pendingCount={pendingCount}
        syncing={syncing}
        onSync={() => { handleSync().catch(console.error); }}
      />

      {patient.notes && (
        <div className="pep-alert-banner" role="alert">
          <strong>Observações / alertas:</strong> {patient.notes}
        </div>
      )}

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <nav className="pep-tabs pep-tabs-top" aria-label="Seções do prontuário">
        <button type="button" className={tab === 'summary' ? 'active' : ''} onClick={() => setTab('summary')}>
          Resumo
        </button>
        <button type="button" className={tab === 'clinical' ? 'active' : ''} onClick={() => setTab('clinical')}>
          Clínico
        </button>
        <button type="button" className={tab === 'care' ? 'active' : ''} onClick={() => setTab('care')}>
          Cuidados
        </button>
        <button type="button" className={tab === 'hospitalization' ? 'active' : ''} onClick={() => setTab('hospitalization')}>
          Intern.
        </button>
        <button type="button" className={tab === 'tiss' ? 'active' : ''} onClick={() => setTab('tiss')}>
          TISS
        </button>
        {hasPermission('connect.read') ? (
          <button type="button" className={tab === 'comunicacao' ? 'active' : ''} onClick={() => setTab('comunicacao')}>
            Comunicação
          </button>
        ) : null}
      </nav>

      <div className="pep-content">
        {tab === 'summary' && (
          <SummaryTab
            patient={patient}
            lastEntry={careStats.lastEntry}
            onGoClinical={() => setTab('clinical')}
            onGoCare={() => setTab('care')}
          />
        )}

        {tab === 'clinical' && (
          <ClinicalTab
            entries={entries}
            activeHospId={activeHosp?.id}
            canSign={canWriteClinical}
            canPrint={canPrintClinical}
            onPrint={(filtered: MedicalRecordEntryDto[]) => printMedicalRecordReport(patient, digital.record, filtered)}
            onSignEntry={(entryId) => {
              setSigningEntryId(entryId);
              setSignatureImage(null);
              setShowSignModal(true);
            }}
          />
        )}

        {tab === 'care' && (
          <CareTab
            appointments={appointments}
            labOrders={labOrders}
            imagingStudies={imagingStudies}
            dispensings={dispensings}
            financialAccounts={canSeeFinancial ? financialAccounts : []}
            showFinancial={canSeeFinancial}
          />
        )}

        {tab === 'hospitalization' && (
          <HospitalizationTab
            active={activeHosp}
            history={digital.hospitalizationHistory}
            canManage={canManageHosp}
            canPrint={canPrintClinical}
            patient={patient}
            recordNumber={digital.record.recordNumber}
            dischargeNotes={dischargeNotes}
            onAdmit={() => setShowAdmitModal(true)}
            onDischarge={() => setShowDischargeModal(true)}
          />
        )}

        {tab === 'tiss' && (
          <TissTab
            guides={digital.tissGuides}
            canManage={canManageTiss}
            canSend={canSendTiss}
            onCreate={openTissModal}
            onSend={handleSendTissGuide}
          />
        )}

        {tab === 'comunicacao' && patientId && hasPermission('connect.read') ? (
          <PatientCommunicationsPanel patientId={patientId} />
        ) : null}
      </div>

      <nav className="pep-tabs pep-tabs-bottom" aria-label="Seções do prontuário">
        <button type="button" className={tab === 'summary' ? 'active' : ''} onClick={() => setTab('summary')}>
          Resumo
        </button>
        <button type="button" className={tab === 'clinical' ? 'active' : ''} onClick={() => setTab('clinical')}>
          Clínico
        </button>
        <button type="button" className={tab === 'care' ? 'active' : ''} onClick={() => setTab('care')}>
          Cuidados
        </button>
        <button type="button" className={tab === 'hospitalization' ? 'active' : ''} onClick={() => setTab('hospitalization')}>
          Intern.
        </button>
        <button type="button" className={tab === 'tiss' ? 'active' : ''} onClick={() => setTab('tiss')}>
          TISS
        </button>
        {hasPermission('connect.read') ? (
          <button type="button" className={tab === 'comunicacao' ? 'active' : ''} onClick={() => setTab('comunicacao')}>
            Comunicação
          </button>
        ) : null}
      </nav>

      {showFab && (
        <button type="button" className="pep-fab" onClick={openFab} aria-label={fabLabel}>
          + {fabLabel}
        </button>
      )}

      <Modal open={showEntryModal} onClose={() => setShowEntryModal(false)} title="Novo registro clínico" subtitle="Anamnese estruturada, CID-10, textos pré-definidos e catálogo clínico." width="lg">
        {!user?.professionalId && professionals.length > 0 && (
          <div className="form-field" style={{ marginBottom: 12 }}>
            <label htmlFor="mr-sign-prof">Profissional assinante</label>
            <select
              id="mr-sign-prof"
              value={signingProfessionalId}
              onChange={(e) => setSigningProfessionalId(e.target.value)}
            >
              {professionals.map((p) => (
                <option key={p.id} value={p.id}>{p.fullName}</option>
              ))}
            </select>
          </div>
        )}
        <ClinicalEntryForm
          key={entryFormKey}
          catalog={catalog}
          patient={patient}
          signOnSave={signOnSave}
          signatureImage={signatureImage}
          onSignOnSaveChange={setSignOnSave}
          onSignatureImageChange={setSignatureImage}
          onCancel={() => setShowEntryModal(false)}
          onSubmit={handleEntryPayload}
          signatureLayoutKey={showEntryModal ? entryFormKey : 'closed'}
        />
      </Modal>

      <Modal open={showAdmitModal} onClose={() => setShowAdmitModal(false)} title="Internar paciente" subtitle="Leitos disponíveis conforme a cobertura do paciente." width="lg">
        <form className="form-grid" onSubmit={handleAdmit}>
          <div className="form-field full">
            <AvailableBedsPicker
              beds={admitBeds}
              value={admitForm.bedId}
              onChange={(bedId) => setAdmitForm({ ...admitForm, bedId })}
              patientModality={patientModality}
              planName={primaryInsurance?.planName}
              loading={loadingAdmitBeds}
              serverFiltered
            />
          </div>
          <div className="form-field">
            <label>Médico *</label>
            <select required value={admitForm.professionalId} onChange={(e) => setAdmitForm({ ...admitForm, professionalId: e.target.value })}>
              <option value="">Selecione</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          {triageHint && (
            <div className="form-field full">
              <div className="alert alert-success" style={{ margin: 0 }}>{triageHint}</div>
            </div>
          )}
          <AdmissionTextField
            id="mr-admit-reason"
            label="Motivo"
            required
            value={admitForm.reason}
            onChange={(reason) => setAdmitForm({ ...admitForm, reason })}
            snippets={reasonSnippets}
          />
          <AdmissionTextField
            id="mr-admit-diagnosis"
            label="Diagnóstico / CID"
            value={admitForm.diagnosis}
            onChange={(diagnosis) => setAdmitForm({ ...admitForm, diagnosis })}
            snippets={diagnosisSnippets}
          />
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowAdmitModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Confirmar internação</button>
          </div>
        </form>
      </Modal>

      <Modal open={showDischargeModal} onClose={() => setShowDischargeModal(false)} title="Alta hospitalar" subtitle={activeHosp ? `${activeHosp.wardName} — Leito ${activeHosp.bedNumber}` : undefined}>
        <form className="form-grid" onSubmit={handleDischarge}>
          <div className="form-field full">
            <label>Resumo da alta / orientações</label>
            <textarea rows={5} value={dischargeNotes} onChange={(e) => setDischargeNotes(e.target.value)} placeholder="Conduta, medicamentos, retorno..." />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowDischargeModal(false)}>Cancelar</button>
            {canPrintClinical && activeHosp ? (
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => printDischargeSummary(patient, activeHosp, {
                  recordNumber: digital.record.recordNumber,
                  dischargeNotes,
                })}
              >
                Imprimir / PDF
              </button>
            ) : null}
            <button type="submit" className="btn">Confirmar alta</button>
          </div>
        </form>
      </Modal>

      <Modal open={showTissModal} onClose={() => setShowTissModal(false)} title="Nova guia TISS" subtitle="Faturamento digital vinculado à internação." width="lg">
        <form className="form-grid" onSubmit={handleTissSubmit}>
          <div className="form-field">
            <label>Convênio *</label>
            <select required value={tissForm.healthInsuranceId} onChange={(e) => setTissForm({ ...tissForm, healthInsuranceId: e.target.value })}>
              <option value="">Selecione</option>
              {insurances.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Tipo</label>
            <select value={tissForm.guideType} onChange={(e) => setTissForm({ ...tissForm, guideType: Number(e.target.value) })}>
              {Object.entries(tissGuideTypeLabels).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </div>
          {activeHosp && (
            <div className="form-field full">
              <div className="pep-info-box">Vinculada à internação ativa — {activeHosp.wardName} Leito {activeHosp.bedNumber}</div>
            </div>
          )}
          <div className="form-field full">
            <label>Procedimentos TUSS</label>
            {tissItems.map((item, idx) => (
              <div key={idx} className="pep-tiss-row">
                <input placeholder="TUSS" value={item.tussCode} onChange={(e) => {
                  const n = [...tissItems]; n[idx] = { ...item, tussCode: e.target.value }; setTissItems(n);
                }} />
                <input placeholder="Descrição" value={item.description} onChange={(e) => {
                  const n = [...tissItems]; n[idx] = { ...item, description: e.target.value }; setTissItems(n);
                }} />
                <input type="number" min={1} value={item.quantity} onChange={(e) => {
                  const n = [...tissItems]; n[idx] = { ...item, quantity: Number(e.target.value) }; setTissItems(n);
                }} />
                <input type="number" step="0.01" value={item.unitPrice} onChange={(e) => {
                  const n = [...tissItems]; n[idx] = { ...item, unitPrice: Number(e.target.value) }; setTissItems(n);
                }} />
              </div>
            ))}
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => setTissItems([...tissItems, { tussCode: '', description: '', quantity: 1, unitPrice: 0 }])}>
              + Procedimento
            </button>
          </div>
          <div className="form-field full">
            <label>Observações</label>
            <input value={tissForm.notes} onChange={(e) => setTissForm({ ...tissForm, notes: e.target.value })} />
          </div>
          {canSendTiss && (
            <div className="form-field full">
              <label className="pep-checkbox-label">
                <input
                  type="checkbox"
                  checked={tissSendOnSave}
                  onChange={(e) => setTissSendOnSave(e.target.checked)}
                />
                Enviar guia ao convênio imediatamente após criar
              </label>
            </div>
          )}
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowTissModal(false)}>Cancelar</button>
            <button type="submit" className="btn">{tissSendOnSave ? 'Criar e enviar' : 'Criar guia'}</button>
          </div>
        </form>
      </Modal>

      <Modal
        open={showSignModal}
        onClose={() => { setShowSignModal(false); setSigningEntryId(null); setSignatureImage(null); setSignPassword(''); }}
        title="Assinatura digital"
        subtitle="Confirme o registro clínico com sua assinatura."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleSignExistingEntry}>
          {!user?.professionalId && professionals.length > 0 && (
            <div className="form-field full">
              <label htmlFor="mr-sign-entry-prof">Profissional assinante</label>
              <select
                id="mr-sign-entry-prof"
                value={signingProfessionalId}
                onChange={(e) => setSigningProfessionalId(e.target.value)}
                required
              >
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
            </div>
          )}
          <div className="form-field full">
            <DigitalSignaturePad
              onChange={setSignatureImage}
              layoutKey={showSignModal ? signingEntryId ?? 'sign' : 'closed'}
            />
          </div>
          <div className="form-field full">
            <label htmlFor="mr-sign-entry-password">Senha (reautenticação)</label>
            <input
              id="mr-sign-entry-password"
              type="password"
              value={signPassword}
              onChange={(e) => setSignPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>
          <div className="form-field full modal-actions">
            <button type="button" className="btn btn-secondary" onClick={() => { setShowSignModal(false); setSigningEntryId(null); setSignPassword(''); }}>Cancelar</button>
            <button type="submit" className="btn">Confirmar assinatura</button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

function InfoItem({ label, value }: { label: string; value?: string | null }) {
  if (!value) return null;
  return (
    <div className="pep-info-item">
      <span className="pep-info-label">{label}</span>
      <span className="pep-info-value">{value}</span>
    </div>
  );
}

function SummaryTab({
  patient,
  lastEntry,
  onGoClinical,
  onGoCare,
}: {
  patient: PatientDetailDto;
  lastEntry?: MedicalRecordEntryDto;
  onGoClinical: () => void;
  onGoCare: () => void;
}) {
  const age = calcAge(patient.birthDate);
  const phone = formatPhone(patient.mobilePhone ?? patient.phone);
  const telLink = phoneHref(patient.mobilePhone ?? patient.phone);

  return (
    <div className="pep-summary">
      <div className="pep-quick-links">
        <Link to="/agendamentos" className="pep-quick-link">Agendamento</Link>
        <Link to="/laboratorio" className="pep-quick-link">Laboratório</Link>
        <Link to="/imagem" className="pep-quick-link">Imagem</Link>
        <Link to="/farmacia" className="pep-quick-link">Farmácia</Link>
        <Link to="/faturamento-tiss" className="pep-quick-link">Faturamento</Link>
      </div>

      {lastEntry && (
        <div className="pep-info-card pep-last-entry">
          <h3>Último registro clínico</h3>
          <p>
            <span className="badge">{formatEntryTypeLabel(lastEntry.entryType)}</span>
            {' · '}{formatBrDateTime(lastEntry.createdAt)}
            {lastEntry.professionalName && <> · Dr(a). {lastEntry.professionalName}</>}
          </p>
          <p className="pep-entry-preview">{lastEntry.content.slice(0, 200)}{lastEntry.content.length > 200 ? '…' : ''}</p>
          <button type="button" className="btn btn-secondary btn-sm" onClick={onGoClinical}>Ver histórico completo</button>
        </div>
      )}

      <section className="pep-summary-section">
        <h3>Dados pessoais</h3>
        <div className="pep-info-grid">
          <InfoItem label="Nome completo" value={patient.fullName} />
          <InfoItem label="Nome social" value={patient.socialName} />
          <InfoItem label="CPF" value={patient.cpf} />
          <InfoItem label="RG" value={patient.rg} />
          <InfoItem label="Nascimento" value={`${formatBirthDate(patient.birthDate)} (${age} anos)`} />
          <InfoItem label="Sexo" value={genderLabels[patient.gender]} />
          <InfoItem label="Tipo sanguíneo" value={patient.bloodType ? bloodTypeLabels[patient.bloodType] ?? patient.bloodType : undefined} />
          <InfoItem label="Nacionalidade" value={patient.nationality} />
          <InfoItem label="Naturalidade" value={patient.birthPlace} />
          <InfoItem label="Profissão" value={patient.occupation} />
          <InfoItem label="Estado civil" value={patient.maritalStatus} />
          <InfoItem label="Nome da mãe" value={patient.motherName} />
        </div>
      </section>

      <section className="pep-summary-section">
        <h3>Contato e endereço</h3>
        <div className="pep-info-grid">
          <InfoItem label="Telefone" value={phone} />
          {telLink && phone && (
            <div className="pep-info-item">
              <span className="pep-info-label">Ligar</span>
              <a href={telLink} className="pep-info-value pep-phone-link">{phone}</a>
            </div>
          )}
          <InfoItem label="E-mail" value={patient.email} />
          <InfoItem label="Endereço" value={formatAddress(patient)} />
          <InfoItem label="Contato de emergência" value={patient.emergencyContactName} />
          <InfoItem label="Tel. emergência" value={formatPhone(patient.emergencyContactPhone)} />
        </div>
      </section>

      {patient.insurances.length > 0 && (
        <section className="pep-summary-section">
          <h3>Convênios e planos</h3>
          <div className="pep-insurance-list">
            {patient.insurances.map((ins) => (
              <article key={ins.id} className={`pep-insurance-card${ins.isPrimary ? ' primary' : ''}`}>
                <div className="pep-insurance-head">
                  <strong>{ins.healthInsuranceName}</strong>
                  {ins.isPrimary && <span className="badge pep-badge-primary">Principal</span>}
                </div>
                <p>Carteira: {ins.cardNumber}</p>
                {ins.planName && <p>Plano: {ins.planName}</p>}
                {ins.cardHolderName && <p>Titular: {ins.cardHolderName}</p>}
                {ins.cnsNumber && <p>CNS: {ins.cnsNumber}</p>}
                {ins.accommodationType && <p>Acomodação: {ins.accommodationType}</p>}
                {(ins.validFrom || ins.validUntil) && (
                  <p className="text-muted">
                    Validade: {ins.validFrom ? formatBrDate(ins.validFrom) : '—'}
                    {' — '}
                    {ins.validUntil ? formatBrDate(ins.validUntil) : '—'}
                  </p>
                )}
              </article>
            ))}
          </div>
        </section>
      )}

      <div className="pep-summary-footer">
        <button type="button" className="btn btn-secondary" onClick={onGoCare}>Ver exames, consultas e medicamentos</button>
      </div>
    </div>
  );
}

function ClinicalTab({
  entries,
  activeHospId,
  canSign,
  canPrint,
  onPrint,
  onSignEntry,
}: {
  entries: MedicalRecordEntryDto[];
  activeHospId?: string;
  canSign: boolean;
  canPrint: boolean;
  onPrint: (entries: MedicalRecordEntryDto[]) => void;
  onSignEntry: (entryId: string) => void;
}) {
  const [typeFilter, setTypeFilter] = useState<number | ''>('');
  const [search, setSearch] = useState('');

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return entries.filter((e) => {
      if (typeFilter !== '' && e.entryType !== typeFilter) return false;
      if (!q) return true;
      return (
        e.content.toLowerCase().includes(q)
        || e.cid10Code?.toLowerCase().includes(q)
        || e.professionalName?.toLowerCase().includes(q)
        || formatEntryTypeLabel(e.entryType).toLowerCase().includes(q)
      );
    });
  }, [entries, typeFilter, search]);

  const grouped = useMemo(() => {
    const groups = new Map<string, MedicalRecordEntryDto[]>();
    for (const entry of filtered) {
      const key = formatBrLongDate(entry.createdAt);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(entry);
    }
    return [...groups.entries()];
  }, [filtered]);

  if (entries.length === 0) {
    return (
      <div className="appt-empty">
        <div className="appt-empty-icon">📋</div>
        <h3>Prontuário vazio</h3>
        <p>Use o botão + para registrar evolução, prescrição ou exames.</p>
      </div>
    );
  }

  return (
    <div className="pep-clinical">
      <div className="pep-clinical-toolbar">
        <input
          type="search"
          className="pep-search"
          placeholder="Buscar no prontuário..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        {canPrint && (
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={() => onPrint(filtered)}
            disabled={filtered.length === 0}
          >
            Imprimir / PDF
          </button>
        )}
        <div className="pep-filter-chips">
          <button
            type="button"
            className={`pep-chip${typeFilter === '' ? ' active' : ''}`}
            onClick={() => setTypeFilter('')}
          >
            Todos ({entries.length})
          </button>
          {Object.entries(entryTypeLabels).map(([value, label]) => {
            const count = entries.filter((e) => e.entryType === Number(value)).length;
            if (count === 0) return null;
            return (
              <button
                key={value}
                type="button"
                className={`pep-chip${typeFilter === Number(value) ? ' active' : ''}`}
                onClick={() => setTypeFilter(Number(value))}
              >
                {entryTypeIcons[Number(value)]} {label} ({count})
              </button>
            );
          })}
        </div>
      </div>

      {filtered.length === 0 ? (
        <div className="appt-empty">
          <h3>Nenhum registro encontrado</h3>
          <p>Ajuste os filtros ou a busca.</p>
        </div>
      ) : (
        <div className="pep-timeline">
          {grouped.map(([dateLabel, dayEntries]) => (
            <div key={dateLabel} className="pep-timeline-group">
              <h4 className="pep-timeline-date">{dateLabel}</h4>
              {dayEntries.map((entry) => (
                <article
                  key={entry.id}
                  className={`pep-entry-card pep-entry-type-${entry.entryType}${entry.hospitalizationId === activeHospId ? ' pep-entry-inpatient' : ''}`}
                >
                  <div className="pep-entry-head">
                    <span className="badge pep-entry-badge">
                      {entryTypeIcons[entryTypeToNumber(entry.entryType)]} {formatEntryTypeLabel(entry.entryType)}
                    </span>
                    <time>{formatBrTime(entry.createdAt)}</time>
                  </div>
                  {entry.hospitalizationId === activeHospId && (
                    <span className="pep-inpatient-tag">Durante internação</span>
                  )}
                  {entry.professionalName && <div className="pep-entry-author">Dr(a). {entry.professionalName}</div>}
                  {entry.cid10Code && <div className="pep-entry-cid">CID-10: {entry.cid10Code}</div>}
                  {entry.isSigned ? (
                    <div className="pep-signature-badge" title={entry.signatureHash ?? undefined}>
                      ✓ Assinado digitalmente
                      {entry.signedByProfessionalName && <> por Dr(a). {entry.signedByProfessionalName}</>}
                      {entry.signedAt && <> · {formatBrDateTime(entry.signedAt)}</>}
                    </div>
                  ) : canSign && (
                    <button type="button" className="btn btn-secondary btn-sm pep-sign-btn" onClick={() => onSignEntry(entry.id)}>
                      Assinar registro
                    </button>
                  )}
                  <p className="pep-entry-body">{entry.content}</p>
                </article>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function CareTab({
  appointments,
  labOrders,
  imagingStudies,
  dispensings,
  financialAccounts,
  showFinancial,
}: {
  appointments: AppointmentDto[];
  labOrders: LabOrderDto[];
  imagingStudies: ImagingStudyDto[];
  dispensings: PharmacyDispensingDto[];
  financialAccounts: FinancialAccountDto[];
  showFinancial: boolean;
}) {
  const upcoming = appointments.filter(
    (a) => !isAppointmentStatus(a.status, 5, 6) && new Date(a.scheduledAt) >= new Date(),
  );
  const past = appointments.filter(
    (a) => new Date(a.scheduledAt) < new Date() || isAppointmentStatus(a.status, 5, 6),
  ).slice(0, 10);

  const isEmpty = appointments.length === 0 && labOrders.length === 0
    && imagingStudies.length === 0 && dispensings.length === 0
    && (!showFinancial || financialAccounts.length === 0);

  if (isEmpty) {
    return (
      <div className="appt-empty">
        <div className="appt-empty-icon">🏥</div>
        <h3>Sem atendimentos registrados</h3>
        <p>Consultas, exames e dispensações aparecerão aqui conforme forem lançados no sistema.</p>
      </div>
    );
  }

  return (
    <div className="pep-care">
      {upcoming.length > 0 && (
        <CareSection title="Próximas consultas" count={upcoming.length}>
          {upcoming.map((a) => (
            <div key={a.id} className="pep-care-item">
              <div className="pep-care-item-head">
                <strong>{formatBrDateTime(a.scheduledAt)}</strong>
                <span className="badge">{appointmentStatusLabel(a.status)}</span>
              </div>
              <p>{a.specialtyName} · Dr(a). {a.professionalName}</p>
            </div>
          ))}
        </CareSection>
      )}

      {labOrders.length > 0 && (
        <CareSection title="Laboratório" count={labOrders.length}>
          {labOrders.map((order) => (
            <div key={order.id} className="pep-care-item">
              <div className="pep-care-item-head">
                <strong>{formatBrDate(order.createdAt)}</strong>
                <span className="badge">{labOrderStatusLabels[order.status]}</span>
              </div>
              <p>Solicitante: {order.requestingProfessionalName}</p>
              <ul className="pep-care-sublist">
                {order.items.map((item) => (
                  <li key={item.id}>
                    {item.examName}
                    {item.result && (
                      <span className={item.result.isAbnormal ? 'pep-abnormal' : 'pep-normal'}>
                        {' — '}{item.result.value}{item.result.unit ? ` ${item.result.unit}` : ''}
                        {item.result.isAbnormal ? ' (alterado)' : ''}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </CareSection>
      )}

      {imagingStudies.length > 0 && (
        <CareSection title="Diagnóstico por imagem" count={imagingStudies.length}>
          {imagingStudies.map((study) => (
            <div key={study.id} className="pep-care-item">
              <div className="pep-care-item-head">
                <strong>{imagingModalityLabels[study.modality]} — {study.studyDescription}</strong>
                <span className="badge">{imagingStatusLabels[study.status]}</span>
              </div>
              <p>
                Agendado: {formatBrDateTime(study.scheduledAt)}
                {study.accessionNumber && <> · Acc. {study.accessionNumber}</>}
              </p>
              {study.reportContent && (
                <p className="pep-entry-preview">{study.reportContent.slice(0, 300)}{study.reportContent.length > 300 ? '…' : ''}</p>
              )}
            </div>
          ))}
        </CareSection>
      )}

      {dispensings.length > 0 && (
        <CareSection title="Farmácia — dispensações" count={dispensings.length}>
          {dispensings.map((d) => (
            <div key={d.id} className="pep-care-item">
              <div className="pep-care-item-head">
                <strong>{d.productName}</strong>
                <time>{formatBrDateTime(d.dispensedAt)}</time>
              </div>
              <p>Qtd: {d.quantity}{d.professionalName ? ` · ${d.professionalName}` : ''}</p>
              {d.notes && <p className="text-muted">{d.notes}</p>}
            </div>
          ))}
        </CareSection>
      )}

      {past.length > 0 && (
        <CareSection title="Consultas anteriores" count={past.length}>
          {past.map((a) => (
            <div key={a.id} className="pep-care-item">
              <div className="pep-care-item-head">
                <strong>{formatBrDateTime(a.scheduledAt)}</strong>
                <span className="badge">{appointmentStatusLabel(a.status)}</span>
              </div>
              <p>{a.specialtyName} · Dr(a). {a.professionalName}</p>
            </div>
          ))}
        </CareSection>
      )}

      {showFinancial && financialAccounts.length > 0 && (
        <CareSection title="Contas financeiras" count={financialAccounts.length}>
          {financialAccounts.map((acc) => (
            <div key={acc.id} className="pep-care-item">
              <div className="pep-care-item-head">
                <strong>{acc.description}</strong>
                <span className="badge">{financialStatusLabel(acc.status)}</span>
              </div>
              <p>
                Total: {acc.amount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                {' · Saldo: '}{acc.balance.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              </p>
              {acc.dueDate && <p className="text-muted">Vencimento: {formatBrDate(acc.dueDate)}</p>}
            </div>
          ))}
        </CareSection>
      )}
    </div>
  );
}

function CareSection({ title, count, children }: { title: string; count: number; children: ReactNode }) {
  return (
    <section className="pep-care-section">
      <h3>{title} <span className="pep-care-count">{count}</span></h3>
      <div className="pep-care-list">{children}</div>
    </section>
  );
}

function HospitalizationTab({
  active,
  history,
  canManage,
  canPrint,
  patient,
  recordNumber,
  dischargeNotes,
  onAdmit,
  onDischarge,
}: {
  active?: DigitalRecordSummaryDto['activeHospitalization'];
  history: DigitalRecordSummaryDto['hospitalizationHistory'];
  canManage: boolean;
  canPrint: boolean;
  patient: PatientDetailDto;
  recordNumber: string;
  dischargeNotes: string;
  onAdmit: () => void;
  onDischarge: () => void;
}) {
  return (
    <>
      {active ? (
        <div className="pep-info-card">
          <h3>Internação ativa</h3>
          <p><strong>{active.wardName}</strong> — Leito {active.bedNumber}</p>
          <p>Médico: {active.professionalName}</p>
          <p>Entrada: {formatBrDateTime(active.admittedAt)}</p>
          <p>Motivo: {active.reason}</p>
          {active.diagnosis && <p>Diagnóstico: {active.diagnosis}</p>}
          <span className="badge">{hospitalizationStatusLabel(active.status)}</span>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 12 }}>
            {canPrint && (
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => printDischargeSummary(patient, active, { recordNumber, dischargeNotes })}
              >
                Imprimir alta / PDF
              </button>
            )}
            {canManage && (
              <button type="button" className="btn btn-secondary btn-sm" onClick={onDischarge}>
                Registrar alta
              </button>
            )}
          </div>
        </div>
      ) : (
        <div className="appt-empty">
          <div className="appt-empty-icon">🛏️</div>
          <h3>Sem internação ativa</h3>
          <p>{canManage ? 'Toque em + Internar para admitir pelo tablet.' : 'Paciente não está internado.'}</p>
          {canManage && <button type="button" className="btn" onClick={onAdmit}>Internar agora</button>}
        </div>
      )}

      {history.length > 0 && (
        <div className="pep-section">
          <h3>Histórico de internações</h3>
          {history.map((h) => (
            <div key={h.id} className="pep-entry-card">
              <div className="pep-entry-head">
                <span className="badge">{hospitalizationStatusLabel(h.status)}</span>
                <time>{formatBrDate(h.admittedAt)}</time>
              </div>
              <p>{h.wardName} — Leito {h.bedNumber}</p>
              <p className="text-muted">{h.reason}</p>
              {h.dischargedAt && (
                <p className="text-muted">Alta: {formatBrDate(h.dischargedAt)}</p>
              )}
              {canPrint && h.dischargedAt && (
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  style={{ marginTop: 8 }}
                  onClick={() => printDischargeSummary(patient, h, { recordNumber })}
                >
                  Imprimir comprovante
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </>
  );
}

function TissTab({
  guides, canManage, canSend, onCreate, onSend,
}: {
  guides: DigitalRecordSummaryDto['tissGuides'];
  canManage: boolean;
  canSend: boolean;
  onCreate: () => void;
  onSend: (guideId: string) => void;
}) {
  if (guides.length === 0) {
    return (
      <div className="appt-empty">
        <div className="appt-empty-icon">📄</div>
        <h3>Nenhuma guia TISS</h3>
        <p>{canManage ? 'Crie guias de consulta, SP/SADT ou internação sem papel.' : 'Sem guias para este paciente.'}</p>
        {canManage && <button type="button" className="btn" onClick={onCreate}>Nova guia</button>}
      </div>
    );
  }
  return (
    <div className="pep-tiss-list">
      <div className="pep-tiss-header">
        <p className="text-muted">{guides.length} guia(s) para este paciente</p>
        <Link to="/faturamento-tiss" className="btn btn-secondary btn-sm">Abrir faturamento</Link>
      </div>
      {guides.map((g) => (
        <article key={g.id} className="pep-entry-card">
          <div className="pep-entry-head">
            <strong>{g.guideNumber}</strong>
            <span className={`badge pep-tiss-status-${g.status}`}>{tissGuideStatusLabels[g.status]}</span>
          </div>
          <p>{tissGuideTypeLabels[g.guideType]} · {g.healthInsuranceName}</p>
          <p className="text-muted">{formatBrDate(g.createdAt)}</p>
          <p className="pep-entry-amount">{g.totalAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</p>
          <ul className="pep-tiss-items">
            {g.items.map((i) => (
              <li key={i.id}>{i.tussCode} — {i.description} ({i.quantity}x)</li>
            ))}
          </ul>
          {g.status === 1 && canSend && (
            <button type="button" className="btn btn-sm" style={{ marginTop: 12 }} onClick={() => onSend(g.id)}>
              Enviar ao convênio
            </button>
          )}
        </article>
      ))}
    </div>
  );
}
